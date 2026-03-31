using BCrypt.Net;
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

        public AuthService(IAuthRepository authRepository, IUnitOfWork unitOfWork)
        {
            _authRepository = authRepository;
            _unitOfWork = unitOfWork;
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

            await _unitOfWork.BeginAsync();
            try
            {
                var userId = await _authRepository.CreateTravelerAsync(request.Name.Trim(), request.Email.Trim(), hash);
                await _unitOfWork.CommitAsync();

                return new AuthUserDto
                {
                    UserId = userId,
                    Name = request.Name.Trim(),
                    Email = request.Email.Trim(),
                    Role = "TRAVELER",
                    CompanyId = null
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
                CompanyId = user.CompanyId
            };
        }
    }
}
