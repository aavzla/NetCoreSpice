using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Spice.Service
{
    public class EmailSender : IEmailSender
    {
        public EmailOptions EmailOptions { get; set; }

        public EmailSender(IOptions<EmailOptions> emailOptions)
        {
            EmailOptions = emailOptions.Value;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Execute(EmailOptions.SendGridKey, subject, htmlMessage, email);
        }

        private Task Execute(string sendGridKey, string subject, string htmlMessage, string email)
        {
            var client = new SendGridClient(sendGridKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("admin@spice.com", "Spice Restaurant"),
                Subject = subject,
                PlainTextContent = htmlMessage,
                HtmlContent = htmlMessage
            };
            msg.AddTo(new EmailAddress(email));
            
            try
            {
                return client.SendEmailAsync(msg);
            }
            catch (Exception ex)
            {
                //Here we can log the exception
                var exceptionMessage = ex.Message;
            }

            return null;
        }
    }
}
