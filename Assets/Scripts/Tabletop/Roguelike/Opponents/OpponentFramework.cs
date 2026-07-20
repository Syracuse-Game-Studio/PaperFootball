using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Match;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Roguelike.Modifiers;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Shots;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Opponents
{
    public enum OpponentContactPreference
    {
        Center,
        SlightlyOffCenter,
        OffCenter
    }

    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Opponent Profile", fileName = "OpponentProfile")]
    public partial class OpponentProfile
    {
        [SerializeField] private string stableId = "opponent";
        [SerializeField] private string displayName = "Opponent";
        [SerializeField, Range(0f, 1f)] private float preferredPower = 0.55f;
        [SerializeField, Range(0f, 1f)] private float powerVariance = 0.12f;
        [SerializeField, Range(-1f, 1f)] private float preferredSpin;
        [SerializeField] private OpponentContactPreference contactPointPreference = OpponentContactPreference.Center;
        [SerializeField, Range(0f, 1f)] private float accuracy = 0.7f;
        [SerializeField, Range(0f, 1f)] private float riskTolerance = 0.45f;
        [SerializeField, Range(0f, 1f)] private float edgeShotPreference = 0.45f;
        [SerializeField, Range(0f, 1f)] private float fieldGoalSkill = 0.5f;
        [SerializeField, Range(0f, 1f)] private float obstacleUsePreference = 0.3f;
        [SerializeField] private float decisionDelay = 0.55f;
        [SerializeField] private FootballModifier[] upgradeModifiers = Array.Empty<FootballModifier>();

        public string StableId => string.IsNullOrWhiteSpace(stableId) ? name : stableId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? StableId : displayName;
        public float PreferredPower => Mathf.Clamp01(preferredPower);
        public float PowerVariance => Mathf.Clamp01(powerVariance);
        public float PreferredSpin => Mathf.Clamp(preferredSpin, -1f, 1f);
        public OpponentContactPreference ContactPointPreference => contactPointPreference;
        public float Accuracy => Mathf.Clamp01(accuracy);
        public float RiskTolerance => Mathf.Clamp01(riskTolerance);
        public float EdgeShotPreference => Mathf.Clamp01(edgeShotPreference);
        public float FieldGoalSkill => Mathf.Clamp01(fieldGoalSkill);
        public float ObstacleUsePreference => Mathf.Clamp01(obstacleUsePreference);
        public float DecisionDelay => Mathf.Max(0f, decisionDelay);
        public IReadOnlyList<FootballModifier> UpgradeModifiers => upgradeModifiers ?? Array.Empty<FootballModifier>();

        public void Configure(
            string id,
            string opponentName,
            float power,
            float variance,
            float spin,
            OpponentContactPreference contactPreference,
            float opponentAccuracy,
            float risk,
            float edgePreference,
            float fieldGoal,
            float obstaclePreference,
            float delay,
            FootballModifier[] modifiers = null)
        {
            stableId = id;
            displayName = opponentName;
            preferredPower = Mathf.Clamp01(power);
            powerVariance = Mathf.Clamp01(variance);
            preferredSpin = Mathf.Clamp(spin, -1f, 1f);
            contactPointPreference = contactPreference;
            accuracy = Mathf.Clamp01(opponentAccuracy);
            riskTolerance = Mathf.Clamp01(risk);
            edgeShotPreference = Mathf.Clamp01(edgePreference);
            fieldGoalSkill = Mathf.Clamp01(fieldGoal);
            obstacleUsePreference = Mathf.Clamp01(obstaclePreference);
            decisionDelay = Mathf.Max(0f, delay);
            upgradeModifiers = modifiers ?? Array.Empty<FootballModifier>();
        }
    }

    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Opponent Catalog", fileName = "OpponentCatalog")]
    public partial class OpponentCatalog
    {
        [SerializeField] private OpponentProfile[] opponents = Array.Empty<OpponentProfile>();

        public IReadOnlyList<OpponentProfile> Opponents => opponents ?? Array.Empty<OpponentProfile>();

        public void Configure(OpponentProfile[] profiles)
        {
            opponents = profiles ?? Array.Empty<OpponentProfile>();
        }

        public OpponentProfile GetById(string stableId)
        {
            return Opponents.FirstOrDefault(profile => profile != null && profile.StableId == stableId);
        }

        public OpponentProfile Pick(IRunRandom random)
        {
            List<OpponentProfile> valid = Opponents.Where(profile => profile != null).OrderBy(profile => profile.StableId, StringComparer.Ordinal).ToList();
            if (valid.Count == 0)
            {
                return null;
            }

            IRunRandom runtimeRandom = random ?? new DeterministicRunRandom(0);
            return valid[runtimeRandom.Range(0, valid.Count)];
        }
    }

    public readonly struct OpponentDecisionContext
    {
        public OpponentDecisionContext(
            OpponentProfile profile,
            Collider footballCollider,
            Bounds tableBounds,
            PaperFootballRuleSet rules,
            PaperFootballPlayer player,
            int possessionNumber,
            int encounterIndex,
            IReadOnlyList<Bounds> obstacleBounds = null)
        {
            Profile = profile;
            FootballCollider = footballCollider;
            TableBounds = tableBounds;
            Rules = rules;
            Player = player;
            PossessionNumber = possessionNumber;
            EncounterIndex = encounterIndex;
            ObstacleBounds = obstacleBounds ?? Array.Empty<Bounds>();
        }

        public OpponentProfile Profile { get; }
        public Collider FootballCollider { get; }
        public Bounds TableBounds { get; }
        public PaperFootballRuleSet Rules { get; }
        public PaperFootballPlayer Player { get; }
        public int PossessionNumber { get; }
        public int EncounterIndex { get; }
        public IReadOnlyList<Bounds> ObstacleBounds { get; }
    }

    public readonly struct OpponentDecision
    {
        public OpponentDecision(FlickCommand command, Vector3 tacticalTarget, float score)
        {
            Command = command;
            TacticalTarget = tacticalTarget;
            Score = score;
        }

        public FlickCommand Command { get; }
        public Vector3 TacticalTarget { get; }
        public float Score { get; }
        public bool IsValid => Command.IsValid;
    }

    public static class OpponentDecisionService
    {
        public static OpponentDecision Decide(OpponentDecisionContext context, IRunRandom random)
        {
            if (context.Profile == null || context.FootballCollider == null)
            {
                return new OpponentDecision(FlickCommand.Invalid(Vector3.zero, Vector3.zero, 0f), Vector3.zero, 0f);
            }

            IRunRandom runtimeRandom = random ?? new DeterministicRunRandom(0);
            PaperFootballRuleSet rules = context.Rules != null ? context.Rules.Clone() : new PaperFootballRuleSet();
            rules.Sanitize();

            Vector3 footballPosition = context.FootballCollider.bounds.center;
            Vector3 baseDirection = context.Player == PaperFootballPlayer.PlayerOne ? Vector3.forward : Vector3.back;
            float targetZ = context.Player == PaperFootballPlayer.PlayerOne ? context.TableBounds.max.z : context.TableBounds.min.z;
            Vector3 tacticalTarget = new(0f, footballPosition.y, targetZ);

            List<FlickCommand> candidates = new();
            for (int i = 0; i < 8; i++)
            {
                Vector3 direction = BuildDirection(baseDirection, context.Profile, runtimeRandom);
                float force = BuildForce(rules, context.Profile, runtimeRandom);
                Vector3 contact = BuildContactPoint(context.FootballCollider, context.Profile, direction, runtimeRandom);
                candidates.Add(BuildCommand(footballPosition, direction, force, contact, rules, FootballShotType.FlatTableShot));

                if (context.ObstacleBounds.Count > 0 || runtimeRandom.Value() < context.Profile.RiskTolerance * 0.35f)
                {
                    candidates.Add(BuildCommand(footballPosition, direction, force, contact, rules, FootballShotType.AirFlickShot));
                }
            }

            FlickCommand selected = candidates
                .OrderByDescending(candidate => ScoreCandidate(candidate, context, tacticalTarget))
                .ThenBy(candidate => candidate.ContactPointWorld.x)
                .First();

            return new OpponentDecision(selected, tacticalTarget, ScoreCandidate(selected, context, tacticalTarget));
        }

        private static Vector3 BuildDirection(Vector3 baseDirection, OpponentProfile profile, IRunRandom random)
        {
            float maxYaw = Mathf.Lerp(14f, 2f, profile.Accuracy);
            float yaw = random.Range(-maxYaw, maxYaw);
            Vector3 direction = Quaternion.AngleAxis(yaw, Vector3.up) * baseDirection;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.000001f ? direction.normalized : baseDirection;
        }

        private static float BuildForce(PaperFootballRuleSet rules, OpponentProfile profile, IRunRandom random)
        {
            float normalized = profile.PreferredPower + random.Range(-profile.PowerVariance, profile.PowerVariance);
            normalized = Mathf.Clamp01(normalized);
            return Mathf.Lerp(rules.minimumFlickForce, rules.maximumFlickForce, normalized);
        }

        private static Vector3 BuildContactPoint(Collider collider, OpponentProfile profile, Vector3 direction, IRunRandom random)
        {
            Transform transform = collider.transform;
            Bounds localBounds = GetLocalBounds(collider);
            float sideSign = Mathf.Sign(profile.PreferredSpin);
            if (Mathf.Approximately(sideSign, 0f))
            {
                sideSign = random.Value() < 0.5f ? -1f : 1f;
            }

            float xOffset = profile.ContactPointPreference switch
            {
                OpponentContactPreference.OffCenter => localBounds.extents.x * 0.78f * sideSign,
                OpponentContactPreference.SlightlyOffCenter => localBounds.extents.x * 0.42f * sideSign,
                _ => localBounds.extents.x * random.Range(-0.12f, 0.12f)
            };

            float zOffset = -Mathf.Sign(Vector3.Dot(direction, transform.forward)) * localBounds.extents.z * 0.2f;
            Vector3 localPoint = localBounds.center + new Vector3(xOffset, localBounds.extents.y, zOffset);
            Vector3 world = transform.TransformPoint(localPoint);
            return collider.ClosestPoint(world);
        }

        private static FlickCommand BuildCommand(
            Vector3 footballPosition,
            Vector3 direction,
            float force,
            Vector3 contact,
            PaperFootballRuleSet rules,
            FootballShotType shotType)
        {
            float strength01 = Mathf.InverseLerp(rules.minimumFlickForce, rules.maximumFlickForce, force);
            float dragDistance = Mathf.Lerp(rules.minimumDragDistance, rules.maximumDragDistance, strength01);
            Vector3 dragStart = footballPosition;
            Vector3 dragCurrent = dragStart - direction * Mathf.Max(rules.minimumDragDistance, dragDistance);
            return new FlickCommand(
                true,
                dragStart,
                dragCurrent,
                dragCurrent,
                direction,
                force,
                dragDistance,
                0.25f,
                strength01,
                contact,
                shotType);
        }

        private static Bounds GetLocalBounds(Collider collider)
        {
            if (collider is BoxCollider box)
            {
                return new Bounds(box.center, box.size);
            }

            Bounds bounds = collider.bounds;
            return new Bounds(Vector3.zero, collider.transform.InverseTransformVector(bounds.size));
        }

        private static float ScoreCandidate(FlickCommand command, OpponentDecisionContext context, Vector3 target)
        {
            Vector3 toTarget = target - context.FootballCollider.bounds.center;
            toTarget.y = 0f;
            Vector3 direction = command.Direction;
            direction.y = 0f;
            float alignment = toTarget.sqrMagnitude > 0.000001f ? Mathf.Max(0f, Vector3.Dot(direction.normalized, toTarget.normalized)) : 0f;
            Vector3 projected = context.FootballCollider.bounds.center + direction.normalized * command.Force;
            float edgeMarginX = Mathf.Min(projected.x - context.TableBounds.min.x, context.TableBounds.max.x - projected.x);
            float edgeMarginZ = Mathf.Min(projected.z - context.TableBounds.min.z, context.TableBounds.max.z - projected.z);
            float edgeSafety = Mathf.Clamp01(Mathf.Min(edgeMarginX, edgeMarginZ) / 1.2f);
            float powerFit = 1f - Mathf.Abs(command.Strength01 - context.Profile.PreferredPower);
            float riskBonus = context.Profile.RiskTolerance * (1f - edgeSafety);
            float spinFit = Mathf.Clamp01(Vector3.Distance(command.ContactPointWorld, context.FootballCollider.bounds.center));
            bool pathBlocked = PathBlockedByObstacle(command, context);
            float shotTypeScore = 0f;
            if (command.ShotType == FootballShotType.AirFlickShot)
            {
                shotTypeScore += pathBlocked ? 2f + context.Profile.ObstacleUsePreference : -0.45f * (1f - context.Profile.RiskTolerance);
                shotTypeScore -= (1f - edgeSafety) * 1.25f;
            }
            else if (pathBlocked)
            {
                shotTypeScore -= 2.2f + context.Profile.ObstacleUsePreference;
            }

            return alignment * 3f +
                   edgeSafety * (1f - context.Profile.RiskTolerance) +
                   powerFit +
                   riskBonus +
                   spinFit * Mathf.Abs(context.Profile.PreferredSpin) +
                   shotTypeScore;
        }

        private static bool PathBlockedByObstacle(FlickCommand command, OpponentDecisionContext context)
        {
            if (context.ObstacleBounds == null || context.ObstacleBounds.Count == 0)
            {
                return false;
            }

            Vector3 origin = context.FootballCollider.bounds.center;
            Vector3 direction = command.Direction;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            direction.Normalize();
            float length = Mathf.Max(0.5f, command.Force * 1.2f);
            Ray ray = new(origin, direction);
            foreach (Bounds obstacle in context.ObstacleBounds)
            {
                Bounds expanded = obstacle;
                expanded.Expand(new Vector3(0.16f, 0.2f, 0.16f));
                if (expanded.IntersectRay(ray, out float distance) && distance <= length)
                {
                    return true;
                }
            }

            return false;
        }
    }

}
