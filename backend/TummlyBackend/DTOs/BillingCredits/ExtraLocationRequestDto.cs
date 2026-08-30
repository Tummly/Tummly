namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class ExtraLocationRequestDto
    {
        /// <summary>
        /// <c>add</c> or <c>remove</c>.
        /// </summary>
        public string Action { get; set; } = string.Empty;
    }
}
