using System.Net;
using System.Net.Mail;

namespace FaydamPDKS.Web;

public interface IWebPasswordResetEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string recipientEmail, string recipientName, string resetUrl, CancellationToken cancellationToken);
}

public sealed class SmtpPasswordResetEmailSender(IConfiguration configuration) : IWebPasswordResetEmailSender
{
    public bool IsConfigured => configuration.GetValue("Smtp:Enabled", false)
        && !string.IsNullOrWhiteSpace(configuration["Smtp:Host"])
        && !string.IsNullOrWhiteSpace(configuration["Smtp:FromAddress"]);

    public async Task SendAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new InvalidOperationException("SMTP_NOT_CONFIGURED");
        var host = configuration["Smtp:Host"]!;
        var port = configuration.GetValue("Smtp:Port", 587);
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        var fromAddress = configuration["Smtp:FromAddress"]!;
        var fromName = configuration["Smtp:FromName"] ?? "Faydam PDKS";

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = "Faydam PDKS şifre sıfırlama bağlantısı",
            Body = $"Merhaba {recipientName},\n\nŞifrenizi yenilemek için aşağıdaki bağlantıyı kullanın. Bağlantı 30 dakika geçerlidir ve yalnızca bir kez kullanılabilir.\n\n{resetUrl}\n\nBu talebi siz oluşturmadıysanız e-postayı dikkate almayın.",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail, recipientName));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = configuration.GetValue("Smtp:EnableSsl", true),
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password);
        await client.SendMailAsync(message, cancellationToken);
    }
}
