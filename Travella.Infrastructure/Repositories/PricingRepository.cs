using Dapper;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class PricingRepository : IPricingRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUnitOfWork _unitOfWork;

        public PricingRepository(IDbConnectionFactory connectionFactory, IUnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateAsync(ItineraryPricingInputDto dto, int createdBy)
        {
            const string sql = """
                INSERT INTO tbl_itinerary_pricing (
                    itinerary_id,
                    created_by,
                    driver_cost,
                    guide_cost,
                    vehicle_cost,
                    mileage_rate,
                    total_km,
                    accommodation_cost,
                    meal_plan,
                    profit_margin,
                    total_amount,
                    status,
                    created_at
                )
                VALUES (
                    @ItineraryId,
                    @CreatedBy,
                    @DriverCost,
                    @GuideCost,
                    @VehicleCost,
                    @MileageRate,
                    @TotalKm,
                    @AccommodationCost,
                    @MealPlan,
                    @ProfitMargin,
                    @TotalAmount,
                    'PENDING',
                    NOW()
                )
                RETURNING id
            """;

            if (_unitOfWork.HasActiveTransaction && _unitOfWork.CurrentTransaction != null)
            {
                return await _unitOfWork.Connection.ExecuteScalarAsync<int>(
                    sql,
                    new
                    {
                        dto.ItineraryId,
                        CreatedBy = createdBy,
                        dto.DriverCost,
                        dto.GuideCost,
                        dto.VehicleCost,
                        dto.MileageRate,
                        dto.TotalKm,
                        dto.AccommodationCost,
                        MealPlan = dto.MealPlan,
                        dto.ProfitMargin,
                        dto.TotalAmount
                    },
                    _unitOfWork.CurrentTransaction);
            }


            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                dto.ItineraryId,
                CreatedBy = createdBy,
                dto.DriverCost,
                dto.GuideCost,
                dto.VehicleCost,
                dto.MileageRate,
                dto.TotalKm,
                dto.AccommodationCost,
                MealPlan = dto.MealPlan,
                dto.ProfitMargin,
                dto.TotalAmount
            });
        }

        public async Task<bool> PricingExistsAsync(int itineraryId)
        {
            const string sql = """
                SELECT EXISTS (
                    SELECT 1
                    FROM tbl_itinerary_pricing
                    WHERE itinerary_id = @ItineraryId
                );
            """;

            if (_unitOfWork.HasActiveTransaction && _unitOfWork.CurrentTransaction != null)
            {
                return await _unitOfWork.Connection.ExecuteScalarAsync<bool>(
                    sql,
                    new { ItineraryId = itineraryId },
                    _unitOfWork.CurrentTransaction);
            }

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<bool>(sql, new { ItineraryId = itineraryId });
        }

        public async Task<bool> UpdateMarginAsync(int itineraryId, decimal profitMargin)
        {
            const string sql = """
                UPDATE tbl_itinerary_pricing p
                SET profit_margin = @ProfitMargin,
                    total_amount = (
                        COALESCE(p.driver_cost, 0) +
                        COALESCE(p.guide_cost, 0) +
                        COALESCE(p.vehicle_cost, 0) +
                        (COALESCE(p.mileage_rate, 0) * COALESCE(p.total_km, 0)) +
                        COALESCE(p.accommodation_cost, 0) +
                        (
                            (COALESCE(p.driver_cost, 0) +
                            COALESCE(p.guide_cost, 0) +
                            COALESCE(p.vehicle_cost, 0) +
                            (COALESCE(p.mileage_rate, 0) * COALESCE(p.total_km, 0)) +
                            COALESCE(p.accommodation_cost, 0))
                            * (@ProfitMargin / 100.0)
                        )
                    )
                WHERE itinerary_id = @ItineraryId;
            """;

            if (_unitOfWork.HasActiveTransaction && _unitOfWork.CurrentTransaction != null)
            {
                var affected = await _unitOfWork.Connection.ExecuteAsync(
                    sql,
                    new { ItineraryId = itineraryId, ProfitMargin = profitMargin },
                    _unitOfWork.CurrentTransaction);
                return affected > 0;
            }

            using var connection = _connectionFactory.CreateConnection();
            var standaloneAffected = await connection.ExecuteAsync(sql, new { ItineraryId = itineraryId, ProfitMargin = profitMargin });
            return standaloneAffected > 0;
        }
    }
}
