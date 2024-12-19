namespace MyChat.Models
{
    using System.Net;
    using System.Net.Mail;
    using System.Threading.Tasks;

    public class EmailService : IEmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUser = "test";
        private readonly string _smtpPassword = "test";

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var smtpClient = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpUser),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }

}
