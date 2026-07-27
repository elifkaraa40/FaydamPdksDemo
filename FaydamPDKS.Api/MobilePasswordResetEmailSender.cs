using System.Net;
using System.Net.Mail;
using FaydamPDKS.Core.DTOs;

namespace FaydamPDKS.Api;

public sealed class MobilePasswordResetEmailSender(IConfiguration configuration)
{
    public bool IsConfigured => configuration.GetValue("Smtp:Enabled", false)
        && !string.IsNullOrWhiteSpace(configuration["Smtp:Host"])
        && !string.IsNullOrWhiteSpace(configuration["Smtp:FromAddress"])
        && Uri.TryCreate(configuration["PasswordReset:WebBaseUrl"], UriKind.Absolute, out _);

    public async Task SendAsync(PasswordResetEmailTicket ticket, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new InvalidOperationException("SMTP_NOT_CONFIGURED");
        var webBaseUrl = configuration["PasswordReset:WebBaseUrl"]!.TrimEnd('/');
        var resetUrl = $"{webBaseUrl}/PasswordRecovery/Reset?token={Uri.EscapeDataString(ticket.RawToken)}";
        using var message = new MailMessage
        {
            From = new MailAddress(configuration["Smtp:FromAddress"]!, configuration["Smtp:FromName"] ?? "Faydam PDKS"),
            Subject = "Faydam PDKS şifre sıfırlama bağlantısı",
            Body = $"Merhaba {ticket.RecipientName},\n\nŞifrenizi yenilemek için aşağıdaki bağlantıyı kullanın. Bağlantı 30 dakika geçerlidir ve yalnızca bir kez kullanılabilir.\n\n{resetUrl}\n\nBu talebi siz oluşturmadıysanız e-postayı dikkate almayın.",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(ticket.RecipientEmail, ticket.RecipientName));
        using var client = new SmtpClient(configuration["Smtp:Host"]!, configuration.GetValue("Smtp:Port", 587))
        {
            EnableSsl = configuration.GetValue("Smtp:EnableSsl", true),
            UseDefaultCredentials = false
        };
        var username = configuration["Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, configuration["Smtp:Password"]);
        await client.SendMailAsync(message, cancellationToken);
    }
}
