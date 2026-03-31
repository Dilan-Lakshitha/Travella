using System;
using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;

namespace Travella.Application.Services
{
    public class ItineraryService : IItineraryService
    {
        private readonly IItineraryRepository _itineraryRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ItineraryService(
            IItineraryRepository itineraryRepository,
            IStaffRepository staffRepository,
            IUnitOfWork unitOfWork)
        {
            _itineraryRepository = itineraryRepository;
            _staffRepository = staffRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateItineraryAsync(CreateItineraryDto dto)
        {
            if (dto.EndDate.Date < dto.StartDate.Date)
            {
                throw new ArgumentException("EndDate must be on or after StartDate.");
            }

            var itinerary = new Itinerary
            {
                GuestId = dto.GuestId,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                Status = "Draft",
                TotalPrice = 0m
            };

            return await _itineraryRepository.CreateItineraryAsync(itinerary);
        }

        public Task<ItineraryResponseDto?> GetItineraryAsync(int itineraryId)
        {
            return _itineraryRepository.GetItineraryDetailsAsync(itineraryId);
        }

        public async Task AddDayAsync(AddItineraryDayDto dto)
        {
            var exists = await _itineraryRepository.ItineraryExistsAsync(dto.ItineraryId);
            if (!exists)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (dto.DayNumber <= 0)
            {
                throw new ArgumentException("DayNumber must be >= 1.");
            }

            var day = new ItineraryDay
            {
                ItineraryId = dto.ItineraryId,
                DayNumber = dto.DayNumber,
                OvernightLocation = dto.OvernightLocation
            };

            await _itineraryRepository.AddItineraryDayAsync(day);
        }

        public async Task AddAttractionAsync(AddAttractionDto dto)
        {
            if (dto.DurationHours <= 0)
            {
                throw new ArgumentException("DurationHours must be > 0.");
            }

            var day = await _itineraryRepository.GetItineraryDayByIdAsync(dto.ItineraryDayId);
            if (day == null)
            {
                throw new InvalidOperationException("Itinerary day not found.");
            }

            var itineraryAttraction = new ItineraryAttraction
            {
                ItineraryDayId = dto.ItineraryDayId,
                AttractionId = dto.AttractionId,
                Description = dto.Description,
                DurationHours = dto.DurationHours
            };

            await _itineraryRepository.AddAttractionToDayAsync(itineraryAttraction);
        }

        public async Task AssignAccommodationAsync(AssignAccommodationDto dto)
        {
            var day = await _itineraryRepository.GetItineraryDayByIdAsync(dto.ItineraryDayId);
            if (day == null)
            {
                throw new InvalidOperationException("Itinerary day not found.");
            }

            var itineraryAccommodation = new ItineraryAccommodation
            {
                ItineraryDayId = dto.ItineraryDayId,
                AccommodationId = dto.AccommodationId,
                MealPlanId = dto.MealPlanId
            };

            await _itineraryRepository.AssignAccommodationAsync(itineraryAccommodation);

            var totalPrice = await _itineraryRepository.CalculateTotalPriceAsync(day.ItineraryId);
            await _itineraryRepository.UpdateItineraryTotalPriceAsync(day.ItineraryId, totalPrice);
        }

        public async Task AssignStaffAsync(AssignStaffDto dto)
        {
            if (dto.EndDate.Date < dto.StartDate.Date)
            {
                throw new ArgumentException("EndDate must be on or after StartDate.");
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var itinerary = await _itineraryRepository.GetItineraryByIdAsync(dto.ItineraryId);
                if (itinerary == null)
                {
                    throw new InvalidOperationException("Itinerary not found.");
                }

                var isAvailable = await _staffRepository.IsStaffAvailableAsync(
                    dto.StaffId,
                    dto.StartDate.Date,
                    dto.EndDate.Date);

                if (!isAvailable)
                {
                    throw new InvalidOperationException("Staff is not available for the selected dates.");
                }

                if (!string.IsNullOrWhiteSpace(dto.RequiredRole))
                {
                    var staff = await _staffRepository.GetStaffByIdAsync(dto.StaffId);
                    if (staff == null)
                    {
                        throw new InvalidOperationException("Staff not found.");
                    }

                    if (!string.Equals(staff.Role, dto.RequiredRole, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Staff does not match the required role.");
                    }
                }

                await _staffRepository.AssignStaffToItineraryAsync(dto.ItineraryId, dto.StaffId);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}