namespace TummlyBackend.DTOs.PublicUnsubscribe
{
    public sealed class UnsubscribeConfirmRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public sealed class UnsubscribeFormRequest
    {
        public string Email { get; set; } = string.Empty;

        public int RestaurantId { get; set; }
    }
}
