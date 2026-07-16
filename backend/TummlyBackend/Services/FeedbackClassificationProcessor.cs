using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class FeedbackClassificationProcessor
        : IFeedbackClassificationProcessor
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeedbackClassificationProvider _provider;
        private readonly ILogger<FeedbackClassificationProcessor> _logger;

        public FeedbackClassificationProcessor(
            ApplicationDbContext context,
            IFeedbackClassificationProvider provider,
            ILogger<FeedbackClassificationProcessor> logger
        )
        {
            _context = context;
            _provider = provider;
            _logger = logger;
        }

        public async Task ProcessAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(
                    f => f.Id == feedbackId,
                    cancellationToken
                );

            if (feedback is null)
            {
                _logger.LogWarning(
                    "Classification skipped — Feedback {FeedbackId} not found",
                    feedbackId
                );
                return;
            }

            if (feedback.ClassificationStatus != ClassificationStatus.Pending)
            {
                return;
            }

            FeedbackClassificationResult result;
            try
            {
                result = await _provider.ClassifyAsync(
                    feedback.Comment,
                    cancellationToken
                );
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException
            )
            {
                _logger.LogError(
                    ex,
                    "Classification provider failed for Feedback {FeedbackId}",
                    feedbackId
                );
                MarkFailed(feedback);
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            switch (result)
            {
                case FeedbackClassificationResult.Succeeded succeeded:
                    feedback.ClassificationStatus =
                        ClassificationStatus.Succeeded;
                    feedback.Sentiment = succeeded.Sentiment;
                    feedback.DetectedIssuesJson =
                        FeedbackClassificationMapping.SerializeDetectedIssues(
                            succeeded.DetectedIssues
                        );
                    break;

                case FeedbackClassificationResult.Failed:
                    MarkFailed(feedback);
                    break;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static void MarkFailed(Feedback feedback)
        {
            feedback.ClassificationStatus = ClassificationStatus.Failed;
            feedback.Sentiment = null;
            feedback.DetectedIssuesJson = null;
        }
    }
}
