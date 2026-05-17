using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;
using Travella.Domain.Interface;
using IUnitOfWork = Travella.Application.Interfaces.IUnitOfWork;

namespace Travella.Application.Services
{
    public class StaffService : IStaffService
    {
        private readonly IStaffRepository _staffRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StaffService(IStaffRepository staffRepository, IUnitOfWork unitOfWork)
        {
            _staffRepository = staffRepository;
            _unitOfWork = unitOfWork;
        }

        public Task<List<Staff>> GetAvailableStaffAsync(int companyId, DateOnly startDate, DateOnly endDate, string? role = null)
        {
            if (endDate < startDate)
            {
                throw new ArgumentException("EndDate must be on or after StartDate.");
            }

            return _staffRepository.GetAvailableStaffAsync(companyId, startDate, endDate, role);
        }

        public Task<List<Staff>> GetDriversAsync(int companyId)
            => _staffRepository.GetDriversAsync(companyId);

        public Task<List<Staff>> GetGuidesAsync(int companyId)
            => _staffRepository.GetGuidesAsync(companyId);

        public async Task<int> CreateStaffResourceAsync(Staff staffResource, int companyId)
        {

                if (string.IsNullOrWhiteSpace(staffResource.Name))
                    throw new ArgumentException("Name is required.");

                if (!string.Equals(staffResource.Role, "DRIVER", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(staffResource.Role, "GUIDE", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Role must be DRIVER or GUIDE.");
                }

                if (string.IsNullOrWhiteSpace(staffResource.Language))
                {
                    throw new ArgumentException("Language is required for drivers and guides.");
                }

                staffResource.CompanyId = companyId;
                staffResource.Availability = string.IsNullOrWhiteSpace(staffResource.Availability)
                    ? "AVAILABLE"
                    : staffResource.Availability.ToUpperInvariant();

                var id = await _staffRepository.CreateStaffResourceAsync(staffResource);
                return id;
        }
    }
}