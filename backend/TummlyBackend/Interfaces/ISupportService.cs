using TummlyBackend.DTOs.HelpCentre;

namespace TummlyBackend.Interfaces
{
    public interface ISupportService
    {
        Task<object> ListQueriesAsync(
            string? status,
            string? topic,
            string? q,
            string? type,
            int page = 1,
            int pageSize = 20
        );

        Task<object?> GetQueryAsync(int queryId);

        Task<object> AddSupportReplyAsync(
            int staffId,
            int queryId,
            SupportReplyDto dto
        );

        Task<object> UpdateStatusAsync(
            int staffId,
            int queryId,
            UpdateQueryStatusDto dto
        );

        Task<(Stream Stream, string ContentType, string FileName)?> GetQueryAttachmentAsync(
            int queryId,
            int attachmentId
        );
    }
}
