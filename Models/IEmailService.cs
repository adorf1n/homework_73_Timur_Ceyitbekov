using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;

namespace MyChat.Models
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

}
