namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// HMAC-SHA256 hex hasher for guest contact keys (WS2a suppression).
    /// </summary>
    public interface IGuestContactHasher
    {
        string Hash(string contactKey);
    }
}
