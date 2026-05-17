using System.Collections.Generic;
using System.Threading.Tasks;

namespace Travella.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);

        Task SendToEmailsAsync(IEnumerable<string> toEmails, string subject, string htmlBody);

        Task SendToCompanyAdminsAsync(int companyId, string subject, string htmlBody);
    }
}
