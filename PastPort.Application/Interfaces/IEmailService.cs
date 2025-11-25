namespace PastPort.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string code);
    Task SendPasswordResetEmailAsync(string email, string code);
    Task SendWelcomeEmailAsync(string email, string firstName);
}