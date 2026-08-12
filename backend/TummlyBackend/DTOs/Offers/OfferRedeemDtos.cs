namespace TummlyBackend.DTOs.Offers
{
    public sealed class OfferRedeemCheckRequest
    {
        public int LocationId { get; set; }

        public string Code { get; set; } = string.Empty;
    }

    public sealed class OfferRedeemMarkRequest
    {
        public int LocationId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string IssueId { get; set; } = string.Empty;
    }

    public sealed class OfferRedeemConfirmPreviewDto
    {
        public string IssueId { get; init; } = string.Empty;

        public string OfferTitle { get; init; } = string.Empty;

        public string GuestName { get; init; } = string.Empty;

        public string ValidAt { get; init; } = string.Empty;

        public string Expires { get; init; } = string.Empty;

        public string Usage { get; init; } = string.Empty;

        public string StaffInstruction { get; init; } = string.Empty;
    }

    public static class OfferRedeemFailureReasons
    {
        public const string Invalid = "invalid";
        public const string Expired = "expired";
        public const string AlreadyUsed = "already_used";
        public const string Voided = "voided";
        public const string WrongLocation = "wrong_location";
    }

    public abstract record OfferRedeemCheckResult
    {
        public sealed record Ok(OfferRedeemConfirmPreviewDto Preview)
            : OfferRedeemCheckResult;

        public sealed record Failed(string Reason) : OfferRedeemCheckResult;
    }

    public abstract record OfferRedeemMarkResult
    {
        public sealed record Ok : OfferRedeemMarkResult;

        public sealed record Failed(string Reason) : OfferRedeemMarkResult;
    }
}
