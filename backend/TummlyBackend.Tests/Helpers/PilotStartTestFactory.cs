using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Builds the real <see cref="PilotStartService"/> for tests, with a real
    /// <see cref="CaptureQrLifecycleService"/> underneath. The print-ready QR
    /// service is passed as null: <c>ActivatePlanPausedCodesAsync</c> never
    /// calls it.
    /// </summary>
    public static class PilotStartTestFactory
    {
        public static PilotStartService Create(
            ApplicationDbContext context,
            IPricebookCatalog pricebook,
            ICreditLedger creditLedger,
            IComplimentaryStarterShopOrderService complimentaryStarterShopOrders,
            IPrintReadyQrMaterialsWork printReadyQrMaterialsWork,
            IEmailService emailService
        )
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://tummly.example",
                    }
                )
                .Build();
            var smartGuestLink = new SmartGuestLinkService(
                context,
                configuration,
                new NoOpBillingAccountLifecycle()
            );
            var captureQrLifecycle = new CaptureQrLifecycleService(
                context,
                smartGuestLink,
                pricebook,
                null!
            );

            return new PilotStartService(
                context,
                creditLedger,
                complimentaryStarterShopOrders,
                printReadyQrMaterialsWork,
                captureQrLifecycle,
                emailService,
                NullLogger<PilotStartService>.Instance
            );
        }
    }
}
