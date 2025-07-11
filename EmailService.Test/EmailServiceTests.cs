using Microsoft.Extensions.Logging;
using Moq;
using ResilientEmailService.Models;
using ResilientEmailService.Services.Email;
using Xunit;


namespace EmailService.Test
{
    public class EmailServiceTests
    {
        [Fact]
        public async Task SendEmail_Success_FirstProvider()
        {
            // Arrange
            var mockProvider1 = new Mock<IEmailProvider>();
            mockProvider1.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider1.Setup(p => p.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync(new EmailResult { Success = true });

            var mockProvider2 = new Mock<IEmailProvider>();
            mockProvider2.Setup(p => p.ProviderName).Returns("Provider2");

            var loggerMock = new Mock<ILogger<ResilientEmailService.Services.Email.EmailServices>>();

            var emailService = new ResilientEmailService.Services.Email.EmailServices(
                new[] { mockProvider1.Object, mockProvider2.Object },
                loggerMock.Object);

            var request = new EmailRequest { To = "test@example.com", Subject = "Test", Body = "Test" };

            // Act
            var result = await emailService.SendEmailAsync(request);

            // Assert
            Assert.True(result.Success);
            mockProvider1.Verify(p => p.SendEmailAsync(It.IsAny<EmailRequest>()), Times.Once);
            mockProvider2.Verify(p => p.SendEmailAsync(It.IsAny<EmailRequest>()), Times.Never);
        }


        [Fact]
        public async Task SendEmail_Fallback_SecondProvider()
        {
            // Arrange
            var mockProvider1 = new Mock<IEmailProvider>();
            mockProvider1.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider1.Setup(p => p.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync(new EmailResult
                {
                    Success = false,
                    Provider = "Provider1" // <-- Added
                });

            var mockProvider2 = new Mock<IEmailProvider>();
            mockProvider2.Setup(p => p.ProviderName).Returns("Provider2");
            mockProvider2.Setup(p => p.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync(new EmailResult
                {
                    Success = true,
                    Provider = "Provider2"
                });

            var loggerMock = new Mock<ILogger<EmailServices>>();
            var emailService = new EmailServices(
                new[] { mockProvider1.Object, mockProvider2.Object },
                loggerMock.Object);

            var request = new EmailRequest { To = "test@example.com", Subject = "Test", Body = "Test" };

            // Act
            var result = await emailService.SendEmailAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Provider2", result.Provider);
            mockProvider1.Verify(p => p.SendEmailAsync(It.IsAny<EmailRequest>()), Times.Once);
            mockProvider2.Verify(p => p.SendEmailAsync(It.IsAny<EmailRequest>()), Times.Once);
        }

        [Fact]
        public async Task SendEmail_WithIdempotencyKey_ReturnsSameResult()
        {
            // Arrange
            var mockProvider = new Mock<IEmailProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider.Setup(p => p.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync(new EmailResult { Success = true });

            var loggerMock = new Mock<ILogger<ResilientEmailService.Services.Email.EmailServices>>();

            var emailService = new ResilientEmailService.Services.Email.EmailServices(
                new[] { mockProvider.Object },
                loggerMock.Object);

            var request = new EmailRequest
            {
                To = "test@example.com",
                Subject = "Test",
                Body = "Test",
                IdempotencyKey = "test-key"
            };

            // Act
            var result1 = await emailService.SendEmailAsync(request);
            var result2 = await emailService.SendEmailAsync(request);

            // Assert
            Assert.Equal(result1.Success, result2.Success);
            Assert.Equal(result1.Provider, result2.Provider);
            mockProvider.Verify(p => p.SendEmailAsync(It.IsAny<EmailRequest>()), Times.Once);
        }


        [Fact]
        public async Task SendEmail_CircuitBreaker_OpensAfterFailures()
        {
            // Arrange
            var mockProvider = new Mock<IEmailProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider.Setup(p => p.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync(new EmailResult
                {
                    Success = false,
                    Provider = "Provider1", // <-- Added
                    Message = "Failed"
                });

            var loggerMock = new Mock<ILogger<EmailServices>>();
            var emailService = new EmailServices(
                new[] { mockProvider.Object },
                loggerMock.Object,
                maxRetries: 1);

            var request = new EmailRequest { To = "test@example.com", Subject = "Test", Body = "Test" };

            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                await emailService.SendEmailAsync(request);
            }

            var result = await emailService.SendEmailAsync(request);
            Assert.False(result.Success);
            Assert.Contains("circuit open", result.Message.ToLower()); // Updated message check
            mockProvider.Verify(p => p.SendEmailAsync(It.IsAny<EmailRequest>()), Times.Exactly(3));
        }
    }
}