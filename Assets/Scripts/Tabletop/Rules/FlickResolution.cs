namespace PaperFootball.Tabletop.Rules
{
    public class FlickResolution
    {
        public FlickResolution(FlickResolutionType type, string message)
        {
            Type = type;
            Message = message;
        }

        public FlickResolutionType Type { get; }
        public string Message { get; }
    }
}
