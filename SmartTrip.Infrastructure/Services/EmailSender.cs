using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

namespace SmartTrip.Infrastructure.Services
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
            // Читаємо налаштування через повний шлях до секції
            var fromEmail = _configuration["EmailSettings:Email"];
            var password = _configuration["EmailSettings:Password"];
            var host = _configuration["EmailSettings:Host"] ?? "smtp.gmail.com";
            var portString = _configuration["EmailSettings:Port"] ?? "587";

            // Перевірка наявності даних (якщо null - кидаємо зрозумілу помилку)
            if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password))
            {
                throw new Exception("Налаштування Email (Email або Password) не знайдені в конфігурації! ");
            }

            if (!int.TryParse(portString, out int port))
            {
                port = 587;
            }

            using (var smtpClient = new SmtpClient(host))
            {
                smtpClient.Port = port;
                smtpClient.Credentials = new NetworkCredential(fromEmail, password);
                smtpClient.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "SmartTrip Support"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                // Виконуємо відправку
                await smtpClient.SendMailAsync(mailMessage);
            }
        }
    }
}