using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class ItineraryRepository : IItineraryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly UnitOfWork _unitOfWork;

        public ItineraryRepository(IDbConnectionFactory connectionFactory, UnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        private System.Data.IDbConnection GetConnection(bool useTransaction)
        {
            return useTransaction ? _unitOfWork.Connection : _connectionFactory.CreateConnection();
        }

        public async Task<int> CreateItineraryAsync(Itinerary itinerary)
        {
            const string sql = @"
INSERT INTO ""Itineraries"" (guest_id, start_date, end_date, status, total_price)
VALUES (@GuestId, @StartDate, @EndDate, @Status, @TotalPrice)
RETURNING id;";

            using var connection = GetConnection(useTransaction: false);
            return await connection.ExecuteScalarAsync<int>(sql, itinerary);
        }

        public async Task<bool> ItineraryExistsAsync(int itineraryId)
        {
            const string sql = @"SELECT COUNT(1) FROM ""Itineraries"" WHERE id = @Id;";
            using var connection = GetConnection(useTransaction: false);
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
    total_price AS TotalPrice
FROM ""Itineraries""
WHERE id = @Id;";

            using var connection = GetConnection(useTransaction: false);
            return await connection.QuerySingleOrDefaultAsync<Itinerary>(sql, new { Id = itineraryId });
        }

        public async Task<ItineraryResponseDto?> GetItineraryDetailsAsync(int itineraryId)
        {
            const string sql = @"
SELECT
    i.id AS itinerary_id,
    i.guest_id,
    u.name AS guest_name,
    i.start_date,
    i.end_date,
    i.status,
    i.total_price,
    d.id AS day_id,
    d.day_number,
    d.overnight_location,
    ia.id AS itinerary_attraction_id,
    a.id AS attraction_id,
    a.name AS attraction_name,
    a.city,
    a.country,
    ia.description,
    ia.duration_hours,
    iac.id AS itinerary_accommodation_id,
    ac.id AS accommodation_id,
    ac.name AS accommodation_name,
    ac.location,
    ac.price_per_night,
    mp.id AS meal_plan_id,
    mp.code AS meal_plan_code,
    s.id AS staff_id,
    s.name AS staff_name,
    s.role AS staff_role
FROM ""Itineraries"" i
JOIN ""Users"" u ON u.id = i.guest_id
LEFT JOIN ""ItineraryDays"" d ON d.itinerary_id = i.id
LEFT JOIN ""ItineraryAttractions"" ia ON ia.itinerary_day_id = d.id
LEFT JOIN ""Attractions"" a ON a.id = ia.attraction_id
LEFT JOIN ""ItineraryAccommodations"" iac ON iac.itinerary_day_id = d.id
LEFT JOIN ""Accommodations"" ac ON ac.id = iac.accommodation_id
LEFT JOIN ""MealPlans"" mp ON mp.id = iac.meal_plan_id
LEFT JOIN ""ItineraryStaff"" ist ON ist.itinerary_id = i.id
LEFT JOIN ""Staff"" s ON s.id = ist.staff_id
WHERE i.id = @Id
ORDER BY d.day_number;";

            using var connection = GetConnection(useTransaction: false);

            ItineraryResponseDto? itinerary = null;
            var dayIdToDay = new Dictionary<int, ItineraryDayResponseDto>();
            var staffIds = new HashSet<int>();

            var rows = await connection.QueryAsync(sql, new { Id = itineraryId });
            foreach (var row in rows)
            {
                if (itinerary == null)
                {
                    itinerary = new ItineraryResponseDto
                    {
                        Id = row.itinerary_id,
                        GuestId = row.guest_id,
                        GuestName = row.guest_name,
                        StartDate = row.start_date,
                        EndDate = row.end_date,
                        Status = row.status,
                        TotalPrice = row.total_price
                    };
                }

                if (row.day_id != null)
                {
                    int dayId = row.day_id;
                    if (!dayIdToDay.TryGetValue(dayId, out var day))
                    {
                        day = new ItineraryDayResponseDto
                        {
                            Id = dayId,
                            DayNumber = row.day_number,
                            OvernightLocation = row.overnight_location
                        };

                        dayIdToDay.Add(dayId, day);
                        itinerary.Days.Add(day);
                    }

                    if (row.itinerary_attraction_id != null)
                    {
                        day.Attractions.Add(new ItineraryAttractionResponseDto
                        {
                            Id = row.itinerary_attraction_id,
                            AttractionId = row.attraction_id,
                            AttractionName = row.attraction_name,
                            City = row.city,
                            Country = row.country,
                            Description = row.description,
                            DurationHours = row.duration_hours
                        });
                    }

                    if (row.itinerary_accommodation_id != null)
                    {
                        day.Accommodations.Add(new ItineraryAccommodationResponseDto
                        {
                            Id = row.itinerary_accommodation_id,
                            AccommodationId = row.accommodation_id,
                            AccommodationName = row.accommodation_name,
                            Location = row.location,
                            PricePerNight = row.price_per_night,
                            MealPlanId = row.meal_plan_id,
                            MealPlanCode = row.meal_plan_code
                        });
                    }
                }

                if (row.staff_id != null)
                {
                    int staffId = row.staff_id;
                    if (staffIds.Add(staffId))
                    {
                        itinerary.Staff.Add(new ItineraryStaffSummaryDto
                        {
                            StaffId = staffId,
                            StaffName = row.staff_name,
                            Role = row.staff_role
                        });
                    }
                }
            }

            return itinerary;
        }

        public async Task AddItineraryDayAsync(ItineraryDay day)
        {
            const string sql = @"
INSERT INTO ""ItineraryDays"" (itinerary_id, day_number, overnight_location)
VALUES (@ItineraryId, @DayNumber, @OvernightLocation);";

            using var connection = GetConnection(useTransaction: false);
            await connection.ExecuteAsync(sql, day);
        }

        public async Task<ItineraryDay?> GetItineraryDayByIdAsync(int itineraryDayId)
        {
            const string sql = @"
SELECT
    id,
    itinerary_id AS ItineraryId,
    day_number AS DayNumber,
    overnight_location AS OvernightLocation
FROM ""ItineraryDays""
WHERE id = @Id;";

            using var connection = GetConnection(useTransaction: false);
            return await connection.QuerySingleOrDefaultAsync<ItineraryDay>(sql, new { Id = itineraryDayId });
        }

        public async Task AddAttractionToDayAsync(ItineraryAttraction itineraryAttraction)
        {
            const string sql = @"
INSERT INTO ""ItineraryAttractions"" (itinerary_day_id, attraction_id, description, duration_hours)
VALUES (@ItineraryDayId, @AttractionId, @Description, @DurationHours);";

            using var connection = GetConnection(useTransaction: false);
            await connection.ExecuteAsync(sql, itineraryAttraction);
        }

        public async Task AssignAccommodationAsync(ItineraryAccommodation itineraryAccommodation)
        {
            const string sql = @"
INSERT INTO ""ItineraryAccommodations"" (itinerary_day_id, accommodation_id, meal_plan_id)
VALUES (@ItineraryDayId, @AccommodationId, @MealPlanId);";

            using var connection = GetConnection(useTransaction: false);
            await connection.ExecuteAsync(sql, itineraryAccommodation);
        }

        public async Task<decimal> CalculateTotalPriceAsync(int itineraryId)
        {
            const string accommodationSql = @"
SELECT COALESCE(SUM(ac.price_per_night), 0)
FROM ""ItineraryDays"" d
JOIN ""ItineraryAccommodations"" iac ON iac.itinerary_day_id = d.id
JOIN ""Accommodations"" ac ON ac.id = iac.accommodation_id
WHERE d.itinerary_id = @ItineraryId;";

            using var connection = GetConnection(useTransaction: false);
            var accommodationTotal = await connection.ExecuteScalarAsync<decimal>(
                accommodationSql,
                new { ItineraryId = itineraryId });

            decimal attractionTotal = 0m;
            return accommodationTotal + attractionTotal;
        }

        public async Task UpdateItineraryTotalPriceAsync(int itineraryId, decimal totalPrice)
        {
            const string sql = @"
UPDATE ""Itineraries""
SET total_price = @TotalPrice
WHERE id = @Id;";

            using var connection = GetConnection(useTransaction: true);
            await connection.ExecuteAsync(sql, new { Id = itineraryId, TotalPrice = totalPrice }, _unitOfWork.Transaction);
        }

        public async Task UpdateItineraryStatusAsync(int itineraryId, string status)
        {
            const string sql = @"
UPDATE ""Itineraries""
SET status = @Status
WHERE id = @Id;";

            using var connection = GetConnection(useTransaction: true);
            await connection.ExecuteAsync(sql, new { Id = itineraryId, Status = status }, _unitOfWork.Transaction);
        }

        public async Task<List<ItineraryStaff>> GetItineraryStaffAsync(int itineraryId)
        {
            const string sql = @"
SELECT id, itinerary_id AS ItineraryId, staff_id AS StaffId
FROM ""ItineraryStaff""
WHERE itinerary_id = @ItineraryId;";

            using var connection = GetConnection(useTransaction: false);
            var result = await connection.QueryAsync<ItineraryStaff>(sql, new { ItineraryId = itineraryId });
            return result.ToList();
        }

        public async Task AddItineraryStaffAsync(ItineraryStaff itineraryStaff)
        {
            const string sql = @"
INSERT INTO ""ItineraryStaff"" (itinerary_id, staff_id)
VALUES (@ItineraryId, @StaffId);";

            using var connection = GetConnection(useTransaction: true);
            await connection.ExecuteAsync(sql, itineraryStaff, _unitOfWork.Transaction);
        }
    }
}