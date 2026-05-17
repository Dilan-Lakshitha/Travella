using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly ICalendarRepository _calendarRepository;

        public CalendarService(ICalendarRepository calendarRepository)
        {
            _calendarRepository = calendarRepository;
        }

        public Task<IReadOnlyList<StaffBookingCalendarItemDto>> GetStaffBookingsAsync(
            int companyId,
            DateOnly startDate,
            DateOnly endDate,
            string? role)
        {
            if (companyId <= 0)
            {
                throw new ArgumentException("CompanyId is required.");
            }

            if (endDate < startDate)
            {
                throw new ArgumentException("EndDate must be on or after StartDate.");
            }

            var normalizedRole = string.IsNullOrWhiteSpace(role)
                ? null
                : role.Trim().ToUpperInvariant();

            if (normalizedRole is not (null or "DRIVER" or "GUIDE"))
            {
                throw new ArgumentException("Role must be DRIVER or GUIDE.");
            }

            return _calendarRepository.GetStaffBookingsAsync(companyId, startDate, endDate, normalizedRole);
        }

        public Task<IReadOnlyList<ItineraryBookingCalendarItemDto>> GetItineraryBookingsAsync(
            int companyId,
            DateOnly? date,
            DateOnly startDate,
            DateOnly endDate,
            int? driverId,
            int? guideId)
        {
            if (companyId <= 0)
            {
                throw new ArgumentException("CompanyId is required.");
            }

            if (date.HasValue)
            {
                startDate = date.Value;
                endDate = date.Value;
            }

            if (endDate < startDate)
            {
                throw new ArgumentException("EndDate must be on or after StartDate.");
            }

            return _calendarRepository.GetItineraryBookingsAsync(companyId, startDate, endDate, driverId, guideId);
        }
    }
}
