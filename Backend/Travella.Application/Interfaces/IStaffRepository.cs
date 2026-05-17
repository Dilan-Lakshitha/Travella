using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Domain.Entities;

namespace Travella.Application.Interfaces
{
    public interface IStaffRepository
    {
        Task<Staff?> GetStaffByIdAsync(int staffId);

        Task<List<Staff>> GetAvailableStaffAsync(int companyId, DateOnly startDate, DateOnly endDate, string? role = null);

        Task<List<Staff>> GetDriversAsync(int companyId);

        Task<List<Staff>> GetGuidesAsync(int companyId);

        Task<int> CreateStaffResourceAsync(Staff staffResource);

        Task<bool> IsStaffAvailableAsync(int staffId, DateOnly startDate, DateOnly endDate);

        Task AssignStaffToItineraryAsync(int itineraryId, int staffId);

        Task LockStaffForItineraryAsync(int itineraryId, int staffId, DateOnly startDate, DateOnly endDate);
    }
}