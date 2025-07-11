using ResilientEmailService.Models;
using ResilientEmailService.Services.CircuitBreakers;

namespace ResilientEmailService.Services.Email
{
    // Services/Email/EmailService.cs
    public class EmailService
    {
        private readonly IEmailProvider[] _providers;
        private readonly Dictionary<string, EmailStatus> _statusTracker = new();
        private readonly Dictionary<string, CircuitBreaker> _circuitBreakers = new();
        private readonly ILogger<EmailService> _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _rateLimitDelay;
        private DateTime _lastSendTime = DateTime.MinValue;

        public EmailService(
            IEnumerable<IEmailProvider> providers,
            ILogger<EmailService> logger,
            int maxRetries = 3,
            TimeSpan? rateLimitDelay = null)
        {
            _providers = providers.ToArray();
            _logger = logger;
            _maxRetries = maxRetries;
            _rateLimitDelay = rateLimitDelay ?? TimeSpan.FromSeconds(1);

            foreach (var provider in _providers)
            {
                _circuitBreakers[provider.ProviderName] = new CircuitBreaker(3, TimeSpan.FromMinutes(5));
            }
        }

        public async Task<EmailResult> SendEmailAsync(EmailRequest request)
        {
            // Check idempotency
            if (!string.IsNullOrEmpty(request.IdempotencyKey) &&
                _statusTracker.TryGetValue(request.IdempotencyKey, out var status))
            {
                if (status.LastResult.Success)
                {
                    return status.LastResult;
                }

                if (status.NextRetryTime.HasValue && status.NextRetryTime > DateTime.UtcNow)
                {
                    return new EmailResult
                    {
                        Success = false,
                        Message = $"Retry not yet allowed. Next retry at {status.NextRetryTime}",
                        Timestamp = DateTime.UtcNow
                    };
                }
            }

            // Rate limiting
            var timeSinceLastSend = DateTime.UtcNow - _lastSendTime;
            if (timeSinceLastSend < _rateLimitDelay)
            {
                await Task.Delay(_rateLimitDelay - timeSinceLastSend);
            }

            // Try providers with retry logic
            EmailResult result = null;
            var retryCount = 0;

            while (retryCount < _maxRetries)
            {
                foreach (var provider in _providers)
                {
                    var circuitBreaker = _circuitBreakers[provider.ProviderName];

                    if (circuitBreaker.IsCircuitOpen())
                    {
                        _logger.LogWarning($"Circuit open for {provider.ProviderName}. Skipping.");
                        continue;
                    }

                    try
                    {
                        result = await provider.SendEmailAsync(request);

                        if (result.Success)
                        {
                            circuitBreaker.Reset();
                            _lastSendTime = DateTime.UtcNow;

                            if (!string.IsNullOrEmpty(request.IdempotencyKey))
                            {
                                UpdateStatus(request.IdempotencyKey, result, retryCount);
                            }

                            return result;
                        }

                        circuitBreaker.RecordFailure();
                        _logger.LogWarning($"Provider {provider.ProviderName} failed: {result.Message}");
                    }
                    catch (Exception ex)
                    {
                        circuitBreaker.RecordFailure();
                        _logger.LogError(ex, $"Error sending email via {provider.ProviderName}");
                        result = new EmailResult
                        {
                            Success = false,
                            Provider = provider.ProviderName,
                            Message = $"Exception: {ex.Message}",
                            Timestamp = DateTime.UtcNow
                        };
                    }
                }

                retryCount++;

                if (retryCount < _maxRetries)
                {
                    var delay = CalculateExponentialBackoff(retryCount);
                    _logger.LogInformation($"Retry {retryCount} in {delay.TotalSeconds} seconds...");
                    await Task.Delay(delay);
                }
            }

            _lastSendTime = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                UpdateStatus(request.IdempotencyKey, result, retryCount);
            }

            return result ?? new EmailResult
            {
                Success = false,
                Message = "All providers failed",
                Timestamp = DateTime.UtcNow
            };
        }

        private void UpdateStatus(string idempotencyKey, EmailResult result, int retryCount)
        {
            var nextRetryTime = retryCount < _maxRetries
                ? DateTime.UtcNow.Add(CalculateExponentialBackoff(retryCount + 1))
                : (DateTime?)null;

            _statusTracker[idempotencyKey] = new EmailStatus
            {
                IdempotencyKey = idempotencyKey,
                LastResult = result,
                RetryCount = retryCount,
                NextRetryTime = nextRetryTime
            };
        }

        private TimeSpan CalculateExponentialBackoff(int retryCount)
        {
            var delaySeconds = Math.Pow(2, retryCount);
            var jitter = new Random().Next(0, 1000);
            return TimeSpan.FromMilliseconds(delaySeconds * 1000 + jitter);
        }

        public EmailStatus GetEmailStatus(string idempotencyKey)
        {
            return _statusTracker.TryGetValue(idempotencyKey, out var status) ? status : null;
        }
    }
}
