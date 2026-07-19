using System;
using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IItineraryRepository _itineraryRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly INotificationService _notificationService;

        public ReviewService(
            IItineraryRepository itineraryRepository,
            IReviewRepository reviewRepository,
            INotificationService notificationService)
        {
            _itineraryRepository = itineraryRepository;
            _reviewRepository = reviewRepository;
            _notificationService = notificationService;
        }

        public async Task<int> AddReviewAsync(int itineraryId, int reviewerId, string reviewerRole, int companyId, string comments, string status)
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

            var normalizedStatus = status?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedStatus))
            {
                throw new InvalidOperationException("Review status is required.");
            }

            // - STAFF:
            //   PENDING: submitted -> under_review (stay)
            //   REQUESTED_CHANGES: under_review -> under_review (stay)
            //   APPROVED_BY_STAFF: under_review -> approved_by_staff
            //   REJECTED: * -> rejected
            // - ADMIN:
            //   APPROVED_BY_ADMIN: sent_to_admin -> approved_by_admin
            //   REJECTED: * -> rejected
            if (string.Equals(reviewerRole, "STAFF", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(normalizedStatus, "REJECTED", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(itinerary.Status, "rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Itinerary is already rejected.");
                    }

                    await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "rejected");
                    await _notificationService.NotifyItineraryRejectedAsync(itineraryId, itinerary.GuestId);
                }
                else if (string.Equals(normalizedStatus, "UNDER_REVIEW", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(normalizedStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    var reviewToInsert = "PENDING";
                    var reviewableStatus = ItineraryStatusHelper.Normalize(itinerary.Status);
                    if (reviewableStatus is not ("submitted" or "under_review" or "resubmitted"))
                    {
                        throw new InvalidOperationException("Staff can start review only for submitted, resubmitted, or under_review itineraries.");
                    }

                    if (reviewableStatus is "submitted" or "resubmitted")
                    {
                        await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "under_review");
                        await _notificationService.NotifyItineraryUnderReviewAsync(itineraryId, itinerary.GuestId);
                    }

                    normalizedStatus = reviewToInsert;
                }
                else if (string.Equals(normalizedStatus, "REQUESTED_CHANGES", StringComparison.OrdinalIgnoreCase))
                {
                    var itineraryStatus = ItineraryStatusHelper.Normalize(itinerary.Status);
                    if (itineraryStatus is "submitted" or "resubmitted")
                    {
                        await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "under_review");
                    }
                    else if (itineraryStatus != "under_review")
                    {
                        throw new InvalidOperationException("Request changes is only allowed when itinerary is submitted, resubmitted, or under_review.");
                    }

                    // Keep itinerary status as under_review (strict rule).
                }
                else if (string.Equals(normalizedStatus, "APPROVED_BY_STAFF", StringComparison.OrdinalIgnoreCase))
                {
                    var itineraryStatus = ItineraryStatusHelper.Normalize(itinerary.Status);
                    if (itineraryStatus is "submitted" or "resubmitted")
                    {
                        await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "under_review");
                    }
                    else if (itineraryStatus != "under_review")
                    {
                        throw new InvalidOperationException("Staff approval is only allowed when itinerary is under_review or resubmitted.");
                    }

                    await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "approved_by_staff");
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported staff review status '{normalizedStatus}'.");
                }
            }
            else if (string.Equals(reviewerRole, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(normalizedStatus, "REJECTED", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(itinerary.Status, "rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Itinerary is already rejected.");
                    }

                    await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "rejected");
                    await _notificationService.NotifyItineraryRejectedAsync(itineraryId, itinerary.GuestId);
                }
                else if (string.Equals(normalizedStatus, "APPROVED_BY_ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(itinerary.Status, "sent_to_admin", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Admin approval is only allowed when itinerary is sent_to_admin.");
                    }

                    await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "approved_by_admin");
                    await _notificationService.NotifyItineraryApprovedAsync(itineraryId, itinerary.GuestId);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported admin review status '{normalizedStatus}'.");
                }
            }
            else
            {
                throw new InvalidOperationException("Unsupported reviewer role.");
            }

            return await _reviewRepository.AddAsync(
                itineraryId,
                reviewerId,
                reviewerRole,
                comments,
                normalizedStatus.ToUpperInvariant()
            );
        }
    }
}
