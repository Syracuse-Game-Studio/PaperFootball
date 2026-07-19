using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Roguelike.Encounters;
using PaperFootball.Tabletop.Roguelike.Modifiers;
using PaperFootball.Tabletop.Roguelike.Opponents;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Roguelike.Run;
using PaperFootball.Tabletop.Roguelike.Variance;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Tests
{
    public class RoguelikePhase2FoundationTests
    {
        [Test]
        public void SameSeedProducesSameRandomSequence()
        {
            DeterministicRunRandom first = new(42);
            DeterministicRunRandom second = new(42);

            Assert.That(first.Value(), Is.EqualTo(second.Value()).Within(0.000001f));
            Assert.That(first.Range(-2f, 2f), Is.EqualTo(second.Range(-2f, 2f)).Within(0.000001f));
            Assert.That(first.Range(0, 99), Is.EqualTo(second.Range(0, 99)));
        }

        [Test]
        public void StableChildSeedDerivationIsReproducibleAndStreamSpecific()
        {
            int encounterSeed = StableSeedUtility.DeriveSeed(99, RunRandomStream.EncounterGeneration, 2, stableIdentifier: "stage");
            int repeated = StableSeedUtility.DeriveSeed(99, RunRandomStream.EncounterGeneration, 2, stableIdentifier: "stage");
            int cosmeticSeed = StableSeedUtility.DeriveSeed(99, RunRandomStream.Cosmetic, 2, stableIdentifier: "stage");

            Assert.That(encounterSeed, Is.EqualTo(repeated));
            Assert.That(encounterSeed, Is.Not.EqualTo(cosmeticSeed));
        }

        [Test]
        public void DisabledVariancePreservesOriginalFlickValues()
        {
            FlickCommand command = Command();
            ResolvedFlickParameters resolved = FlickParameterResolver.Resolve(
                command,
                ShotVarianceTuning.Disabled,
                Rules(),
                null,
                new DeterministicRunRandom(1),
                1,
                1);

            Assert.That(resolved.FinalForce, Is.EqualTo(command.Force).Within(0.0001f));
            Assert.That(Vector3.Angle(resolved.FinalDirection, command.Direction), Is.LessThan(0.001f));
            Assert.That(resolved.FinalContactPointWorld, Is.EqualTo(command.ContactPointWorld));
        }

        [Test]
        public void VarianceStaysWithinConfiguredBoundsAndIsReproducible()
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Collider collider = football.GetComponent<Collider>();
                ShotVarianceTuning tuning = new(true, 0.03f, 1.5f, 0.01f, false, "Stable");
                ResolvedFlickParameters first = FlickParameterResolver.Resolve(Command(), tuning, Rules(), collider, new DeterministicRunRandom(12), 12, 1);
                ResolvedFlickParameters second = FlickParameterResolver.Resolve(Command(), tuning, Rules(), collider, new DeterministicRunRandom(12), 12, 1);

                Assert.That(first.AppliedForceMultiplier, Is.InRange(0.97f, 1.03f));
                Assert.That(Mathf.Abs(first.AppliedDirectionVarianceDegrees), Is.LessThanOrEqualTo(1.5f));
                Assert.That(first.AppliedContactOffsetLocal.magnitude, Is.LessThanOrEqualTo(0.0101f));
                Assert.That(first.FinalForce, Is.EqualTo(second.FinalForce).Within(0.0001f));
                Assert.That(first.FinalDirection.x, Is.EqualTo(second.FinalDirection.x).Within(0.0001f));
                Assert.That(first.FinalContactPointWorld.x, Is.EqualTo(second.FinalContactPointWorld.x).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(football);
            }
        }

        [Test]
        public void ModifierCompositionOrderIsDeterministic()
        {
            FootballModifier[] modifiers =
            {
                new("b_multiply", FootballModifierType.FlickForce, FootballModifierOperation.Multiply, 2f, 10),
                new("a_add", FootballModifierType.FlickForce, FootballModifierOperation.Add, 3f, 0),
                new("c_max", FootballModifierType.FlickForce, FootballModifierOperation.Maximum, 12f, 20)
            };

            float value = ModifierPipeline.Compose(2f, modifiers, FootballModifierType.FlickForce);

            Assert.That(value, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void RewardChoicesAreDeterministicUniqueAndRespectStackLimits()
        {
            UpgradeCatalog catalog = ScriptableObject.CreateInstance<UpgradeCatalog>();
            FootballUpgradeDefinition tight = Upgrade("tight", UpgradeRarity.Common, 1, 1f, FootballModifierType.DirectionVariance, 0.75f);
            FootballUpgradeDefinition loose = Upgrade("loose", UpgradeRarity.Common, 1, 1f, FootballModifierType.SpinTorque, 1.25f);
            FootballUpgradeDefinition wax = Upgrade("wax", UpgradeRarity.Uncommon, 2, 1f, FootballModifierType.Friction, 0.75f);
            catalog.Configure(new[] { tight, loose, wax });
            FootballBuild build = new();
            Assert.IsTrue(build.Apply(tight));

            List<FootballUpgradeDefinition> first = catalog.GetRewardChoices(build, new DeterministicRunRandom(7), 3);
            List<FootballUpgradeDefinition> second = catalog.GetRewardChoices(build, new DeterministicRunRandom(7), 3);

            Assert.IsFalse(first.Any(choice => choice.StableId == "tight"));
            Assert.That(first.Select(choice => choice.StableId).Distinct().Count(), Is.EqualTo(first.Count));
            Assert.That(first.Select(choice => choice.StableId), Is.EqualTo(second.Select(choice => choice.StableId)));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(tight);
            Object.DestroyImmediate(loose);
            Object.DestroyImmediate(wax);
        }

        [Test]
        public void UpgradeEvaluationChangesExpectedVarianceAndSpinValues()
        {
            UpgradeCatalog catalog = ScriptableObject.CreateInstance<UpgradeCatalog>();
            FootballUpgradeDefinition tight = Upgrade("tight", UpgradeRarity.Common, 3, 1f, FootballModifierType.DirectionVariance, 0.75f);
            FootballUpgradeDefinition loose = Upgrade("loose", UpgradeRarity.Common, 3, 1f, FootballModifierType.SpinTorque, 1.3f);
            catalog.Configure(new[] { tight, loose });
            FootballBuild build = new();
            build.Apply(tight);
            build.Apply(loose);

            FootballBuildEvaluation evaluation = FootballBuildEvaluator.Evaluate(build, catalog);

            Assert.That(evaluation.DirectionVarianceScale, Is.LessThan(1f));
            Assert.That(evaluation.SpinTorqueMultiplier, Is.GreaterThan(1f));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(tight);
            Object.DestroyImmediate(loose);
        }

        [Test]
        public void OpponentProfilesProduceDistinctSharedFlickCommands()
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Collider collider = football.GetComponent<Collider>();
                Bounds table = new(Vector3.zero, new Vector3(8f, 1f, 12f));
                OpponentProfile spinner = Opponent("spinner", 0.55f, 0.15f, 1f, OpponentContactPreference.OffCenter, 0.6f, 0.65f);
                OpponentProfile calculator = Opponent("calculator", 0.45f, 0.04f, 0f, OpponentContactPreference.Center, 0.9f, 0.2f);
                OpponentProfile power = Opponent("power", 0.9f, 0.08f, 0.2f, OpponentContactPreference.SlightlyOffCenter, 0.55f, 0.8f);

                OpponentDecision spinnerDecision = OpponentDecisionService.Decide(new OpponentDecisionContext(spinner, collider, table, Rules(), PaperFootballPlayer.PlayerTwo, 1, 0), new DeterministicRunRandom(5));
                OpponentDecision calculatorDecision = OpponentDecisionService.Decide(new OpponentDecisionContext(calculator, collider, table, Rules(), PaperFootballPlayer.PlayerTwo, 1, 0), new DeterministicRunRandom(5));
                OpponentDecision powerDecision = OpponentDecisionService.Decide(new OpponentDecisionContext(power, collider, table, Rules(), PaperFootballPlayer.PlayerTwo, 1, 0), new DeterministicRunRandom(5));

                float spinnerOffset = Vector3.Distance(spinnerDecision.Command.ContactPointWorld, collider.bounds.center);
                float calculatorOffset = Vector3.Distance(calculatorDecision.Command.ContactPointWorld, collider.bounds.center);

                Assert.IsTrue(spinnerDecision.Command.IsValid);
                Assert.IsTrue(calculatorDecision.Command.IsValid);
                Assert.That(spinnerOffset, Is.GreaterThan(calculatorOffset));
                Assert.That(powerDecision.Command.Force, Is.GreaterThan(calculatorDecision.Command.Force));

                Object.DestroyImmediate(spinner);
                Object.DestroyImmediate(calculator);
                Object.DestroyImmediate(power);
            }
            finally
            {
                Object.DestroyImmediate(football);
            }
        }

        [Test]
        public void SameRunSeedGeneratesSameSixEncountersWithBossLast()
        {
            OpponentCatalog opponents = ScriptableObject.CreateInstance<OpponentCatalog>();
            TableSurfaceCatalog surfaces = ScriptableObject.CreateInstance<TableSurfaceCatalog>();
            ObstacleLayoutCatalog obstacles = ScriptableObject.CreateInstance<ObstacleLayoutCatalog>();

            List<GeneratedEncounter> first = EncounterGenerator.GenerateSixEncounterRun(123, opponents, surfaces, obstacles);
            List<GeneratedEncounter> second = EncounterGenerator.GenerateSixEncounterRun(123, opponents, surfaces, obstacles);

            Assert.That(first.Count, Is.EqualTo(6));
            Assert.That(first.Select(encounter => encounter.encounterId), Is.EqualTo(second.Select(encounter => encounter.encounterId)));
            Assert.That(first[4].encounterType, Is.EqualTo(EncounterType.EliteMatch));
            Assert.That(first[5].encounterType, Is.EqualTo(EncounterType.BossMatch));

            Object.DestroyImmediate(opponents);
            Object.DestroyImmediate(surfaces);
            Object.DestroyImmediate(obstacles);
        }

        [Test]
        public void RunSnapshotSerializesStableData()
        {
            RunState state = new()
            {
                runSeed = 123,
                currentEncounterIndex = 2,
                status = RunStatus.Active
            };
            state.results.Add(new RunEncounterResult { encounterId = "01_standard", succeeded = true, resultText = "won" });

            string json = state.ToJson();

            Assert.That(json, Does.Contain("\"runSeed\": 123"));
            Assert.That(json, Does.Contain("\"currentEncounterIndex\": 2"));
            Assert.That(json, Does.Contain("01_standard"));
        }

        private static FlickCommand Command()
        {
            return new FlickCommand(
                true,
                Vector3.zero,
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f),
                Vector3.forward,
                2f,
                1f,
                0.2f,
                0.5f,
                new Vector3(0.5f, 0f, 0f));
        }

        private static PaperFootballRuleSet Rules()
        {
            return new PaperFootballRuleSet
            {
                minimumFlickForce = 0.35f,
                maximumFlickForce = 4f
            };
        }

        private static FootballUpgradeDefinition Upgrade(string id, UpgradeRarity rarity, int maxStacks, float weight, FootballModifierType type, float multiplier)
        {
            FootballUpgradeDefinition upgrade = ScriptableObject.CreateInstance<FootballUpgradeDefinition>();
            upgrade.Configure(
                id,
                id,
                id,
                rarity,
                maxStacks,
                weight,
                new[] { new FootballModifier($"{id}.{type}", type, FootballModifierOperation.Multiply, multiplier) },
                new[] { id });
            return upgrade;
        }

        private static OpponentProfile Opponent(string id, float power, float variance, float spin, OpponentContactPreference preference, float accuracy, float risk)
        {
            OpponentProfile profile = ScriptableObject.CreateInstance<OpponentProfile>();
            profile.Configure(id, id, power, variance, spin, preference, accuracy, risk, 0.5f, 0.5f, 0.3f, 0f);
            return profile;
        }
    }
}
