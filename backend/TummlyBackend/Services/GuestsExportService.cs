using TummlyBackend.DTOs.Guests;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Export surface adapter — keeps <see cref="IGuestsExportService"/> off the
    /// list service type while reusing shared list/export query machinery.
    /// </summary>
    public class GuestsExportService : IGuestsExportService
    {
        private readonly GuestsListService _guestsList;

        public GuestsExportService(GuestsListService guestsList)
        {
            _guestsList = guestsList;
        }

        public Task<GuestsExportResult> ExportAsync(GuestsExportQuery query) =>
            _guestsList.ExportAsync(query);
    }
}
