namespace TummlyBackend.Exceptions
{
    public class DemoSessionDisabledException : Exception
    {
        public DemoSessionDisabledException()
            : base("Product demo session is not available.")
        {
        }
    }
}
