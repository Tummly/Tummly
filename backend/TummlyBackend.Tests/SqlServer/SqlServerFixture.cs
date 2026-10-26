using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.SqlServer
{
    public sealed class SqlServerFixture : IAsyncLifetime
    {
        private const string DatabaseName = "tummly_ws2a";
        private const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

        private MsSqlContainer? _container;

        public bool IsAvailable { get; private set; }

        public string? SkipReason { get; private set; }

        public string ConnectionString { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            MsSqlContainer container;
            try
            {
                container = new MsSqlBuilder(Image).Build();
                await container.StartAsync();
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                SkipReason = $"Docker not available: {ex.GetType().Name}";
                return;
            }

            _container = container;

            var masterBuilder = new SqlConnectionStringBuilder(
                container.GetConnectionString()
            )
            {
                InitialCatalog = "master",
            };

            await using (
                var connection = new SqlConnection(masterBuilder.ConnectionString)
            )
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText =
                    $"IF DB_ID(N'{DatabaseName}') IS NULL CREATE DATABASE [{DatabaseName}]";
                await command.ExecuteNonQueryAsync();
            }

            var appBuilder = new SqlConnectionStringBuilder(
                container.GetConnectionString()
            )
            {
                InitialCatalog = DatabaseName,
            };
            ConnectionString = appBuilder.ConnectionString;

            await using (var context = CreateContext())
            {
                await context.Database.MigrateAsync();
            }

            IsAvailable = true;
        }

        public async Task DisposeAsync()
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }

        public ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;
            return new ApplicationDbContext(options);
        }

        public WebApplicationFactory<Program> CreateFactory()
        {
            return new TummlyWebApplicationFactory().WithWebHostBuilder(
                builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var descriptors = services
                            .Where(d =>
                                d.ServiceType
                                    == typeof(
                                        DbContextOptions<ApplicationDbContext>
                                    )
                                || d.ServiceType == typeof(ApplicationDbContext)
                            )
                            .ToList();

                        foreach (var descriptor in descriptors)
                        {
                            services.Remove(descriptor);
                        }

                        services.AddDbContext<ApplicationDbContext>(options =>
                        {
                            options.UseSqlServer(ConnectionString);
                        });

                        // Concurrency tests use the real SQL Server guest reply lock.
                        services.RemoveAll<IGuestReplyLock>();
                        services.AddScoped<IGuestReplyLock, GuestReplyLock>();
                    });
                }
            );
        }
    }
}
