namespace TummlyBackend.Helpers
{
    public static class OperatorAreaLabels
    {
        public static string For(string areaId)
        {
            return areaId switch
            {
                OperatorAreaIds.AccountWorkspace => "Workspace & account",
                OperatorAreaIds.Locations => "Locations",
                OperatorAreaIds.TeamPermissions => "Team & permissions",
                OperatorAreaIds.Capture => "Capture & Guest Forms",
                OperatorAreaIds.Feedback => "Feedback & recovery",
                OperatorAreaIds.Guests => "Guests",
                OperatorAreaIds.Campaigns => "Campaigns",
                OperatorAreaIds.Offers => "Offers & Redemption",
                OperatorAreaIds.Reports => "Reports & Weekly Brief",
                OperatorAreaIds.TummlyShop => "Shop & QR materials",
                OperatorAreaIds.BillingCredits => "Billing & credits",
                OperatorAreaIds.PrivacyConsent => "Privacy & access activity",
                OperatorAreaIds.AiAssistant => "AI Assistant",
                _ => areaId,
            };
        }
    }
}
