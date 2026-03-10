using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Configs;

namespace Application.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailConfiguration _options;

    public EmailService(AppConfiguration options)
    {
        _options = options.Email;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var emailMessage = new MimeMessage();

        emailMessage.From.Add(new MailboxAddress(_options.Name, _options.EmailAddress));
        emailMessage.To.Add(new MailboxAddress("customer", email));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = message
        };

        using (var client = new SmtpClient())
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds)))
            {
                try
                {
                    await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.SslOnConnect, cts.Token);
                    await client.AuthenticateAsync(_options.EmailAddress, _options.Password, cts.Token);
                    await client.SendAsync(emailMessage, cts.Token);
                    await client.DisconnectAsync(true, cts.Token);
                }
                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                {
                    throw new TimeoutException($"Не удалось подключиться к SMTP серверу {_options.Host}:{_options.Port} в течение {_options.TimeoutSeconds} секунд.");
                }
            }
        }
    }

}
