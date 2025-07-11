using ResilientEmailService.Models;

namespace ResilientEmailService.Services.Email
{
    public interface IEmailProvider
    {
        string ProviderName { get; }
        Task<EmailResult> SendEmailAsync(EmailRequest request);
    }
}
