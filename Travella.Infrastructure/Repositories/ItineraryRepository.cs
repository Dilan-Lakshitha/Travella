using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class ItineraryRepository : IItineraryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUnitOfWork _unitOfWork;

        public ItineraryRepository(IDbConnectionFactory connectionFactory, IUnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        private async Task<IDbConnection> OpenStandaloneConnectionAsync()
        {
            var c = _connectionFactory.CreateConnection();
            if (c.State != ConnectionState.Open)
            {
                if (c is NpgsqlConnection npg)
                {
                    await npg.OpenAsync();
                }
                else
                {
                    c.Open();
                }
            }

            return c;
        }

        private (IDbConnection Conn, IDbTransaction Tran) RequireActiveTransaction()
        {
            if (!_unitOfWork.HasActiveTransaction || _unitOfWork.CurrentTransaction == null)
            {
                throw new InvalidOperationException("An active database transaction is required for this operation.");
            }

            return (_unitOfWork.Connection, _unitOfWork.CurrentTransaction);
        }

        public async Task<int> CreateItineraryAsync(Itinerary itinerary)
        {
            const string sql = @"
INSERT INTO tbl_itineraries (guest_id, start_date, end_date, status, total_price, company_id, created_by)
VALUES (@GuestId, @StartDate, @EndDate, @Status, @TotalPrice, @CompanyId, @GuestId)
RETURNING id;";

            var (connection, tran) = RequireActiveTransaction();

            return await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    itinerary.GuestId,
                    StartDate = itinerary.StartDate.ToDateTime(TimeOnly.MinValue),
                    EndDate = itinerary.EndDate.ToDateTime(TimeOnly.MinValue),
                    itinerary.Status,
                    itinerary.TotalPrice,
                    itinerary.CompanyId
                },
                tran
            );
        }

        public async Task<SaveGoogleAttractionResponseDto> GetOrCreateGoogleAttractionAsync(SaveGoogleAttractionDto dto)
        {
            const string findSql = """
                SELECT id
                FROM tbl_attractions
                WHERE place_id = @PlaceId
                LIMIT 1
            """;

            const string insertSql = """
                INSERT INTO tbl_attractions (place_id, name, address, latitude, longitude, source, created_at, updated_at)
                VALUES (@PlaceId, @Name, @Address, @Latitude, @Longitude, 'GOOGLE', NOW(), NOW())
                RETURNING id
            """;

            using var connection = await OpenStandaloneConnectionAsync();
            var existingId = await connection.QueryFirstOrDefaultAsync<int?>(findSql, new { dto.PlaceId });
            if (existingId.HasValue)
            {
                return new SaveGoogleAttractionResponseDto
                {
                    Id = existingId.Value,
                    AlreadyExists = true
                };
            }

            var id = await connection.ExecuteScalarAsync<int>(insertSql, dto);
            return new SaveGoogleAttractionResponseDto
            {
                Id = id,
                AlreadyExists = false
            };
        }

        public async Task<bool> ItineraryExistsAsync(int itineraryId)
        {
            const string sql = @"SELECT COUNT(1) FROM tbl_itineraries WHERE id = @Id;";
            using var connection = await OpenStandaloneConnectionAsync();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = itineraryId });
            return count > 0;
        }

        public async Task<Itinerary?> GetItineraryByIdAsync(int itineraryId)
        {
            const string sql = @"
SELECT
    id,
    guest_id AS GuestId,
    start_date AS StartDate,
    end_date AS EndDate,
    status,
    total_price AS TotalPrice,
    company_id AS CompanyId
FROM tbl_itineraries
WHERE id = @Id;";

            using var connection = await OpenStandaloneConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<Itinerary>(sql, new { Id = itineraryId });
        }

        public async Task<ItineraryFullResponseDto?> GetItineraryFullAsync(int itineraryId)
        {
            const string headerSql = """
                SELECT
                    i.id AS Id,
                    i.guest_id AS GuestId,
                    u.name AS GuestName,
                    i.start_date AS StartDate,
                    i.end_date AS EndDate,
                    i.status AS Status,
                    COALESCE(p.total_amount, i.total_price) AS TotalPrice,
                    i.company_id AS CompanyId
                FROM tbl_itineraries i
                INNER JOIN tbl_users u ON u.id = i.guest_id
                LEFT JOIN LATERAL (
                    SELECT pp.total_amount
                    FROM tbl_itinerary_pricing pp
                    WHERE pp.itinerary_id = i.id
                    ORDER BY pp.created_at DESC
                    LIMIT 1
                ) p ON true
                WHERE i.id = @Id
                LIMIT 1;
                """;

            const string daysSql = """
                SELECT id AS Id, day_number AS DayNumber, overnight_location AS OvernightLocation
                FROM tbl_itinerary_days
                WHERE itinerary_id = @Id
                ORDER BY day_number;
                """;

            const string attractionsSql = """
                SELECT
                    ia.id AS Id,
                    ia.itinerary_day_id AS ItineraryDayId,
                    ia.attraction_id AS AttractionId,
                    a.name AS Name,
                    a.address AS Address,
                    a.latitude AS Latitude,
                    a.longitude AS Longitude,
                    ia.description AS Description,
                    ia.duration_hours AS DurationHours
                FROM tbl_itinerary_attractions ia
                INNER JOIN tbl_itinerary_days d ON d.id = ia.itinerary_day_id
                INNER JOIN tbl_attractions a ON a.id = ia.attraction_id
                WHERE d.itinerary_id = @Id;
                """;

            const string accommodationsSql = """
                SELECT
                    iac.id AS Id,
                    iac.itinerary_day_id AS ItineraryDayId,
                    iac.accommodation_id AS AccommodationId,
                    ac.name AS AccommodationName,
                    iac.meal_plan_id AS MealPlanId,
                    COALESCE(mp.code, '') AS MealPlanCode
                FROM tbl_itinerary_accommodations iac
                INNER JOIN tbl_itinerary_days d ON d.id = iac.itinerary_day_id
                LEFT JOIN tbl_accommodations ac ON ac.id = iac.accommodation_id
                LEFT JOIN tbl_meal_plans mp ON mp.id = iac.meal_plan_id
                WHERE d.itinerary_id = @Id;
                """;

            using var connection = await OpenStandaloneConnectionAsync();

            var header = await connection.QuerySingleOrDefaultAsync<ItineraryHeaderDto>(headerSql, new { Id = itineraryId });
            if (header == null)
            {
                return null;
            }

            var days = (await connection.QueryAsync<ItineraryDayFlatDto>(daysSql, new { Id = itineraryId })).ToList();
            var attractions = (await connection.QueryAsync<ItineraryAttractionFlatDto>(attractionsSql, new { Id = itineraryId })).ToList();
            var accommodations = (await connection.QueryAsync<ItineraryAccommodationFlatDto>(accommodationsSql, new { Id = itineraryId })).ToList();

            return new ItineraryFullResponseDto
            {
                Itinerary = header,
                Days = days,
                Attractions = attractions,
                Accommodations = accommodations
            };
        }

        public async Task DeleteItineraryNestedContentAsync(int itineraryId)
        {
            var (conn, tran) = RequireActiveTransaction();

            const string deleteAccommodations = """
                DELETE FROM tbl_itinerary_accommodations iac
                USING tbl_itinerary_days d
                WHERE iac.itinerary_day_id = d.id
                  AND d.itinerary_id = @ItineraryId;
                """;

            const string deleteAttractions = """
                DELETE FROM tbl_itinerary_attractions ia
                USING tbl_itinerary_days d
                WHERE ia.itinerary_day_id = d.id
                  AND d.itinerary_id = @ItineraryId;
                """;

            const string deleteDays = """
                DELETE FROM tbl_itinerary_days
                WHERE itinerary_id = @ItineraryId;
                """;

            await conn.ExecuteAsync(deleteAccommodations, new { ItineraryId = itineraryId }, tran);
            await conn.ExecuteAsync(deleteAttractions, new { ItineraryId = itineraryId }, tran);
            await conn.ExecuteAsync(deleteDays, new { ItineraryId = itineraryId }, tran);
        }

        public async Task UpdateItineraryDatesAsync(int itineraryId, DateOnly startDate, DateOnly endDate)
        {
            var (conn, tran) = RequireActiveTransaction();
            const string sql = """
                UPDATE tbl_itineraries
                SET start_date = @StartDate,
                    end_date = @EndDate,
                    updated_at = NOW()
                WHERE id = @Id;
                """;

            await conn.ExecuteAsync(
                sql,
                new
                {
                    Id = itineraryId,
                    StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                    EndDate = endDate.ToDateTime(TimeOnly.MinValue)
                },
                tran);
        }

        public async Task<int> EnsureMealPlanIdAsync(string? mealPlanCode)
        {
            var code = string.IsNullOrWhiteSpace(mealPlanCode) ? "BB" : mealPlanCode.Trim().ToUpperInvariant();
            var (conn, tran) = RequireActiveTransaction();

            const string findSql = """
                SELECT id
                FROM tbl_meal_plans
                WHERE UPPER(TRIM(Code)) = UPPER(TRIM(@Code))
                LIMIT 1;
                """;

            var existing = await conn.QueryFirstOrDefaultAsync<int?>(findSql, new { Code = code }, tran);
            if (existing.HasValue)
            {
                return existing.Value;
            }

            const string insertSql = """
                INSERT INTO tbl_meal_plans (Code)
                VALUES (@Name)
                RETURNING id;
                """;

            return await conn.ExecuteScalarAsync<int>(insertSql, new { Name = code }, tran);
        }

        public async Task<int> EnsureAccommodationIdAsync(string? accommodationType)
        {
            var name = string.IsNullOrWhiteSpace(accommodationType) ? "General" : accommodationType.Trim();
            var (conn, tran) = RequireActiveTransaction();

            const string findSql = """
                SELECT id
                FROM tbl_accommodations
                WHERE LOWER(TRIM(name)) = LOWER(TRIM(@Name))
                LIMIT 1;
                """;

            var existing = await conn.QueryFirstOrDefaultAsync<int?>(findSql, new { Name = name }, tran);
            if (existing.HasValue)
            {
                return existing.Value;
            }

            const string insertSql = """
                INSERT INTO tbl_accommodations (name, price_per_night)
                VALUES (@Name, 0)
                RETURNING id;
                """;

            return await conn.ExecuteScalarAsync<int>(insertSql, new { Name = name }, tran);
        }

        public async Task<int> CountItineraryDaysAsync(int itineraryId)
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                var (conn, tran) = RequireActiveTransaction();
                const string sqlTx = """
                    SELECT COUNT(1)
                    FROM tbl_itinerary_days
                    WHERE itinerary_id = @ItineraryId;
                    """;
                return await conn.ExecuteScalarAsync<int>(sqlTx, new { ItineraryId = itineraryId }, tran);
            }

            using var connection = await OpenStandaloneConnectionAsync();
            const string sql = """
                SELECT COUNT(1)
                FROM tbl_itinerary_days
                WHERE itinerary_id = @ItineraryId;
                """;
            return await connection.ExecuteScalarAsync<int>(sql, new { ItineraryId = itineraryId });
        }

        public async Task<int> AddItineraryDayAsync(ItineraryDay day)
        {
            const string sql = @"
INSERT INTO tbl_itinerary_days (itinerary_id, day_number, overnight_location)
VALUES (@ItineraryId, @DayNumber, @OvernightLocation)
RETURNING id;";

            if (_unitOfWork.HasActiveTransaction)
            {
                var (conn, tran) = RequireActiveTransaction();
                return await conn.ExecuteScalarAsync<int>(sql, day, tran);
            }

            using var connection = await OpenStandaloneConnectionAsync();
            return await connection.ExecuteScalarAsync<int>(sql, day);
        }

        public async Task<ItineraryDay?> GetItineraryDayByIdAsync(int itineraryDayId)
        {
            const string sql = @"
SELECT
    id,
    itinerary_id AS ItineraryId,
    day_number AS DayNumber,
    overnight_location AS OvernightLocation
FROM tbl_itinerary_days
WHERE id = @Id;";

            using var connection = await OpenStandaloneConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<ItineraryDay>(sql, new { Id = itineraryDayId });
        }

        public async Task AddAttractionToDayAsync(ItineraryAttraction itineraryAttraction)
        {
            const string sql = @"
INSERT INTO tbl_itinerary_attractions (itinerary_day_id, attraction_id, description, duration_hours)
VALUES (@ItineraryDayId, @AttractionId, @Description, @DurationHours);";

            if (_unitOfWork.HasActiveTransaction)
            {
                var (conn, tran) = RequireActiveTransaction();
                await conn.ExecuteAsync(sql, itineraryAttraction, tran);
                return;
            }

            using var connection = await OpenStandaloneConnectionAsync();
            await connection.ExecuteAsync(sql, itineraryAttraction);
        }

        public async Task AssignAccommodationAsync(ItineraryAccommodation itineraryAccommodation)
        {
            const string sql = @"
INSERT INTO tbl_itinerary_accommodations (itinerary_day_id, accommodation_id, meal_plan_id)
VALUES (@ItineraryDayId, @AccommodationId, @MealPlanId);";

            if (_unitOfWork.HasActiveTransaction)
            {
                var (conn, tran) = RequireActiveTransaction();
                await conn.ExecuteAsync(sql, itineraryAccommodation, tran);
                return;
            }

            using var connection = await OpenStandaloneConnectionAsync();
            await connection.ExecuteAsync(sql, itineraryAccommodation);
        }

        public async Task<decimal> CalculateTotalPriceAsync(int itineraryId)
        {
            const string accommodationSql = @"
SELECT COALESCE((
    SELECT SUM(ac.price_per_night)
    FROM tbl_itinerary_days d
    JOIN tbl_itinerary_accommodations iac ON iac.itinerary_day_id = d.id
    JOIN tbl_accommodations ac ON ac.id = iac.accommodation_id
    WHERE d.itinerary_id = @ItineraryId
), 0);";

            if (_unitOfWork.HasActiveTransaction)
            {
                var (conn, tran) = RequireActiveTransaction();
                return await conn.ExecuteScalarAsync<decimal>(accommodationSql, new { ItineraryId = itineraryId }, tran);
            }

            using var connection = await OpenStandaloneConnectionAsync();
            return await connection.ExecuteScalarAsync<decimal>(accommodationSql, new { ItineraryId = itineraryId });
        }

        public async Task UpdateItineraryTotalPriceAsync(int itineraryId, decimal totalPrice)
        {
            const string sql = @"
UPDATE tbl_itineraries
SET total_price = @TotalPrice
WHERE id = @Id;";

            if (_unitOfWork.HasActiveTransaction)
            {
                var (conn, tran) = RequireActiveTransaction();
                await conn.ExecuteAsync(sql, new { Id = itineraryId, TotalPrice = totalPrice }, tran);
                return;
            }

            using var connection = await OpenStandaloneConnectionAsync();
            await connection.ExecuteAsync(sql, new { Id = itineraryId, TotalPrice = totalPrice });
        }

        public async Task UpdateItineraryStatusAsync(int itineraryId, string status)
        {
            const string sql = @"
UPDATE tbl_itineraries
SET status = @Status,
    updated_at = NOW()
WHERE id = @Id;";

            if (_unitOfWork.HasActiveTransaction)
            {
                var (conn, tran) = RequireActiveTransaction();
                await conn.ExecuteAsync(sql, new { Id = itineraryId, Status = status }, tran);
                return;
            }

            using var connection = await OpenStandaloneConnectionAsync();
            await connection.ExecuteAsync(sql, new { Id = itineraryId, Status = status });
        }

        public async Task<List<ItineraryStaff>> GetItineraryStaffAsync(int itineraryId)
        {
            const string sql = @"
SELECT id, itinerary_id AS ItineraryId, staff_id AS StaffId
FROM tbl_itinerary_staff
WHERE itinerary_id = @ItineraryId;";

            using var connection = await OpenStandaloneConnectionAsync();
            var result = await connection.QueryAsync<ItineraryStaff>(sql, new { ItineraryId = itineraryId });
            return result.ToList();
        }

        public async Task AddItineraryStaffAsync(ItineraryStaff itineraryStaff)
        {
            const string sql = @"
INSERT INTO tbl_itinerary_staff (itinerary_id, staff_id)
VALUES (@ItineraryId, @StaffId);";

            if (_unitOfWork.HasActiveTransaction)
            {
                var (conn, tran) = RequireActiveTransaction();
                await conn.ExecuteAsync(sql, itineraryStaff, tran);
                return;
            }

            using var connection = await OpenStandaloneConnectionAsync();
            await connection.ExecuteAsync(sql, itineraryStaff);
        }

        public async Task<List<ItineraryListItemDto>> GetGuestItinerariesAsync(int guestId)
        {
            const string sql = """
                SELECT
                    i.id,
                    i.guest_id AS GuestId,
                    u.name AS GuestName,
                    ('Trip ' || i.id::text) AS TripName,
                    COALESCE(
                        (SELECT d1.overnight_location
                         FROM tbl_itinerary_days d1
                         WHERE d1.itinerary_id = i.id
                         ORDER BY d1.day_number ASC
                         LIMIT 1),
                        ''
                    ) AS Destination,
                    COALESCE(
                        (SELECT COUNT(1) FROM tbl_itinerary_days d2 WHERE d2.itinerary_id = i.id),
                        0
                    ) AS DaysCount,
                    i.start_date AS StartDate,
                    i.end_date AS EndDate,
                    i.status AS RawStatus,
                    CASE
                        WHEN lower(i.status) = 'draft' THEN 'draft'
                        WHEN lower(i.status) = 'submitted' THEN 'submitted'
                        WHEN lower(i.status) = 'under_review' THEN 'under_review'
                        WHEN lower(i.status) = 'returned_for_correction' THEN 'returned'
                        WHEN lower(i.status) = 'resubmitted' THEN 'submitted'
                        WHEN lower(i.status) = 'approved_by_admin' THEN 'approved'
                        WHEN lower(i.status) = 'confirmed' THEN 'confirmed'
                        WHEN lower(i.status) IN ('approved_by_staff', 'priced', 'sent_to_admin') THEN 'approved'
                        WHEN lower(i.status) = 'rejected' THEN 'rejected'
                        ELSE lower(i.status)
                    END AS Status,
                    NULL::timestamp AS SubmittedDate,
                    i.company_id AS CompanyId,
                    COALESCE(p.total_amount, i.total_price) AS TotalPrice,
                    (
                        SELECT m.message
                        FROM tbl_itinerary_messages m
                        WHERE m.itinerary_id = i.id
                        ORDER BY m.created_at DESC, m.id DESC
                        LIMIT 1
                    ) AS LastMessagePreview
                FROM tbl_itineraries i
                INNER JOIN tbl_users u ON u.id = i.guest_id
                LEFT JOIN LATERAL (
                    SELECT pp.total_amount
                    FROM tbl_itinerary_pricing pp
                    WHERE pp.itinerary_id = i.id
                    ORDER BY pp.created_at DESC
                    LIMIT 1
                ) p ON true
                WHERE i.guest_id = @GuestId
                ORDER BY i.created_at DESC
            """;

            using var connection = await OpenStandaloneConnectionAsync();
            var rows = await connection.QueryAsync<ItineraryListItemDto>(sql, new { GuestId = guestId });
            return rows.ToList();
        }

        public async Task<List<ItineraryListItemDto>> GetSubmittedItinerariesAsync(int companyId)
        {
            const string sql = """
                SELECT
                    i.id,
                    i.guest_id AS GuestId,
                    u.name AS GuestName,
                    ('Trip ' || i.id::text) AS TripName,
                    COALESCE(
                        (SELECT d1.overnight_location
                         FROM tbl_itinerary_days d1
                         WHERE d1.itinerary_id = i.id
                         ORDER BY d1.day_number ASC
                         LIMIT 1),
                        ''
                    ) AS Destination,
                    COALESCE(
                        (SELECT COUNT(1) FROM tbl_itinerary_days d2 WHERE d2.itinerary_id = i.id),
                        0
                    ) AS DaysCount,
                    i.start_date AS StartDate,
                    i.end_date AS EndDate,
                    i.status AS RawStatus,
                    'pending' AS Status,
                    i.updated_at::date AS SubmittedDate,
                    i.company_id AS CompanyId,
                    COALESCE(p.total_amount, i.total_price) AS TotalPrice
                FROM tbl_itineraries i
                INNER JOIN tbl_users u ON u.id = i.guest_id
                LEFT JOIN LATERAL (
                    SELECT pp.total_amount
                    FROM tbl_itinerary_pricing pp
                    WHERE pp.itinerary_id = i.id
                    ORDER BY pp.created_at DESC
                    LIMIT 1
                ) p ON true
                WHERE i.company_id = @CompanyId
                  AND LOWER(i.status) IN ('submitted', 'under_review', 'returned_for_correction', 'resubmitted')
                ORDER BY i.created_at DESC
            """;

            using var connection = await OpenStandaloneConnectionAsync();
            var rows = await connection.QueryAsync<ItineraryListItemDto>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }

        public async Task<List<ItineraryListItemDto>> GetCompanyItinerariesAsync(int companyId)
        {
            const string sql = """
                SELECT
                    i.id,
                    i.guest_id AS GuestId,
                    u.name AS GuestName,
                    ('Trip ' || i.id::text) AS TripName,
                    COALESCE(
                        (SELECT d1.overnight_location
                         FROM tbl_itinerary_days d1
                         WHERE d1.itinerary_id = i.id
                         ORDER BY d1.day_number ASC
                         LIMIT 1),
                        ''
                    ) AS Destination,
                    COALESCE(
                        (SELECT COUNT(1) FROM tbl_itinerary_days d2 WHERE d2.itinerary_id = i.id),
                        0
                    ) AS DaysCount,
                    i.start_date AS StartDate,
                    i.end_date AS EndDate,
                    i.status AS RawStatus,
                    CASE
                        WHEN lower(i.status) = 'draft' THEN 'draft'
                        WHEN lower(i.status) = 'submitted' THEN 'submitted'
                        WHEN lower(i.status) = 'under_review' THEN 'under_review'
                        WHEN lower(i.status) = 'returned_for_correction' THEN 'returned'
                        WHEN lower(i.status) = 'resubmitted' THEN 'submitted'
                        WHEN lower(i.status) = 'approved_by_admin' THEN 'approved'
                        WHEN lower(i.status) = 'confirmed' THEN 'confirmed'
                        WHEN lower(i.status) IN ('approved_by_staff', 'priced', 'sent_to_admin') THEN 'approved'
                        WHEN lower(i.status) = 'rejected' THEN 'rejected'
                        ELSE lower(i.status)
                    END AS Status,
                    i.updated_at::date AS SubmittedDate,
                    i.company_id AS CompanyId,
                    COALESCE(p.total_amount, i.total_price) AS TotalPrice
                FROM tbl_itineraries i
                INNER JOIN tbl_users u ON u.id = i.guest_id
                LEFT JOIN LATERAL (
                    SELECT pp.total_amount
                    FROM tbl_itinerary_pricing pp
                    WHERE pp.itinerary_id = i.id
                    ORDER BY pp.created_at DESC
                    LIMIT 1
                ) p ON true
                WHERE i.company_id = @CompanyId
                  AND LOWER(i.status) IN ('submitted', 'under_review', 'priced', 'sent_to_admin', 'approved_by_admin')
                ORDER BY i.created_at DESC
            """;

            using var connection = await OpenStandaloneConnectionAsync();
            var rows = await connection.QueryAsync<ItineraryListItemDto>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }

        public async Task<List<ItineraryListItemDto>> GetAllOwnerSubmittedItinerariesAsync()
        {
            const string sql = """
                SELECT
                    i.id,
                    i.guest_id AS GuestId,
                    u.name AS GuestName,
                    ('Trip ' || i.id::text) AS TripName,
                    COALESCE(
                        (SELECT d1.overnight_location
                         FROM tbl_itinerary_days d1
                         WHERE d1.itinerary_id = i.id
                         ORDER BY d1.day_number ASC
                         LIMIT 1),
                        ''
                    ) AS Destination,
                    COALESCE(
                        (SELECT COUNT(1) FROM tbl_itinerary_days d2 WHERE d2.itinerary_id = i.id),
                        0
                    ) AS DaysCount,
                    i.start_date AS StartDate,
                    i.end_date AS EndDate,
                    i.status AS RawStatus,
                    'submitted' AS Status,
                    i.updated_at::date AS SubmittedDate,
                    i.company_id AS CompanyId,
                    COALESCE(p.total_amount, i.total_price) AS TotalPrice
                FROM tbl_itineraries i
                INNER JOIN tbl_users u ON u.id = i.guest_id
                LEFT JOIN LATERAL (
                    SELECT pp.total_amount
                    FROM tbl_itinerary_pricing pp
                    WHERE pp.itinerary_id = i.id
                    ORDER BY pp.created_at DESC
                    LIMIT 1
                ) p ON true
                WHERE LOWER(i.status) = 'submitted'
                   OR (
                        LOWER(i.status) = 'under_review' AND (
                            SELECT r.status
                            FROM tbl_itinerary_reviews r
                            WHERE r.itinerary_id = i.id
                            ORDER BY r.created_at DESC
                            LIMIT 1
                        ) = 'REQUESTED_CHANGES'
                   )
                ORDER BY i.updated_at DESC NULLS LAST, i.created_at DESC
            """;

            using var connection = await OpenStandaloneConnectionAsync();
            var rows = await connection.QueryAsync<ItineraryListItemDto>(sql);
            return rows.ToList();
        }

        public async Task<string?> GetLatestReviewStatusAsync(int itineraryId)
        {
            const string sql = """
                SELECT status
                FROM tbl_itinerary_reviews
                WHERE itinerary_id = @ItineraryId
                ORDER BY created_at DESC
                LIMIT 1;
            """;

            using var connection = await OpenStandaloneConnectionAsync();
            return await connection.ExecuteScalarAsync<string?>(sql, new { ItineraryId = itineraryId });
        }

        public async Task DeleteDraftItineraryAsync(int itineraryId, int travelerId)
        {
            var (conn, tran) = RequireActiveTransaction();
            const string sql = """
                DELETE FROM tbl_itineraries
                WHERE id = @ItineraryId
                  AND guest_id = @TravelerId
                  AND LOWER(status) = 'draft';
            """;
            var affected = await conn.ExecuteAsync(sql, new { ItineraryId = itineraryId, TravelerId = travelerId }, tran);
            if (affected == 0)
            {
                throw new InvalidOperationException("Only draft itineraries can be deleted.");
            }
        }

        public async Task<List<ItineraryMessageDto>> GetItineraryMessagesAsync(int itineraryId)
        {
            const string sql = """
                SELECT
                    id,
                    itinerary_id AS ItineraryId,
                    sender_id AS SenderId,
                    sender_role AS SenderRole,
                    message,
                    type,
                    created_at AS CreatedAt
                FROM tbl_itinerary_messages
                WHERE itinerary_id = @ItineraryId
                ORDER BY created_at ASC, id ASC;
            """;

            using var connection = await OpenStandaloneConnectionAsync();
            var rows = await connection.QueryAsync<ItineraryMessageDto>(sql, new { ItineraryId = itineraryId });
            return rows.ToList();
        }

        public async Task<int> AddItineraryMessageAsync(int itineraryId, int senderId, string senderRole, string message, string type)
        {
            const string sql = """
                INSERT INTO tbl_itinerary_messages (itinerary_id, sender_id, sender_role, message, type, created_at)
                VALUES (@ItineraryId, @SenderId, @SenderRole, @Message, @Type, NOW())
                RETURNING id;
            """;

            if (_unitOfWork.HasActiveTransaction)
            {
                var (conn, tran) = RequireActiveTransaction();
                return await conn.ExecuteScalarAsync<int>(sql, new
                {
                    ItineraryId = itineraryId,
                    SenderId = senderId,
                    SenderRole = senderRole,
                    Message = message,
                    Type = type
                }, tran);
            }

            using var connection = await OpenStandaloneConnectionAsync();
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                ItineraryId = itineraryId,
                SenderId = senderId,
                SenderRole = senderRole,
                Message = message,
                Type = type
            });
        }

        public async Task<string?> GetLastItineraryMessageAsync(int itineraryId)
        {
            const string sql = """
                SELECT message
                FROM tbl_itinerary_messages
                WHERE itinerary_id = @ItineraryId
                ORDER BY created_at DESC, id DESC
                LIMIT 1;
            """;
            using var connection = await OpenStandaloneConnectionAsync();
            return await connection.ExecuteScalarAsync<string?>(sql, new { ItineraryId = itineraryId });
        }
    }
}