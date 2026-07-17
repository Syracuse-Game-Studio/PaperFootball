using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Scoring
{
    public readonly struct OverhangDebugSnapshot
    {
        public OverhangDebugSnapshot(
            PaperFootballPlayer attackingPlayer,
            ScoringEdge attackingEdge,
            Bounds footballBounds,
            Bounds tableBounds,
            float overhangDistance,
            float overhangPercent,
            float supportedPercent,
            bool isSupported,
            bool footballFell,
            bool hasPositiveOverhang,
            float requiredOverhangPercent,
            float requiredSupportedPercent,
            bool overhangQualifiesForTouchdown,
            bool scoringEventAlreadyProcessed)
        {
            AttackingPlayer = attackingPlayer;
            AttackingEdge = attackingEdge;
            FootballBounds = footballBounds;
            TableBounds = tableBounds;
            OverhangDistance = overhangDistance;
            OverhangPercent = overhangPercent;
            SupportedPercent = supportedPercent;
            IsSupported = isSupported;
            FootballFell = footballFell;
            HasPositiveOverhang = hasPositiveOverhang;
            RequiredOverhangPercent = requiredOverhangPercent;
            RequiredSupportedPercent = requiredSupportedPercent;
            OverhangQualifiesForTouchdown = overhangQualifiesForTouchdown;
            FinalTouchdownDecision = !footballFell && overhangQualifiesForTouchdown;
            ScoringEventAlreadyProcessed = scoringEventAlreadyProcessed;
        }

        public PaperFootballPlayer AttackingPlayer { get; }
        public ScoringEdge AttackingEdge { get; }
        public Bounds FootballBounds { get; }
        public Bounds TableBounds { get; }
        public float OverhangDistance { get; }
        public float OverhangPercent { get; }
        public float SupportedPercent { get; }
        public bool IsSupported { get; }
        public bool FootballFell { get; }
        public bool HasPositiveOverhang { get; }
        public float RequiredOverhangPercent { get; }
        public float RequiredSupportedPercent { get; }
        public bool OverhangQualifiesForTouchdown { get; }
        public bool FinalTouchdownDecision { get; }
        public bool ScoringEventAlreadyProcessed { get; }

        public EdgeOverhangResult ToOverhangResult()
        {
            return new EdgeOverhangResult(
                OverhangQualifiesForTouchdown,
                OverhangPercent,
                SupportedPercent,
                OverhangDistance,
                AttackingEdge);
        }

        public OverhangDebugSnapshot WithScoringEventProcessed(bool processed)
        {
            return new OverhangDebugSnapshot(
                AttackingPlayer,
                AttackingEdge,
                FootballBounds,
                TableBounds,
                OverhangDistance,
                OverhangPercent,
                SupportedPercent,
                IsSupported,
                FootballFell,
                HasPositiveOverhang,
                RequiredOverhangPercent,
                RequiredSupportedPercent,
                OverhangQualifiesForTouchdown,
                processed);
        }
    }
}
