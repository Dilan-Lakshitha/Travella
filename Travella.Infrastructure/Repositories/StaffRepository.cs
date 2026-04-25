using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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

        private IDbConnection GetConnection(bool useTransaction)
        {
            if (useTransaction)
                return _unitOfWork.Connection;

            return _connectionFactory.CreateConnection();
        }
        public async Task<Staff?> GetStaffByIdAsync(int staffId)
        {
            const string sql = @"
SELECT id, name, company_id AS CompanyId, type AS Role, phone AS Phone, experience AS Experience, availability AS Availability
FROM tbl_staff
WHERE id = @Id;";

            var connection = _unitOfWork.HasActiveTransaction
        ? _unitOfWork.Connection
        : _connectionFactory.CreateConnection();

            var transaction = _unitOfWork.HasActiveTransaction
                ? _unitOfWork.CurrentTransaction
                : null;

            return await connection.QueryFirstOrDefaultAsync<Staff>(sql, new { Id = staffId }, transaction);
        }

        public async Task<List<Staff>> GetAvailableStaffAsync(int companyId, DateOnly startDate, DateOnly endDate, string? role = null)
        {
            const string sql = @"
SELECT s.id, s.name, s.company_id AS CompanyId, s.type AS Role, s.phone AS Phone, s.experience AS Experience, s.availability AS Availability
FROM tbl_staff s
WHERE NOT EXISTS (
    SELECT 1
    FROM tbl_staff_availability sa
    WHERE sa.staff_id = s.id
      AND sa.date BETWEEN @StartDate::date AND @EndDate::date
      AND UPPER(sa.status) = 'BOOKED'
)
AND s.company_id = @CompanyId
AND (@Role IS NULL OR s.type = @Role);";

            using var connection = GetConnection(useTransaction: false);
            var result = await connection.QueryAsync<Staff>(sql, new
            {
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MinValue),
                CompanyId = companyId,
                Role = role
            });

            return result.ToList();
        }

        public async Task<List<Staff>> GetDriversAsync(int companyId)
        {
            const string sql = @"
SELECT id, name, company_id AS CompanyId, type AS Role, phone AS Phone, experience AS Experience, availability AS Availability
FROM tbl_staff
WHERE company_id = @CompanyId
  AND type = 'DRIVER'
ORDER BY id DESC;";

            using var connection = GetConnection(useTransaction: false);
            var rows = await connection.QueryAsync<Staff>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }

        public async Task<List<Staff>> GetGuidesAsync(int companyId)
        {
            const string sql = @"
SELECT id, name, company_id AS CompanyId, type AS Role, phone AS Phone, experience AS Experience, availability AS Availability
FROM tbl_staff
WHERE company_id = @CompanyId
  AND type = 'GUIDE'
ORDER BY id DESC;";

            using var connection = GetConnection(useTransaction: false);
            var rows = await connection.QueryAsync<Staff>(sql, new { CompanyId = companyId });
            return rows.ToList();
        }

        public async Task<int> CreateStaffResourceAsync(Staff staffResource)
        {
            const string sql = @"
INSERT INTO tbl_staff (name, company_id, type, phone, experience, availability)
VALUES (@Name, @CompanyId, @Role, @Phone, @Experience, @Availability)
RETURNING id;";


            var connection = _unitOfWork.Connection;

            return await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    staffResource.Name,
                    staffResource.CompanyId,
                    Role = staffResource.Role,
                    staffResource.Phone,
                    staffResource.Experience,
                    Availability = staffResource.Availability
                },
                _unitOfWork.Transaction
            );
        }

        public async Task<bool> IsStaffAvailableAsync(int staffId, DateOnly startDate, DateOnly endDate)
        {
            const string sql = @"
SELECT COUNT(1)
FROM tbl_staff_availability
WHERE staff_id = @StaffId
  AND date BETWEEN @StartDate AND @EndDate
  AND UPPER(status) = 'BOOKED';";

            using var connection = GetConnection(useTransaction: false);
            var lockedCount = await connection.ExecuteScalarAsync<int>(sql, new
            {
                StaffId = staffId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MinValue),
            });

            return lockedCount == 0;
        }

        public async Task AssignStaffToItineraryAsync(int itineraryId, int staffId)
        {
            const string sql = @"
INSERT INTO tbl_itinerary_staff (itinerary_id, staff_id)
VALUES (@ItineraryId, @StaffId);";

            var connection = GetConnection(useTransaction: true);
            await connection.ExecuteAsync(sql, new { ItineraryId = itineraryId, StaffId = staffId }, _unitOfWork.Transaction);
        }

        public async Task LockStaffForItineraryAsync(
    int itineraryId,
    int staffId,
    DateOnly startDate,
    DateOnly endDate)
        {
            const string updateSql = @"
UPDATE tbl_staff_availability
SET status = 'BOOKED'
WHERE staff_id = @StaffId
  AND date BETWEEN @StartDate AND @EndDate;";

            const string insertSql = @"
INSERT INTO tbl_staff_availability (staff_id, date, status)
SELECT @StaffId, d::date, 'BOOKED'
FROM generate_series(@StartDate::date, @EndDate::date, interval '1 day') AS d
WHERE NOT EXISTS (
    SELECT 1
    FROM tbl_staff_availability existing
    WHERE existing.staff_id = @StaffId
      AND existing.date = d::date
);";

            if (!_unitOfWork.HasActiveTransaction)
                throw new InvalidOperationException("Transaction required");

            var connection = _unitOfWork.Connection;
            var transaction = _unitOfWork.CurrentTransaction;

            await connection.ExecuteAsync(updateSql, new
            {
                StaffId = staffId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MinValue),
            }, transaction);

            await connection.ExecuteAsync(insertSql, new
            {
                StaffId = staffId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue),
                EndDate = endDate.ToDateTime(TimeOnly.MinValue),
            }, transaction);
        }
    }
}