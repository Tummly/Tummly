namespace TummlyBackend.Helpers
{
    public static class AssistantDraftCatalogueCopy
    {
        public static string Ask(
            string prompt,
            string catalogueTitle,
            IEnumerable<string> labels,
            string? footer = null
        )
        {
            var body = prompt.TrimEnd()
                + "\n\n"
                + $"### {catalogueTitle}\n"
                + string.Join("\n", labels.Select(label => $"- {label}"));
            if (!string.IsNullOrWhiteSpace(footer))
            {
                body += "\n\n" + footer.Trim();
            }

            return body;
        }

        public static string AskCandidates(
            string prompt,
            string catalogueTitle,
            IReadOnlyList<string> candidates,
            string emptyMessage,
            string? moreNote = null,
            string? footer = null
        )
        {
            if (candidates.Count == 0)
            {
                return $"{prompt.TrimEnd()}\n\n{emptyMessage}";
            }

            var combinedFooter = string.IsNullOrWhiteSpace(moreNote)
                ? footer
                : string.Join(
                    " ",
                    new[] { moreNote, footer }.Where(part => !string.IsNullOrWhiteSpace(part))
                );
            return Ask(prompt, catalogueTitle, candidates, combinedFooter);
        }
    }
}
