namespace ResilientEmailService.Models
{
    public class EmailRequest
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string IdempotencyKey { get; set; }
    }
}
