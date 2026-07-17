using PaperFootball.Tabletop.Scoring;

namespace PaperFootball.Tabletop.Rules
{
    public static class PaperFootballRules
    {
        public static FlickResolutionType ResolveStoppedFootball(bool footballFell, EdgeOverhangResult overhangResult)
        {
            if (footballFell)
            {
                return FlickResolutionType.FellFromTable;
            }

            return overhangResult.IsTouchdown ? FlickResolutionType.Touchdown : FlickResolutionType.StoppedNoScore;
        }
    }
}
