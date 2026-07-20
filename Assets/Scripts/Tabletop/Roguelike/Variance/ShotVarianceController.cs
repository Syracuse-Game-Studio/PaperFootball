using System;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Variance
{
    public class ShotVarianceController : MonoBehaviour
    {
        [SerializeField] private ShotVarianceSettings settings;
        [SerializeField] private Collider footballCollider;
        [SerializeField] private bool varianceEnabled;
        [SerializeField] private int runSeed = 12345;

        private float forceVarianceScale = 1f;
        private float directionVarianceScale = 1f;
        private float contactVarianceScale = 1f;
        private float previewAccuracyBonus;
        private int encounterIndex;
        private int flickSequenceNumber;

        public bool VarianceEnabled => varianceEnabled && settings != null && settings.VarianceEnabled;
        public int RunSeed => runSeed;
        public int EncounterIndex => encounterIndex;
        public int FlickSequenceNumber => flickSequenceNumber;
        public int LastRandomStreamSeed { get; private set; }
        public ResolvedFlickParameters LastResolved { get; private set; }
        public ShotVarianceTuning CurrentTuning => VarianceEnabled
            ? settings.CreateTuning(forceVarianceScale, directionVarianceScale, contactVarianceScale, previewAccuracyBonus)
            : ShotVarianceTuning.Disabled;

        public event Action<ResolvedFlickParameters> FlickResolved;

        public void Configure(ShotVarianceSettings varianceSettings, Collider targetFootball, int seed)
        {
            settings = varianceSettings;
            footballCollider = targetFootball;
            SetRunSeed(seed);
        }

        public void SetRunSeed(int seed)
        {
            runSeed = seed;
            flickSequenceNumber = 0;
            LastRandomStreamSeed = 0;
            LastResolved = default;
        }

        public void SetEncounterIndex(int index)
        {
            encounterIndex = Mathf.Max(0, index);
        }

        public void SetVarianceEnabled(bool enabled)
        {
            varianceEnabled = enabled;
        }

        public void SetModifierScales(float forceScale, float directionScale, float contactScale, float accuracyBonus)
        {
            forceVarianceScale = Mathf.Max(0f, forceScale);
            directionVarianceScale = Mathf.Max(0f, directionScale);
            contactVarianceScale = Mathf.Max(0f, contactScale);
            previewAccuracyBonus = accuracyBonus;
        }

        public ResolvedFlickParameters Resolve(
            FlickCommand command,
            PaperFootballRuleSet rules,
            PaperFootballPlayer player,
            int possessionNumber,
            string stableIdentifier)
        {
            return Resolve(command, rules, player, possessionNumber, stableIdentifier, CurrentTuning);
        }

        public ResolvedFlickParameters Resolve(
            FlickCommand command,
            PaperFootballRuleSet rules,
            PaperFootballPlayer player,
            int possessionNumber,
            string stableIdentifier,
            ShotVarianceTuning tuningOverride)
        {
            int nextSequence = command.IsValid ? flickSequenceNumber + 1 : flickSequenceNumber;
            int streamSeed = StableSeedUtility.DeriveSeed(
                runSeed,
                RunRandomStream.ShotVariance,
                encounterIndex,
                player,
                possessionNumber,
                nextSequence,
                stableIdentifier);

            ShotVarianceTuning tuning = tuningOverride;
            IRunRandom random = tuning.VarianceEnabled ? new DeterministicRunRandom(streamSeed) : null;
            ResolvedFlickParameters resolved = FlickParameterResolver.Resolve(
                command,
                tuning,
                rules,
                footballCollider,
                random,
                streamSeed,
                nextSequence);

            if (command.IsValid)
            {
                flickSequenceNumber = nextSequence;
                LastRandomStreamSeed = streamSeed;
                LastResolved = resolved;
                FlickResolved?.Invoke(resolved);
            }

            return resolved;
        }
    }
}
