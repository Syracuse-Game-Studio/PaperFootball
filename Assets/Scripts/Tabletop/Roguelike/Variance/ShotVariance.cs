using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Variance
{
    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Shot Variance Settings", fileName = "ShotVarianceSettings")]
    public partial class ShotVarianceSettings
    {
        [SerializeField] private bool varianceEnabled = true;
        [SerializeField] private float forceVariancePercent = 0.03f;
        [SerializeField] private float directionVarianceDegrees = 1.5f;
        [SerializeField] private float contactPointVarianceRadius = 0.0075f;
        [SerializeField] private bool revealSampledResult;
        [SerializeField] private string accuracyRating = "Stable";

        public bool VarianceEnabled => varianceEnabled;
        public float ForceVariancePercent => Mathf.Max(0f, forceVariancePercent);
        public float DirectionVarianceDegrees => Mathf.Max(0f, directionVarianceDegrees);
        public float ContactPointVarianceRadius => Mathf.Max(0f, contactPointVarianceRadius);
        public bool RevealSampledResult => revealSampledResult;
        public string AccuracyRating => string.IsNullOrWhiteSpace(accuracyRating) ? "Stable" : accuracyRating;

        public void Configure(bool enabled, float forcePercent, float directionDegrees, float contactRadius, bool revealSample, string rating)
        {
            varianceEnabled = enabled;
            forceVariancePercent = Mathf.Max(0f, forcePercent);
            directionVarianceDegrees = Mathf.Max(0f, directionDegrees);
            contactPointVarianceRadius = Mathf.Max(0f, contactRadius);
            revealSampledResult = revealSample;
            accuracyRating = string.IsNullOrWhiteSpace(rating) ? "Stable" : rating;
        }

        public ShotVarianceTuning CreateTuning(float forceScale = 1f, float directionScale = 1f, float contactScale = 1f, float previewAccuracyBonus = 0f)
        {
            float accuracyScore = Mathf.Clamp01(0.5f + previewAccuracyBonus);
            string rating = accuracyScore >= 0.75f ? "Precise" : accuracyScore <= 0.3f ? "Wild" : AccuracyRating;
            return new ShotVarianceTuning(
                varianceEnabled,
                ForceVariancePercent * Mathf.Max(0f, forceScale),
                DirectionVarianceDegrees * Mathf.Max(0f, directionScale),
                ContactPointVarianceRadius * Mathf.Max(0f, contactScale),
                revealSampledResult,
                rating);
        }

        private void OnValidate()
        {
            forceVariancePercent = Mathf.Max(0f, forceVariancePercent);
            directionVarianceDegrees = Mathf.Max(0f, directionVarianceDegrees);
            contactPointVarianceRadius = Mathf.Max(0f, contactPointVarianceRadius);
        }
    }

    public readonly struct ShotVarianceTuning
    {
        public ShotVarianceTuning(
            bool varianceEnabled,
            float forceVariancePercent,
            float directionVarianceDegrees,
            float contactPointVarianceRadius,
            bool revealSampledResult,
            string accuracyRating)
        {
            VarianceEnabled = varianceEnabled;
            ForceVariancePercent = Mathf.Max(0f, forceVariancePercent);
            DirectionVarianceDegrees = Mathf.Max(0f, directionVarianceDegrees);
            ContactPointVarianceRadius = Mathf.Max(0f, contactPointVarianceRadius);
            RevealSampledResult = revealSampledResult;
            AccuracyRating = string.IsNullOrWhiteSpace(accuracyRating) ? "Stable" : accuracyRating;
        }

        public bool VarianceEnabled { get; }
        public float ForceVariancePercent { get; }
        public float DirectionVarianceDegrees { get; }
        public float ContactPointVarianceRadius { get; }
        public bool RevealSampledResult { get; }
        public string AccuracyRating { get; }

        public static ShotVarianceTuning Disabled => new(false, 0f, 0f, 0f, false, "Perfect");

        public ShotVarianceTuning Scaled(float forceScale, float directionScale, float contactScale, float previewAccuracyBonus = 0f)
        {
            float accuracyScore = Mathf.Clamp01(0.5f + previewAccuracyBonus);
            string rating = accuracyScore >= 0.75f ? "Precise" : accuracyScore <= 0.3f ? "Wild" : AccuracyRating;
            return new ShotVarianceTuning(
                VarianceEnabled,
                ForceVariancePercent * Mathf.Max(0f, forceScale),
                DirectionVarianceDegrees * Mathf.Max(0f, directionScale),
                ContactPointVarianceRadius * Mathf.Max(0f, contactScale),
                RevealSampledResult,
                rating);
        }
    }

    public readonly struct ResolvedFlickParameters
    {
        public ResolvedFlickParameters(
            FlickCommand baseCommand,
            Vector3 baseDirection,
            Vector3 finalDirection,
            float baseForce,
            float finalForce,
            Vector3 selectedContactPointWorld,
            Vector3 finalContactPointWorld,
            float appliedDirectionVarianceDegrees,
            float appliedForceMultiplier,
            Vector3 appliedContactOffsetLocal,
            int randomStreamSeed,
            int flickSequenceNumber)
        {
            BaseCommand = baseCommand;
            BaseDirection = NormalizeTabletop(baseDirection);
            FinalDirection = NormalizeTabletop(finalDirection);
            BaseForce = Mathf.Max(0f, baseForce);
            FinalForce = Mathf.Max(0f, finalForce);
            SelectedContactPointWorld = selectedContactPointWorld;
            FinalContactPointWorld = finalContactPointWorld;
            AppliedDirectionVarianceDegrees = appliedDirectionVarianceDegrees;
            AppliedForceMultiplier = appliedForceMultiplier;
            AppliedContactOffsetLocal = appliedContactOffsetLocal;
            RandomStreamSeed = randomStreamSeed;
            FlickSequenceNumber = flickSequenceNumber;
        }

        public FlickCommand BaseCommand { get; }
        public Vector3 BaseDirection { get; }
        public Vector3 FinalDirection { get; }
        public float BaseForce { get; }
        public float FinalForce { get; }
        public Vector3 SelectedContactPointWorld { get; }
        public Vector3 FinalContactPointWorld { get; }
        public float AppliedDirectionVarianceDegrees { get; }
        public float AppliedForceMultiplier { get; }
        public Vector3 AppliedContactOffsetLocal { get; }
        public int RandomStreamSeed { get; }
        public int FlickSequenceNumber { get; }
        public bool IsValid => BaseCommand.IsValid;

        public FlickCommand ToFlickCommand()
        {
            if (!BaseCommand.IsValid)
            {
                return BaseCommand;
            }

            return new FlickCommand(
                true,
                BaseCommand.DragStartWorld,
                BaseCommand.CurrentWorld,
                BaseCommand.ReleaseWorld,
                FinalDirection,
                FinalForce,
                BaseCommand.DragDistance,
                BaseCommand.DragDuration,
                BaseCommand.Strength01,
                FinalContactPointWorld,
                BaseCommand.ShotType);
        }

        public static ResolvedFlickParameters FromUnmodified(FlickCommand command, int streamSeed = 0, int flickSequenceNumber = 0)
        {
            Vector3 contact = command.HasContactPoint ? command.ContactPointWorld : command.DragStartWorld;
            return new ResolvedFlickParameters(
                command,
                command.Direction,
                command.Direction,
                command.Force,
                command.Force,
                contact,
                contact,
                0f,
                1f,
                Vector3.zero,
                streamSeed,
                flickSequenceNumber);
        }

        private static Vector3 NormalizeTabletop(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.000001f ? value.normalized : Vector3.forward;
        }
    }

    public static class FlickParameterResolver
    {
        public static ResolvedFlickParameters Resolve(
            FlickCommand command,
            ShotVarianceTuning tuning,
            PaperFootballRuleSet rules,
            Collider footballCollider,
            IRunRandom random,
            int randomStreamSeed,
            int flickSequenceNumber)
        {
            if (!command.IsValid)
            {
                return ResolvedFlickParameters.FromUnmodified(command, randomStreamSeed, flickSequenceNumber);
            }

            PaperFootballRuleSet runtimeRules = rules != null ? rules.Clone() : new PaperFootballRuleSet();
            runtimeRules.Sanitize();

            if (!tuning.VarianceEnabled || random == null)
            {
                return ResolvedFlickParameters.FromUnmodified(command, randomStreamSeed, flickSequenceNumber);
            }

            Vector3 baseDirection = NormalizeTabletop(command.Direction);
            float baseForce = command.Force;

            float forceMultiplier = tuning.ForceVariancePercent <= 0f
                ? 1f
                : random.Range(1f - tuning.ForceVariancePercent, 1f + tuning.ForceVariancePercent);
            float finalForce = Mathf.Clamp(baseForce * forceMultiplier, runtimeRules.minimumFlickForce, runtimeRules.maximumFlickForce);

            float directionOffset = tuning.DirectionVarianceDegrees <= 0f
                ? 0f
                : random.Range(-tuning.DirectionVarianceDegrees, tuning.DirectionVarianceDegrees);
            Vector3 finalDirection = Quaternion.AngleAxis(directionOffset, Vector3.up) * baseDirection;
            finalDirection = NormalizeTabletop(finalDirection);

            Vector3 selectedContact = command.HasContactPoint ? command.ContactPointWorld : command.DragStartWorld;
            Vector3 finalContact = selectedContact;
            Vector3 contactOffsetLocal = Vector3.zero;

            if (command.HasContactPoint && tuning.ContactPointVarianceRadius > 0f)
            {
                contactOffsetLocal = SampleLocalContactOffset(random, tuning.ContactPointVarianceRadius);
                finalContact = ResolveContactPoint(footballCollider, selectedContact, contactOffsetLocal);
            }

            return new ResolvedFlickParameters(
                command,
                baseDirection,
                finalDirection,
                baseForce,
                finalForce,
                selectedContact,
                finalContact,
                directionOffset,
                forceMultiplier,
                contactOffsetLocal,
                randomStreamSeed,
                flickSequenceNumber);
        }

        private static Vector3 SampleLocalContactOffset(IRunRandom random, float radius)
        {
            float angle = random.Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(random.Value()) * radius;
            return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }

        private static Vector3 ResolveContactPoint(Collider collider, Vector3 selectedWorldPoint, Vector3 localOffset)
        {
            if (collider == null)
            {
                return selectedWorldPoint + localOffset;
            }

            if (collider is BoxCollider box)
            {
                return ResolveBoxColliderContact(box, selectedWorldPoint, localOffset);
            }

            Vector3 candidate = collider.transform.TransformPoint(collider.transform.InverseTransformPoint(selectedWorldPoint) + localOffset);
            Vector3 closest = collider.ClosestPoint(candidate);
            if ((closest - candidate).sqrMagnitude <= 0.0000001f && collider.bounds.Contains(candidate))
            {
                Vector3 fromCenter = candidate - collider.bounds.center;
                fromCenter.y = 0f;
                if (fromCenter.sqrMagnitude <= 0.000001f)
                {
                    fromCenter = selectedWorldPoint - collider.bounds.center;
                    fromCenter.y = 0f;
                }

                if (fromCenter.sqrMagnitude > 0.000001f)
                {
                    closest = collider.ClosestPoint(collider.bounds.center + fromCenter.normalized * 100f);
                }
            }

            closest.y = selectedWorldPoint.y;
            return closest;
        }

        private static Vector3 ResolveBoxColliderContact(BoxCollider box, Vector3 selectedWorldPoint, Vector3 localOffset)
        {
            Transform transform = box.transform;
            Vector3 selectedLocal = transform.InverseTransformPoint(selectedWorldPoint);
            Vector3 candidateLocal = selectedLocal + localOffset;
            Vector3 halfSize = box.size * 0.5f;
            Vector3 min = box.center - halfSize;
            Vector3 max = box.center + halfSize;

            candidateLocal.x = Mathf.Clamp(candidateLocal.x, min.x, max.x);
            candidateLocal.y = Mathf.Clamp(candidateLocal.y, min.y, max.y);
            candidateLocal.z = Mathf.Clamp(candidateLocal.z, min.z, max.z);

            Vector3 relative = selectedLocal - box.center;
            float xScore = halfSize.x > 0.0001f ? Mathf.Abs(relative.x / halfSize.x) : 0f;
            float yScore = halfSize.y > 0.0001f ? Mathf.Abs(relative.y / halfSize.y) : 0f;
            float zScore = halfSize.z > 0.0001f ? Mathf.Abs(relative.z / halfSize.z) : 0f;

            if (xScore >= yScore && xScore >= zScore)
            {
                candidateLocal.x = box.center.x + Mathf.Sign(Mathf.Approximately(relative.x, 0f) ? 1f : relative.x) * halfSize.x;
            }
            else if (zScore >= xScore && zScore >= yScore)
            {
                candidateLocal.z = box.center.z + Mathf.Sign(Mathf.Approximately(relative.z, 0f) ? 1f : relative.z) * halfSize.z;
            }
            else
            {
                candidateLocal.y = box.center.y + Mathf.Sign(Mathf.Approximately(relative.y, 0f) ? 1f : relative.y) * halfSize.y;
            }

            Vector3 world = transform.TransformPoint(candidateLocal);
            world.y = selectedWorldPoint.y;
            return world;
        }

        private static Vector3 NormalizeTabletop(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.000001f ? value.normalized : Vector3.forward;
        }
    }
}
