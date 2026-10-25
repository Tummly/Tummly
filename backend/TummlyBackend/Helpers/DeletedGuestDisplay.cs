using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Display copy for Claims / Campaign rows after guest erasure (WS2a).
    /// </summary>
    public static class DeletedGuestDisplay
    {
        public const string Name = "Deleted guest";

        public static string NameOrDeleted(LocationGuest? guest)
        {
            if (guest is null)
            {
                return Name;
            }

            return guest.Name?.Trim() ?? string.Empty;
        }
    }
}
