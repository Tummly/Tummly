using Microsoft.AspNetCore.Mvc;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared format + location-id parsing for Reports / Offers / consent exports.
    /// </summary>
    public static class ReportsExportRequestGate
    {
        public static IActionResult? TryParseFormat(
            ControllerBase controller,
            string? format,
            string[] allowed,
            string defaultFormat,
            out string normalized
        )
        {
            normalized = (format ?? defaultFormat).Trim().ToLowerInvariant();
            if (normalized.Length == 0)
            {
                normalized = defaultFormat;
            }

            if (!allowed.Contains(normalized, StringComparer.Ordinal))
            {
                return controller.BadRequest(new
                {
                    success = false,
                    message =
                        $"format must be one of: {string.Join(", ", allowed)}.",
                });
            }

            return null;
        }

        /// <summary>
        /// Prefer repeated/comma <c>locationIds</c>; else single <c>locationId</c>.
        /// </summary>
        public static IActionResult? TryResolveLocationIds(
            ControllerBase controller,
            int? locationId,
            int[]? locationIds,
            string? locationIdsCsv,
            out IReadOnlyList<int> resolved
        )
        {
            resolved = Array.Empty<int>();
            var collected = new List<int>();

            if (locationIds != null)
            {
                foreach (var id in locationIds)
                {
                    if (id > 0)
                    {
                        collected.Add(id);
                    }
                }
            }

            if (
                collected.Count == 0
                && !string.IsNullOrWhiteSpace(locationIdsCsv)
            )
            {
                foreach (
                    var part in locationIdsCsv.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries
                            | StringSplitOptions.TrimEntries
                    )
                )
                {
                    if (
                        int.TryParse(part, out var id)
                        && id > 0
                    )
                    {
                        collected.Add(id);
                    }
                }
            }

            if (collected.Count == 0 && locationId is > 0)
            {
                collected.Add(locationId.Value);
            }

            if (collected.Count == 0)
            {
                return controller.BadRequest(new
                {
                    success = false,
                    message = "locationId is required.",
                });
            }

            resolved = collected.Distinct().ToList();
            return null;
        }
    }
}
