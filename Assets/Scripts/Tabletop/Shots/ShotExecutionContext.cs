using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Shots
{
    public readonly struct ShotExecutionContext
    {
        public ShotExecutionContext(
            FootballShotType shotType,
            PaperFootballPlayer player,
            int runSeed,
            int encounterIndex,
            int possessionNumber,
            int shotSequenceNumber,
            bool canScoreTouchdown,
            bool canScoreFieldGoal)
        {
            ShotType = shotType;
            Player = player;
            RunSeed = runSeed;
            EncounterIndex = Mathf.Max(0, encounterIndex);
            PossessionNumber = Mathf.Max(0, possessionNumber);
            ShotSequenceNumber = Mathf.Max(0, shotSequenceNumber);
            CanScoreTouchdown = canScoreTouchdown;
            CanScoreFieldGoal = canScoreFieldGoal && shotType == FootballShotType.FieldGoalKick;
        }

        public FootballShotType ShotType { get; }
        public PaperFootballPlayer Player { get; }
        public int RunSeed { get; }
        public int EncounterIndex { get; }
        public int PossessionNumber { get; }
        public int ShotSequenceNumber { get; }
        public bool CanScoreTouchdown { get; }
        public bool CanScoreFieldGoal { get; }

        public static ShotExecutionContext Normal(
            FootballShotType shotType,
            PaperFootballPlayer player,
            int runSeed,
            int encounterIndex,
            int possessionNumber,
            int shotSequenceNumber)
        {
            FootballShotType normalShotType = shotType == FootballShotType.AirFlickShot
                ? FootballShotType.AirFlickShot
                : FootballShotType.FlatTableShot;

            return new ShotExecutionContext(
                normalShotType,
                player,
                runSeed,
                encounterIndex,
                possessionNumber,
                shotSequenceNumber,
                canScoreTouchdown: true,
                canScoreFieldGoal: false);
        }

        public static ShotExecutionContext FieldGoal(
            PaperFootballPlayer player,
            int runSeed,
            int encounterIndex,
            int possessionNumber,
            int shotSequenceNumber)
        {
            return new ShotExecutionContext(
                FootballShotType.FieldGoalKick,
                player,
                runSeed,
                encounterIndex,
                possessionNumber,
                shotSequenceNumber,
                canScoreTouchdown: false,
                canScoreFieldGoal: true);
        }

        public static ShotExecutionContext None => new(
            FootballShotType.FlatTableShot,
            PaperFootballPlayer.PlayerOne,
            0,
            0,
            0,
            0,
            canScoreTouchdown: false,
            canScoreFieldGoal: false);
    }
}
