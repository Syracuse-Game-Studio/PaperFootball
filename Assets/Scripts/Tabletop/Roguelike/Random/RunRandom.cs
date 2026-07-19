using System;
using System.Text;
using PaperFootball.Tabletop.Rules;

namespace PaperFootball.Tabletop.Roguelike.Random
{
    public interface IRunRandom
    {
        int Seed { get; }
        float Value();
        float Range(float minimumInclusive, float maximumInclusive);
        int Range(int minimumInclusive, int maximumExclusive);
    }

    public enum RunRandomStream
    {
        RunGeneration,
        EncounterGeneration,
        RewardGeneration,
        OpponentDecisions,
        ShotVariance,
        Cosmetic
    }

    public sealed class DeterministicRunRandom : IRunRandom
    {
        private readonly System.Random random;

        public DeterministicRunRandom(int seed)
        {
            Seed = seed;
            random = new System.Random(seed);
        }

        public int Seed { get; }

        public float Value()
        {
            return (float)random.NextDouble();
        }

        public float Range(float minimumInclusive, float maximumInclusive)
        {
            if (maximumInclusive < minimumInclusive)
            {
                float swap = minimumInclusive;
                minimumInclusive = maximumInclusive;
                maximumInclusive = swap;
            }

            return minimumInclusive + (maximumInclusive - minimumInclusive) * Value();
        }

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                return minimumInclusive;
            }

            return random.Next(minimumInclusive, maximumExclusive);
        }
    }

    public sealed class SequenceRunRandom : IRunRandom
    {
        private readonly float[] values;
        private int index;

        public SequenceRunRandom(int seed, params float[] deterministicValues)
        {
            Seed = seed;
            values = deterministicValues == null || deterministicValues.Length == 0
                ? new[] { 0.5f }
                : deterministicValues;
        }

        public int Seed { get; }

        public float Value()
        {
            float value = values[index % values.Length];
            index++;
            return Clamp01(value);
        }

        public float Range(float minimumInclusive, float maximumInclusive)
        {
            if (maximumInclusive < minimumInclusive)
            {
                float swap = minimumInclusive;
                minimumInclusive = maximumInclusive;
                maximumInclusive = swap;
            }

            return minimumInclusive + (maximumInclusive - minimumInclusive) * Value();
        }

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                return minimumInclusive;
            }

            int count = maximumExclusive - minimumInclusive;
            int offset = Math.Min(count - 1, (int)Math.Floor(Value() * count));
            return minimumInclusive + offset;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }

    public static class StableSeedUtility
    {
        private const uint OffsetBasis = 2166136261;
        private const uint Prime = 16777619;

        public static int DeriveSeed(
            int runSeed,
            RunRandomStream stream,
            int encounterIndex = 0,
            PaperFootballPlayer player = PaperFootballPlayer.PlayerOne,
            int possessionNumber = 0,
            int flickSequenceNumber = 0,
            string stableIdentifier = "")
        {
            StringBuilder builder = new();
            builder.Append(runSeed).Append('|');
            builder.Append(stream).Append('|');
            builder.Append(encounterIndex).Append('|');
            builder.Append((int)player).Append('|');
            builder.Append(possessionNumber).Append('|');
            builder.Append(flickSequenceNumber).Append('|');
            builder.Append(stableIdentifier ?? string.Empty);
            return HashToPositiveInt(builder.ToString());
        }

        public static int DeriveSeed(int runSeed, string streamName, string stableIdentifier)
        {
            return HashToPositiveInt($"{runSeed}|{streamName}|{stableIdentifier}");
        }

        private static int HashToPositiveInt(string text)
        {
            unchecked
            {
                uint hash = OffsetBasis;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= Prime;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
