namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared next-step sentence when a live answer has no facts or did not
    /// persist. Points at Change Scope and a more specific ask.
    /// </summary>
    public static class AssistantNextTryCopy
    {
        public const string Sentence =
            "Use Change Scope to pick another Owned location or Reporting period, "
            + "or send a more specific ask.";
    }
}
