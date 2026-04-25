using System;
using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;

namespace Travella.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IItineraryRepository _itineraryRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(
            IItineraryRepository itineraryRepository,
            IBookingRepository bookingRepository,
            IStaffRepository staffRepository,
            IUnitOfWork unitOfWork)
        {
            _itineraryRepository = itineraryRepository;
            _bookingRepository = bookingRepository;
            _staffRepository = staffRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateBookingAsync(BookingDto dto)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                var itinerary = await _itineraryRepository.GetItineraryByIdAsync(dto.ItineraryId);
                if (itinerary == null)
                {
                    throw new InvalidOperationException("Itinerary not found.");
                }

                if (string.Equals(itinerary.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Itinerary is already confirmed.");
                }

                var existingBooking = await _bookingRepository.GetBookingByItineraryIdAsync(dto.ItineraryId);
                if (existingBooking != null)
                {
                    throw new InvalidOperationException("Itinerary is already booked.");
                }

                var totalPrice = await _itineraryRepository.CalculateTotalPriceAsync(dto.ItineraryId);
                await _itineraryRepository.UpdateItineraryTotalPriceAsync(dto.ItineraryId, totalPrice);

                var itineraryStaff = await _itineraryRepository.GetItineraryStaffAsync(dto.ItineraryId);
                foreach (var staffAssignment in itineraryStaff)
                {
                    var isAvailable = await _staffRepository.IsStaffAvailableAsync(
                        staffAssignment.StaffId,
                        itinerary.StartDate,
                        itinerary.EndDate);

                    if (!isAvailable)
                    {
                        throw new InvalidOperationException($"Staff member {staffAssignment.StaffId} is no longer available.");
                    }

                    await _staffRepository.LockStaffForItineraryAsync(
                        dto.ItineraryId,
                        staffAssignment.StaffId,
                        itinerary.StartDate,
                        itinerary.EndDate);
                }

                var booking = new Booking
                {
                    ItineraryId = dto.ItineraryId,
                    ConfirmedAt = DateTime.UtcNow,
                    InvoiceNumber = GenerateInvoiceNumber(dto.ItineraryId)
                };

                var bookingId = await _bookingRepository.CreateBookingAsync(booking);

                await _itineraryRepository.UpdateItineraryStatusAsync(dto.ItineraryId, "Confirmed");

                await _unitOfWork.CommitAsync();
                return bookingId;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private static string GenerateInvoiceNumber(int itineraryId)
        {
            return $"INV-{itineraryId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}