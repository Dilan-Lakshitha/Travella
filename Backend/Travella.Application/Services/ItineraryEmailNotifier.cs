using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Travella.Application.DTOs;
using Travella.Application.Enums;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;

namespace Travella.Application.Services
{
    public class ItineraryEmailNotifier : IItineraryEmailNotifier
    {
        private readonly IEmailService _emailService;
        private readonly IAuthRepository _authRepository;
        private readonly IItineraryRepository _itineraryRepository;
        private readonly ILogger<ItineraryEmailNotifier> _logger;

        public ItineraryEmailNotifier(
            IEmailService emailService,
            IAuthRepository authRepository,
            IItineraryRepository itineraryRepository,
            ILogger<ItineraryEmailNotifier> logger)
        {
            _emailService = emailService;
            _authRepository = authRepository;
            _itineraryRepository = itineraryRepository;
            _logger = logger;
        }

        public async Task NotifyAsync(Itinerary itinerary, ItineraryEmailEvent workflowEvent, ItineraryEmailContext? context = null)
        {
            if (itinerary.CompanyId is not int companyId || companyId <= 0)
            {
                return;
            }

            try
            {
                var details = await BuildDetailsAsync(itinerary, context);
                var (subject, body) = ItineraryEmailTemplates.Build(workflowEvent, details);

                switch (workflowEvent)
                {
                    case ItineraryEmailEvent.Submitted:
                        await SendToCompanyAdminsAndAllStaffAsync(companyId, subject, body);
                        break;

                    case ItineraryEmailEvent.Resubmitted:
                        await SendToCompanyReviewersAsync(companyId, itinerary, subject, body);
                        break;

                    case ItineraryEmailEvent.ReturnedForCorrection:
                    case ItineraryEmailEvent.Rejected:
                        await SendToTravelerOnlyAsync(itinerary, subject, body);
                        break;

                    case ItineraryEmailEvent.Approved:
                        await SendToTravelerOnlyAsync(itinerary, subject, body);
                        var staffTracking = ItineraryEmailTemplates.BuildStaffApprovedTracking(details);
                        await SendToCompanyStaffOnlyAsync(companyId, itinerary, staffTracking.Subject, staffTracking.Body);
                        break;

                    case ItineraryEmailEvent.Priced:
                        await _emailService.SendToCompanyAdminsAsync(companyId, subject, body);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(workflowEvent), workflowEvent, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Itinerary workflow email failed for itinerary {ItineraryId}, event {Event}",
                    itinerary.Id,
                    workflowEvent);
            }
        }

        private async Task SendToCompanyAdminsAndAllStaffAsync(int companyId, string subject, string body)
        {
            var recipients = await GetCompanyAdminsAndAllStaffAsync(companyId);
            await SendDistinctAsync(recipients, subject, body);
        }

        private async Task SendToCompanyReviewersAsync(int companyId, Itinerary itinerary, string subject, string body)
        {
            var recipients = await GetCompanyReviewRecipientsAsync(companyId, itinerary);
            await SendDistinctAsync(recipients, subject, body);
        }

        private async Task SendToCompanyStaffOnlyAsync(int companyId, Itinerary itinerary, string subject, string body)
        {
            var recipients = await GetStaffReviewerRecipientsAsync(companyId, itinerary);
            await SendDistinctAsync(recipients, subject, body);
        }

        private async Task SendToTravelerOnlyAsync(Itinerary itinerary, string subject, string body)
        {
            var traveler = await _authRepository.GetUserContactAsync(itinerary.GuestId);
            if (traveler == null || string.IsNullOrWhiteSpace(traveler.Value.Email))
            {
                _logger.LogInformation(
                    "No traveler email for itinerary {ItineraryId}, guest {GuestId}",
                    itinerary.Id,
                    itinerary.GuestId);
                return;
            }

            await _emailService.SendAsync(traveler.Value.Email, subject, body);
        }

        private async Task SendDistinctAsync(IReadOnlyList<(string Name, string Email)> recipients, string subject, string body)
        {
            var emails = recipients
                .Where(r => !string.IsNullOrWhiteSpace(r.Email))
                .Select(r => r.Email.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);

            await _emailService.SendToEmailsAsync(emails, subject, body);
        }

        private async Task<List<(string Name, string Email)>> GetCompanyAdminsAndAllStaffAsync(int companyId)
        {
            var admins = await _authRepository.GetCompanyAdminUsersAsync(companyId);
            var staff = await _authRepository.GetCompanyStaffUsersAsync(companyId);

            return admins
                .Select(a => (a.Name, a.Email))
                .Concat(staff.Select(s => (s.Name, s.Email)))
                .Where(r => !string.IsNullOrWhiteSpace(r.Email))
                .GroupBy(r => r.Email.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        private async Task<List<(string Name, string Email)>> GetCompanyReviewRecipientsAsync(
            int companyId,
            Itinerary itinerary)
        {
            var admins = await _authRepository.GetCompanyAdminUsersAsync(companyId);
            var staff = await GetStaffReviewerRecipientsAsync(companyId, itinerary);

            return admins
                .Select(a => (a.Name, a.Email))
                .Concat(staff)
                .Where(r => !string.IsNullOrWhiteSpace(r.Email))
                .GroupBy(r => r.Email.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        private async Task<List<(string Name, string Email)>> GetStaffReviewerRecipientsAsync(
            int companyId,
            Itinerary itinerary)
        {
            var reviewerId = itinerary.AssignedReviewerId
                ?? await _itineraryRepository.GetAssignedReviewerIdAsync(itinerary.Id);

            if (reviewerId.HasValue)
            {
                var reviewer = await _authRepository.GetUserContactAsync(reviewerId.Value);
                if (reviewer != null && !string.IsNullOrWhiteSpace(reviewer.Value.Email))
                {
                    return new List<(string Name, string Email)> { reviewer.Value };
                }
            }

            var staff = await _authRepository.GetCompanyStaffUsersAsync(companyId);
            return staff.Select(s => (s.Name, s.Email)).ToList();
        }

        private async Task<ItineraryEmailDetails> BuildDetailsAsync(Itinerary itinerary, ItineraryEmailContext? context)
        {
            var traveler = await _authRepository.GetUserContactAsync(itinerary.GuestId);
            var guestName = traveler?.Name;
            if (string.IsNullOrWhiteSpace(guestName))
            {
                guestName = $"Guest #{itinerary.GuestId}";
            }

            return new ItineraryEmailDetails
            {
                TripId = itinerary.Id,
                GuestName = guestName,
                TravelDates = $"{itinerary.StartDate:yyyy-MM-dd} to {itinerary.EndDate:yyyy-MM-dd}",
                StatusLabel = FormatStatus(itinerary.Status),
                CorrectionNotes = context?.CorrectionNotes,
                TotalPrice = itinerary.TotalPrice,
                Pricing = context?.Pricing,
            };
        }

        private static string FormatStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "Unknown";
            }

            return status.Replace("_", " ");
        }
    }
}
