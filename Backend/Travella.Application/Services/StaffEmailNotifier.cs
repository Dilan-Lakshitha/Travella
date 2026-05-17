using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;

namespace Travella.Application.Services
{
    public class StaffEmailNotifier : IStaffEmailNotifier
    {
        private readonly IEmailService _emailService;
        private readonly IItineraryRepository _itineraryRepository;
        private readonly IStaffRepository _staffRepository;

        public StaffEmailNotifier(
            IEmailService emailService,
            IItineraryRepository itineraryRepository,
            IStaffRepository staffRepository)
        {
            _emailService = emailService;
            _itineraryRepository = itineraryRepository;
            _staffRepository = staffRepository;
        }

        public async Task NotifyDriverCreatedAsync(Staff driver, int companyId)
        {
            var body = BuildBody(
                "New driver created",
                $"Driver <strong>{driver.Name}</strong> (language: {driver.Language}) was added to your company.",
                null,
                null,
                "DRIVER",
                "Created");

            await SendToAdminsAndStaffAsync(companyId, driver.Email, "Travella — New driver created", body);
        }

        public Task NotifyGuideCreatedAsync(Staff guide, int companyId)
        {
            var body = BuildBody(
                "New guide created",
                $"Guide <strong>{guide.Name}</strong> (language: {guide.Language}) was added to your company.",
                null,
                null,
                "GUIDE",
                "Created");

            return SendToAdminsAndStaffAsync(companyId, guide.Email, "Travella — New guide created", body);
        }

        public async Task NotifyStaffAssignedAsync(
            Itinerary itinerary,
            Staff driver,
            Staff guide,
            int companyId)
        {
            var range = FormatRange(itinerary.StartDate, itinerary.EndDate);
            var body = BuildBody(
                $"Staff assigned to itinerary #{itinerary.Id}",
                $"Driver <strong>{driver.Name}</strong> ({driver.Language}) and guide <strong>{guide.Name}</strong> ({guide.Language}) were assigned.",
                itinerary.Id,
                range,
                "DRIVER/GUIDE",
                "Assigned");

            await _emailService.SendToCompanyAdminsAsync(companyId, $"Travella — Staff assigned (#{itinerary.Id})", body);

            var staffEmails = new[] { driver.Email, guide.Email }
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!);
            await _emailService.SendToEmailsAsync(staffEmails, $"Travella — You were assigned (#{itinerary.Id})", body);
        }

        public async Task NotifyItineraryConfirmedAsync(Itinerary itinerary, int companyId)
        {
            var range = FormatRange(itinerary.StartDate, itinerary.EndDate);
            var body = BuildBody(
                $"Itinerary #{itinerary.Id} confirmed",
                "The itinerary booking is confirmed and staff availability has been locked.",
                itinerary.Id,
                range,
                "BOOKING",
                "Confirmed");

            await _emailService.SendToCompanyAdminsAsync(companyId, $"Travella — Itinerary confirmed (#{itinerary.Id})", body);

            var assigned = await _itineraryRepository.GetItineraryStaffAsync(itinerary.Id);
            var staffEmails = new List<string>();
            foreach (var assignment in assigned)
            {
                var staff = await _staffRepository.GetStaffByIdAsync(assignment.StaffId);
                if (!string.IsNullOrWhiteSpace(staff?.Email))
                {
                    staffEmails.Add(staff.Email);
                }
            }

            await _emailService.SendToEmailsAsync(staffEmails, $"Travella — Itinerary confirmed (#{itinerary.Id})", body);
        }

        public async Task NotifyItineraryStatusChangedAsync(Itinerary itinerary, int companyId, string statusLabel)
        {
            var range = FormatRange(itinerary.StartDate, itinerary.EndDate);
            var body = BuildBody(
                $"Itinerary #{itinerary.Id} status updated",
                $"The itinerary status is now <strong>{statusLabel}</strong>.",
                itinerary.Id,
                range,
                "ITINERARY",
                statusLabel);

            await _emailService.SendToCompanyAdminsAsync(
                companyId,
                $"Travella — Itinerary #{itinerary.Id}: {statusLabel}",
                body);

            var assigned = await _itineraryRepository.GetItineraryStaffAsync(itinerary.Id);
            var staffEmails = new List<string>();
            foreach (var assignment in assigned)
            {
                var staff = await _staffRepository.GetStaffByIdAsync(assignment.StaffId);
                if (!string.IsNullOrWhiteSpace(staff?.Email))
                {
                    staffEmails.Add(staff.Email);
                }
            }

            await _emailService.SendToEmailsAsync(
                staffEmails,
                $"Travella — Itinerary #{itinerary.Id}: {statusLabel}",
                body);
        }

        private async Task SendToAdminsAndStaffAsync(int companyId, string? staffEmail, string subject, string body)
        {
            await _emailService.SendToCompanyAdminsAsync(companyId, subject, body);
            if (!string.IsNullOrWhiteSpace(staffEmail))
            {
                await _emailService.SendAsync(staffEmail, subject, body);
            }
        }

        private static string FormatRange(DateOnly start, DateOnly end)
            => $"{start:yyyy-MM-dd} to {end:yyyy-MM-dd}";

        private static string BuildBody(
            string heading,
            string summary,
            int? itineraryId,
            string? dateRange,
            string role,
            string status)
        {
            var itineraryLine = itineraryId.HasValue
                ? $"<p><strong>Itinerary #:</strong> {itineraryId}</p>"
                : string.Empty;
            var dateLine = !string.IsNullOrWhiteSpace(dateRange)
                ? $"<p><strong>Date range:</strong> {dateRange}</p>"
                : string.Empty;

            return $"""
                <html><body style="font-family:Arial,sans-serif">
                <h2>{heading}</h2>
                <p>{summary}</p>
                {itineraryLine}
                {dateLine}
                <p><strong>Role:</strong> {role}</p>
                <p><strong>Status:</strong> {status}</p>
                <hr/>
                <p style="color:#666;font-size:12px">Travella Travel Management</p>
                </body></html>
                """;
        }
    }
}
