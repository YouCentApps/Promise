using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Promise.Api.Helpers;

internal sealed class MailSender
{
    public const string SystemEMail = "YouCent<arkfen@youcent.app>";
    private readonly MailSettings _settings;

    public MailSender(IOptions<MailSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
    }

    internal sealed class EmailSendException : Exception
    {
        public EmailSendException(string message, Exception innerException)
            : base(message, innerException) { }

        public EmailSendException()
        {
        }

        public EmailSendException(string message) : base(message)
        {
        }
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

            if(_settings.Host == null || _settings.UserName == null || _settings.Password == null)
            {
                MainLogger.LogError(" MAILKIT ERROR: Host, UserName or Password is null. Check your MailSettings configuration.");
                return false;
            }

            if (_settings.UseSSL)
            {
                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.SslOnConnect, ct).ConfigureAwait(false);
            }
            else if (_settings.UseStartTls)
            {
                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct).ConfigureAwait(false);
            }
            await smtp.AuthenticateAsync(_settings.UserName, _settings.Password, ct).ConfigureAwait(false);
            string response = await smtp.SendAsync(mail, ct).ConfigureAwait(false);
            await smtp.DisconnectAsync(true, ct).ConfigureAwait(false);
            
            #endregion

            if (response != null)
            {
                MainLogger.Log(" MAILKIT RESPONSE: " + response);
                return true;
            }
            MainLogger.LogError(" MAILKIT ERROR: No response received from SMTP server.");
            return false;

        }
        catch (Exception ex)
        {
            MainLogger.LogError(" MAILKIT ERROR: " + ex.Message + " ======== " + ex.ToString() + " +++++++++ "
            + ex.InnerException?.Message + " ======== " + ex.InnerException?.ToString());
            MainLogger.Log(" MailsSettings: " + _settings.Host + " | " + _settings.UserName + " | " + _settings.Port + " | " + _settings.UseStartTls);
            throw new EmailSendException("Error sending email", ex);
        }
    }
}



internal sealed class MailData(
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
    public List<string> To { get; } = to;
    public List<string> Bcc { get; } = bcc ?? [];
    public List<string> Cc { get; } = cc ?? [];

    // Sender
    public string? From { get; } = from;
    public string? DisplayName { get; } = displayName;
    public string? ReplyTo { get; } = replyTo;
    public string? ReplyToName { get; } = replyToName;

    // Content
    public string Subject { get; } = subject;
    public string? Body { get; } = body;
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
