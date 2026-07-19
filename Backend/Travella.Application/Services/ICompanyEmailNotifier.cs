using System;
using System.Collections.Generic;
using System.Text;

namespace Travella.Application.Services
{
    public interface ICompanyEmailNotifier
    {
        Task SendCompanyAdminWelcomeAsync(
            string adminName,
            string adminEmail,
            string companyName,
            string temporaryPassword,
            string companyUrl);
    }
}
