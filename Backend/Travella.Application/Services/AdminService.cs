using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;

namespace Travella.Application.Services
{
    public interface IAdminService
    {
        Task<(string Email, string TemporaryPassword)> CreateStaffUserAsync(string name, string email, int companyId);
        Task<int> CreateDriverAsync(string name, string phone, int experience, string availability, string language, string? email, int companyId);
        Task<int> CreateGuideAsync(string name, string phone, int experience, string availability, string language, string? email, int companyId);
        Task<List<(int UserId, string Name, string Email)>> GetCompanyStaffUsersAsync(int companyId);
    }

    public class AdminService : IAdminService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IStaffService _staffService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStaffEmailNotifier _staffEmailNotifier;

        public AdminService(
            IAuthRepository authRepository,
            IStaffService staffService,
            IUnitOfWork unitOfWork,
            IStaffEmailNotifier staffEmailNotifier)
        {
            _authRepository = authRepository;
            _staffService = staffService;
            _unitOfWork = unitOfWork;
            _staffEmailNotifier = staffEmailNotifier;
        }

        public async Task<(string Email, string TemporaryPassword)> CreateStaffUserAsync(string name, string email, int companyId)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
            if (companyId <= 0) throw new ArgumentException("CompanyId is required.");

            var existing = await _authRepository.GetByEmailAsync(email.Trim());
            if (existing != null) throw new InvalidOperationException("Email already exists.");

            var tempPassword = GenerateTemporaryPassword();
            var hash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            await _unitOfWork.BeginAsync();
            try
            {
                _ = await _authRepository.CreateStaffUserAsync(name.Trim(), email.Trim(), companyId, hash, mustChangePassword: true);
                await _unitOfWork.CommitAsync();
                return (email.Trim(), tempPassword);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<int> CreateDriverAsync(string name, string phone, int experience, string availability, string language, string? email, int companyId)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("Language is required for drivers.");
            }

            var staff = new Staff
            {
                Name = name.Trim(),
                Role = "DRIVER",
                Phone = phone,
                Experience = experience,
                Availability = availability,
                Language = language.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            };
             await _unitOfWork.BeginAsync();
            try
            {
                var id = await _staffService.CreateStaffResourceAsync(staff, companyId);
                await _unitOfWork.CommitAsync();
                staff.Id = id;
                await _staffEmailNotifier.NotifyDriverCreatedAsync(staff, companyId);
                return id;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<int> CreateGuideAsync(string name, string phone, int experience, string availability, string language, string? email, int companyId)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("Language is required for guides.");
            }

            var staff = new Staff
            {
                Name = name.Trim(),
                Role = "GUIDE",
                Phone = phone,
                Experience = experience,
                Availability = availability,
                Language = language.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            };
            await _unitOfWork.BeginAsync();
            try
            {
                var id = await _staffService.CreateStaffResourceAsync(staff, companyId);
                await _unitOfWork.CommitAsync();
                staff.Id = id;
                await _staffEmailNotifier.NotifyGuideCreatedAsync(staff, companyId);
                return id;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public Task<List<(int UserId, string Name, string Email)>> GetCompanyStaffUsersAsync(int companyId)
            => _authRepository.GetCompanyStaffUsersAsync(companyId);

        private static string GenerateTemporaryPassword()
        {
            // 12 chars: uppercase + lowercase + digits; URL-safe-ish
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            Span<byte> bytes = stackalloc byte[12];
            RandomNumberGenerator.Fill(bytes);
            var chars = new char[12];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = alphabet[bytes[i] % alphabet.Length];
            }
            return new string(chars);
        }
    }
}

