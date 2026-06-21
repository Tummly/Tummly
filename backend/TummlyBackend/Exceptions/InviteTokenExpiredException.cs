namespace TummlyBackend.Exceptions
{
    public class InviteTokenExpiredException : Exception
    {
        public InviteTokenExpiredException()
            : base("Invite link expired.")
        {
        }
    }
}
