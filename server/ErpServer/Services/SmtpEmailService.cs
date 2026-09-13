using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ErpServer.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;

    public SmtpEmailService (IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var emailMessage = new MimeMessage();

        var senderName = _config["EmailSettings:SenderName"] ?? "Enterprise ERP";
        var senderEmail = _config["EmailSettings:SenderEmail"] ?? "noreply@erpsystem.com";
        emailMessage.From.Add(new MailboxAddress(senderName, senderEmail));
        
        emailMessage.To.Add(new MailboxAddress("", toEmail));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart("html")
        {
            Text = body
        };

        using var client = new SmtpClient();
        var smtpServer = _config["EmailSettings:SmtpServer"] ?? "sandbox.smtp.mailtrap.io";
        var smtpPort = int.TryParse(_config["EmailSettings:SmtpPort"], out var p) ? p : 2525;
        var username = _config["EmailSettings:Username"] ?? "";
        var password = (_config["EmailSettings:Password"] ?? "").Replace(" ", "");

        try
        {
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.Auto);
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                await client.AuthenticateAsync(username, password);
            }
            await client.SendAsync(emailMessage);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}
