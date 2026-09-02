namespace TummlyBackend.DTOs.Shop
{
    public class SaveShopLocationDetailsRequest
    {
        public int TableCount { get; set; }

        public int CounterCount { get; set; }

        public int EntranceCount { get; set; }

        public int SecondaryEntranceCount { get; set; }

        public string TakeawayVolume { get; set; } = "not-sure";

        public string PromptLocations { get; set; } = string.Empty;

        public string ExistingMaterials { get; set; } = "no";
    }

    public class ShopLocationDetailsBasisDto
    {
        public int TableCount { get; set; }

        public int CounterCount { get; set; }

        public int EntranceCount { get; set; }

        public int SecondaryEntranceCount { get; set; }

        public string TakeawayVolume { get; set; } = "not-sure";

        public IReadOnlyList<string> PromptLocations { get; set; } =
            Array.Empty<string>();

        public string ExistingMaterials { get; set; } = "no";
    }

    public class ShopRecommendationsWindowDto
    {
        public string From { get; set; } = string.Empty;

        public string To { get; set; } = string.Empty;
    }

    public class ShopRecommendationLineDto
    {
        public string SkuId { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Title { get; set; } = string.Empty;

        public int UnitNetPence { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public string AllocationText { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
    }

    public class ShopRecommendationsSummaryDto
    {
        public int MaterialTypeCount { get; set; }

        public int TotalPieces { get; set; }

        public int MaterialsNetPence { get; set; }

        public string Currency { get; set; } = "GBP";
    }

    public class ShopLocationRecommendationsDto
    {
        public int LocationId { get; set; }

        public bool NeedsLocationDetails { get; set; }

        public ShopRecommendationsWindowDto Window { get; set; } = new();

        public ShopLocationDetailsBasisDto? BasedOn { get; set; }

        public IReadOnlyList<ShopRecommendationLineDto> Lines { get; set; } =
            Array.Empty<ShopRecommendationLineDto>();

        public ShopRecommendationsSummaryDto Summary { get; set; } = new();
    }
}
