using System.Text.RegularExpressions;

namespace TummlyBackend.Tests.Helpers
{
    public class LogTemplateContactGuardTests
    {
        private static readonly string[] ForbiddenPlaceholders =
        [
            "{Email}",
            "{ToEmail}",
            "{Phone}",
            "{PhoneNumber}",
            "{Mobile}",
            "{To}",
            "{Recipient}",
            "{GuestContact}",
            "{Contact}",
        ];

        // Non-interpolated C# string literal (ordinary or verbatim). Not preceded by $.
        private static readonly Regex StringLiteral = new(
            @"(?<!\$)(?:@""(?:[^""]|"""")*""|""(?:\\.|[^""\\])*"")",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        [Fact]
        public void BackendSource_HasNoForbiddenContactLogPlaceholders()
        {
            var sourceDir = ResolveBackendSourceDir();
            var violations = new List<string>();

            foreach (
                var path in Directory.EnumerateFiles(
                    sourceDir,
                    "*.cs",
                    SearchOption.AllDirectories
                )
            )
            {
                if (path.Contains(
                        $"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal
                    )
                    || path.EndsWith(
                        $"{Path.DirectorySeparatorChar}Migrations",
                        StringComparison.Ordinal
                    ))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(sourceDir, path);
                if (relative.StartsWith(
                        $"Migrations{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal
                    ))
                {
                    continue;
                }

                var lines = File.ReadAllLines(path);
                for (var i = 0; i < lines.Length; i++)
                {
                    foreach (Match match in StringLiteral.Matches(lines[i]))
                    {
                        var literal = match.Value;
                        foreach (var placeholder in ForbiddenPlaceholders)
                        {
                            if (literal.Contains(
                                    placeholder,
                                    StringComparison.Ordinal
                                ))
                            {
                                violations.Add(
                                    $"{relative}:{i + 1}: contains {placeholder}"
                                );
                            }
                        }
                    }
                }
            }

            Assert.True(
                violations.Count == 0,
                "Forbidden contact placeholders in non-interpolated string literals:\n"
                    + string.Join("\n", violations)
            );
        }

        private static string ResolveBackendSourceDir()
        {
            var candidates = new List<string>();

            var assemblyDir = AppContext.BaseDirectory;
            candidates.Add(
                Path.GetFullPath(
                    Path.Combine(
                        assemblyDir,
                        "..",
                        "..",
                        "..",
                        "..",
                        "TummlyBackend"
                    )
                )
            );

            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                candidates.Add(
                    Path.Combine(dir.FullName, "backend", "TummlyBackend")
                );
                candidates.Add(Path.Combine(dir.FullName, "TummlyBackend"));
                dir = dir.Parent;
            }

            foreach (var candidate in candidates.Distinct(
                         StringComparer.Ordinal
                     ))
            {
                if (Directory.Exists(candidate)
                    && File.Exists(
                        Path.Combine(candidate, "TummlyBackend.csproj")
                    ))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                "Could not locate backend/TummlyBackend source directory. "
                    + "Tried: "
                    + string.Join(", ", candidates.Distinct())
            );
        }
    }
}
