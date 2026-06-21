namespace TummlyBackend.Exceptions
{
    public class InviteTokenNotApprovedException : Exception
    {
        public InviteTokenNotApprovedException()
            : base("Request not approved yet.")
        {
        }
    }
}
