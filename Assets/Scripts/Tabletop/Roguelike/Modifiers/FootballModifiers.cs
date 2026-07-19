using System;
using System.Collections.Generic;
using System.Linq;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Modifiers
{
    public enum FootballModifierType
    {
        FlickForce,
        FlickForceCurve,
        ForceVariance,
        DirectionVariance,
        ContactPointVariance,
        SpinTorque,
        MaximumAngularVelocity,
        AngularDamping,
        LinearDamping,
        Friction,
        CenterOfMassX,
        CenterOfMassY,
        CenterOfMassZ,
        TouchdownScoring,
        FieldGoalForce,
        FieldGoalDirectionVariance,
        ConsumableCapacity,
        RewardGeneration,
        EncounterRules,
        PreviewAccuracy
    }

    public enum FootballModifierOperation
    {
        Add,
        Multiply,
        Minimum,
        Maximum,
        Override
    }

    public enum UpgradeRarity
    {
        Common,
        Uncommon,
        Rare
    }

    [Serializable]
    public sealed class FootballModifier
    {
        [SerializeField] private string stableId = "modifier";
        [SerializeField] private FootballModifierType modifierType;
        [SerializeField] private FootballModifierOperation operation = FootballModifierOperation.Add;
        [SerializeField] private float value;
        [SerializeField] private int priority;

        public FootballModifier()
        {
        }

        public FootballModifier(string id, FootballModifierType type, FootballModifierOperation modifierOperation, float modifierValue, int modifierPriority = 0)
        {
            stableId = string.IsNullOrWhiteSpace(id) ? $"{type}.{modifierOperation}" : id;
            modifierType = type;
            operation = modifierOperation;
            value = modifierValue;
            priority = modifierPriority;
        }

        public string StableId => string.IsNullOrWhiteSpace(stableId) ? $"{modifierType}.{operation}" : stableId;
        public FootballModifierType ModifierType => modifierType;
        public FootballModifierOperation Operation => operation;
        public float Value => value;
        public int Priority => priority;
    }

    public static class ModifierPipeline
    {
        public static float Compose(
            float baseValue,
            IEnumerable<FootballModifier> modifiers,
            FootballModifierType type,
            float minimum = float.NegativeInfinity,
            float maximum = float.PositiveInfinity)
        {
            float value = baseValue;
            List<FootballModifier> ordered = modifiers == null
                ? new List<FootballModifier>()
                : modifiers
                    .Where(modifier => modifier != null && modifier.ModifierType == type)
                    .OrderBy(modifier => modifier.Priority)
                    .ThenBy(modifier => modifier.StableId, StringComparer.Ordinal)
                    .ToList();

            foreach (FootballModifier modifier in ordered.Where(modifier => modifier.Operation == FootballModifierOperation.Add))
            {
                value += modifier.Value;
            }

            foreach (FootballModifier modifier in ordered.Where(modifier => modifier.Operation == FootballModifierOperation.Multiply))
            {
                value *= modifier.Value;
            }

            foreach (FootballModifier modifier in ordered.Where(modifier => modifier.Operation == FootballModifierOperation.Minimum))
            {
                value = Mathf.Max(value, modifier.Value);
            }

            foreach (FootballModifier modifier in ordered.Where(modifier => modifier.Operation == FootballModifierOperation.Maximum))
            {
                value = Mathf.Min(value, modifier.Value);
            }

            foreach (FootballModifier modifier in ordered.Where(modifier => modifier.Operation == FootballModifierOperation.Override))
            {
                value = modifier.Value;
            }

            return Mathf.Clamp(value, minimum, maximum);
        }
    }

    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Upgrade", fileName = "FootballUpgrade")]
    public class FootballUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string stableId = "upgrade";
        [SerializeField] private string displayName = "Upgrade";
        [SerializeField] private string description = "Changes paper football behavior.";
        [SerializeField] private UpgradeRarity rarity;
        [SerializeField] private Sprite icon;
        [SerializeField] private int maximumStackCount = 1;
        [SerializeField] private FootballModifier[] modifiers = Array.Empty<FootballModifier>();
        [SerializeField] private string[] tags = Array.Empty<string>();
        [SerializeField] private string[] mutualExclusionTags = Array.Empty<string>();
        [SerializeField] private float rewardWeight = 1f;

        public string StableId => string.IsNullOrWhiteSpace(stableId) ? name : stableId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? StableId : displayName;
        public string Description => description ?? string.Empty;
        public UpgradeRarity Rarity => rarity;
        public Sprite Icon => icon;
        public int MaximumStackCount => Mathf.Max(1, maximumStackCount);
        public IReadOnlyList<FootballModifier> Modifiers => modifiers ?? Array.Empty<FootballModifier>();
        public IReadOnlyList<string> Tags => tags ?? Array.Empty<string>();
        public IReadOnlyList<string> MutualExclusionTags => mutualExclusionTags ?? Array.Empty<string>();
        public float RewardWeight => Mathf.Max(0f, rewardWeight);

        public void Configure(
            string id,
            string upgradeName,
            string upgradeDescription,
            UpgradeRarity upgradeRarity,
            int maxStacks,
            float weight,
            FootballModifier[] upgradeModifiers,
            string[] upgradeTags = null,
            string[] excludedTags = null)
        {
            stableId = id;
            displayName = upgradeName;
            description = upgradeDescription;
            rarity = upgradeRarity;
            maximumStackCount = Mathf.Max(1, maxStacks);
            rewardWeight = Mathf.Max(0f, weight);
            modifiers = upgradeModifiers ?? Array.Empty<FootballModifier>();
            tags = upgradeTags ?? Array.Empty<string>();
            mutualExclusionTags = excludedTags ?? Array.Empty<string>();
        }

        public string BuildEffectSummary()
        {
            if (modifiers == null || modifiers.Length == 0)
            {
                return "No modifier";
            }

            return string.Join(", ", modifiers.Select(modifier => $"{modifier.ModifierType} {modifier.Operation} {modifier.Value:0.###}"));
        }

        private void OnValidate()
        {
            maximumStackCount = Mathf.Max(1, maximumStackCount);
            rewardWeight = Mathf.Max(0f, rewardWeight);
        }
    }

    [Serializable]
    public readonly struct AppliedUpgrade
    {
        public AppliedUpgrade(string stableId, int stackCount)
        {
            StableId = stableId;
            StackCount = Mathf.Max(0, stackCount);
        }

        public string StableId { get; }
        public int StackCount { get; }
    }

    [Serializable]
    public sealed class FootballBuild
    {
        [SerializeField] private List<AppliedUpgradeSnapshot> upgrades = new();

        public IReadOnlyList<AppliedUpgradeSnapshot> UpgradeSnapshots => upgrades;

        public int GetStackCount(string upgradeId)
        {
            AppliedUpgradeSnapshot existing = upgrades.FirstOrDefault(upgrade => upgrade.stableId == upgradeId);
            return existing != null ? existing.stackCount : 0;
        }

        public bool CanApply(FootballUpgradeDefinition upgrade)
        {
            return upgrade != null && GetStackCount(upgrade.StableId) < upgrade.MaximumStackCount;
        }

        public bool Apply(FootballUpgradeDefinition upgrade)
        {
            if (!CanApply(upgrade))
            {
                return false;
            }

            AppliedUpgradeSnapshot existing = upgrades.FirstOrDefault(snapshot => snapshot.stableId == upgrade.StableId);
            if (existing == null)
            {
                upgrades.Add(new AppliedUpgradeSnapshot(upgrade.StableId, 1));
            }
            else
            {
                existing.stackCount++;
            }

            return true;
        }

        public bool HasAnyTag(IEnumerable<string> requestedTags, UpgradeCatalog catalog)
        {
            if (requestedTags == null || catalog == null)
            {
                return false;
            }

            HashSet<string> requested = new(requestedTags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
            if (requested.Count == 0)
            {
                return false;
            }

            foreach (AppliedUpgradeSnapshot snapshot in upgrades)
            {
                FootballUpgradeDefinition upgrade = catalog.GetById(snapshot.stableId);
                if (upgrade != null && upgrade.Tags.Any(requested.Contains))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<FootballModifier> EnumerateModifiers(UpgradeCatalog catalog)
        {
            if (catalog == null)
            {
                yield break;
            }

            foreach (AppliedUpgradeSnapshot snapshot in upgrades.OrderBy(upgrade => upgrade.stableId, StringComparer.Ordinal))
            {
                FootballUpgradeDefinition upgrade = catalog.GetById(snapshot.stableId);
                if (upgrade == null)
                {
                    continue;
                }

                for (int stack = 0; stack < snapshot.stackCount; stack++)
                {
                    foreach (FootballModifier modifier in upgrade.Modifiers)
                    {
                        yield return modifier;
                    }
                }
            }
        }

        public string ToSummary(UpgradeCatalog catalog)
        {
            if (upgrades.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", upgrades.Select(snapshot =>
            {
                FootballUpgradeDefinition upgrade = catalog != null ? catalog.GetById(snapshot.stableId) : null;
                string name = upgrade != null ? upgrade.DisplayName : snapshot.stableId;
                return $"{name} x{snapshot.stackCount}";
            }));
        }

        public FootballBuild Clone()
        {
            FootballBuild clone = new();
            foreach (AppliedUpgradeSnapshot snapshot in upgrades)
            {
                clone.upgrades.Add(new AppliedUpgradeSnapshot(snapshot.stableId, snapshot.stackCount));
            }

            return clone;
        }
    }

    [Serializable]
    public sealed class AppliedUpgradeSnapshot
    {
        public string stableId;
        public int stackCount;

        public AppliedUpgradeSnapshot(string id, int stacks)
        {
            stableId = id;
            stackCount = Mathf.Max(0, stacks);
        }
    }

    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Upgrade Catalog", fileName = "UpgradeCatalog")]
    public class UpgradeCatalog : ScriptableObject
    {
        [SerializeField] private FootballUpgradeDefinition[] upgrades = Array.Empty<FootballUpgradeDefinition>();

        public IReadOnlyList<FootballUpgradeDefinition> Upgrades => upgrades ?? Array.Empty<FootballUpgradeDefinition>();

        public void Configure(FootballUpgradeDefinition[] definitions)
        {
            upgrades = definitions ?? Array.Empty<FootballUpgradeDefinition>();
        }

        public FootballUpgradeDefinition GetById(string stableId)
        {
            return Upgrades.FirstOrDefault(upgrade => upgrade != null && upgrade.StableId == stableId);
        }

        public bool HasUniqueIds()
        {
            return Upgrades
                .Where(upgrade => upgrade != null)
                .GroupBy(upgrade => upgrade.StableId)
                .All(group => group.Count() == 1);
        }

        public List<FootballUpgradeDefinition> GetRewardChoices(
            FootballBuild build,
            IRunRandom random,
            int choiceCount,
            UpgradeRarity minimumRarity = UpgradeRarity.Common)
        {
            FootballBuild runtimeBuild = build ?? new FootballBuild();
            IRunRandom runtimeRandom = random ?? new DeterministicRunRandom(0);
            List<FootballUpgradeDefinition> candidates = Upgrades
                .Where(upgrade => IsEligible(upgrade, runtimeBuild, minimumRarity))
                .OrderBy(upgrade => upgrade.StableId, StringComparer.Ordinal)
                .ToList();

            List<FootballUpgradeDefinition> choices = new();
            while (choices.Count < choiceCount && candidates.Count > 0)
            {
                float totalWeight = candidates.Sum(upgrade => Mathf.Max(0.0001f, upgrade.RewardWeight));
                float roll = runtimeRandom.Range(0f, totalWeight);
                float cursor = 0f;
                int selectedIndex = 0;

                for (int i = 0; i < candidates.Count; i++)
                {
                    cursor += Mathf.Max(0.0001f, candidates[i].RewardWeight);
                    if (roll <= cursor)
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                choices.Add(candidates[selectedIndex]);
                candidates.RemoveAt(selectedIndex);
            }

            return choices;
        }

        public bool IsEligible(FootballUpgradeDefinition upgrade, FootballBuild build, UpgradeRarity minimumRarity)
        {
            if (upgrade == null || build == null || upgrade.Rarity < minimumRarity)
            {
                return false;
            }

            if (build.GetStackCount(upgrade.StableId) >= upgrade.MaximumStackCount)
            {
                return false;
            }

            return !build.HasAnyTag(upgrade.MutualExclusionTags, this);
        }
    }

    public readonly struct FootballBuildEvaluation
    {
        public FootballBuildEvaluation(
            float flickForceMultiplier,
            float flickForceCurveOffset,
            float forceVarianceScale,
            float directionVarianceScale,
            float contactPointVarianceScale,
            float spinTorqueMultiplier,
            float maximumAngularVelocityMultiplier,
            float angularDampingMultiplier,
            float linearDampingMultiplier,
            float frictionMultiplier,
            float touchdownScoringMultiplier,
            float fieldGoalForceMultiplier,
            float fieldGoalDirectionVarianceScale,
            float previewAccuracyBonus,
            Vector3 centerOfMassOffset)
        {
            FlickForceMultiplier = flickForceMultiplier;
            FlickForceCurveOffset = flickForceCurveOffset;
            ForceVarianceScale = forceVarianceScale;
            DirectionVarianceScale = directionVarianceScale;
            ContactPointVarianceScale = contactPointVarianceScale;
            SpinTorqueMultiplier = spinTorqueMultiplier;
            MaximumAngularVelocityMultiplier = maximumAngularVelocityMultiplier;
            AngularDampingMultiplier = angularDampingMultiplier;
            LinearDampingMultiplier = linearDampingMultiplier;
            FrictionMultiplier = frictionMultiplier;
            TouchdownScoringMultiplier = touchdownScoringMultiplier;
            FieldGoalForceMultiplier = fieldGoalForceMultiplier;
            FieldGoalDirectionVarianceScale = fieldGoalDirectionVarianceScale;
            PreviewAccuracyBonus = previewAccuracyBonus;
            CenterOfMassOffset = centerOfMassOffset;
        }

        public float FlickForceMultiplier { get; }
        public float FlickForceCurveOffset { get; }
        public float ForceVarianceScale { get; }
        public float DirectionVarianceScale { get; }
        public float ContactPointVarianceScale { get; }
        public float SpinTorqueMultiplier { get; }
        public float MaximumAngularVelocityMultiplier { get; }
        public float AngularDampingMultiplier { get; }
        public float LinearDampingMultiplier { get; }
        public float FrictionMultiplier { get; }
        public float TouchdownScoringMultiplier { get; }
        public float FieldGoalForceMultiplier { get; }
        public float FieldGoalDirectionVarianceScale { get; }
        public float PreviewAccuracyBonus { get; }
        public Vector3 CenterOfMassOffset { get; }

        public static FootballBuildEvaluation Default => new(
            1f,
            0f,
            1f,
            1f,
            1f,
            1f,
            1f,
            1f,
            1f,
            1f,
            1f,
            1f,
            1f,
            0f,
            Vector3.zero);

        public PaperFootballRuleSet ApplyToRules(PaperFootballRuleSet baseRules)
        {
            PaperFootballRuleSet rules = baseRules != null ? baseRules.Clone() : new PaperFootballRuleSet();
            rules.maximumFlickForce *= FlickForceMultiplier;
            rules.minimumFlickForce *= FlickForceMultiplier;
            rules.flickForceResponseExponent = Mathf.Max(0.1f, rules.flickForceResponseExponent + FlickForceCurveOffset);
            rules.contactYawTorqueMultiplier *= SpinTorqueMultiplier;
            rules.maximumFootballAngularVelocity *= MaximumAngularVelocityMultiplier;
            rules.footballAngularDamping *= AngularDampingMultiplier;
            rules.minimumFieldGoalForce *= FieldGoalForceMultiplier;
            rules.maximumFieldGoalForce *= FieldGoalForceMultiplier;
            rules.Sanitize();
            return rules;
        }
    }

    public static class FootballBuildEvaluator
    {
        public static FootballBuildEvaluation Evaluate(FootballBuild build, UpgradeCatalog catalog)
        {
            List<FootballModifier> modifiers = build?.EnumerateModifiers(catalog).ToList() ?? new List<FootballModifier>();
            return new FootballBuildEvaluation(
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.FlickForce, 0.1f, 4f),
                ModifierPipeline.Compose(0f, modifiers, FootballModifierType.FlickForceCurve, -1f, 3f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.ForceVariance, 0f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.DirectionVariance, 0f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.ContactPointVariance, 0f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.SpinTorque, 0f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.MaximumAngularVelocity, 0.1f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.AngularDamping, 0.1f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.LinearDamping, 0.1f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.Friction, 0.05f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.TouchdownScoring, 0.1f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.FieldGoalForce, 0.1f, 4f),
                ModifierPipeline.Compose(1f, modifiers, FootballModifierType.FieldGoalDirectionVariance, 0f, 4f),
                ModifierPipeline.Compose(0f, modifiers, FootballModifierType.PreviewAccuracy, -1f, 1f),
                new Vector3(
                    ModifierPipeline.Compose(0f, modifiers, FootballModifierType.CenterOfMassX, -0.25f, 0.25f),
                    ModifierPipeline.Compose(0f, modifiers, FootballModifierType.CenterOfMassY, -0.25f, 0.25f),
                    ModifierPipeline.Compose(0f, modifiers, FootballModifierType.CenterOfMassZ, -0.25f, 0.25f)));
        }
    }
}
