using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace LabAssistanteProject.Services
{
    // Email Service
    public class EmailService
    {
        private readonly IConfiguration _config;

        // Constructor
        public EmailService(IConfiguration config)
        {
            _config = config;
        }
        // Send Email Method
        public void SendEmail(string? toEmail, string subject, string body)
        {
            if (string.IsNullOrEmpty(toEmail))
            {
                // Agar recipient email null/empty ho, email send na kare
                return;
            }

            var emailSettings = _config.GetSection("EmailSettings");
            string host = emailSettings["Host"] ?? "smtp.example.com";
            int port = 587;
            if (!int.TryParse(emailSettings["Port"], out port))
            {
                port = 587; // default port
            }

            string username = emailSettings["Username"] ?? "";
            string password = emailSettings["Password"] ?? "";
            string senderName = emailSettings["SenderName"] ?? "LabAssist OHD";
            string senderEmail = emailSettings["SenderEmail"] ?? "no-reply@labassist.com";

            using (var client = new SmtpClient(host, port))
            {
                client.Credentials = new NetworkCredential(username, password);
                client.EnableSsl = true;

                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(senderEmail, senderName);
                mailMessage.To.Add(toEmail);
                mailMessage.Subject = subject ?? "";
                mailMessage.Body = body ?? "";
                mailMessage.IsBodyHtml = true;

                try
                {
                    client.Send(mailMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Email sending failed: " + ex.Message);
                }
            }
        }
    }
}
