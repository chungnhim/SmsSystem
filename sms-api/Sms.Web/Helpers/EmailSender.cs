using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Newtonsoft.Json;

namespace Sms.Web.Helpers
{
    public class EmailRequest
    {
        public EmailRequest()
        {
            Tos = new List<string>();
        }
        public List<string> Tos { get; set; }
        public string Subject { get; set; }
        public string Body { get; internal set; }
        public string TemplateName { get; set; }
        public List<string> Params { get; set; }
    }
    public interface IEmailSender
    {
        void SendEmail(EmailRequest emailRequest);
        Task SendEmailAsync(EmailRequest emailRequest);
    }
    public class EmailSender : IEmailSender
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<EmailSender> _logger;
        private readonly string wwwRootPath;
        public EmailSender(IOptions<AppSettings> appSettings, ILogger<EmailSender> logger, IHostingEnvironment env)
        {
            _appSettings = appSettings.Value;
            _logger = logger;
            wwwRootPath = env.WebRootPath;
        }
        public void SendEmail(EmailRequest emailRequest)
        {
            if (_appSettings == null || _appSettings.Smtp == null)
            {
                _logger.LogError("Cannot send email, the Smtp config is null");
                throw new Exception("SmtpConfigIsNull");
            }
            var body = emailRequest.Body ?? GenerateBodyFromTemplateAndParams(emailRequest.TemplateName, emailRequest.Params);
            using (var message = new MailMessage())
            {
                foreach (var email in emailRequest?.Tos ?? new List<string>())
                {
                    if (!string.IsNullOrEmpty(email))
                    {
                        message.To.Add(email);
                    }
                }
                message.From = new MailAddress(_appSettings.Smtp.Email, _appSettings.Smtp.Email);
                message.Subject = emailRequest.Subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(_appSettings.Smtp.Server))
                {
                    client.Port = 587;
                    client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    client.Credentials = new NetworkCredential(_appSettings.Smtp.Email, _appSettings.Smtp.Password);
                    client.EnableSsl = true;
                    client.Send(message);
                }
            }
        }
        public async Task SendEmailAsync(EmailRequest emailRequest)
        {
            try
            {
                if (_appSettings == null || _appSettings.Smtp == null)
                {
                    _logger.LogError("Cannot send email, the Smtp config is null");
                    throw new Exception("SmtpConfigIsNull");
                }
                var body = emailRequest.Body ?? GenerateBodyFromTemplateAndParams(emailRequest.TemplateName, emailRequest.Params);
                using (var message = new MailMessage())
                {
                    foreach (var email in emailRequest?.Tos ?? new List<string>())
                    {
                        if (!string.IsNullOrEmpty(email))
                        {
                            message.To.Add(email);
                        }
                    }
                    message.From = new MailAddress(_appSettings.Smtp.Email, _appSettings.Smtp.Email);
                    message.Subject = emailRequest.Subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(_appSettings.Smtp.Server))
                    {
                        client.Port = 587;
                        client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                        client.Credentials = new NetworkCredential(_appSettings.Smtp.Email, _appSettings.Smtp.Password);
                        client.EnableSsl = true;
                        await client.SendMailAsync(message);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, JsonConvert.SerializeObject(emailRequest));
            }
        }
        private string GenerateBodyFromTemplateAndParams(string templateName, List<string> variables)
        {
            var pathToFile = wwwRootPath
                               + Path.DirectorySeparatorChar.ToString()
                               + "Templates"
                               + Path.DirectorySeparatorChar.ToString()
                               + templateName + ".html";
            string body;
            using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
            {
                body = SourceReader.ReadToEnd();
            }
            body = string.Format(body, variables.ToArray());
            return body;
        }
    }
}
