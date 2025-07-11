namespace ResilientEmailService.Models
{
    public class EmailResult
    {
        public bool Success { get; set; }
        public string Provider { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
