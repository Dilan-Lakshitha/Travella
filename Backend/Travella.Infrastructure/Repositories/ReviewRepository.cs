using Dapper;
using Travella.Application.Interfaces;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ReviewRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> AddAsync(int itineraryId, int reviewerId, string reviewerRole, string comments, string status)
        {
            const string sql = """
                INSERT INTO tbl_itinerary_reviews (itinerary_id, reviewer_id, reviewer_role, comments, status, created_at)
                VALUES (@ItineraryId, @ReviewerId, @ReviewerRole, @Comments, @Status, NOW())
                RETURNING id
            """;

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                ItineraryId = itineraryId,
                ReviewerId = reviewerId,
                ReviewerRole = reviewerRole,
                Comments = comments,
                Status = status
            });
        }
    }
}
