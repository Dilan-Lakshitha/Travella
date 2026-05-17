using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Application.DTOs;

namespace Travella.Application.Interfaces
{
    public interface ICalendarRepository
    {
        Task<IReadOnlyList<StaffBookingCalendarItemDto>> GetStaffBookingsAsync(
            int companyId,
            DateOnly startDate,
            DateOnly endDate,
            string? role);

        Task<IReadOnlyList<ItineraryBookingCalendarItemDto>> GetItineraryBookingsAsync(
            int companyId,
            DateOnly startDate,
            DateOnly endDate,
            int? driverId,
            int? guideId);
    }
}
