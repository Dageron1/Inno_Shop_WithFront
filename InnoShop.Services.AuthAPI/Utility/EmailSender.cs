using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;
using System.Net;

namespace InnoShop.Services.AuthAPI.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var from = _configuration["EmailSettings:From"];
            var host = _configuration["EmailSettings:SmtpHost"];
            var userName = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];
            var prot = _configuration.GetValue<int>("EmailSettings:SmtpPort");

            MailMessage message = new MailMessage();
            message.To.Add(email);
            message.Subject = subject;
            message.From = new MailAddress(from);
            message.Body = $"<html><body> {htmlMessage}</body></html>";
            message.IsBodyHtml = true;

            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Host = host;
                smtpClient.Port = prot;
                smtpClient.Credentials = new NetworkCredential(userName, password);
                smtpClient.EnableSsl = true;

                await smtpClient.SendMailAsync(message);
            }
        }
    }
}
