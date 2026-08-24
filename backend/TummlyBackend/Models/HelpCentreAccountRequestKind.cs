namespace TummlyBackend.Models
{
    public enum HelpCentreAccountRequestKind
    {
        TransferOwnership,
        AccountExport,
        AccountClosure,
    }

    public static class HelpCentreAccountRequestKindExtensions
    {
        public static string ToWireString(this HelpCentreAccountRequestKind kind) =>
            kind switch
            {
                HelpCentreAccountRequestKind.TransferOwnership =>
                    "TransferOwnership",
                HelpCentreAccountRequestKind.AccountExport =>
                    "AccountExport",
                HelpCentreAccountRequestKind.AccountClosure =>
                    "AccountClosure",
                _ => kind.ToString(),
            };

        public static string ToDisplayLabel(this HelpCentreAccountRequestKind kind) =>
            kind switch
            {
                HelpCentreAccountRequestKind.TransferOwnership =>
                    "Transfer ownership",
                HelpCentreAccountRequestKind.AccountExport =>
                    "Account export",
                HelpCentreAccountRequestKind.AccountClosure =>
                    "Account closure",
                _ => kind.ToString(),
            };

        public static bool TryParseWireString(
            string? stored,
            out HelpCentreAccountRequestKind kind
        )
        {
            var normalized = stored?.Trim() ?? string.Empty;

            switch (normalized)
            {
                case "TransferOwnership":
                    kind = HelpCentreAccountRequestKind.TransferOwnership;
                    return true;
                case "AccountExport":
                    kind = HelpCentreAccountRequestKind.AccountExport;
                    return true;
                case "AccountClosure":
                    kind = HelpCentreAccountRequestKind.AccountClosure;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        public static HelpCentreAccountRequestKind FromWireString(string stored)
        {
            if (TryParseWireString(stored, out var kind))
            {
                return kind;
            }

            throw new ArgumentException("Invalid account request kind.");
        }

        public static HelpCentreQueryTopic TopicForKind(
            HelpCentreAccountRequestKind kind
        ) =>
            kind switch
            {
                HelpCentreAccountRequestKind.TransferOwnership =>
                    HelpCentreQueryTopic.SomethingElse,
                HelpCentreAccountRequestKind.AccountExport =>
                    HelpCentreQueryTopic.PrivacyData,
                HelpCentreAccountRequestKind.AccountClosure =>
                    HelpCentreQueryTopic.PrivacyData,
                _ => HelpCentreQueryTopic.SomethingElse,
            };
    }
}
