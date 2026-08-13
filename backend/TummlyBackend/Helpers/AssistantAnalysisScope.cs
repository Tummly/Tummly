using System.Globalization;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class AssistantAnalysisScope
    {
        public const string FailureBody =
            "The answer could not be completed. Retry this turn.";

        public static string TitleFromFirstUserMessage(string message)
        {
            var firstLine = message
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')[0]
                .Trim();

            if (firstLine.Length <= 200)
            {
                return firstLine;
            }

            return firstLine[..200];
        }

        public static string PeriodPhrase(AssistantReportingPeriodDto period)
        {
            if (string.Equals(period.Kind, "custom", StringComparison.OrdinalIgnoreCase))
            {
                return FormatCustomPeriod(period.StartDate, period.EndDate);
            }

            return period.PresetId switch
            {
                "last30" => "the last 30 days",
                "thisMonth" => "this month",
                _ => "the last 7 days",
            };
        }

        public static void CopyToConversation(
            AssistantConversation conversation,
            AssistantAnalysisScopeDto scope,
            string ownedLocationName
        )
        {
            conversation.OwnedLocationId = scope.OwnedLocationId;
            conversation.OwnedLocationName = ownedLocationName;
            conversation.ReportingPeriodKind = NormalizeKind(scope.ReportingPeriod.Kind);
            conversation.ReportingPeriodPresetId = conversation.ReportingPeriodKind == "preset"
                ? NormalizePreset(scope.ReportingPeriod.PresetId)
                : null;
            conversation.ReportingPeriodStartDate = conversation.ReportingPeriodKind == "custom"
                ? scope.ReportingPeriod.StartDate
                : null;
            conversation.ReportingPeriodEndDate = conversation.ReportingPeriodKind == "custom"
                ? scope.ReportingPeriod.EndDate
                : null;
        }

        public static void CopyToUserMessage(
            AssistantMessage message,
            AssistantAnalysisScopeDto scope,
            string ownedLocationName
        )
        {
            message.OwnedLocationId = scope.OwnedLocationId;
            message.OwnedLocationName = ownedLocationName;
            message.ReportingPeriodKind = NormalizeKind(scope.ReportingPeriod.Kind);
            message.ReportingPeriodPresetId = message.ReportingPeriodKind == "preset"
                ? NormalizePreset(scope.ReportingPeriod.PresetId)
                : null;
            message.ReportingPeriodStartDate = message.ReportingPeriodKind == "custom"
                ? scope.ReportingPeriod.StartDate
                : null;
            message.ReportingPeriodEndDate = message.ReportingPeriodKind == "custom"
                ? scope.ReportingPeriod.EndDate
                : null;
        }

        public static AssistantAnalysisScopeDto FromConversation(
            AssistantConversation conversation
        )
            => new()
            {
                OwnedLocationId = conversation.OwnedLocationId,
                OwnedLocationName = conversation.OwnedLocationName,
                ReportingPeriod = ToPeriodDto(
                    conversation.ReportingPeriodKind,
                    conversation.ReportingPeriodPresetId,
                    conversation.ReportingPeriodStartDate,
                    conversation.ReportingPeriodEndDate
                )
            };

        public static AssistantAnalysisScopeDto? FromUserMessage(
            AssistantMessage message
        )
        {
            if (message.OwnedLocationId is null || message.ReportingPeriodKind is null)
            {
                return null;
            }

            return new AssistantAnalysisScopeDto
            {
                OwnedLocationId = message.OwnedLocationId.Value,
                OwnedLocationName = message.OwnedLocationName ?? string.Empty,
                ReportingPeriod = ToPeriodDto(
                    message.ReportingPeriodKind,
                    message.ReportingPeriodPresetId,
                    message.ReportingPeriodStartDate,
                    message.ReportingPeriodEndDate
                )
            };
        }

        public static AssistantMessageDto ToMessageDto(AssistantMessage message)
            => new()
            {
                Id = message.Id,
                Role = message.Role.ToWireString(),
                Class = message.Class?.ToWireString(),
                Title = message.Title,
                Body = message.Body,
                AnalysisScope = message.Role == AssistantMessageRole.User
                    ? FromUserMessage(message)
                    : null
            };

        public static AssistantConversationDto ToConversationDto(
            AssistantConversation conversation
        )
            => new()
            {
                Id = conversation.Id,
                Title = conversation.Title,
                AnalysisScope = FromConversation(conversation),
                LastActivityAt = conversation.LastActivityAt,
                Messages = conversation.Messages
                    .OrderBy(message => message.CreatedAt)
                    .ThenBy(message => message.Id)
                    .Select(ToMessageDto)
                    .ToList()
            };

        private static AssistantReportingPeriodDto ToPeriodDto(
            string kind,
            string? presetId,
            string? startDate,
            string? endDate
        )
        {
            if (kind == "custom")
            {
                return new AssistantReportingPeriodDto
                {
                    Kind = "custom",
                    StartDate = startDate,
                    EndDate = endDate
                };
            }

            return new AssistantReportingPeriodDto
            {
                Kind = "preset",
                PresetId = NormalizePreset(presetId)
            };
        }

        private static string NormalizeKind(string? kind)
            => string.Equals(kind, "custom", StringComparison.OrdinalIgnoreCase)
                ? "custom"
                : "preset";

        private static string NormalizePreset(string? presetId)
            => presetId switch
            {
                "last30" => "last30",
                "thisMonth" => "thisMonth",
                _ => "last7"
            };

        private static string FormatCustomPeriod(string? startDate, string? endDate)
        {
            if (!DateTime.TryParseExact(
                    startDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var start
                )
                || !DateTime.TryParseExact(
                    endDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var end
                ))
            {
                return "the selected dates";
            }

            if (startDate == endDate)
            {
                return start.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            }

            return $"{start.Day}–{end.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
        }
    }
}
