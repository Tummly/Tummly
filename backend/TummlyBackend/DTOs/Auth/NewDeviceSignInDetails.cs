namespace TummlyBackend.DTOs.Auth
{
    public sealed class NewDeviceSignInDetails
    {
        public string FirstName { get; init; } = string.Empty;

        public string SignInTime { get; init; } = string.Empty;

        public string DeviceSummary { get; init; } = string.Empty;

        public string LocationSummary { get; init; } = string.Empty;
    }
}
