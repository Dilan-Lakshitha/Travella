using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly UnitOfWork _unitOfWork;

        public StaffRepository(IDbConnectionFactory connectionFactory, UnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        private System.Data.IDbConnection GetConnection(bool useTransaction)
        {
            return useTransaction ? _unitOfWork.Connection : _connectionFactory.CreateConnection();
        }

        public async Task<Staff?> GetStaffByIdAsync(int staffId)
        {
            const string sql = @"
SELECT id, name, role
FROM ""Staff""
WHERE id = @Id;";

            using var connection = GetConnection(useTransaction: false);
            return await connection.QuerySingleOrDefaultAsync<Staff>(sql, new { Id = staffId });
        }

        public async Task<List<Staff>> GetAvailableStaffAsync(DateTime startDate, DateTime endDate, string? role = null)
        {
            const string sql = @"
SELECT s.id, s.name, s.role
FROM ""Staff"" s
WHERE NOT EXISTS (
    SELECT 1
    FROM ""StaffAvailability"" sa
    WHERE sa.staff_id = s.id
      AND sa.date BETWEEN @StartDate AND @EndDate
      AND sa.status = 'Locked'
)
AND (@Role IS NULL OR s.role = @Role);";

            using var connection = GetConnection(useTransaction: false);
            var result = await connection.QueryAsync<Staff>(sql, new
            {
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                Role = role
            });

            return result.ToList();
        }

        public async Task<bool> IsStaffAvailableAsync(int staffId, DateTime startDate, DateTime endDate)
        {
            const string sql = @"
SELECT COUNT(1)
FROM ""StaffAvailability""
WHERE staff_id = @StaffId
  AND date BETWEEN @StartDate AND @EndDate
  AND status = 'Locked';";

            using var connection = GetConnection(useTransaction: false);
            var lockedCount = await connection.ExecuteScalarAsync<int>(sql, new
            {
                StaffId = staffId,
                StartDate = startDate.Date,
                EndDate = endDate.Date
            });

            return lockedCount == 0;
        }

        public async Task AssignStaffToItineraryAsync(int itineraryId, int staffId)
        {
            const string sql = @"
INSERT INTO ""ItineraryStaff"" (itinerary_id, staff_id)
VALUES (@ItineraryId, @StaffId);";

            using var connection = GetConnection(useTransaction: true);
            await connection.ExecuteAsync(sql, new { ItineraryId = itineraryId, StaffId = staffId }, _unitOfWork.Transaction);
        }

        public async Task LockStaffForItineraryAsync(int itineraryId, int staffId, DateTime startDate, DateTime endDate)
        {
            const string sql = @"
INSERT INTO ""StaffAvailability"" (staff_id, date, status)
SELECT @StaffId, d::date, 'Locked'
FROM generate_series(@StartDate::date, @EndDate::date, interval '1 day') AS d
ON CONFLICT (staff_id, date) DO UPDATE SET status = 'Locked';";

            using var connection = GetConnection(useTransaction: true);
            await connection.ExecuteAsync(sql, new
            {
                StaffId = staffId,
                StartDate = startDate.Date,
                EndDate = endDate.Date
            }, _unitOfWork.Transaction);
        }
    }
}