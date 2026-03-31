using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Domain.Entities;

namespace Travella.Application.Interfaces
{
    public interface IStaffRepository
    {
        Task<Staff?> GetStaffByIdAsync(int staffId);

        Task<List<Staff>> GetAvailableStaffAsync(DateTime startDate, DateTime endDate, string? role = null);

        Task<bool> IsStaffAvailableAsync(int staffId, DateTime startDate, DateTime endDate);

        Task AssignStaffToItineraryAsync(int itineraryId, int staffId);

        Task LockStaffForItineraryAsync(int itineraryId, int staffId, DateTime startDate, DateTime endDate);
    }
}