namespace TummlyBackend.DTOs.Auth
{
    public class SessionRoutingFields
    {
        public string AccountType { get; set; } = string.Empty;

        public bool WorkspaceSetupRequired { get; set; }

        public int? SelectedLocationId { get; set; }

        public bool ActivationRequired { get; set; }

        public DateTime? ActivationExpiresAt { get; set; }
    }
}
