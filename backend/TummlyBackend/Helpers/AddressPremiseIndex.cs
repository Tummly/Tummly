using TummlyBackend.DTOs.Address;

namespace TummlyBackend.Helpers
{
    public static class AddressPremiseIndex
    {
        public static string BuildPremiseKey(string normalizedPostcode, string normalizedAddress)
        {
            return $"address_premise:{normalizedPostcode}|{normalizedAddress}";
        }

        public static string BuildIndexKey(string normalizedPostcode)
        {
            return $"address_premise_index:{normalizedPostcode}";
        }

        public static string NormalizeAddressKey(string address)
        {
            return AddressFormatting.NormalizeForComparison(address);
        }

        public static AddressResolveResultDto? TryResolveFromIndex(
            IReadOnlyDictionary<string, AddressPremiseDto> premisesByAddressKey,
            string displayPostcode,
            string addressHint
        )
        {
            if (premisesByAddressKey.Count == 0 ||
                string.IsNullOrWhiteSpace(addressHint))
            {
                return null;
            }

            var premises = premisesByAddressKey.Values.ToList();
            var bestAddress = AddressFormatting.PickBestMatch(
                premises.Select(premise => premise.Address),
                addressHint
            );

            if (string.IsNullOrWhiteSpace(bestAddress) ||
                !AddressFormatting.StreetLinesOverlap(bestAddress, addressHint))
            {
                return null;
            }

            return new AddressResolveResultDto
            {
                Postcode = displayPostcode,
                Address = bestAddress,
                Premises = premises,
                MultiplePremises = premises.Count > 1,
                UsedBestMatch = premises.Count > 1 ||
                    !string.Equals(
                        bestAddress,
                        premises[0].Address,
                        StringComparison.OrdinalIgnoreCase
                    ),
            };
        }
    }
}
