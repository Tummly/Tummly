namespace TummlyBackend.Exceptions
{
    public class DemoSessionUnavailableException : Exception
    {
        public DemoSessionUnavailableException()
            : base(
                "Product demo tenant is not configured. Seed the DemoSession owner restaurant first."
            )
        {
        }
    }
}
