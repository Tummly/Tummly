using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Tests.Services
{
    public sealed class PrintReadyQrMaterialsWorkTests
    {
        [Fact]
        public async Task DrainAsync_UsesFreshScopes_AndContinuesAfterEnsureFailure()
        {
            var state = new FakeMaterialsState(failingLocationId: 11);
            var shopOrderId = Guid.NewGuid();
            var services = new ServiceCollection();
            services.AddSingleton(state);
            services.AddScoped<
                IPrintReadyQrMaterialsService,
                ControlledPrintReadyQrMaterialsService
            >();
            await using var provider = services.BuildServiceProvider();
            var work = new PrintReadyQrMaterialsWork(
                provider.GetRequiredService<IServiceScopeFactory>(),
                new TestHostEnvironment("Testing"),
                NullLogger<PrintReadyQrMaterialsWork>.Instance
            );

            await work.RequestEnsureAsync(11);
            await work.RequestShopOrderEnsureAsync(shopOrderId);
            await work.RequestEnsureAsync(22);
            await work.DrainAsync();

            Assert.Equal(new[] { 11, 22 }, state.LocationIds);
            Assert.Equal(new[] { shopOrderId }, state.ShopOrderIds);
            Assert.Equal(3, state.CreatedInstances);
        }

        [Fact]
        public async Task RunAsync_StopsWhileIdle_WhenHostCancels()
        {
            var services = new ServiceCollection();
            await using var provider = services.BuildServiceProvider();
            var work = new PrintReadyQrMaterialsWork(
                provider.GetRequiredService<IServiceScopeFactory>(),
                new TestHostEnvironment("Production"),
                NullLogger<PrintReadyQrMaterialsWork>.Instance
            );
            using var cancellation = new CancellationTokenSource(
                TimeSpan.FromMilliseconds(50)
            );

            await work
                .RunAsync(cancellation.Token)
                .WaitAsync(TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task RunAsync_RecoversMissingStarterRequestAfterRestart()
        {
            var databaseName = Guid.NewGuid().ToString();
            using var cancellation = new CancellationTokenSource(
                TimeSpan.FromSeconds(5)
            );
            var state = new FakeMaterialsState(failingLocationId: -1)
            {
                OnEnsure = cancellation.Cancel,
            };
            var services = new ServiceCollection();
            services.AddSingleton(state);
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName)
            );
            services.AddSingleton<IMaterialsCatalog>(
                MaterialsCatalog.LoadFromContentRoot(AppContext.BaseDirectory)
            );
            services.AddScoped<
                IPrintReadyQrMaterialsService,
                ControlledPrintReadyQrMaterialsService
            >();
            await using var provider = services.BuildServiceProvider();
            await using (var scope = provider.CreateAsyncScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var owner = new User
                {
                    FullName = "Restart owner",
                    Email = $"restart-{Guid.NewGuid():N}@example.com",
                    PasswordHash = "hash",
                    Role = "Owner",
                };
                var restaurant = new Restaurant
                {
                    Name = "Restart venue",
                    OwnerUser = owner,
                };
                var location = new RestaurantLocation
                {
                    Restaurant = restaurant,
                    LocationName = "Main",
                    Address = "1 High Street",
                };
                context.QrCodes.Add(new QrCode
                {
                    RestaurantLocation = location,
                    QrType = QrType.TableTent,
                    Token = "restart-recovery-token",
                    Status = QrCodeStatus.Active,
                });
                await context.SaveChangesAsync();
            }

            var work = new PrintReadyQrMaterialsWork(
                provider.GetRequiredService<IServiceScopeFactory>(),
                new TestHostEnvironment("Production"),
                NullLogger<PrintReadyQrMaterialsWork>.Instance
            );

            await work
                .RunAsync(cancellation.Token)
                .WaitAsync(TimeSpan.FromSeconds(6));

            Assert.Single(state.LocationIds);
        }

        private sealed class FakeMaterialsState
        {
            public FakeMaterialsState(int failingLocationId)
            {
                FailingLocationId = failingLocationId;
            }

            public int FailingLocationId { get; }

            public int CreatedInstances { get; set; }

            public List<int> LocationIds { get; } = [];

            public List<Guid> ShopOrderIds { get; } = [];

            public Action? OnEnsure { get; init; }
        }

        private sealed class ControlledPrintReadyQrMaterialsService
            : IPrintReadyQrMaterialsService
        {
            private readonly FakeMaterialsState _state;

            public ControlledPrintReadyQrMaterialsService(
                FakeMaterialsState state
            )
            {
                _state = state;
                _state.CreatedInstances++;
            }

            public Task EnsureStarterMaterialsAsync(
                int locationId,
                CancellationToken cancellationToken = default
            )
            {
                _state.LocationIds.Add(locationId);
                if (locationId == _state.FailingLocationId)
                {
                    throw new InvalidOperationException(
                        "Controlled print failure."
                    );
                }

                _state.OnEnsure?.Invoke();
                return Task.CompletedTask;
            }

            public Task EnsureShopOrderMaterialsAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            )
            {
                _state.ShopOrderIds.Add(shopOrderId);
                return Task.CompletedTask;
            }

            public Task<bool> TryInvalidateAfterQrRotationAsync(
                int locationId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task<
                IReadOnlyList<PrintMaterialsLocationReadinessDto>
            > ListReadinessAsync(
                int operatorUserId,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task<
                IReadOnlyList<ShopPrintAssetReadinessDto>
            > ListShopOrderReadinessAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task EnsureAllStarterMaterialsForOperatorAsync(
                int operatorUserId,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task<PrintReadyQrDownload?> DownloadAsync(
                int operatorUserId,
                int locationId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task<PrintReadyQrDownload?> DownloadShopOrderAsync(
                Guid shopOrderId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task<PrintMaterialsAssetReadinessDto?> RetryAsync(
                int operatorUserId,
                int locationId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task<ShopPrintAssetReadinessDto?> RetryShopOrderAsync(
                Guid shopOrderId,
                QrType qrType,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public TestHostEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "Tests";

            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

            public IFileProvider ContentRootFileProvider { get; set; }
                = new NullFileProvider();
        }
    }
}
