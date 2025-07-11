using ResilientEmailService.Models;
using System.Net;
using System.Net.Mail;

namespace ResilientEmailService.Services.Email
{
    // Services/Email/SmtpEmailProvider.cs
    public class SmtpEmailProvider : IEmailProvider
    {
        public string ProviderName => "SMTP Provider";

        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailProvider> _logger;

        public SmtpEmailProvider(IConfiguration configuration, ILogger<SmtpEmailProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<EmailResult> SendEmailAsync(EmailRequest request)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");

                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"]);
                var enableSsl = bool.Parse(emailSettings["EnableSsl"]);
                var smtpUsername = emailSettings["SmtpUsername"];
                var smtpPassword = emailSettings["SmtpPassword"];
                var fromAddress = emailSettings["FromAddress"];
                var fromName = emailSettings["FromName"];

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };

                var from = new MailAddress(fromAddress, fromName);
                var to = new MailAddress(request.To);

                using var message = new MailMessage(from, to)
                {
                    Subject = request.Subject,
                    Body = request.Body,
                    IsBodyHtml = false
                };

                await client.SendMailAsync(message);

                return new EmailResult
                {
                    Success = true,
                    Provider = ProviderName,
                    Message = "Email sent successfully",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email via SMTP");
                return new EmailResult
                {
                    Success = false,
                    Provider = ProviderName,
                    Message = $"Failed to send email: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }
}

