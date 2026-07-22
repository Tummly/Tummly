namespace TummlyBackend.Interfaces
{
    public interface IGuestProfileService
    {
        Task<object?> GetDetailAsync(
            int guestId,
            int locationId,
            string locationName
        );
    }
}
