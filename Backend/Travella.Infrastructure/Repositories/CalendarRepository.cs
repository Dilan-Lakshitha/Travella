using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class CalendarRepository : ICalendarRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CalendarRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private async Task<IDbConnection> OpenConnectionAsync()
        {
            var c = _connectionFactory.CreateConnection();
            if (c is NpgsqlConnection npg)
            {
                await npg.OpenAsync();
            }
            else
            {
                c.Open();
            }

            return c;
        }

        public async Task<IReadOnlyList<StaffBookingCalendarItemDto>> GetStaffBookingsAsync(
            int companyId,
            DateOnly startDate,
            DateOnly endDate,
            string? role)
        {
            const string rosterSql = """
                SELECT
                    s.id AS StaffId,
                    s.name AS StaffName,
                    s.type AS Role,
                    s.language AS Language,
                    s.email AS Email,
                    s.availability AS AvailabilityStatus
                FROM tbl_staff s
                WHERE s.company_id = @CompanyId
                  AND (@Role IS NULL OR s.type = @Role)
                ORDER BY s.name
                """;

            const string bookingSql = """
                SELECT DISTINCT ON (s.id, i.id)
                    s.id AS StaffId,
                    s.name AS StaffName,
                    s.type AS Role,
                    s.language AS Language,
                    s.email AS Email,
                    'BOOKED' AS AvailabilityStatus,
                    i.id AS ItineraryId,
                    COALESCE(u.name, 'Itinerary #' || i.id::text) AS ItineraryTitle,
                    i.start_date::date AS StartDate,
                    i.end_date::date AS EndDate,
                    i.status AS Status,
                    'BOOKED' AS BookedStatus,
                    to_char(i.start_date::date, 'YYYY-MM-DD') || ' to ' || to_char(i.end_date::date, 'YYYY-MM-DD') AS BookedDateRange
                FROM tbl_staff_availability sa
                INNER JOIN tbl_staff s ON s.id = sa.staff_id
                INNER JOIN tbl_itinerary_staff ist ON ist.staff_id = s.id
                INNER JOIN tbl_itineraries i ON i.id = ist.itinerary_id
                    AND sa.date BETWEEN i.start_date::date AND i.end_date::date
                LEFT JOIN tbl_users u ON u.id = i.guest_id
                WHERE s.company_id = @CompanyId
                  AND UPPER(sa.status) = 'BOOKED'
                  AND sa.date BETWEEN @StartDate::date AND @EndDate::date
                  AND (@Role IS NULL OR s.type = @Role)
                ORDER BY s.id, i.id, i.start_date
                """;

            using var connection = await OpenConnectionAsync();
            var parameters = new
            {
                CompanyId = companyId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MinValue),
                Role = role,
            };

            var bookings = (await connection.QueryAsync<StaffBookingCalendarItemDto>(bookingSql, parameters)).ToList();
            var bookedStaffIds = bookings.Select(b => b.StaffId).ToHashSet();

            var roster = await connection.QueryAsync<StaffBookingCalendarItemDto>(rosterSql, parameters);
            var availableRows = roster
                .Where(r => !bookedStaffIds.Contains(r.StaffId))
                .Select(r => new StaffBookingCalendarItemDto
                {
                    StaffId = r.StaffId,
                    StaffName = r.StaffName,
                    Role = r.Role,
                    Language = r.Language,
                    Email = r.Email,
                    AvailabilityStatus = MapProfileAvailabilityStatus(r.AvailabilityStatus),
                    BookedStatus = null,
                    BookedDateRange = null,
                });

            return bookings.Concat(availableRows).OrderBy(r => r.AvailabilityStatus == "AVAILABLE" ? 1 : 0)
                .ThenBy(r => r.StaffName)
                .ToList();
        }

        public async Task<IReadOnlyList<ItineraryBookingCalendarItemDto>> GetItineraryBookingsAsync(
            int companyId,
            DateOnly startDate,
            DateOnly endDate,
            int? driverId,
            int? guideId)
        {
            const string sql = """
                SELECT
                    i.id AS ItineraryId,
                    COALESCE(u.name, 'Itinerary #' || i.id::text) AS ItineraryTitle,
                    i.start_date::date AS StartDate,
                    i.end_date::date AS EndDate,
                    i.status AS ItineraryStatus,
                    to_char(i.start_date::date, 'YYYY-MM-DD') || ' to ' || to_char(i.end_date::date, 'YYYY-MM-DD') AS BookedDateRange,
                    (
                        SELECT s.id FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id AND s.type = 'DRIVER'
                        WHERE ist.itinerary_id = i.id
                        LIMIT 1
                    ) AS DriverId,
                    (
                        SELECT s.name FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id AND s.type = 'DRIVER'
                        WHERE ist.itinerary_id = i.id
                        LIMIT 1
                    ) AS DriverName,
                    (
                        SELECT s.language FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id AND s.type = 'DRIVER'
                        WHERE ist.itinerary_id = i.id
                        LIMIT 1
                    ) AS DriverLanguage,
                    (
                        SELECT s.email FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id AND s.type = 'DRIVER'
                        WHERE ist.itinerary_id = i.id
                        LIMIT 1
                    ) AS DriverEmail,
                    (
                        SELECT s.id FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id AND s.type = 'GUIDE'
                        WHERE ist.itinerary_id = i.id
                        LIMIT 1
                    ) AS GuideId,
                    (
                        SELECT s.name FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id AND s.type = 'GUIDE'
                        WHERE ist.itinerary_id = i.id
                        LIMIT 1
                    ) AS GuideName,
                    (
                        SELECT s.language FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id AND s.type = 'GUIDE'
                        WHERE ist.itinerary_id = i.id
                        LIMIT 1
                    ) AS GuideLanguage,
                    (
                        SELECT s.email FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id AND s.type = 'GUIDE'
                        WHERE ist.itinerary_id = i.id
                        LIMIT 1
                    ) AS GuideEmail
                FROM tbl_itineraries i
                LEFT JOIN tbl_users u ON u.id = i.guest_id
                WHERE i.company_id = @CompanyId
                  AND i.end_date::date >= @StartDate::date
                  AND i.start_date::date <= @EndDate::date
                  AND (
                    EXISTS (SELECT 1 FROM tbl_itinerary_staff x WHERE x.itinerary_id = i.id)
                    OR LOWER(i.status) IN ('confirmed', 'approved_by_admin', 'sent_to_admin', 'priced', 'under_review')
                  )
                  AND (
                    @DriverId IS NULL OR EXISTS (
                        SELECT 1 FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id
                        WHERE ist.itinerary_id = i.id AND s.id = @DriverId AND s.type = 'DRIVER'
                    )
                  )
                  AND (
                    @GuideId IS NULL OR EXISTS (
                        SELECT 1 FROM tbl_itinerary_staff ist
                        INNER JOIN tbl_staff s ON s.id = ist.staff_id
                        WHERE ist.itinerary_id = i.id AND s.id = @GuideId AND s.type = 'GUIDE'
                    )
                  )
                ORDER BY i.start_date, i.id
                """;

            using var connection = await OpenConnectionAsync();
            var rows = await connection.QueryAsync<ItineraryBookingCalendarItemDto>(sql, new
            {
                CompanyId = companyId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MinValue),
                DriverId = driverId,
                GuideId = guideId,
            });

            return rows.AsList();
        }

        private static string MapProfileAvailabilityStatus(string? profileAvailability)
        {
            if (string.IsNullOrWhiteSpace(profileAvailability))
            {
                return "AVAILABLE";
            }

            var normalized = profileAvailability.Trim().ToUpperInvariant();
            return normalized switch
            {
                "OFF_DUTY" or "ON_TRIP" or "UNAVAILABLE" => "UNAVAILABLE",
                _ => "AVAILABLE",
            };
        }
    }
}
