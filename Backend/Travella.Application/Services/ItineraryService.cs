using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Application.Enums;
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
        private readonly IItineraryChatNotifier _chatNotifier;
        private readonly INotificationService _notificationService;
        private readonly IStaffEmailNotifier _staffEmailNotifier;
        private readonly IItineraryEmailNotifier _itineraryEmailNotifier;

        public ItineraryService(
            IItineraryRepository itineraryRepository,
            IStaffRepository staffRepository,
            IPricingRepository pricingRepository,
            IUnitOfWork unitOfWork,
            IItineraryChatNotifier chatNotifier,
            INotificationService notificationService,
            IStaffEmailNotifier staffEmailNotifier,
            IItineraryEmailNotifier itineraryEmailNotifier)
        {
            _itineraryRepository = itineraryRepository;
            _staffRepository = staffRepository;
            _pricingRepository = pricingRepository;
            _unitOfWork = unitOfWork;
            _chatNotifier = chatNotifier;
            _notificationService = notificationService;
            _staffEmailNotifier = staffEmailNotifier;
            _itineraryEmailNotifier = itineraryEmailNotifier;
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
                await _notificationService.NotifyItineraryCreatedAsync(id, companyId);
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

                full.Pricing = await _pricingRepository.GetLatestByItineraryIdAsync(itineraryId);
                if (full.Pricing != null)
                {
                    full.Itinerary.TotalPrice = full.Pricing.TotalAmount;
                }

                return full;
            }

            return null;
        }

        public Task<List<ItineraryListItemDto>> GetOwnerSubmittedItinerariesAsync()
        {
            return _itineraryRepository.GetAllOwnerSubmittedItinerariesAsync();
        }

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
        {
            return _itineraryRepository.GetGuestItinerariesAsync(guestId);
        }

        public Task<List<ItineraryListItemDto>> GetSubmittedItinerariesAsync(int companyId)
        {
            return _itineraryRepository.GetSubmittedItinerariesAsync(companyId);
        }

        public async Task<List<ItineraryListItemDto>> GetStaffItinerariesByTabAsync(int companyId, string tab)
        {
            var items = await _itineraryRepository.GetStaffItinerariesByTabAsync(companyId, tab);
            await AttachPricingAsync(items);
            return items;
        }

        public async Task<List<ItineraryListItemDto>> GetCompanyItinerariesAsync(int companyId)
        {
            var items = await _itineraryRepository.GetCompanyItinerariesAsync(companyId);
            await AttachPricingAsync(items);
            return items;
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync(int companyId)
        {
            if (companyId <= 0)
            {
                throw new ArgumentException("Company id is required.", nameof(companyId));
            }

            var items = await _itineraryRepository.GetCompanyItinerariesAllAsync(companyId);
            await AttachPricingAsync(items);
            var dbCounts = await _itineraryRepository.GetCompanyItineraryStatusCountsAsync(companyId);

            var statusCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var status in AdminItineraryTabHelper.WorkflowStatuses)
            {
                statusCounts[status] = dbCounts.TryGetValue(status, out var count) ? count : 0;
            }

            foreach (var pair in dbCounts)
            {
                if (!statusCounts.ContainsKey(pair.Key))
                {
                    statusCounts[pair.Key] = pair.Value;
                }
            }

            static List<ItineraryListItemDto> FilterByStatuses(
                IEnumerable<ItineraryListItemDto> source,
                params string[] statuses)
            {
                var set = new HashSet<string>(statuses, StringComparer.OrdinalIgnoreCase);
                return source
                    .Where(i => set.Contains(ItineraryStatusHelper.Normalize(i.RawStatus)))
                    .ToList();
            }

            return new AdminDashboardDto
            {
                TotalItineraries = items.Count,
                PendingReviewCount = statusCounts.GetValueOrDefault("submitted"),
                AwaitingApprovalCount = statusCounts.GetValueOrDefault("sent_to_admin"),
                ConfirmedCount = statusCounts.GetValueOrDefault("confirmed"),
                StatusCounts = statusCounts,
                Sections = new AdminDashboardSectionsDto
                {
                    All = items,
                    PendingReview = FilterByStatuses(items, "submitted"),
                    InReview = FilterByStatuses(items, "under_review"),
                    Returned = FilterByStatuses(items, "returned_for_correction", "resubmitted"),
                    Priced = FilterByStatuses(items, "priced"),
                    AwaitingApproval = FilterByStatuses(items, "sent_to_admin"),
                    Approved = FilterByStatuses(items, "approved_by_admin"),
                    Confirmed = FilterByStatuses(items, "confirmed"),
                    Rejected = FilterByStatuses(items, "rejected"),
                },
            };
        }

        public Task SubmitItineraryAsync(int itineraryId, int travelerId)
        {
            return SubmitOrResubmitItineraryAsync(itineraryId, travelerId, requireReturned: false);
        }

        public Task ResubmitItineraryAsync(int itineraryId, int travelerId)
        {
            return SubmitOrResubmitItineraryAsync(itineraryId, travelerId, requireReturned: true);
        }

        private async Task SubmitOrResubmitItineraryAsync(int itineraryId, int travelerId, bool requireReturned)
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

            var status = ItineraryStatusHelper.Normalize(itinerary.Status);
            var isDraft = status == "draft";
            var isReturned = status == "returned_for_correction";

            if (requireReturned)
            {
                ItineraryStatusTransitions.EnsureTravelerResubmit(itinerary.Status);
            }
            else if (!isDraft && !isReturned)
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

                var nextStatus = isReturned ? "resubmitted" : "submitted";
                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, nextStatus);
                await _unitOfWork.CommitAsync();
                await _notificationService.NotifyItinerarySubmittedAsync(
                    itineraryId,
                    travelerId,
                    itinerary.CompanyId ?? 0,
                    isReturned);
                await NotifyWorkflowEmailAsync(
                    itineraryId,
                    isReturned ? ItineraryEmailEvent.Resubmitted : ItineraryEmailEvent.Submitted);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public Task MarkUnderReviewAsync(int itineraryId, int companyId, int staffUserId)
        {
            return AssignReviewerAsync(itineraryId, companyId, staffUserId);
        }

        public async Task<AssignReviewerResultDto> AssignReviewerAsync(int itineraryId, int companyId, int staffUserId)
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

            var status = ItineraryStatusHelper.Normalize(itinerary.Status);
            if (status is not ("submitted" or "resubmitted" or "under_review"))
            {
                throw new InvalidOperationException("Reviewer can only be assigned while itinerary is submitted, resubmitted, or under review.");
            }

            var existingReviewerId = itinerary.AssignedReviewerId
                ?? await _itineraryRepository.GetAssignedReviewerIdAsync(itineraryId);

            if (existingReviewerId.HasValue && existingReviewerId.Value != staffUserId)
            {
                return new AssignReviewerResultDto
                {
                    ItineraryId = itineraryId,
                    Status = status,
                    AssignedReviewerId = existingReviewerId,
                    IsCurrentUserReviewer = false,
                    ReviewerAssignedByThisRequest = false,
                };
            }

            if (existingReviewerId.HasValue && existingReviewerId.Value == staffUserId)
            {
                if (status == "submitted")
                {
                    await _unitOfWork.BeginAsync();
                    try
                    {
                        await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "under_review");
                        await _unitOfWork.CommitAsync();
                        status = "under_review";
                        await _notificationService.NotifyItineraryUnderReviewAsync(itineraryId, itinerary.GuestId);
                    }
                    catch
                    {
                        await _unitOfWork.RollbackAsync();
                        throw;
                    }
                }

                return new AssignReviewerResultDto
                {
                    ItineraryId = itineraryId,
                    Status = status,
                    AssignedReviewerId = existingReviewerId,
                    IsCurrentUserReviewer = true,
                    ReviewerAssignedByThisRequest = false,
                };
            }

            var reviewerAssignedByThisRequest = false;
            var movedToUnderReview = false;
            await _unitOfWork.BeginAsync();
            try
            {
                if (status == "submitted")
                {
                    await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "under_review");
                    status = "under_review";
                    movedToUnderReview = true;
                }

                reviewerAssignedByThisRequest = await _itineraryRepository.TryAssignReviewerIfUnsetAsync(
                    itineraryId,
                    staffUserId);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            if (movedToUnderReview)
            {
                await _notificationService.NotifyItineraryUnderReviewAsync(itineraryId, itinerary.GuestId);
            }

            var assignedReviewerId = await _itineraryRepository.GetAssignedReviewerIdAsync(itineraryId);
            if (!assignedReviewerId.HasValue)
            {
                assignedReviewerId = staffUserId;
            }

            return new AssignReviewerResultDto
            {
                ItineraryId = itineraryId,
                Status = status,
                AssignedReviewerId = assignedReviewerId,
                IsCurrentUserReviewer = assignedReviewerId == staffUserId,
                ReviewerAssignedByThisRequest = reviewerAssignedByThisRequest,
            };
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
                // Removed: await _staffRepository.LockStaffForItineraryAsync(itineraryId, driverId, start, end);

                await _staffRepository.AssignStaffToItineraryAsync(itineraryId, guideId);
                // Locks are applied when itinerary is confirmed or booked.

                await _unitOfWork.CommitAsync();

                await _staffEmailNotifier.NotifyStaffAssignedAsync(itinerary, driver, guide, companyId);
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
                throw new InvalidOperationException("Only company owners can approve itineraries. Staff may review, price, and return itineraries for correction.");
            }

            if (string.Equals(approverRole, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(itinerary.Status, "sent_to_admin", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Only sent_to_admin itineraries can be approved by admin.");
                }

                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "approved_by_admin");
                await _notificationService.NotifyItineraryApprovedAsync(itineraryId, itinerary.GuestId);
                await NotifyWorkflowEmailAsync(itineraryId, ItineraryEmailEvent.Approved);
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

            await _unitOfWork.BeginAsync();
            try
            {
                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "confirmed");

                // Fetch assigned staff and lock their availability
                var assignedStaff = await _itineraryRepository.GetItineraryStaffAsync(itineraryId);
                foreach (var staff in assignedStaff)
                {
                    await _staffRepository.LockStaffForItineraryAsync(itineraryId, staff.StaffId, itinerary.StartDate, itinerary.EndDate);
                }

                await _unitOfWork.CommitAsync();
                await _notificationService.NotifyItineraryConfirmedAsync(itineraryId, itinerary.GuestId);
                await _staffEmailNotifier.NotifyItineraryConfirmedAsync(itinerary, companyId);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task RejectItineraryAsync(int itineraryId, int companyId, int? actorUserId = null, string? actorRole = null)
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

            if (ItineraryStatusTransitions.IsLockedAfterRejection(itinerary.Status))
            {
                throw new InvalidOperationException("Itinerary is already rejected.");
            }

            var status = ItineraryStatusHelper.Normalize(itinerary.Status);
            if (string.Equals(actorRole, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                if (status is not ("sent_to_admin" or "approved_by_admin"))
                {
                    throw new InvalidOperationException("Admin rejection is only allowed for itineraries awaiting or after owner approval.");
                }
            }
            else
            {
                ItineraryStatusTransitions.EnsureStaffReject(itinerary.Status);
                await EnsureStaffReviewerForActionAsync(itineraryId, itinerary, actorUserId);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "rejected");
                await _unitOfWork.CommitAsync();
                await _notificationService.NotifyItineraryRejectedAsync(itineraryId, itinerary.GuestId);
                await NotifyWorkflowEmailAsync(itineraryId, ItineraryEmailEvent.Rejected);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
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
                await _notificationService.NotifyItinerarySentToAdminAsync(
                    itineraryId,
                    itinerary.GuestId,
                    companyId);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<ItineraryConversationDto> GetItineraryConversationAsync(int itineraryId, int userId, string role, int? companyId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            var normalizedRole = (role ?? string.Empty).ToUpperInvariant();
            var assignedReviewerId = itinerary.AssignedReviewerId
                ?? await _itineraryRepository.GetAssignedReviewerIdAsync(itineraryId);

            (bool canView, bool canSend) permissions;
            if (normalizedRole == "TRAVELER")
            {
                if (itinerary.GuestId != userId)
                {
                    throw new InvalidOperationException("Forbidden.");
                }

                permissions = ItineraryConversationRules.ResolveForTraveler(itinerary.Status, assignedReviewerId);
            }
            else if (normalizedRole is "STAFF" or "ADMIN")
            {
                if (!companyId.HasValue || itinerary.CompanyId != companyId.Value)
                {
                    throw new InvalidOperationException("Forbidden.");
                }

                permissions = ItineraryConversationRules.ResolveForStaff(itinerary.Status, userId, assignedReviewerId);
            }
            else
            {
                throw new InvalidOperationException("Forbidden.");
            }

            if (!permissions.canView)
            {
                throw new InvalidOperationException("Conversation is not available for this itinerary.");
            }

            var messages = await _itineraryRepository.GetItineraryMessagesAsync(itineraryId, includeInternalNotes: false);
            return new ItineraryConversationDto
            {
                Messages = messages,
                AssignedReviewerId = assignedReviewerId,
                CanViewConversation = permissions.canView,
                CanSendMessage = permissions.canSend,
            };
        }

        public async Task<ItineraryMessageDto> AddItineraryMessageAsync(int itineraryId, int senderId, string senderRole, int? companyId, AddItineraryMessageDto dto)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            var role = (senderRole ?? string.Empty).ToUpperInvariant();
            var type = string.IsNullOrWhiteSpace(dto.Type) ? "COMMENT" : dto.Type.Trim().ToUpperInvariant();
            if (type is not ("COMMENT" or "REQUEST_CHANGE"))
            {
                throw new InvalidOperationException("Unsupported message type.");
            }

            var assignedReviewerId = itinerary.AssignedReviewerId
                ?? await _itineraryRepository.GetAssignedReviewerIdAsync(itineraryId);

            (bool canView, bool canSend) permissions;
            if (role == "TRAVELER")
            {
                if (itinerary.GuestId != senderId)
                {
                    throw new InvalidOperationException("Forbidden.");
                }

                permissions = ItineraryConversationRules.ResolveForTraveler(itinerary.Status, assignedReviewerId);
            }
            else if (role is "STAFF" or "ADMIN")
            {
                if (!companyId.HasValue || itinerary.CompanyId != companyId.Value)
                {
                    throw new InvalidOperationException("Forbidden.");
                }

                permissions = ItineraryConversationRules.ResolveForStaff(itinerary.Status, senderId, assignedReviewerId);
            }
            else
            {
                throw new InvalidOperationException("Forbidden.");
            }

            if (!permissions.canView)
            {
                throw new InvalidOperationException("Conversation is not available for this itinerary.");
            }

            if (!permissions.canSend)
            {
                throw new InvalidOperationException("You are not allowed to send messages in this conversation.");
            }

            await _unitOfWork.BeginAsync();
            int messageId;
            try
            {
                messageId = await _itineraryRepository.AddItineraryMessageAsync(itineraryId, senderId, role, dto.Message, type);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var allMessages = await _itineraryRepository.GetItineraryMessagesAsync(itineraryId, includeInternalNotes: false);
            var created = allMessages.FirstOrDefault(m => m.Id == messageId)
                ?? allMessages.LastOrDefault()
                ?? new ItineraryMessageDto
                {
                    Id = messageId,
                    ItineraryId = itineraryId,
                    SenderId = senderId,
                    SenderRole = role,
                    Message = dto.Message,
                    Type = type,
                    CreatedAt = DateTime.UtcNow,
                };

            await _chatNotifier.NotifyMessageAsync(itineraryId, created);
            await _notificationService.NotifyConversationMessageAsync(itineraryId, senderId, role, itinerary);

            return created;
        }

        public Task RequestCorrectionAsync(int itineraryId, int senderId, string senderRole, int companyId, string message)
        {
            return ReturnItineraryForCorrectionAsync(itineraryId, senderId, senderRole, companyId, message);
        }

        public async Task ReturnItineraryForCorrectionAsync(
            int itineraryId,
            int senderId,
            string senderRole,
            int companyId,
            string message)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
                if (itinerary == null) throw new InvalidOperationException("Itinerary not found.");
                if (itinerary.CompanyId != companyId)
                {
                    throw new InvalidOperationException("You can only review itineraries within your company.");
                }

                var status = ItineraryStatusHelper.Normalize(itinerary.Status);
                if (status is not ("under_review" or "resubmitted"))
                {
                    throw new InvalidOperationException(
                        "Return for correction is only allowed while itinerary is under review or resubmitted.");
                }

                // Resubmitted itineraries may still carry a prior assigned reviewer; allow a fresh return.
                if (status == "under_review")
                {
                    await EnsureStaffReviewerForActionAsync(itineraryId, itinerary, senderId);
                }

                await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "returned_for_correction");
                await _itineraryRepository.SetAssignedReviewerAsync(itineraryId, senderId);
                var correctionNotes = string.IsNullOrWhiteSpace(message)
                    ? "Please update your itinerary based on staff feedback."
                    : message;
                var messageId = await _itineraryRepository.AddItineraryMessageAsync(
                    itineraryId,
                    senderId,
                    senderRole.ToUpperInvariant(),
                    correctionNotes,
                    "REQUEST_CHANGE");
                await _unitOfWork.CommitAsync();

                await _notificationService.NotifyItineraryReturnedForCorrectionAsync(
                    itineraryId,
                    itinerary.GuestId,
                    senderId);
                await NotifyWorkflowEmailAsync(
                    itineraryId,
                    ItineraryEmailEvent.ReturnedForCorrection,
                    new ItineraryEmailContext { CorrectionNotes = correctionNotes });

                var travelerMessages = await _itineraryRepository.GetItineraryMessagesAsync(itineraryId, includeInternalNotes: false);
                var created = travelerMessages.FirstOrDefault(m => m.Id == messageId);
                if (created != null)
                {
                    await _chatNotifier.NotifyMessageAsync(itineraryId, created);
                }
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private async Task EnsureStaffReviewerForActionAsync(int itineraryId, Itinerary itinerary, int? staffUserId)
        {
            if (!staffUserId.HasValue || staffUserId.Value <= 0)
            {
                return;
            }

            var status = ItineraryStatusHelper.Normalize(itinerary.Status);
            if (status is not ("under_review" or "resubmitted"))
            {
                return;
            }

            var assignedReviewerId = itinerary.AssignedReviewerId
                ?? await _itineraryRepository.GetAssignedReviewerIdAsync(itineraryId);

            if (!assignedReviewerId.HasValue)
            {
                await _itineraryRepository.SetAssignedReviewerAsync(itineraryId, staffUserId.Value);
                return;
            }

            if (assignedReviewerId.Value != staffUserId.Value)
            {
                throw new InvalidOperationException("Only the assigned reviewer can perform this action.");
            }
        }

        private async Task AttachPricingAsync(List<ItineraryListItemDto> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            var pricingByItinerary = await _pricingRepository.GetLatestByItineraryIdsAsync(
                items.Select(i => i.Id).Distinct().ToList());

            foreach (var item in items)
            {
                if (pricingByItinerary.TryGetValue(item.Id, out var pricing))
                {
                    item.Pricing = pricing;
                    item.TotalPrice = pricing.TotalAmount;
                }
            }
        }

        private async Task NotifyWorkflowEmailAsync(
            int itineraryId,
            ItineraryEmailEvent workflowEvent,
            ItineraryEmailContext? context = null)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                return;
            }

            await _itineraryEmailNotifier.NotifyAsync(itinerary, workflowEvent, context);
        }
    }
}