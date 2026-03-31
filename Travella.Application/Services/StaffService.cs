using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;

namespace Travella.Application.Services
{
    public class StaffService : IStaffService
    {
        private readonly IStaffRepository _staffRepository;

        public StaffService(IStaffRepository staffRepository)
        {
            _staffRepository = staffRepository;
        }

        public Task<List<Staff>> GetAvailableStaffAsync(DateTime startDate, DateTime endDate, string? role = null)
        {
            if (endDate.Date < startDate.Date)
            {
                throw new ArgumentException("EndDate must be on or after StartDate.");
            }

            return _staffRepository.GetAvailableStaffAsync(startDate.Date, endDate.Date, role);
        }
    }
}