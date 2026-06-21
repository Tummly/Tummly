namespace TummlyBackend.Exceptions
{
    public class LinkTokenGenerationException : Exception
    {
        public LinkTokenGenerationException()
            : base("Unable to generate a unique link token.")
        {
        }

        public LinkTokenGenerationException(string message)
            : base(message)
        {
        }
    }
}
