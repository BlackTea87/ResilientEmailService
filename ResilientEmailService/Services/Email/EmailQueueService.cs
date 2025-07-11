using ResilientEmailService.Models;

namespace ResilientEmailService.Services.Email
{
    // Services/Email/EmailQueueService.cs
    public class EmailQueueService : BackgroundService
    {
        private readonly Queue<EmailRequest> _queue = new();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly EmailService _emailService;
        private readonly ILogger<EmailQueueService> _logger;

        public EmailQueueService(EmailService emailService, ILogger<EmailQueueService> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public void Enqueue(EmailRequest request)
        {
            lock (_queue)
            {
                _queue.Enqueue(request);
            }
            _signal.Release();
            _logger.LogInformation($"Email queued. Queue size: {_queue.Count}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _signal.WaitAsync(stoppingToken);

                EmailRequest request;
                lock (_queue)
                {
                    request = _queue.Dequeue();
                }

                try
                {
                    var result = await _emailService.SendEmailAsync(request);
                    _logger.LogInformation($"Email processed. Success: {result.Success}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email from queue");
                }
            }
        }
    }
}
