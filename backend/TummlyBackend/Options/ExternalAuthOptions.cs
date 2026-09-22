namespace TummlyBackend.Configurations
{
    public class ExternalAuthOptions
    {
        public ExternalAuthProviderOptions Google { get; set; } = new();

        public ExternalAuthProviderOptions Microsoft { get; set; } = new();
    }

    public class ExternalAuthProviderOptions
    {
        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;
    }
}
