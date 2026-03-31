using System.Threading.Tasks;
using Dapper;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly UnitOfWork _unitOfWork;

        public BookingRepository(IDbConnectionFactory connectionFactory, UnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        private System.Data.IDbConnection GetConnection(bool useTransaction)
        {
            return useTransaction ? _unitOfWork.Connection : _connectionFactory.CreateConnection();
        }

        public async Task<Booking?> GetBookingByItineraryIdAsync(int itineraryId)
        {
            const string sql = @"
SELECT id, itinerary_id AS ItineraryId, confirmed_at AS ConfirmedAt, invoice_number AS InvoiceNumber
FROM ""Bookings""
WHERE itinerary_id = @ItineraryId;";

            using var connection = GetConnection(useTransaction: false);
            return await connection.QuerySingleOrDefaultAsync<Booking>(sql, new { ItineraryId = itineraryId });
        }

        public async Task<int> CreateBookingAsync(Booking booking)
        {
            const string sql = @"
INSERT INTO ""Bookings"" (itinerary_id, confirmed_at, invoice_number)
VALUES (@ItineraryId, @ConfirmedAt, @InvoiceNumber)
RETURNING id;";

            using var connection = GetConnection(useTransaction: true);
            return await connection.ExecuteScalarAsync<int>(sql, booking, _unitOfWork.Transaction);
        }
    }
}