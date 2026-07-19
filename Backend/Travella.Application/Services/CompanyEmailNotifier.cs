using System;
using System.Collections.Generic;
using System.Text;
using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class CompanyEmailNotifier : ICompanyEmailNotifier
    {
        private readonly IEmailService _emailService;

        public CompanyEmailNotifier(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendCompanyAdminWelcomeAsync(
            string adminName,
            string adminEmail,
            string companyName,
            string temporaryPassword,
            string companyUrl)
        {
            var subject = $"Welcome to Travella - {companyName}";

            var body = $"""
            <h2>Welcome to Travella</h2>

            <p>Hello {adminName},</p>

            <p>Your travel company account has been created successfully.</p>

            <h3>Company Details</h3>

            <p><strong>Company:</strong> {companyName}</p>
            <p><strong>Admin Email:</strong> {adminEmail}</p>
            <p><strong>Temporary Password:</strong> {temporaryPassword}</p>

            <p>
                <a href="{companyUrl}">
                    Open Your Travella Portal
                </a>
            </p>

            <p>
                You will be required to change your temporary password
                when you log in for the first time.
            </p>
            """;

            await _emailService.SendAsync(
                adminEmail,
                subject,
                body);
        }
    }    
}
