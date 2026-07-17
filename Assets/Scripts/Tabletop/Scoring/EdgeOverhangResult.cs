namespace PaperFootball.Tabletop.Scoring
{
    public struct EdgeOverhangResult
    {
        public EdgeOverhangResult(
            bool isTouchdown,
            float overhangPercent,
            float supportedPercent,
            float overhangDistance,
            ScoringEdge edge)
        {
            IsTouchdown = isTouchdown;
            OverhangPercent = overhangPercent;
            SupportedPercent = supportedPercent;
            OverhangDistance = overhangDistance;
            Edge = edge;
        }

        public bool IsTouchdown { get; }
        public float OverhangPercent { get; }
        public float SupportedPercent { get; }
        public float OverhangDistance { get; }
        public ScoringEdge Edge { get; }
    }
}
