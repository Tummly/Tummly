namespace TummlyBackend.Models
{
    public enum AssistantMessageClass
    {
        Grounded = 0,
        Refusal = 1,
        Failure = 2,
        Clarify = 3,
        Gap = 4
    }

    public static class AssistantMessageClassExtensions
    {
        public static string ToWireString(this AssistantMessageClass value)
            => value switch
            {
                AssistantMessageClass.Grounded => "grounded",
                AssistantMessageClass.Refusal => "refusal",
                AssistantMessageClass.Failure => "failure",
                AssistantMessageClass.Clarify => "clarify",
                AssistantMessageClass.Gap => "gap",
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };

        public static AssistantMessageClass FromWireString(string value)
            => value switch
            {
                "grounded" => AssistantMessageClass.Grounded,
                "refusal" => AssistantMessageClass.Refusal,
                "failure" => AssistantMessageClass.Failure,
                "clarify" => AssistantMessageClass.Clarify,
                "gap" => AssistantMessageClass.Gap,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
    }
}
