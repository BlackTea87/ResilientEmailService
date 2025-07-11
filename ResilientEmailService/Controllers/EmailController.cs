using Microsoft.AspNetCore.Mvc;
using ResilientEmailService.Models;
using ResilientEmailService.Services.Email;

namespace ResilientEmailService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly IServiceProvider _serviceProvider;

        // Renamed field from '_emailQueueService' to '_serviceProvider' to match the actual usage
        public EmailController(EmailService emailService, IServiceProvider serviceProvider)
        {
            _emailService = emailService;
            _serviceProvider = serviceProvider;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            if (string.IsNullOrEmpty(request.To) || string.IsNullOrEmpty(request.Subject))
            {
                return BadRequest("To and Subject are required");
            }

            var result = await _emailService.SendEmailAsync(request);
            return Ok(result);
        }

        [HttpPost("queue")]
        public IActionResult QueueEmail([FromBody] EmailRequest request)
        {
            if (string.IsNullOrEmpty(request.To) || string.IsNullOrEmpty(request.Subject))
            {
                return BadRequest("To and Subject are required");
            }

            var queueService = _serviceProvider.GetRequiredService<EmailQueueService>();
            queueService.Enqueue(request);
            return Accepted(new { Message = "Email queued for processing" });
        }

        [HttpGet("status/{idempotencyKey}")]
        public IActionResult GetStatus(string idempotencyKey)
        {
            var status = _emailService.GetEmailStatus(idempotencyKey);
            if (status == null)
            {
                return NotFound();
            }
            return Ok(status);
        }
    }
}