using TummlyBackend.DTOs.HelpCentre;

namespace TummlyBackend.Interfaces
{
    public interface IHelpCentreService
    {
        Task<object> CreateQueryAsync(
            CreateHelpCentreQueryDto dto,
            int? userId,
            IReadOnlyList<IFormFile>? attachments = null
        );

        Task<object> ListMyQueriesAsync(int userId);

        Task<object?> GetMyQueryAsync(int userId, int queryId);

        Task<object> AddOperatorReplyAsync(
            int userId,
            int queryId,
            OperatorReplyDto dto
        );

        Task<object?> GetContactPrefillAsync(int userId);

        Task<(Stream Stream, string ContentType, string FileName)?> GetMyQueryAttachmentAsync(
            int userId,
            int queryId,
            int attachmentId
        );

        Task<object?> GetOpenAccountRequestAsync(
            int userId,
            int restaurantId,
            string accountRequestKind
        );
    }
}
