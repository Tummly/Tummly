namespace TummlyBackend.Models
{
    public enum HelpCentreQueryStatus
    {
        New,
        InProgress,
        WaitingOnCustomer,
        EscalatedToAdmin,
        Resolved,
        Closed,
    }

    public static class HelpCentreQueryStatusExtensions
    {
        public static string ToWireString(this HelpCentreQueryStatus status) =>
            status switch
            {
                HelpCentreQueryStatus.New => "NEW",
                HelpCentreQueryStatus.InProgress => "IN_PROGRESS",
                HelpCentreQueryStatus.WaitingOnCustomer =>
                    "WAITING_ON_CUSTOMER",
                HelpCentreQueryStatus.EscalatedToAdmin =>
                    "ESCALATED_TO_ADMIN",
                HelpCentreQueryStatus.Resolved => "RESOLVED",
                HelpCentreQueryStatus.Closed => "CLOSED",
                _ => status.ToString(),
            };

        public static string ToDisplayLabel(this HelpCentreQueryStatus status) =>
            status switch
            {
                HelpCentreQueryStatus.New => "New",
                HelpCentreQueryStatus.InProgress => "In progress",
                HelpCentreQueryStatus.WaitingOnCustomer =>
                    "Waiting on customer",
                HelpCentreQueryStatus.EscalatedToAdmin =>
                    "Escalated to Admin",
                HelpCentreQueryStatus.Resolved => "Resolved",
                HelpCentreQueryStatus.Closed => "Closed",
                _ => status.ToString(),
            };

        public static bool TryParseWireString(
            string? stored,
            out HelpCentreQueryStatus status
        )
        {
            var normalized =
                stored?.Trim().ToUpperInvariant().Replace(" ", "_")
                ?? string.Empty;

            switch (normalized)
            {
                case "NEW":
                    status = HelpCentreQueryStatus.New;
                    return true;
                case "IN_PROGRESS":
                    status = HelpCentreQueryStatus.InProgress;
                    return true;
                case "WAITING_ON_CUSTOMER":
                    status = HelpCentreQueryStatus.WaitingOnCustomer;
                    return true;
                case "ESCALATED_TO_ADMIN":
                    status = HelpCentreQueryStatus.EscalatedToAdmin;
                    return true;
                case "RESOLVED":
                    status = HelpCentreQueryStatus.Resolved;
                    return true;
                case "CLOSED":
                    status = HelpCentreQueryStatus.Closed;
                    return true;
                default:
                    status = default;
                    return false;
            }
        }

        public static HelpCentreQueryStatus FromWireString(string stored)
        {
            if (TryParseWireString(stored, out var status))
            {
                return status;
            }

            return HelpCentreQueryStatus.New;
        }
    }
}
