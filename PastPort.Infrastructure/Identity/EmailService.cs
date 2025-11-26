using PastPort.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PastPort.Infrastructure.Identity;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string email, string code)
    {
        _logger.LogInformation("📧 Verification Code for {Email}: {Code}", email, code);
        Console.WriteLine($"📧 Verification Code for {email}: {code}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string code)
    {
        _logger.LogInformation("📧 Password Reset Code for {Email}: {Code}", email, code);
        Console.WriteLine($"📧 Password Reset Code for {email}: {code}");
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string firstName)
    {
        _logger.LogInformation("📧 Welcome email sent to {Email}", email);
        Console.WriteLine($"📧 Welcome {firstName}! Email sent to {email}");
        return Task.CompletedTask;
    }

    public Task SendPasswordChangedNotificationAsync(string email)
    {
        _logger.LogInformation("📧 Password changed notification sent to {Email}", email);
        Console.WriteLine($"📧 Password Changed! Notification sent to {email}");
        return Task.CompletedTask;
    }
}