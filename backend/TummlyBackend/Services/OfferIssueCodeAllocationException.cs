namespace TummlyBackend.Services
{
    /// <summary>
    /// Thrown when unique Offer Claim code allocation exhausts bounded retries.
    /// </summary>
    public class OfferIssueCodeAllocationException : Exception
    {
        public OfferIssueCodeAllocationException()
            : base("Could not allocate a unique Offer Claim code.")
        {
        }
    }
}
