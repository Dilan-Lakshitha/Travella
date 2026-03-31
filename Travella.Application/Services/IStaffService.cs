using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Domain.Entities;

namespace Travella.Application.Services
{
    public interface IStaffService
    {
        Task<List<Staff>> GetAvailableStaffAsync(DateTime startDate, DateTime endDate, string? role = null);
    }
}

