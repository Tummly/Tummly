namespace TummlyBackend.Exceptions
{
    public class AccountAlreadyCreatedException : Exception
    {
        public AccountAlreadyCreatedException()
            : base("Account already created.")
        {
        }

        public AccountAlreadyCreatedException(string message)
            : base(message)
        {
        }
    }
}
