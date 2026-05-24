using System;
using System.Net;
using Travella.Application.DTOs;
using Travella.Application.Enums;

namespace Travella.Application.Services
{
    internal static class ItineraryEmailTemplates
    {
        public static (string Subject, string Body) Build(ItineraryEmailEvent workflowEvent, ItineraryEmailDetails details)
        {
            return workflowEvent switch
            {
                ItineraryEmailEvent.Submitted => (
                    "New itinerary submitted for review",
                    BuildStaffReviewBody(
                        "New itinerary submitted for review",
                        "A traveler has submitted a new itinerary for your review.",
                        details)),

                ItineraryEmailEvent.Resubmitted => (
                    "Itinerary resubmitted and ready for review",
                    BuildStaffReviewBody(
                        "Itinerary resubmitted and ready for review",
                        "A traveler has resubmitted an itinerary after corrections. It is ready for review again.",
                        details)),

                ItineraryEmailEvent.ReturnedForCorrection => (
                    "Your itinerary has been returned for correction",
                    BuildTravelerBody(
                        "Your itinerary has been returned for correction",
                        "Please review the staff feedback below, update your itinerary, and resubmit when ready.",
                        details,
                        includeCorrectionNotes: true)),

                ItineraryEmailEvent.Approved => (
                    "Your itinerary has been approved",
                    BuildTravelerBody(
                        "Your itinerary has been approved",
                        "Your itinerary has been approved by the company owner. You will be notified when it is confirmed.",
                        details)),

                ItineraryEmailEvent.Rejected => (
                    "Your itinerary has been rejected",
                    BuildTravelerBody(
                        "Your itinerary has been rejected",
                        "Your itinerary was rejected. If you have questions, please contact your travel company.",
                        details)),

                ItineraryEmailEvent.Priced => (
                    "Itinerary pricing saved",
                    BuildPricedBody(details)),

                _ => throw new ArgumentOutOfRangeException(nameof(workflowEvent), workflowEvent, null),
            };
        }

        public static (string Subject, string Body) BuildStaffApprovedTracking(ItineraryEmailDetails details)
        {
            return (
                $"Itinerary #{details.TripId} approved",
                BuildStaffReviewBody(
                    "Itinerary approved",
                    "An itinerary has been approved by the company owner.",
                    details));
        }

        private static string BuildStaffReviewBody(string heading, string summary, ItineraryEmailDetails details)
            => Wrap(heading, summary, details, includeCorrectionNotes: false, includePricing: false);

        private static string BuildTravelerBody(string heading, string summary, ItineraryEmailDetails details, bool includeCorrectionNotes = false, bool includePricing = false)
            => Wrap(heading, summary, details, includeCorrectionNotes, includePricing);

        private static string BuildPricedBody(ItineraryEmailDetails details)
        {
            var pricingSection = details.Pricing != null
                ? BuildPricingSummaryHtml(details.Pricing)
                : $"<p><strong>Total price:</strong> {details.TotalPrice:N2}</p>";

            return Wrap(
                "Itinerary pricing saved",
                "Pricing has been saved for an itinerary under review.",
                details,
                includeCorrectionNotes: false,
                includePricing: false,
                extraHtml: pricingSection);
        }

        private static string Wrap(string heading, string summary,ItineraryEmailDetails details, bool includeCorrectionNotes, bool includePricing, string? extraHtml = null)
        {
            var correctionBlock = includeCorrectionNotes && !string.IsNullOrWhiteSpace(details.CorrectionNotes)
                ? $"""
                    <p><strong>Correction notes:</strong></p>
                    <blockquote style="margin:8px 0;padding:12px;border-left:4px solid #2563eb;background:#f8fafc">
                    {WebUtility.HtmlEncode(details.CorrectionNotes)}
                    </blockquote>
                    """
                : string.Empty;

            var pricingBlock = includePricing && details.Pricing != null
                ? BuildPricingSummaryHtml(details.Pricing)
                : string.Empty;

            var extra = extraHtml ?? string.Empty;

            return $"""
                <html><body style="font-family:Arial,sans-serif;color:#111827;line-height:1.5">
                <h2 style="margin:0 0 12px">{WebUtility.HtmlEncode(heading)}</h2>
                <p>{WebUtility.HtmlEncode(summary)}</p>
                <table style="border-collapse:collapse;margin:16px 0">
                <tr><td style="padding:4px 12px 4px 0"><strong>Trip ID:</strong></td><td>#{details.TripId}</td></tr>
                <tr><td style="padding:4px 12px 4px 0"><strong>Guest:</strong></td><td>{WebUtility.HtmlEncode(details.GuestName)}</td></tr>
                <tr><td style="padding:4px 12px 4px 0"><strong>Travel dates:</strong></td><td>{WebUtility.HtmlEncode(details.TravelDates)}</td></tr>
                <tr><td style="padding:4px 12px 4px 0"><strong>Status:</strong></td><td>{WebUtility.HtmlEncode(details.StatusLabel)}</td></tr>
                </table>
                {correctionBlock}
                {pricingBlock}
                {extra}
                <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0"/>
                <p style="color:#6b7280;font-size:12px">Travella Travel Management</p>
                </body></html>
                """;
        }

        private static string BuildPricingSummaryHtml(ItineraryPricingDetailDto pricing)
            => $"""
                <p><strong>Pricing summary</strong></p>
                <ul style="margin:8px 0;padding-left:20px">
                <li>Driver: {pricing.DriverCost:N2}</li>
                <li>Guide: {pricing.GuideCost:N2}</li>
                <li>Vehicle: {pricing.VehicleCost:N2}</li>
                <li>Mileage ({pricing.TotalKm:N1} km @ {pricing.MileageRate:N2}): {(pricing.MileageRate * pricing.TotalKm):N2}</li>
                <li>Accommodation: {pricing.AccommodationCost:N2}</li>
                <li>Meal plan: {WebUtility.HtmlEncode(pricing.MealPlan)}</li>
                <li>Profit margin: {pricing.ProfitMargin:N1}%</li>
                <li><strong>Total: {pricing.TotalAmount:N2}</strong></li>
                </ul>
                """;
    }

    internal sealed class ItineraryEmailDetails
    {
        public int TripId { get; init; }

        public string GuestName { get; init; } = string.Empty;

        public string TravelDates { get; init; } = string.Empty;

        public string StatusLabel { get; init; } = string.Empty;

        public string? CorrectionNotes { get; init; }

        public decimal TotalPrice { get; init; }

        public ItineraryPricingDetailDto? Pricing { get; init; }
    }
}
