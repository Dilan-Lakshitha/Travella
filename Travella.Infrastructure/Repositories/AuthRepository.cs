using Dapper;
using Travella.Application.Interfaces;
using Travella.Domain.Entities.Auth;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
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

        public async Task<int> CreateTravelerAsync(string name, string email, string passwordHash)
        {
            const string userSql = """
                INSERT INTO tbl_users (name, email, role, company_id, is_deleted)
                VALUES (@Name, @Email, 'TRAVELER', NULL, false)
                RETURNING id
            """;

            const string authSql = """
                INSERT INTO tbl_auth (user_id, password_hash, must_change_password, created_at)
                VALUES (@UserId, @PasswordHash, false, NOW())
            """;

            var connection = GetConnection(useTransaction: true);
            var userId = await connection.ExecuteScalarAsync<int>(
                userSql,
                new { Name = name, Email = email },
                _unitOfWork.Transaction
            );

            await connection.ExecuteAsync(
                authSql,
                new { UserId = userId, PasswordHash = passwordHash },
                _unitOfWork.Transaction
            );

            return userId;
        }
    }
}
