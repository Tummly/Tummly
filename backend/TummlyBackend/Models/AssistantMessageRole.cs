namespace TummlyBackend.Models
{
    public enum AssistantMessageRole
    {
        User = 0,
        Assistant = 1
    }

    public static class AssistantMessageRoleExtensions
    {
        public static string ToWireString(this AssistantMessageRole role)
            => role switch
            {
                AssistantMessageRole.User => "user",
                AssistantMessageRole.Assistant => "assistant",
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
            };

        public static AssistantMessageRole FromWireString(string value)
            => value switch
            {
                "user" => AssistantMessageRole.User,
                "assistant" => AssistantMessageRole.Assistant,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
    }
}
