using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using YousefZuaianatAPI.Services;

namespace YousefZuaianatAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlContent)
        {
            // 1. استدعاء إعدادات الـ SMTP من ملف appsettings.json
            var server = _config["SmtpSettings:Server"];
            var port = int.Parse(_config["SmtpSettings:Port"]);
            var senderEmail = _config["SmtpSettings:SenderEmail"];
            var senderName = _config["SmtpSettings:SenderName"];
            var username = _config["SmtpSettings:Username"];
            var password = _config["SmtpSettings:Password"];

            // 2. إنشاء عميل SMTP (المسؤول عن نقل الإيميل)
            using var smtpClient = new SmtpClient(server)
            {
                Port = port,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true, // Gmail يتطلب تشفير SSL
            };

            // 3. إنشاء الرسالة نفسها
            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlContent,
                IsBodyHtml = true, // لتفعيل تنسيق HTML
            };

            mailMessage.To.Add(to);

            // 4. إرسال الرسالة بشكل غير متزامن
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
