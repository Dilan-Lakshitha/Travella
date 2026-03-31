using Dapper;
using Travella.Application.Interfaces;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class PricingRepository : IPricingRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PricingRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateAsync(int itineraryId, int createdBy, decimal totalAmount)
        {
            const string sql = """
                INSERT INTO tbl_itinerary_pricing (itinerary_id, created_by, total_amount, status, created_at)
                VALUES (@ItineraryId, @CreatedBy, @TotalAmount, 'PENDING', NOW())
                RETURNING id
            """;

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                ItineraryId = itineraryId,
                CreatedBy = createdBy,
                TotalAmount = totalAmount
            });
        }
    }
}
