using System.Threading.Tasks;
using Travella.Domain.Entities;

namespace Travella.Application.Interfaces
{
    public interface IBookingRepository
    {
        Task<Booking?> GetBookingByItineraryIdAsync(int itineraryId);

        Task<int> CreateBookingAsync(Booking booking);
    }
}
