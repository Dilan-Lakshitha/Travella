using System;
using System.Threading.Tasks;
using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IItineraryRepository _itineraryRepository;
        private readonly IReviewRepository _reviewRepository;

        public ReviewService(IItineraryRepository itineraryRepository, IReviewRepository reviewRepository)
        {
            _itineraryRepository = itineraryRepository;
            _reviewRepository = reviewRepository;
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

            // Strict workflow transitions driven by review decision.
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
                }
                else if (string.Equals(normalizedStatus, "UNDER_REVIEW", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(normalizedStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    // BACKCOMPAT: if caller still sends UNDER_REVIEW, store it as PENDING.
                    var reviewToInsert = "PENDING";
                    if (!string.Equals(itinerary.Status, "submitted", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(itinerary.Status, "under_review", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Staff can start review only for submitted/under_review itineraries.");
                    }

                    if (string.Equals(itinerary.Status, "submitted", StringComparison.OrdinalIgnoreCase))
                    {
                        await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "under_review");
                    }

                    // Keep itinerary status under_review.
                    normalizedStatus = reviewToInsert;
                }
                else if (string.Equals(normalizedStatus, "REQUESTED_CHANGES", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(itinerary.Status, "submitted", StringComparison.OrdinalIgnoreCase))
                    {
                        await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "under_review");
                    }
                    else if (!string.Equals(itinerary.Status, "under_review", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Request changes is only allowed when itinerary is submitted or under_review.");
                    }

                    // Keep itinerary status as under_review (strict rule).
                }
                else if (string.Equals(normalizedStatus, "APPROVED_BY_STAFF", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(itinerary.Status, "submitted", StringComparison.OrdinalIgnoreCase))
                    {
                        await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "under_review");
                    }
                    else if (!string.Equals(itinerary.Status, "under_review", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Staff approval is only allowed when itinerary is under_review.");
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
                }
                else if (string.Equals(normalizedStatus, "APPROVED_BY_ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(itinerary.Status, "sent_to_admin", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Admin approval is only allowed when itinerary is sent_to_admin.");
                    }

                    await _itineraryRepository.UpdateItineraryStatusAsync(itineraryId, "approved_by_admin");
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

            // Insert the review record after status transition (or after validating stay-in-place).
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
