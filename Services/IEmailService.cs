namespace YousefZuaianatAPI.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}