namespace TummlyBackend.DTOs.Assistant
{
    public static class AssistantTurnProgressSteps
    {
        public const string Checking = "checking";
        public const string Retrieving = "retrieving";
        public const string Preparing = "preparing";
    }

    public sealed class AssistantTurnProgressDto
    {
        public int ConversationId { get; init; }

        public string Step { get; init; } = string.Empty;
    }
}
