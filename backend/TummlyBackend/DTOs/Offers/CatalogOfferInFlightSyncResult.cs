namespace TummlyBackend.DTOs.Offers
{
    public abstract class CatalogOfferInFlightSyncResult
    {
        private CatalogOfferInFlightSyncResult()
        {
        }

        public sealed class Ok : CatalogOfferInFlightSyncResult
        {
        }

        public sealed class CapReached : CatalogOfferInFlightSyncResult
        {
            public required int Cap { get; init; }

            public required int Current { get; init; }
        }

        public sealed class FailClosed : CatalogOfferInFlightSyncResult
        {
        }
    }
}
