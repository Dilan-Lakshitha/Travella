using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Travella.Application.Interfaces;

namespace Travella.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            ILogger<SmtpEmailService> logger)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return;
            }

            var enabled = _configuration.GetValue("Email:Enabled", false);
            if (!enabled)
            {
                _logger.LogInformation("Email disabled. Would send to {Email}: {Subject}", toEmail, subject);
                return;
            }

            var host = _configuration["Email:Host"] ?? "localhost";
            var port = _configuration.GetValue("Email:Port", 25);
            var from = _configuration["Email:From"] ?? "noreply@travella.local";
            var useSsl = _configuration.GetValue("Email:UseSsl", false);
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];

            using var message = new MailMessage
            {
                From = new MailAddress(from, "Travella"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail.Trim());

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = useSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            await client.SendMailAsync(message);
        }

        public async Task SendToEmailsAsync(IEnumerable<string> toEmails, string subject, string htmlBody)
        {
            var distinct = toEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var email in distinct)
            {
                try
                {
                    await SendAsync(email, subject, htmlBody);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send email to {Email}", email);
                }
            }
        }

        public async Task SendToCompanyAdminsAsync(int companyId, string subject, string htmlBody)
        {
            var admins = await _authRepository.GetCompanyAdminUsersAsync(companyId);
            var emails = admins.Select(a => a.Email).Where(e => !string.IsNullOrWhiteSpace(e));
            await SendToEmailsAsync(emails, subject, htmlBody);
        }
    }
}
