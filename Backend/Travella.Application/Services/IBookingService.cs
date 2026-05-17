using System.Threading.Tasks;
using Travella.Application.DTOs;

namespace Travella.Application.Services
{
    public interface IBookingService
    {
        Task<int> CreateBookingAsync(BookingDto dto);
    }
}

