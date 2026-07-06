namespace TummlyBackend.Models
{
    public enum HelpCentreQueryAuthorKind
    {
        Submitter,
        Support,
        Operator,
    }

    public static class HelpCentreQueryAuthorKindExtensions
    {
        public static string ToWireString(this HelpCentreQueryAuthorKind kind) =>
            kind switch
            {
                HelpCentreQueryAuthorKind.Submitter => "SUBMITTER",
                HelpCentreQueryAuthorKind.Support => "SUPPORT",
                HelpCentreQueryAuthorKind.Operator => "OPERATOR",
                _ => kind.ToString(),
            };

        public static HelpCentreQueryAuthorKind FromWireString(string stored)
        {
            var normalized =
                stored?.Trim().ToUpperInvariant() ?? string.Empty;

            return normalized switch
            {
                "SUBMITTER" => HelpCentreQueryAuthorKind.Submitter,
                "SUPPORT" => HelpCentreQueryAuthorKind.Support,
                "OPERATOR" => HelpCentreQueryAuthorKind.Operator,
                _ => HelpCentreQueryAuthorKind.Submitter,
            };
        }
    }
}
