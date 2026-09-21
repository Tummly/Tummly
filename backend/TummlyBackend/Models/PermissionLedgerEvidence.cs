namespace TummlyBackend.Models
{
    public sealed record PermissionLedgerEvidence(
        string? Basis,
        string GuestFormVersion,
        string WordingVersion,
        string PrivacyNoticeVersion,
        string WordingSnapshot
    );
}
