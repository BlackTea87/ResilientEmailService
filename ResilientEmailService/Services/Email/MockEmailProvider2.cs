using ResilientEmailService.Models;

namespace ResilientEmailService.Services.Email
{
    public class MockEmailProvider2 : IEmailProvider
    {
        public string ProviderName => "Mock Provider 2";

        private readonly Random _random = new Random();

        public async Task<EmailResult> SendEmailAsync(EmailRequest request)
        {
            // Simulate network delay
            await Task.Delay(_random.Next(100, 500));

            // Simulate 30% failure rate
            if (_random.Next(0, 3) == 0)
            {
                return new EmailResult
                {
                    Success = false,
                    Provider = ProviderName,
                    Message = "Provider 2 failed randomly",
                    Timestamp = DateTime.UtcNow
                };
            }

            return new EmailResult
            {
                Success = true,
                Provider = ProviderName,
                Message = "Email sent successfully via Provider 2",
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
