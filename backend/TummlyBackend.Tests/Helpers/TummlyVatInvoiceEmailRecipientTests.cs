using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class TummlyVatInvoiceEmailRecipientTests
    {
        [Fact]
        public void Resolve_PrefersBillingEmail_WhenSet()
        {
            var to = TummlyVatInvoiceEmailRecipient.Resolve(
                " invoices@venue.test ",
                "owner@venue.test"
            );

            Assert.Equal("invoices@venue.test", to);
        }

        [Fact]
        public void Resolve_FallsBackToBillingContact_WhenBillingEmailEmpty()
        {
            var to = TummlyVatInvoiceEmailRecipient.Resolve(
                "  ",
                " billing@venue.test "
            );

            Assert.Equal("billing@venue.test", to);
        }

        [Fact]
        public void Resolve_ReturnsNull_WhenBothEmpty()
        {
            Assert.Null(TummlyVatInvoiceEmailRecipient.Resolve(null, null));
            Assert.Null(TummlyVatInvoiceEmailRecipient.Resolve("", "  "));
        }
    }
}
