namespace TummlyBackend.Models
{
    /// <summary>
    /// Lifecycle of a <see cref="PrintReadyQrAsset"/> PDF for Admin fulfilment.
    /// </summary>
    public enum PrintReadyQrAssetStatus
    {
        Preparing = 0,

        Ready = 1,

        Failed = 2,
    }
}
