using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PastPort.Application.Interfaces;

namespace PastPort.Infrastructure.Identity;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _enableEmailSending;

    public EmailService(
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Check if email sending is enabled
        _enableEmailSending = !string.IsNullOrEmpty(_configuration["EmailSettings:SmtpUsername"]) &&
                             !string.IsNullOrEmpty(_configuration["EmailSettings:SmtpPassword"]);
    }

    public async Task SendVerificationEmailAsync(string email, string code)
    {
        var subject = "تفعيل حسابك - PastPort";
        var body = GenerateVerificationEmailBody(code);

        if (_enableEmailSending)
        {
            await SendEmailAsync(email, subject, body);
        }
        else
        {
            // Fallback to console logging for development
            _logger.LogInformation("📧 [VERIFICATION] Email: {Email} | Code: {Code}", email, code);
            Console.WriteLine($"\n╔══════════════════════════════════════╗");
            Console.WriteLine($"║  VERIFICATION CODE                   ║");
            Console.WriteLine($"╠══════════════════════════════════════╣");
            Console.WriteLine($"║  Email: {email,-28}║");
            Console.WriteLine($"║  Code:  {code,-28}║");
            Console.WriteLine($"╚══════════════════════════════════════╝\n");
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string code)
    {
        var subject = "إعادة تعيين كلمة المرور - PastPort";
        var body = GeneratePasswordResetEmailBody(code);

        if (_enableEmailSending)
        {
            await SendEmailAsync(email, subject, body);
        }
        else
        {
            // Fallback to console logging for development
            _logger.LogInformation("📧 [PASSWORD RESET] Email: {Email} | Code: {Code}", email, code);
            Console.WriteLine($"\n╔══════════════════════════════════════╗");
            Console.WriteLine($"║  PASSWORD RESET CODE                 ║");
            Console.WriteLine($"╠══════════════════════════════════════╣");
            Console.WriteLine($"║  Email: {email,-28}║");
            Console.WriteLine($"║  Code:  {code,-28}║");
            Console.WriteLine($"╚══════════════════════════════════════╝\n");
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        var subject = "أهلاً بك في PastPort! 🏛️";
        var body = GenerateWelcomeEmailBody(firstName);

        if (_enableEmailSending)
        {
            await SendEmailAsync(email, subject, body);
        }
        else
        {
            _logger.LogInformation("📧 [WELCOME] Sent to: {Email}", email);
            Console.WriteLine($"📧 Welcome {firstName}! Email sent to {email}");
        }
    }

    public async Task SendPasswordChangedNotificationAsync(string email)
    {
        var subject = "تم تغيير كلمة المرور - PastPort";
        var body = GeneratePasswordChangedEmailBody();

        if (_enableEmailSending)
        {
            await SendEmailAsync(email, subject, body);
        }
        else
        {
            _logger.LogInformation("📧 [PASSWORD CHANGED] Notification sent to: {Email}", email);
            Console.WriteLine($"📧 Password Changed! Notification sent to {email}");
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "PastPort";
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("✅ Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send email to {Email}", toEmail);
            // Log but don't throw - fallback to console
            Console.WriteLine($"❌ Email sending failed: {ex.Message}");
        }
    }

    #region Email Templates

    private string GenerateVerificationEmailBody(string code)
    {
        return $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 40px 30px; text-align: center; }}
        .code-box {{ background: #f8f9fa; border: 3px dashed #667eea; border-radius: 10px; padding: 30px; margin: 30px 0; }}
        .code {{ font-size: 48px; font-weight: bold; color: #667eea; letter-spacing: 10px; font-family: monospace; }}
        .info {{ color: #666; font-size: 14px; margin: 20px 0; }}
        .warning {{ background: #fff3cd; border-right: 4px solid #ffc107; padding: 15px; margin: 20px 0; text-align: right; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏛️ مرحباً بك في PastPort</h1>
        </div>
        <div class='content'>
            <h2>تفعيل البريد الإلكتروني</h2>
            <p>شكراً لتسجيلك في PastPort. يرجى استخدام الكود التالي لتفعيل حسابك:</p>
            
            <div class='code-box'>
                <div class='code'>{code}</div>
            </div>
            
            <div class='info'>
                <p><strong>⏰ صلاحية الكود: 10 دقائق</strong></p>
            </div>
            
            <div class='warning'>
                <strong>⚠️ تنبيه:</strong><br>
                إذا لم تقم بإنشاء حساب في PastPort، يرجى تجاهل هذه الرسالة.
            </div>
        </div>
        <div class='footer'>
            <p>© 2025 PastPort - منصة الواقع الافتراضي للتجارب التاريخية</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetEmailBody(string code)
    {
        return $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; background-color: #f5f5f5; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 40px 30px; text-align: center; }}
        .code-box {{ background: #f8f9fa; border: 3px dashed #f5576c; border-radius: 10px; padding: 30px; margin: 30px 0; }}
        .code {{ font-size: 48px; font-weight: bold; color: #f5576c; letter-spacing: 10px; font-family: monospace; }}
        .info {{ color: #666; font-size: 14px; margin: 20px 0; }}
        .warning {{ background: #fff3cd; border-right: 4px solid #ffc107; padding: 15px; margin: 20px 0; text-align: right; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 إعادة تعيين كلمة المرور</h1>
        </div>
        <div class='content'>
            <h2>طلب إعادة تعيين كلمة المرور</h2>
            <p>تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بك. استخدم الكود التالي للمتابعة:</p>
            
            <div class='code-box'>
                <div class='code'>{code}</div>
            </div>
            
            <div class='info'>
                <p><strong>⏰ صلاحية الكود: 10 دقائق</strong></p>
            </div>
            
            <div class='warning'>
                <strong>⚠️ تنبيه أمني:</strong><br>
                إذا لم تطلب إعادة تعيين كلمة المرور، يرجى تجاهل هذه الرسالة والتأكد من أمان حسابك.
            </div>
        </div>
        <div class='footer'>
            <p>© 2025 PastPort - منصة الواقع الافتراضي للتجارب التاريخية</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateWelcomeEmailBody(string firstName)
    {
        return $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏛️ أهلاً بك في PastPort يا {firstName}!</h1>
        </div>
        <div class='content'>
            <p>مرحباً بك في رحلة عبر الزمن! 🚀</p>
            <p>يمكنك الآن استكشاف التاريخ بطريقة لم تشهدها من قبل.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordChangedEmailBody()
    {
        return $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%); color: white; padding: 40px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .warning {{ background: #fff3cd; border-right: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔒 تم تغيير كلمة المرور</h1>
        </div>
        <div class='content'>
            <p>تم تغيير كلمة المرور الخاصة بحسابك بنجاح.</p>
            <div class='warning'>
                <strong>⚠️ تنبيه:</strong><br>
                إذا لم تقم بهذا الإجراء، يرجى التواصل معنا فوراً.
            </div>
        </div>
    </div>
</body>
</html>";
    }

    #endregion
}