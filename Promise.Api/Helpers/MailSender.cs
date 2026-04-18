using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

// using YouCent.Common;
// using YouCent.Promise.Models;
// using YouCent.Promise.Settings;

namespace Promise.Api;

internal sealed class MailSender
{
    public const string SystemEMail = "YouCent<arkfen@youcent.app>";
    private readonly MailSettings _settings;

    public MailSender(IOptions<MailSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
    }


    public async Task<bool> SendAsync(MailData mailData, CancellationToken ct = default)
    {
        try
        {
            // Initialize a new instance of the MimeKit.MimeMessage class
            using var mail = new MimeMessage();

            #region Sender / Receiver
            // Sender
            mail.From.Add(new MailboxAddress(_settings.DisplayName, mailData.From ?? _settings.From ?? string.Empty));
            mail.Sender = new MailboxAddress(mailData.DisplayName ?? _settings.DisplayName, mailData.From ?? _settings.From ?? string.Empty);

            // Receiver
            foreach (string mailAddress in mailData.To)
                mail.To.Add(MailboxAddress.Parse(mailAddress));

            // Set Reply to if specified in mail data
            if (!string.IsNullOrEmpty(mailData.ReplyTo))
                mail.ReplyTo.Add(new MailboxAddress(mailData.ReplyToName, mailData.ReplyTo));

            // BCC
            // Check if a BCC was supplied in the request
            if (mailData.Bcc != null)
            {
                // Get only addresses where value is not null or with whitespace. x = value of address
                foreach (string mailAddress in mailData.Bcc.Where(x => !string.IsNullOrWhiteSpace(x)))
                    mail.Bcc.Add(MailboxAddress.Parse(mailAddress.Trim()));
            }

            // CC
            // Check if a CC address was supplied in the request
            if (mailData.Cc != null)
            {
                foreach (string mailAddress in mailData.Cc.Where(x => !string.IsNullOrWhiteSpace(x)))
                    mail.Cc.Add(MailboxAddress.Parse(mailAddress.Trim()));
            }
            #endregion

            #region Content

            // Add Content to Mime Message
            var body = new BodyBuilder();
            mail.Subject = mailData.Subject;
            body.HtmlBody = mailData.Body;
            mail.Body = body.ToMessageBody();

            #endregion

            #region Send Mail

            using var smtp = new SmtpClient();

            if (_settings.UseSSL)
            {
                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.SslOnConnect, ct).ConfigureAwait(false);
            }
            else if (_settings.UseStartTls)
            {
                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct).ConfigureAwait(false);
            }
            await smtp.AuthenticateAsync(_settings.UserName, _settings.Password, ct).ConfigureAwait(false);
            await smtp.SendAsync(mail, ct).ConfigureAwait(false);
            await smtp.DisconnectAsync(true, ct).ConfigureAwait(false);

            #endregion

            return true;

        }
        #pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError(" MAILKIT ERROR: " + ex.Message + " ======== " + ex.ToString() + " +++++++++ "
            + ex.InnerException?.Message + " ======== " + ex.InnerException?.ToString());
            MainLogger.Log(" MailsSettings: " + _settings.Host + " | " + _settings.UserName + " | " + _settings.Port + " | " + _settings.UseStartTls);
            return false;
        }
#pragma warning restore CA1031
    }
}



internal sealed class MailData
{
    // Receiver
    public List<string> To { get; }
    public List<string> Bcc { get; }
    public List<string> Cc { get; }

    // Sender
    public string? From { get; }
    public string? DisplayName { get; }
    public string? ReplyTo { get; }
    public string? ReplyToName { get; }

    // Content
    public string Subject { get; }
    public string? Body { get; }

    public MailData(
        List<string> to,
        string subject,
        string? body = null,
        string? from = null,
        string? displayName = null,
        string? replyTo = null,
        string? replyToName = null,
        List<string>? bcc = null,
        List<string>? cc = null)
    {
        // Receiver
        To = to;
        Bcc = bcc ?? [];
        Cc = cc ?? [];

        // Sender
        From = from;
        DisplayName = displayName;
        ReplyTo = replyTo;
        ReplyToName = replyToName;

        // Content
        Subject = subject;
        Body = body;
    }
}


[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by IOptions<MailSettings> dependency injection")]
internal sealed class MailSettings
{
    public string? DisplayName { get; set; }
    public string? From { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; }
    public bool UseSSL { get; set; }
    public bool UseStartTls { get; set; }
}
