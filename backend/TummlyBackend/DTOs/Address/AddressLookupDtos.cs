namespace TummlyBackend.DTOs.Address
{
    public class AddressSuggestionDto
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Postcode { get; set; } = string.Empty;
    }

    public class AddressPremiseDto
    {
        public string Address { get; set; } = string.Empty;

        public string Postcode { get; set; } = string.Empty;
    }

    public class AddressResolveResultDto
    {
        public string Postcode { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public List<AddressPremiseDto> Premises { get; set; } = new();

        public bool MultiplePremises { get; set; }

        public bool UsedBestMatch { get; set; }
    }
}
