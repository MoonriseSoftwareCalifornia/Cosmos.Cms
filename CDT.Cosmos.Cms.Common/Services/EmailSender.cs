using CDT.Cosmos.Cms.Common.Services.Configurations;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Common.Services
{
    /// <summary>
    ///     SendGrid Email sender service
    /// </summary>
    public class EmailSender : IEmailSender
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="options"></param>
        public EmailSender(IOptions<CosmosConfig> options)
        {
            if (options.Value != null && options.Value.SendGridConfig != null) Options = options.Value.SendGridConfig;
        }

        /// <summary>
        ///     SendGrid configuration options
        /// </summary>
        private SendGridConfig Options { get; } //set only via Secret Manager

        /// <summary>
        ///     Email response from SendGrid
        /// </summary>
        public Response Response { get; private set; }

        /// <summary>
        ///     Send email method
        /// </summary>
        /// <param name="email"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="emailFrom"></param>
        /// <returns></returns>
        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Execute(subject, message, email);
        }

        /// <summary>
        ///     Send email method
        /// </summary>
        /// <param name="email"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="emailFrom"></param>
        /// <returns></returns>
        public Task SendEmailAsync(string email, string subject, string message, string emailFrom)
        {
            return Execute(subject, message, email, emailFrom);
        }

        /// <summary>
        ///     Determine if this service is configured
        /// </summary>
        /// <returns></returns>
        public bool IsConfigured()
        {
            return Options != null;
        }

        /// <summary>
        ///     Execute send email method
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="email"></param>
        /// <param name="emailFrom"></param>
        /// <returns></returns>
        private Task Execute(string subject, string message, string email, string emailFrom = null)
        {
            var client = new SendGridClient(Options.SendGridKey);
            var msg = new SendGridMessage
            {
                From = new EmailAddress(emailFrom ?? Options.EmailFrom),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(true, true);

            Response = client.SendEmailAsync(msg).Result;

            return Task.CompletedTask;
        }
    }
}