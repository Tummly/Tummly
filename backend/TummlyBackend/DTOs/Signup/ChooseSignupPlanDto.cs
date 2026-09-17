namespace TummlyBackend.DTOs.Signup
{
    public class ChooseSignupPlanDto
    {
        public string PlanId { get; set; } = string.Empty;

        public string Cadence { get; set; } = "monthly";
    }
}
