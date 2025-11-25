using Microsoft.Extensions.Logging;
using PastPort.Application.Interfaces;

namespace PastPort.Infrastructure.ExternalServices.Email;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetCodeAsync(string email, string code)
    {
        // Mock implementation - في Phase 4 هنستخدم SendGrid أو MailGun
        _logger.LogInformation("Sending password reset code {Code} to {Email}", code, email);

        // TODO: Implement actual email sending
        Console.WriteLine($"========================================");
        Console.WriteLine($"Password Reset Code for: {email}");
        Console.WriteLine($"Code: {code}");
        Console.WriteLine($"This code will expire in 10 minutes");
        Console.WriteLine($"========================================");

        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string name)
    {
        _logger.LogInformation("Sending welcome email to {Email}", email);
        Console.WriteLine($"Welcome to PastPort, {name}!");
        return Task.CompletedTask;
    }

    public Task SendPasswordChangedNotificationAsync(string email)
    {
        _logger.LogInformation("Sending password changed notification to {Email}", email);
        Console.WriteLine($"Your password has been changed successfully: {email}");
        return Task.CompletedTask;
    }
}