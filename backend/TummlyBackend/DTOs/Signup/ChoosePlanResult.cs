namespace TummlyBackend.DTOs.Signup
{
    public class ChoosePlanResult
    {
        /// <summary>provisioned | checkout (Task 9)</summary>
        public string Mode { get; set; } = string.Empty;

        public string? CheckoutUrl { get; set; }
    }
}
