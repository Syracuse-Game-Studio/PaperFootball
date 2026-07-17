using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Scoring
{
    public static class EdgeOverhangCalculator
    {
        public static EdgeOverhangResult Calculate(
            Bounds tableBounds,
            Bounds footballBounds,
            PaperFootballPlayer attackingPlayer,
            PaperFootballRuleSet rules)
        {
            PaperFootballRuleSet runtimeRules = rules ?? new PaperFootballRuleSet();
            runtimeRules.Sanitize();

            ScoringEdge edge = attackingPlayer == PaperFootballPlayer.PlayerOne
                ? ScoringEdge.PositiveZ
                : ScoringEdge.NegativeZ;

            return Calculate(tableBounds, footballBounds, edge, runtimeRules);
        }

        public static EdgeOverhangResult Calculate(
            Bounds tableBounds,
            Bounds footballBounds,
            ScoringEdge edge,
            PaperFootballRuleSet rules)
        {
            PaperFootballRuleSet runtimeRules = rules ?? new PaperFootballRuleSet();
            runtimeRules.Sanitize();

            float footballLength = Mathf.Max(footballBounds.size.z, 0.0001f);
            float overhangDistance;
            float supportedDepth;

            if (edge == ScoringEdge.PositiveZ)
            {
                overhangDistance = Mathf.Max(0f, footballBounds.max.z - tableBounds.max.z);
                supportedDepth = Mathf.Max(0f, tableBounds.max.z - footballBounds.min.z);
            }
            else
            {
                overhangDistance = Mathf.Max(0f, tableBounds.min.z - footballBounds.min.z);
                supportedDepth = Mathf.Max(0f, footballBounds.max.z - tableBounds.min.z);
            }

            float overlapX = Mathf.Max(0f, Mathf.Min(footballBounds.max.x, tableBounds.max.x) -
                                           Mathf.Max(footballBounds.min.x, tableBounds.min.x));
            float supportedDepthPercent = Mathf.Clamp01(supportedDepth / footballLength);
            float supportedWidthPercent = Mathf.Clamp01(overlapX / Mathf.Max(footballBounds.size.x, 0.0001f));
            float supportedPercent = Mathf.Min(supportedDepthPercent, supportedWidthPercent);
            float overhangPercent = Mathf.Clamp01(overhangDistance / footballLength);
            float requiredOverhang = runtimeRules.touchdownRequiresOverhang ? runtimeRules.requiredOverhangPercent : 0.0001f;

            bool isTouchdown = overhangPercent >= requiredOverhang &&
                               supportedPercent >= runtimeRules.minimumSupportedPercent &&
                               overhangDistance > 0f;

            return new EdgeOverhangResult(isTouchdown, overhangPercent, supportedPercent, overhangDistance, edge);
        }
    }
}
