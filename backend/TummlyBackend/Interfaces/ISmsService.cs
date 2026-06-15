namespace TummlyBackend.Interfaces
{
    public interface ISmsService
    {
        Task SendOtpSmsAsync(
            string phoneNumber,
            string otp
        );
    }
}
