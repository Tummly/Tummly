using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

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
