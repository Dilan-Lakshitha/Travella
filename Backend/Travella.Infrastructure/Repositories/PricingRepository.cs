using System;
using System.Collections.Generic;
using System.Linq;
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

        private const string PricingSelectColumns = """
            id AS Id,
            itinerary_id AS ItineraryId,
            created_by AS CreatedBy,
            driver_cost AS DriverCost,
            guide_cost AS GuideCost,
            vehicle_cost AS VehicleCost,
            mileage_rate AS MileageRate,
            total_km AS TotalKm,
            accommodation_cost AS AccommodationCost,
            meal_plan AS MealPlan,
            profit_margin AS ProfitMargin,
            total_amount AS TotalAmount,
            status AS Status,
            created_at AS CreatedAt
            """;

        public PricingRepository(IDbConnectionFactory connectionFactory, IUnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateAsync(ItineraryPricingInputDto dto, int createdBy)
        {
            PricingCalculator.ApplyCalculatedTotal(dto);

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
                    'PRICED',
                    NOW()
                )
                RETURNING id
            """;

            var parameters = new
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
            };

            if (_unitOfWork.HasActiveTransaction && _unitOfWork.CurrentTransaction != null)
            {
                return await _unitOfWork.Connection.ExecuteScalarAsync<int>(
                    sql,
                    parameters,
                    _unitOfWork.CurrentTransaction);
            }

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<ItineraryPricingDetailDto?> GetLatestByItineraryIdAsync(int itineraryId)
        {
            var sql = $"""
                SELECT {PricingSelectColumns}
                FROM tbl_itinerary_pricing
                WHERE itinerary_id = @ItineraryId
                ORDER BY created_at DESC, id DESC
                LIMIT 1
            """;

            if (_unitOfWork.HasActiveTransaction && _unitOfWork.CurrentTransaction != null)
            {
                return await _unitOfWork.Connection.QuerySingleOrDefaultAsync<ItineraryPricingDetailDto>(
                    sql,
                    new { ItineraryId = itineraryId },
                    _unitOfWork.CurrentTransaction);
            }

            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<ItineraryPricingDetailDto>(sql, new { ItineraryId = itineraryId });
        }

        public async Task<Dictionary<int, ItineraryPricingDetailDto>> GetLatestByItineraryIdsAsync(IReadOnlyCollection<int> itineraryIds)
        {
            if (itineraryIds.Count == 0)
            {
                return new Dictionary<int, ItineraryPricingDetailDto>();
            }

            const string sql = """
                SELECT DISTINCT ON (itinerary_id)
                    id AS Id,
                    itinerary_id AS ItineraryId,
                    created_by AS CreatedBy,
                    driver_cost AS DriverCost,
                    guide_cost AS GuideCost,
                    vehicle_cost AS VehicleCost,
                    mileage_rate AS MileageRate,
                    total_km AS TotalKm,
                    accommodation_cost AS AccommodationCost,
                    meal_plan AS MealPlan,
                    profit_margin AS ProfitMargin,
                    total_amount AS TotalAmount,
                    status AS Status,
                    created_at AS CreatedAt
                FROM tbl_itinerary_pricing
                WHERE itinerary_id = ANY(@ItineraryIds)
                ORDER BY itinerary_id, created_at DESC, id DESC
            """;

            using var connection = _connectionFactory.CreateConnection();
            var rows = await connection.QueryAsync<ItineraryPricingDetailDto>(
                sql,
                new { ItineraryIds = itineraryIds.ToArray() });

            return rows.ToDictionary(r => r.ItineraryId, r => r);
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

        public async Task<ItineraryPricingDetailDto?> UpdateMarginAsync(int itineraryId, decimal profitMargin)
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
                WHERE p.id = (
                    SELECT pp.id
                    FROM tbl_itinerary_pricing pp
                    WHERE pp.itinerary_id = @ItineraryId
                    ORDER BY pp.created_at DESC, pp.id DESC
                    LIMIT 1
                )
                RETURNING id
            """;

            int? updatedId;
            if (_unitOfWork.HasActiveTransaction && _unitOfWork.CurrentTransaction != null)
            {
                updatedId = await _unitOfWork.Connection.ExecuteScalarAsync<int?>(
                    sql,
                    new { ItineraryId = itineraryId, ProfitMargin = profitMargin },
                    _unitOfWork.CurrentTransaction);
            }
            else
            {
                using var connection = _connectionFactory.CreateConnection();
                updatedId = await connection.ExecuteScalarAsync<int?>(
                    sql,
                    new { ItineraryId = itineraryId, ProfitMargin = profitMargin });
            }

            if (!updatedId.HasValue)
            {
                return null;
            }

            return await GetLatestByItineraryIdAsync(itineraryId);
        }
    }
}
