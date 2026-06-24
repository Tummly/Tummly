namespace TummlyBackend.Interfaces
{
    public interface ISmsService
    {
        Task SendOtpSmsAsync(string phoneNumber);

        Task<bool> VerifyOtpSmsAsync(
            string phoneNumber,
            string otp
        );
    }
}
