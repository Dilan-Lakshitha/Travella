using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Travella.API.Models;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Application.Services;

namespace Travella.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthService(IAuthRepository authRepository, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _authRepository = authRepository;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<AuthUserDto> RegisterTravelerAsync(RegisterTravelerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name is required.");
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email is required.");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required.");

            var existing = await _authRepository.GetByEmailAsync(request.Email.Trim());
            if (existing != null)
                throw new InvalidOperationException("Email already exists.");

            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var companyId = request.CompanyId;
            if (companyId is null or <= 0)
            {
                companyId = _configuration.GetValue<int?>("Travella:DefaultTravelerCompanyId");
            }

            if (companyId is null or <= 0)
            {
                throw new ArgumentException("CompanyId is required (set request.companyId or Travella:DefaultTravelerCompanyId in configuration).");
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var userId = await _authRepository.CreateTravelerAsync(
                    request.Name.Trim(),
                    request.Email.Trim(),
                    hash,
                    companyId.Value);
                await _unitOfWork.CommitAsync();

                return new AuthUserDto
                {
                    UserId = userId,
                    Name = request.Name.Trim(),
                    Email = request.Email.Trim(),
                    Role = "TRAVELER",
                    CompanyId = companyId
                };
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<AuthUserDto?> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return null;

            var user = await _authRepository.GetByEmailAsync(request.Email.Trim());
            if (user == null || user.IsDeleted)
                return null;

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
                return null;

            if (!string.IsNullOrWhiteSpace(request.Role) &&
                !user.Role.Equals(request.Role, StringComparison.OrdinalIgnoreCase))
                return null;

            return new AuthUserDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                CompanyId = user.CompanyId,
                IsFirstLogin = user.MustChangePassword
            };
        }

        public async Task ResetPasswordAsync(string email, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.");
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("NewPassword is required.");
            if (newPassword.Length < 8)
                throw new ArgumentException("NewPassword must be at least 8 characters.");

            var user = await _authRepository.GetByEmailAsync(email.Trim());
            if (user == null || user.IsDeleted)
                throw new InvalidOperationException("User not found.");

            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _unitOfWork.BeginAsync();
            try
            {
                var updated = await _authRepository.UpdatePasswordAsync(email.Trim(), hash, mustChangePassword: false);
                if (!updated)
                {
                    throw new InvalidOperationException("Failed to update password.");
                }

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
