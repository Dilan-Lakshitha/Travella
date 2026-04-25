using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Domain.Entities;

namespace Travella.Application.Services
{
    public interface IStaffService
    {
        Task<List<Staff>> GetAvailableStaffAsync(int companyId, DateOnly startDate, DateOnly endDate, string? role = null);

        Task<List<Staff>> GetDriversAsync(int companyId);

        Task<List<Staff>> GetGuidesAsync(int companyId);

        Task<int> CreateStaffResourceAsync(Staff staffResource, int companyId);
    }
}

