using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IPricingRepository _pricingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ItineraryService(
            IItineraryRepository itineraryRepository,
            IStaffRepository staffRepository,
            IPricingRepository pricingRepository,
            IUnitOfWork unitOfWork)
        {
            _itineraryRepository = itineraryRepository;
            _staffRepository = staffRepository;
            _pricingRepository = pricingRepository;
            _unitOfWork = unitOfWork;
        }

        public Task<SaveGoogleAttractionResponseDto> SaveGoogleAttractionAsync(SaveGoogleAttractionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PlaceId))
            {
                throw new ArgumentException("PlaceId is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Name is required.");
            }

            return _itineraryRepository.GetOrCreateGoogleAttractionAsync(dto);
        }

        public async Task<int> CreateItineraryAsync(ItineraryDraftUpsertDto dto, int guestId, int companyId)
        {
            if (companyId <= 0)
            {
                throw new ArgumentException("CompanyId is required.");
            }

            if (dto.EndDate < dto.StartDate)
            {
                throw new ArgumentException("EndDate must be on or after StartDate.");
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var itinerary = new Itinerary
                {
                    GuestId = guestId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = "draft",
                    TotalPrice = 0m,
                    CompanyId = companyId,
                };

                var id = await _itineraryRepository.CreateItineraryAsync(itinerary);
                await WriteDraftDaysWithinTransactionAsync(id, dto, deleteExisting: false);
                await _unitOfWork.CommitAsync();
                return id;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task SaveItineraryDraftAsync(int itineraryId, ItineraryDraftUpsertDto dto, int travelerId)
        {
            if (dto.EndDate < dto.StartDate)
            {
                throw new ArgumentException("EndDate must be on or after StartDate.");
            }

            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.GuestId != travelerId)
            {
                throw new InvalidOperationException("You can only modify your own itinerary.");
            }

            if (!await CanTravelerEditItineraryAsync(itinerary))
            {
                throw new InvalidOperationException("You can only save changes while the itinerary is a draft or staff requested changes.");
            }

            await _unitOfWork.BeginAsync();
            try
            {
                await WriteDraftDaysWithinTransactionAsync(itineraryId, dto, deleteExisting: true);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<ItineraryFullResponseDto?> GetItineraryFullAsync(int itineraryId, int userId, string role, int? companyId)
        {
            var full = await _itineraryRepository.GetItineraryFullAsync(itineraryId);
            if (full == null)
            {
                return null;
            }

            var roleUpper = (role ?? string.Empty).ToUpperInvariant();
            if (string.Equals(roleUpper, "TRAVELER", StringComparison.Ordinal))
            {
                if (full.Itinerary.GuestId != userId)
                {
                    return null;
                }

                return full;
            }

            if (string.Equals(roleUpper, "STAFF", StringComparison.Ordinal) ||
                string.Equals(roleUpper, "ADMIN", StringComparison.Ordinal))
            {
                if (!companyId.HasValue || companyId.Value <= 0)
                {
                    return null;
                }

                if (full.Itinerary.CompanyId != companyId.Value)
                {
                    return null;
                }

                return full;
            }

            return null;
        }

        public Task<List<ItineraryListItemDto>> GetOwnerSubmittedItinerariesAsync()
            => _itineraryRepository.GetAllOwnerSubmittedItinerariesAsync();

        private async Task<bool> CanTravelerEditItineraryAsync(Itinerary itinerary)
        {
            if (string.Equals(itinerary.Status, "draft", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(itinerary.Status, "under_review", StringComparison.OrdinalIgnoreCase))
            {
                var latestReview = await _itineraryRepository.GetLatestReviewStatusAsync(itinerary.Id);
                return string.Equals(latestReview, "REQUESTED_CHANGES", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(itinerary.Status, "returned_for_correction", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private async Task WriteDraftDaysWithinTransactionAsync(int itineraryId, ItineraryDraftUpsertDto dto, bool deleteExisting)
        {
            if (deleteExisting)
            {
                await _itineraryRepository.DeleteItineraryNestedContentAsync(itineraryId);
            }

            await _itineraryRepository.UpdateItineraryDatesAsync(itineraryId, dto.StartDate, dto.EndDate);

            foreach (var dayDto in dto.Days.OrderBy(d => d.DayNumber))
            {
                var day = new ItineraryDay
                {
                    ItineraryId = itineraryId,
                    DayNumber = dayDto.DayNumber,
                    OvernightLocation = dayDto.OvernightLocation ?? string.Empty,
                };

                var dayId = await _itineraryRepository.AddItineraryDayAsync(day);

                foreach (var att in dayDto.Attractions)
                {
                    if (att.AttractionId <= 0)
                    {
                        continue;
                    }

                    var hours = att.DurationHours <= 0 ? 2m : att.DurationHours;
                    await _itineraryRepository.AddAttractionToDayAsync(new ItineraryAttraction
                    {
                        ItineraryDayId = dayId,
                        AttractionId = att.AttractionId,
                        Description = att.Description,
                        DurationHours = hours,
                    });
                }

                var mealPlanId = await _itineraryRepository.EnsureMealPlanIdAsync(dayDto.MealPlanCode);
                var accommodationId = await _itineraryRepository.EnsureAccommodationIdAsync(dayDto.AccommodationType);
                await _itineraryRepository.AssignAccommodationAsync(new ItineraryAccommodation
                {
                    ItineraryDayId = dayId,
                    AccommodationId = accommodationId,
                    MealPlanId = mealPlanId,
                });
            }

            var totalPrice = await _itineraryRepository.CalculateTotalPriceAsync(itineraryId);
            await _itineraryRepository.UpdateItineraryTotalPriceAsync(itineraryId, totalPrice);
        }

        public async Task<int> AddDayAsync(AddItineraryDayDto dto, int travelerId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(dto.ItineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.GuestId != travelerId)
            {
                throw new InvalidOperationException("You can only modify your own itinerary.");
            }

            if (!await CanTravelerEditItineraryAsync(itinerary))
            {
                throw new InvalidOperationException("You can only add days to draft or requested-changes itineraries.");
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

            await _unitOfWork.BeginAsync();
            try
            {
                var id = await _itineraryRepository.AddItineraryDayAsync(day);
                await _unitOfWork.CommitAsync();
                return id;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task AddAttractionAsync(AddAttractionDto dto, int travelerId)
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

            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(day.ItineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.GuestId != travelerId)
            {
                throw new InvalidOperationException("You can only modify your own itinerary.");
            }

            if (!await CanTravelerEditItineraryAsync(itinerary))
            {
                throw new InvalidOperationException("You can only add attractions to draft or requested-changes itineraries.");
            }

            var itineraryAttraction = new ItineraryAttraction
            {
                ItineraryDayId = dto.ItineraryDayId,
                AttractionId = dto.AttractionId,
                Description = dto.Description,
                DurationHours = dto.DurationHours
            };

            await _unitOfWork.BeginAsync();
            try
            {
                await _itineraryRepository.AddAttractionToDayAsync(itineraryAttraction);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
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

            await _unitOfWork.BeginAsync();
            try
            {
                await _itineraryRepository.AssignAccommodationAsync(itineraryAccommodation);

                var totalPrice = await _itineraryRepository.CalculateTotalPriceAsync(day.ItineraryId);
                await _itineraryRepository.UpdateItineraryTotalPriceAsync(day.ItineraryId, totalPrice);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task AssignStaffAsync(AssignStaffDto dto, int companyId)
        {
            if (dto.EndDate < dto.StartDate)
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

                if (itinerary.CompanyId != companyId)
                {
                    throw new InvalidOperationException("You can only assign staff within your company.");
                }

                if (!string.Equals(itinerary.Status, "priced", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Staff assignment is only allowed for priced itineraries.");
                }

                var isAvailable = await _staffRepository.IsStaffAvailableAsync(
                    dto.StaffId,
                    dto.StartDate,
                    dto.EndDate);

                if (!isAvailable)
                {
                    throw new InvalidOperationException("Staff is not available for the selected dates.");
                }

                var staff = await _staffRepository.GetStaffByIdAsync(dto.StaffId);
                if (staff == null)
                {
                    throw new InvalidOperationException("Staff not found.");
                }

                if (staff.CompanyId != companyId)
                {
                    throw new InvalidOperationException("You can only assign staff from within your company.");
                }

                if (!string.IsNullOrWhiteSpace(dto.RequiredRole))
                {
                    if (!string.Equals(staff.Role, dto.RequiredRole, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Staff does not match the required role.");
                    }
                }

                await _staffRepository.AssignStaffToItineraryAsync(dto.ItineraryId, dto.StaffId);
                await _staffRepository.LockStaffForItineraryAsync(
                    dto.ItineraryId,
                    dto.StaffId,
                    dto.StartDate,
                    dto.EndDate);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public Task<List<ItineraryListItemDto>> GetGuestItinerariesAsync(int guestId)
            => _itineraryRepository.GetGuestItinerariesAsync(guestId);

        public Task<List<ItineraryListItemDto>> GetSubmittedItinerariesAsync(int companyId)
            => _itineraryRepository.GetSubmittedItinerariesAsync(companyId);

        public Task<List<ItineraryListItemDto>> GetCompanyItinerariesAsync(int companyId)
            => _itineraryRepository.GetCompanyItinerariesAsync(companyId);

        public async Task SubmitItineraryAsync(int itineraryId, int travelerId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.GuestId != travelerId)
            {
                throw new InvalidOperationException("You can only submit your own itinerary.");
            }

            var isDraft = string.Equals(itinerary.Status, "draft", StringComparison.OrdinalIgnoreCase);
            var isReturned = string.Equals(itinerary.Status, "returned_for_correction", StringComparison.OrdinalIgnoreCase);
            if (!isDraft && !isReturned)
            {
                throw new InvalidOperationException("Only draft or returned itineraries can be submitted.");
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var dayCount = await _itineraryRepository.CountItineraryDaysAsync(itineraryId);
                if (dayCount < 1)
                {
                    throw new InvalidOperationException("Add at least one day before submitting your itinerary.");
                }

                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, isReturned ? "resubmitted" : "submitted");
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task MarkUnderReviewAsync(int itineraryId, int companyId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.CompanyId != companyId)
            {
                throw new InvalidOperationException("You can only review itineraries within your company.");
            }

            if (!string.Equals(itinerary.Status, "submitted", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(itinerary.Status, "resubmitted", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "under_review");
        }

        public async Task DeleteDraftItineraryAsync(int itineraryId, int travelerId)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                await _itineraryRepository.DeleteDraftItineraryAsync(itineraryId, travelerId);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task AssignDriverGuideAsync(int itineraryId, int driverId, int guideId, int companyId)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
                if (itinerary == null)
                {
                    throw new InvalidOperationException("Itinerary not found.");
                }

                if (itinerary.CompanyId != companyId)
                {
                    throw new InvalidOperationException("You can only assign staff within your company.");
                }

                if (!string.Equals(itinerary.Status, "priced", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Driver/guide assignment is only allowed for priced itineraries.");
                }

                var start = itinerary.StartDate;
                var end = itinerary.EndDate;

                var driver = await _staffRepository.GetStaffByIdAsync(driverId);
                var guide = await _staffRepository.GetStaffByIdAsync(guideId);
                if (driver == null || guide == null)
                {
                    throw new InvalidOperationException("Driver/guide not found.");
                }

                if (driver.CompanyId != companyId || guide.CompanyId != companyId)
                {
                    throw new InvalidOperationException("Driver/guide must belong to your company.");
                }

                if (!string.Equals(driver.Role, "DRIVER", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Selected driver is not of type DRIVER.");
                }

                if (!string.Equals(guide.Role, "GUIDE", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Selected guide is not of type GUIDE.");
                }

                if (!await _staffRepository.IsStaffAvailableAsync(driverId, start, end))
                {
                    throw new InvalidOperationException("Driver is already BOOKED for the selected date range.");
                }

                if (!await _staffRepository.IsStaffAvailableAsync(guideId, start, end))
                {
                    throw new InvalidOperationException("Guide is already BOOKED for the selected date range.");
                }

                await _staffRepository.AssignStaffToItineraryAsync(itineraryId, driverId);
                await _staffRepository.LockStaffForItineraryAsync(itineraryId, driverId, start, end);

                await _staffRepository.AssignStaffToItineraryAsync(itineraryId, guideId);
                await _staffRepository.LockStaffForItineraryAsync(itineraryId, guideId, start, end);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task ApproveItineraryAsync(int itineraryId, string approverRole, int companyId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.CompanyId != companyId)
            {
                throw new InvalidOperationException("You can only approve itineraries within your company.");
            }

            if (string.Equals(approverRole, "STAFF", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(itinerary.Status, "submitted", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(itinerary.Status, "under_review", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(itinerary.Status, "resubmitted", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Only submitted itineraries can enter staff review.");
                }

                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "approved_by_staff");
                return;
            }

            if (string.Equals(approverRole, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(itinerary.Status, "sent_to_admin", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Only sent_to_admin itineraries can be approved by admin.");
                }

                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "approved_by_admin");
                return;
            }

            throw new InvalidOperationException("Unsupported approver role.");
        }

        public async Task ConfirmItineraryAsync(int itineraryId, int companyId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.CompanyId != companyId)
            {
                throw new InvalidOperationException("You can only confirm itineraries within your company.");
            }

            if (!string.Equals(itinerary.Status, "approved_by_admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only approved_by_admin itineraries can be confirmed.");
            }

            var hasPricing = await _pricingRepository.PricingExistsAsync(itineraryId);
            if (!hasPricing)
            {
                throw new InvalidOperationException("Cannot confirm without pricing details.");
            }

            await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "confirmed");
        }

        public async Task RejectItineraryAsync(int itineraryId, int companyId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.CompanyId != companyId)
            {
                throw new InvalidOperationException("You can only reject itineraries within your company.");
            }

            if (string.Equals(itinerary.Status, "rejected", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Itinerary is already rejected.");
            }

            await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "rejected");
        }

        public async Task SendToAdminAsync(int itineraryId, int companyId)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
                if (itinerary == null)
                {
                    throw new InvalidOperationException("Itinerary not found.");
                }

                if (itinerary.CompanyId != companyId)
                {
                    throw new InvalidOperationException("You can only send itineraries within your company.");
                }

                if (!string.Equals(itinerary.Status, "priced", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Itinerary must be priced before sending to admin.");
                }

                var hasPricing = await _pricingRepository.PricingExistsAsync(itineraryId);
                if (!hasPricing)
                {
                    throw new InvalidOperationException("Cannot send to admin without pricing details.");
                }

                var assigned = await _itineraryRepository.GetItineraryStaffAsync(itineraryId);
                if (assigned.Count < 2)
                {
                    throw new InvalidOperationException("Assign both driver and guide before sending to owner.");
                }

                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "sent_to_admin");

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ItineraryMessageDto>> GetItineraryMessagesAsync(int itineraryId, int userId, string role, int? companyId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            var normalizedRole = (role ?? string.Empty).ToUpperInvariant();
            if (normalizedRole == "TRAVELER")
            {
                if (itinerary.GuestId != userId) throw new InvalidOperationException("Forbidden.");
            }
            else
            {
                if (!companyId.HasValue || itinerary.CompanyId != companyId.Value) throw new InvalidOperationException("Forbidden.");
            }

            return await _itineraryRepository.GetItineraryMessagesAsync(itineraryId);
        }

        public async Task<int> AddItineraryMessageAsync(int itineraryId, int senderId, string senderRole, int? companyId, AddItineraryMessageDto dto)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null) throw new InvalidOperationException("Itinerary not found.");

            var role = (senderRole ?? string.Empty).ToUpperInvariant();
            if (role == "TRAVELER")
            {
                if (itinerary.GuestId != senderId) throw new InvalidOperationException("Forbidden.");
            }
            else
            {
                if (!companyId.HasValue || itinerary.CompanyId != companyId.Value) throw new InvalidOperationException("Forbidden.");
            }

            var type = string.IsNullOrWhiteSpace(dto.Type) ? "COMMENT" : dto.Type.Trim().ToUpperInvariant();
            return await _itineraryRepository.AddItineraryMessageAsync(itineraryId, senderId, role, dto.Message, type);
        }

        public async Task RequestCorrectionAsync(int itineraryId, int senderId, string senderRole, int companyId, string message)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
                if (itinerary == null) throw new InvalidOperationException("Itinerary not found.");
                if (itinerary.CompanyId != companyId) throw new InvalidOperationException("You can only review itineraries within your company.");
                if (!string.Equals(itinerary.Status, "under_review", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Request changes is only allowed while itinerary is under_review.");
                }

                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "returned_for_correction");
                await _itineraryRepository.AddItineraryMessageAsync(
                    itineraryId,
                    senderId,
                    senderRole.ToUpperInvariant(),
                    string.IsNullOrWhiteSpace(message) ? "Please update your itinerary based on staff feedback." : message,
                    "REQUEST_CHANGE");
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