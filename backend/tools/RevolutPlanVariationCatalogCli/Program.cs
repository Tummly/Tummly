using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TummlyBackend.Billing.Revolut;
using TummlyBackend.Configurations;

/// <summary>
/// Ops CLI: dry-run or create the eight recurring Revolut plan variations from
/// the pack pricebook (ticket 13). Create only — never PATCH amounts.
/// </summary>
static class Program
{
    static async Task<int> Main(string[] args)
    {
        var apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
        var packPath = GetArg(args, "--pack") ?? DefaultPackPath();
        var outPath = GetArg(args, "--out");
        var apiBase =
            GetArg(args, "--api-base")
            ?? Environment.GetEnvironmentVariable("REVOLUT_API_BASE_URL")
            ?? RevolutSettings.SandboxApiBaseUrl;
        var apiVersion =
            GetArg(args, "--api-version")
            ?? Environment.GetEnvironmentVariable("REVOLUT_API_VERSION")
            ?? RevolutSettings.DefaultApiVersion;
        var secret =
            GetArg(args, "--secret")
            ?? Environment.GetEnvironmentVariable("REVOLUT_SECRET_KEY");

        if (!File.Exists(packPath))
        {
            Console.Error.WriteLine($"Pack JSON not found: {packPath}");
            return 1;
        }

        var json = await File.ReadAllTextAsync(packPath);
        var rows = RevolutPlanVariationCatalog.BuildFromPackJson(json);
        var bodies = RevolutPlanVariationCatalog.ToCreatePlanBodies(rows);

        Console.WriteLine(
            $"# Revolut plan variations from {Path.GetFileName(packPath)}"
        );
        Console.WriteLine("# Create only. Never PATCH a live variation amount.");
        Console.WriteLine();
        foreach (var row in rows)
        {
            Console.WriteLine(
                $"# {row.LookupKey}  net={row.NetPence}  gross={row.GrossMinor}  {row.CycleDuration}"
            );
        }

        Console.WriteLine();
        foreach (var body in bodies)
        {
            Console.WriteLine($"# POST /api/subscription-plans name={body.Name}");
            Console.WriteLine(
                RevolutPlanVariationCatalog.ToCreatePlanRequestJson(body)
            );
            Console.WriteLine();
        }

        if (!apply)
        {
            Console.WriteLine(
                "# Dry-run only. Pass --apply with REVOLUT_SECRET_KEY to create."
            );
            return 0;
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            Console.Error.WriteLine(
                "REVOLUT_SECRET_KEY (or --secret) is required for --apply."
            );
            return 1;
        }

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        using var http = new HttpClient
        {
            BaseAddress = new Uri(apiBase.TrimEnd('/') + "/"),
        };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            secret.Trim()
        );
        http.DefaultRequestHeaders.TryAddWithoutValidation(
            "Revolut-Api-Version",
            apiVersion.Trim()
        );

        foreach (var body in bodies)
        {
            var planRows = rows.Where(r => r.PlanKey == body.Name).ToList();
            var payload = RevolutPlanVariationCatalog.ToCreatePlanRequestJson(
                body
            );
            using var content = new StringContent(
                payload,
                Encoding.UTF8,
                "application/json"
            );
            using var response = await http.PostAsync(
                "api/subscription-plans",
                content
            );
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine(
                    $"Create failed for plan '{body.Name}': {(int)response.StatusCode} {raw}"
                );
                return 1;
            }

            using var doc = JsonDocument.Parse(raw);
            foreach (
                var pair in RevolutPlanVariationCatalog.MapCreateResponse(
                    planRows,
                    doc.RootElement
                )
            )
            {
                map[pair.Key] = pair.Value;
            }

            Console.WriteLine($"# Created plan '{body.Name}'");
        }

        var lines = RevolutPlanVariationCatalog.FormatEnvMapLines(map);
        Console.WriteLine();
        Console.WriteLine("# Mount these in the target environment (.env / ACA):");
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }

        if (!string.IsNullOrWhiteSpace(outPath))
        {
            await File.WriteAllLinesAsync(outPath, lines);
            Console.WriteLine($"# Wrote {outPath}");
        }

        return 0;
    }

    static string? GetArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    static string DefaultPackPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "docs",
                "product",
                "billing-pack-v3.0",
                "tummly_uk_billing_config_v3.0.json"
            );
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate tummly_uk_billing_config_v3.0.json"
        );
    }
}
