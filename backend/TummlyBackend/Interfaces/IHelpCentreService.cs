using TummlyBackend.DTOs.HelpCentre;

namespace TummlyBackend.Interfaces
{
    public interface IHelpCentreService
    {
        Task<object> CreateQueryAsync(
            CreateHelpCentreQueryDto dto,
            int? userId
        );

        Task<object> ListMyQueriesAsync(int userId);

        Task<object?> GetMyQueryAsync(int userId, int queryId);

        Task<object> AddOperatorReplyAsync(
            int userId,
            int queryId,
            OperatorReplyDto dto
        );

        Task<object?> GetContactPrefillAsync(int userId);
    }
}
