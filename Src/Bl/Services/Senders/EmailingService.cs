using Microsoft.Extensions.Options;
using SharedKernel.Bl.Contracts;
using SharedKernel.Bl.Models;
using System.Net;
using System.Net.Mail;

namespace SharedKernel.Bl.Services.Senders;

public class EmailingService(IOptions<EmailSettings> options) : IEmailing
{

    private readonly EmailSettings _settings = options.Value;

    public async Task SendAsync(string email, string subject, string message)
    {
        string MyMail = _settings.SenderEmail
            ?? throw new ArgumentNullException("Email:Sender", "Sender key is missing in configuration.");

        if (string.IsNullOrWhiteSpace(MyMail))
            throw new ArgumentException("Sender Email key cannot be empty or whitespace.", nameof(MyMail));

        string pw = _settings.Password
            ?? throw new ArgumentNullException("Email:Password", "Password is missing in configuration.");

        if (string.IsNullOrWhiteSpace(pw))
            throw new ArgumentException("Password cannot be empty or whitespace.", nameof(pw));

        string senderName = _settings.SenderName
            ?? throw new ArgumentNullException("Email:SenderName", "Sender Name is missing in configuration.");

        if (string.IsNullOrWhiteSpace(pw))
            throw new ArgumentException("Password cannot be empty or whitespace.", nameof(pw));

        try
        {
            using SmtpClient? client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(MyMail, pw),
                EnableSsl = true
            };

            using MailMessage? mailMessage = new MailMessage
            {
                From = new MailAddress(MyMail, senderName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
        catch (SmtpException smtpEx)
        {
            throw new InvalidOperationException(
                "Failed to send email via SMTP. Please check your credentials or internet connection.",
                smtpEx
            );
        }
        catch (Exception ex)
        {
            throw new ApplicationException("An unexpected error occurred while sending email.", ex);
        }
    }

}
