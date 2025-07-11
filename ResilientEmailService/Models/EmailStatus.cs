namespace ResilientEmailService.Models
{
    public class EmailStatus
    {
        public string IdempotencyKey { get; set; }
        public EmailResult LastResult { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryTime { get; set; }
    }
}
