using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Infrastructure;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace ProductDemoSeed;

internal static class BrandLogoSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, Restaurant restaurant)
    {
        restaurant.BrandLogoObjectKey = DemoSeedConstants.BrandLogoObjectKey;
        restaurant.BrandLogoContentType = DemoSeedConstants.BrandLogoContentType;

        var location = await db.RestaurantLocations
            .Where(l => l.RestaurantId == restaurant.Id)
            .OrderBy(l => l.Id)
            .FirstOrDefaultAsync();
        if (location != null)
        {
            location.LogoUploadedAt = DemoSeedConstants.SeedNowUtc.AddDays(-30);
        }

        await db.SaveChangesAsync();

        var logoPath = ResolveLogoPath();
        if (logoPath is null)
        {
            Console.WriteLine(
                "WARN: mavero-kitchen-logo.jpg not found; BrandLogoObjectKey set but bytes not uploaded."
            );
            return;
        }

        await using var stream = File.OpenRead(logoPath);
        var uploaded = await TryUploadAsync(stream, stream.Length);
        if (!uploaded)
        {
            Console.WriteLine(
                "WARN: Object storage not configured; logo key set on restaurant but file not uploaded. "
                    + "Set ObjectStorage__* in backend/TummlyBackend/.env and re-run seed to serve the mark."
            );
        }
        else
        {
            Console.WriteLine($"Brand logo uploaded → {DemoSeedConstants.BrandLogoObjectKey}");
        }
    }

    private static string? ResolveLogoPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "mavero-kitchen-logo.jpg"),
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "backend",
                "tools",
                "ProductDemoSeed",
                "Assets",
                "mavero-kitchen-logo.jpg"
            ),
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "mavero-kitchen-logo.jpg"
            ),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private static async Task<bool> TryUploadAsync(Stream content, long length)
    {
        try
        {
            // Match run-local-api.sh: load ObjectStorage__* from backend .env
            // (process env alone is empty when seed is run from the CLI).
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(LoadObjectStorageFromBackendEnv())
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddQueryAttachmentStorage(config);
            await using var sp = services.BuildServiceProvider();
            var storage = sp.GetRequiredService<IQueryAttachmentStorage>();
            if (!storage.IsConfigured)
            {
                return false;
            }

            content.Position = 0;
            await storage.UploadAsync(
                DemoSeedConstants.BrandLogoObjectKey,
                content,
                DemoSeedConstants.BrandLogoContentType,
                length
            );
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARN: Brand logo upload failed: {ex.Message}");
            return false;
        }
    }

    private static Dictionary<string, string?> LoadObjectStorageFromBackendEnv()
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in BackendEnvPaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim().TrimStart('\uFEFF');
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                var key = line[..eq].Trim();
                if (
                    !key.StartsWith("ObjectStorage__", StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                var value = line[(eq + 1)..].Trim().Trim('\'').Trim('"');
                // Configuration binds ObjectStorage:Provider from ObjectStorage__Provider.
                var configKey = key.Replace("__", ":", StringComparison.Ordinal);
                map[configKey] = value;
            }

            if (map.Count > 0)
            {
                break;
            }
        }

        return map;
    }

    private static IEnumerable<string> BackendEnvPaths() =>
        [
            Path.GetFullPath(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "backend",
                    "TummlyBackend",
                    ".env"
                )
            ),
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "TummlyBackend",
                    ".env"
                )
            ),
        ];
}
