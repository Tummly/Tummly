namespace TummlyBackend.Models
{
    /// <summary>
    /// Current Location Guest permission state derived from the append-only
    /// permission ledger (latest event per guest, kind, and location).
    /// </summary>
    public enum LocationGuestPermissionState
    {
        Granted,
        Withdrawn,
        NotRecorded,
    }

    public static class LocationGuestPermissionStateExtensions
    {
        public const string GrantedWire = "granted";
        public const string WithdrawnWire = "withdrawn";
        public const string NotRecordedWire = "not_recorded";

        public static string ToWireString(this LocationGuestPermissionState state) =>
            state switch
            {
                LocationGuestPermissionState.Granted => GrantedWire,
                LocationGuestPermissionState.Withdrawn => WithdrawnWire,
                LocationGuestPermissionState.NotRecorded => NotRecordedWire,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(state),
                    state,
                    "Unknown Location Guest permission state."
                ),
            };

        public static LocationGuestPermissionState FromWireString(string stored)
        {
            if (!TryFromWireString(stored, out var state))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stored),
                    stored,
                    "Unknown Location Guest permission state."
                );
            }

            return state;
        }

        public static bool TryFromWireString(
            string? stored,
            out LocationGuestPermissionState state
        )
        {
            state = default;
            if (string.IsNullOrWhiteSpace(stored))
            {
                return false;
            }

            switch (stored.Trim())
            {
                case GrantedWire:
                    state = LocationGuestPermissionState.Granted;
                    return true;
                case WithdrawnWire:
                    state = LocationGuestPermissionState.Withdrawn;
                    return true;
                case NotRecordedWire:
                    state = LocationGuestPermissionState.NotRecorded;
                    return true;
                default:
                    return false;
            }
        }
    }
}
