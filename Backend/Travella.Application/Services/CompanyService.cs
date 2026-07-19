using System.Text.RegularExpressions;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly ICompanyEmailNotifier _companyEmailNotifier;

        public CompanyService(ICompanyRepository companyRepository, ICompanyEmailNotifier companyEmailNotifier)
        {
            _companyRepository = companyRepository;
            _companyEmailNotifier = companyEmailNotifier;
        }

        public async Task<CreateCompanyResponse> CreateCompanyAsync(CreateCompanyRequest request,int? createdBy)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Company name is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Company email is required.");

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new ArgumentException("Company phone is required.");

            if (string.IsNullOrWhiteSpace(request.OwnerName))
                throw new ArgumentException("Owner name is required.");

            if (string.IsNullOrWhiteSpace(request.AdminEmail))
                throw new ArgumentException("Admin email is required.");

            if (await _companyRepository.CompanyEmailExistsAsync(request.Email))
            {
                throw new InvalidOperationException(
                    "A company already exists with this email.");
            }

            if (await _companyRepository.UserEmailExistsAsync(request.AdminEmail))
            {
                throw new InvalidOperationException(
                    "A user already exists with this admin email.");
            }

            var slug = await GenerateUniqueSlugAsync(request.Name);

            var temporaryPassword = GenerateTemporaryPassword();

            var passwordHash =
                BCrypt.Net.BCrypt.HashPassword(temporaryPassword);

            var created =
                await _companyRepository.CreateCompanyWithAdminAsync(request,slug,passwordHash,createdBy);

            var companyUrl =
                $"http://localhost:4200/company/{slug}";

            var emailSent = false;

            try
            {
                await _companyEmailNotifier.SendCompanyAdminWelcomeAsync(
                    request.OwnerName,
                    request.AdminEmail,
                    request.Name,
                    temporaryPassword,
                    companyUrl);

                emailSent = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return new CreateCompanyResponse
            {
                CompanyId = created.CompanyId,
                AdminUserId = created.AdminUserId,
                CompanyName = request.Name,
                Slug = slug,
                CompanyUrl = companyUrl,
                AdminEmail = request.AdminEmail,
                WelcomeEmailSent = emailSent
            };
        }

        private async Task<string> GenerateUniqueSlugAsync(
            string companyName)
        {
            var baseSlug = GenerateSlug(companyName);

            var slug = baseSlug;

            var number = 2;

            while (await _companyRepository.SlugExistsAsync(slug))
            {
                slug = $"{baseSlug}-{number}";
                number++;
            }

            return slug;
        }

        private static string GenerateTemporaryPassword()
        {
            return $"Tv@{Guid.NewGuid():N}"[..14];
        }

        private static string GenerateSlug(string companyName)
        {
            var slug = companyName
                .Trim()
                .ToLowerInvariant();

            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");

            return slug.Trim('-');
        }


        public async Task<int> SubmitApplicationAsync(CreateCompanyApplicationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName))
                throw new ArgumentException("Company name is required.");

            if (string.IsNullOrWhiteSpace(request.OwnerName))
                throw new ArgumentException("Owner name is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Business email is required.");

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new ArgumentException("Phone number is required.");

            var alreadyPending = await _companyRepository.HasPendingApplicationAsync(request.Email);

            if (alreadyPending)
            {
                throw new InvalidOperationException("A pending application already exists for this email.");
            }

            return await _companyRepository.CreateAsync(request);
        }
    }
}
