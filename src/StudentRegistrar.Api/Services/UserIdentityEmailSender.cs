using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace StudentRegistrar.Api.Services;

public sealed class TransactionalEmailOptions
{
    public const string SectionName = "TransactionalEmail";

    public string Provider { get; set; } = "Log";
    public string? FromEmail { get; set; }
    public string? FromName { get; set; } = "Ruff Registrar";
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool UseStartTls { get; set; } = true;
}

public sealed record PendingEmailChangeEmail(
    string CurrentEmail,
    string PendingEmail,
    string ConfirmationUrl,
    DateTime ExpiresAtUtc);

public sealed record EmailDispatchResult(string? DebugConfirmationUrl);

public interface IUserIdentityEmailSender
{
    Task<EmailDispatchResult> SendEmailChangeConfirmationAsync(PendingEmailChangeEmail email, CancellationToken cancellationToken = default);
}

public sealed class UserIdentityEmailSender : IUserIdentityEmailSender
{
    private readonly TransactionalEmailOptions _options;
    private readonly ILogger<UserIdentityEmailSender> _logger;

    public UserIdentityEmailSender(
        IOptions<TransactionalEmailOptions> options,
        ILogger<UserIdentityEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailDispatchResult> SendEmailChangeConfirmationAsync(PendingEmailChangeEmail email, CancellationToken cancellationToken = default)
    {
        var provider = string.IsNullOrWhiteSpace(_options.Provider) ? "Log" : _options.Provider.Trim();
        if (string.Equals(provider, "Log", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Transactional email suppressed by Log provider. Pending email change from {CurrentEmail} to {PendingEmail} expires at {ExpiresAtUtc}. Confirmation URL: {ConfirmationUrl}",
                email.CurrentEmail,
                email.PendingEmail,
                email.ExpiresAtUtc,
                email.ConfirmationUrl);

            return new EmailDispatchResult(email.ConfirmationUrl);
        }

        if (!string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported transactional email provider '{provider}'.");
        }

        await SendWithSmtpAsync(email, cancellationToken);
        return new EmailDispatchResult(null);
    }

    private async Task SendWithSmtpAsync(PendingEmailChangeEmail email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("Transactional email FromEmail is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            throw new InvalidOperationException("Transactional email SMTP host is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = "Confirm your new Ruff Registrar email address",
            Body = BuildEmailChangeBody(email),
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(email.PendingEmail));

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseStartTls
        };

        if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }

    private static string BuildEmailChangeBody(PendingEmailChangeEmail email) =>
        $"""
        A request was made to change the email address on your Ruff Registrar account.

        Current email: {email.CurrentEmail}
        New email: {email.PendingEmail}

        Confirm this change by opening the link below:
        {email.ConfirmationUrl}

        This link expires at {email.ExpiresAtUtc:O}.

        If you did not request this change, you can ignore this message and keep using your current email address.
        """;
}