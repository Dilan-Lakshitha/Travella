using Dapper;
using System.Collections.Generic;
using System.Linq;
using Travella.Application.Interfaces;
using Travella.Domain.Entities.Auth;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private class StaffUserRow
        {
            public int UserId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly UnitOfWork _unitOfWork;

        public AuthRepository(IDbConnectionFactory connectionFactory, UnitOfWork unitOfWork)
        {
            _connectionFactory = connectionFactory;
            _unitOfWork = unitOfWork;
        }

        private System.Data.IDbConnection GetConnection(bool useTransaction)
            => useTransaction ? _unitOfWork.Connection : _connectionFactory.CreateConnection();

        public async Task<AuthUserRecord?> GetByEmailAsync(string email)
        {
            const string sql = """
                SELECT
                    u.id AS UserId,
                    u.name,
                    u.email,
                    u.role,
                    u.company_id AS CompanyId,
                    u.is_deleted AS IsDeleted,
                    a.password_hash AS PasswordHash,
                    a.must_change_password AS MustChangePassword
                FROM tbl_users u
                INNER JOIN tbl_auth a ON a.user_id = u.id
                WHERE LOWER(u.email) = LOWER(@Email)
                LIMIT 1
            """;

            using var connection = GetConnection(useTransaction: false);
            return await connection.QueryFirstOrDefaultAsync<AuthUserRecord>(sql, new { Email = email });
        }

        public async Task<int> CreateTravelerAsync(string name, string email, string passwordHash, int companyId)
        {
            const string userSql = """
                INSERT INTO tbl_users (name, email, role, company_id, is_deleted)
                VALUES (@Name, @Email, 'TRAVELER', @CompanyId, false)
                RETURNING id
            """;

            const string authSql = """
                INSERT INTO tbl_auth (user_id, password_hash, must_change_password, created_at)
                VALUES (@UserId, @PasswordHash, false, NOW())
            """;

            var connection = GetConnection(useTransaction: true);
            var userId = await connection.ExecuteScalarAsync<int>(
                userSql,
                new { Name = name, Email = email, CompanyId = companyId },
                _unitOfWork.Transaction
            );

            await connection.ExecuteAsync(
                authSql,
                new { UserId = userId, PasswordHash = passwordHash },
                _unitOfWork.Transaction
            );

            return userId;
        }

        public async Task<int> CreateStaffUserAsync(string name, string email, int companyId, string passwordHash, bool mustChangePassword)
        {
            const string userSql = """
                INSERT INTO tbl_users (name, email, role, company_id, is_deleted)
                VALUES (@Name, @Email, 'STAFF', @CompanyId, false)
                RETURNING id
            """;

            const string authSql = """
                INSERT INTO tbl_auth (user_id, password_hash, must_change_password, created_at)
                VALUES (@UserId, @PasswordHash, @MustChangePassword, NOW())
            """;

            var connection = GetConnection(useTransaction: true);
            var userId = await connection.ExecuteScalarAsync<int>(
                userSql,
                new { Name = name, Email = email, CompanyId = companyId },
                _unitOfWork.Transaction
            );

            await connection.ExecuteAsync(
                authSql,
                new { UserId = userId, PasswordHash = passwordHash, MustChangePassword = mustChangePassword },
                _unitOfWork.Transaction
            );

            return userId;
        }

        public async Task<bool> UpdatePasswordAsync(string email, string newPasswordHash, bool mustChangePassword)
        {
            const string sql = """
                UPDATE tbl_auth a
                SET password_hash = @PasswordHash,
                    must_change_password = @MustChangePassword
                FROM tbl_users u
                WHERE a.user_id = u.id
                  AND LOWER(u.email) = LOWER(@Email)
            """;

            var connection = GetConnection(useTransaction: true);
            var affected = await connection.ExecuteAsync(
                sql,
                new { Email = email, PasswordHash = newPasswordHash, MustChangePassword = mustChangePassword },
                _unitOfWork.Transaction
            );

            return affected > 0;
        }

        public async Task<List<(int UserId, string Name, string Email)>> GetCompanyStaffUsersAsync(int companyId)
        {
            const string sql = """
                SELECT id AS UserId, name, email
                FROM tbl_users
                WHERE company_id = @CompanyId
                  AND role = 'STAFF'
                  AND is_deleted = false
                ORDER BY id DESC
            """;

            using var connection = GetConnection(useTransaction: false);
            var rows = await connection.QueryAsync<StaffUserRow>(sql, new { CompanyId = companyId });
            return rows.Select(r => (r.UserId, r.Name, r.Email)).ToList();
        }
    }
}
