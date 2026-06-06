using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;

namespace HotstarApi.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose)
    {
        var host = _config["SmtpSettings:Host"];
        var portStr = _config["SmtpSettings:Port"];
        var user = _config["SmtpSettings:User"];
        var pass = _config["SmtpSettings:Pass"];
        var fromEmail = _config["SmtpSettings:FromEmail"];
        var fromName = _config["SmtpSettings:FromName"] ?? "Hotstar Clone";

        // If SMTP is not properly configured, log a warning and fall back to simulation
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || host == "smtp.example.com")
        {
            _logger.LogWarning($"[OTP SIMULATION] Purpose: {purpose} | Email: {toEmail} | Code: {otpCode}");
            return;
        }

        if (!int.TryParse(portStr, out int port))
        {
            port = 587; // default port
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = $"Your OTP Code for {GetFriendlyPurposeName(purpose)}";

        // Create the responsive HTML template
        var htmlBody = GetOtpEmailTemplate(otpCode, purpose);

        message.Body = new TextPart(TextFormat.Html)
        {
            Text = htmlBody
        };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation($"OTP email sent successfully to {toEmail}");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, $"Failed to send OTP email to {toEmail}");
            // Optional: rethrow or handle if you want the API to fail when email fails
            throw; 
        }
    }

    private string GetFriendlyPurposeName(string purpose)
    {
        return purpose switch
        {
            "Login" => "Account Login",
            "PasswordReset" => "Password Reset",
            "EmailVerification" => "Email Verification",
            _ => purpose
        };
    }

    private string GetOtpEmailTemplate(string otpCode, string purpose)
    {
        string title = $"Verify your {GetFriendlyPurposeName(purpose)}";

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: 'Inter', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #0f1014;
            color: #ffffff;
            margin: 0;
            padding: 0;
            -webkit-font-smoothing: antialiased;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #192133;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
            border: 1px solid #2d3b5c;
        }}
        .header {{
            background: linear-gradient(90deg, #1f4287, #071e3d);
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            color: #1f80e0;
            font-size: 28px;
            font-weight: 800;
            letter-spacing: 1px;
        }}
        .content {{
            padding: 40px 30px;
            text-align: center;
        }}
        .content h2 {{
            color: #e1e6f0;
            font-size: 22px;
            margin-bottom: 20px;
            font-weight: 600;
        }}
        .content p {{
            color: #a9b2c8;
            font-size: 16px;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        .otp-box {{
            background-color: #0f1014;
            border: 1px dashed #1f80e0;
            border-radius: 8px;
            padding: 20px;
            margin: 0 auto 30px auto;
            display: inline-block;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: 700;
            letter-spacing: 8px;
            color: #1f80e0;
            margin: 0;
        }}
        .footer {{
            background-color: #121622;
            padding: 20px;
            text-align: center;
            border-top: 1px solid #2d3b5c;
        }}
        .footer p {{
            color: #7b88a8;
            font-size: 13px;
            margin: 0;
        }}
        .brand-text {{
            color: #ffffff;
            font-weight: 600;
        }}
        @media only screen and (max-width: 600px) {{
            .container {{
                margin: 20px;
                width: auto;
            }}
            .content {{
                padding: 30px 20px;
            }}
            .otp-code {{
                font-size: 28px;
                letter-spacing: 6px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>HOTSTAR<span style=""color: #ffffff;"">CLONE</span></h1>
        </div>
        <div class=""content"">
            <h2>{title}</h2>
            <p>You requested an OTP for <strong>{GetFriendlyPurposeName(purpose)}</strong>. Please use the following 6-digit code to complete your request. This code is valid for 5 minutes.</p>
            
            <div class=""otp-box"">
                <p class=""otp-code"">{otpCode}</p>
            </div>
            
            <p style=""font-size: 14px;"">If you did not request this code, please ignore this email or contact support if you have concerns.</p>
        </div>
        <div class=""footer"">
            <p>&copy; {System.DateTime.UtcNow.Year} <span class=""brand-text"">Hotstar Clone</span>. All rights reserved.</p>
            <p style=""margin-top: 8px;"">This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";
    }
}
