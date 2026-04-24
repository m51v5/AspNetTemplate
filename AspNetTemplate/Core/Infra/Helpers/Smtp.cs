using System.Net;
using System.Net.Mail;

namespace AspNetTemplate.Core.Infra.Helpers
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string title, string content);
    }
    public class GmailSmtp : IEmailSender
    {
        private const string HOST = "smtp.gmail.com";  // Correct Gmail SMTP server
        private const int PORT = 587;                  // Port for TLS (use 465 for SSL)
        private const bool USE_SSL = true;             // We want SSL

        private readonly string _email;
        private readonly string _password;

        public GmailSmtp(string email, string password)
        {
            _email = email;
            _password = password;
        }

        public async Task SendAsync(string to, string title, string content)
        {
            await Task.Run(() => PushEmail(to, title, content).GetAwaiter().GetResult());
        }

        private async Task PushEmail(string to, string title, string content)
        {
            using (var smtpClient = new SmtpClient(HOST, PORT))
            {
                smtpClient.EnableSsl = USE_SSL;  // Enable SSL

                smtpClient.Credentials = new NetworkCredential(_email, _password);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_email),
                    Subject = title,
                    Body = content,
                    IsBodyHtml = true  // Set this depending on the content type
                };

                mailMessage.To.Add(to);

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
                catch (SmtpException ex)
                {
                    // Log the detailed exception for further analysis
                    throw new Exception($"Error sending email: {ex.Message}", ex);
                }
            }
        }

    }
}