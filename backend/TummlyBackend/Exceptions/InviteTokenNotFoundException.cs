namespace TummlyBackend.Exceptions
{
    public class InviteTokenNotFoundException : Exception
    {
        public InviteTokenNotFoundException()
            : base("Invalid invite token.")
        {
        }

        public InviteTokenNotFoundException(string message)
            : base(message)
        {
        }
    }
}
