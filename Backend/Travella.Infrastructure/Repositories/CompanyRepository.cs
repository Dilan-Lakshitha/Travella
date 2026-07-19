using Dapper;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CompanyRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<CreatedCompanyAdminResult> CreateCompanyWithAdminAsync(CreateCompanyRequest request, string slug,string passwordHash,int? createdBy)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                const string companySql = """
            INSERT INTO tbl_company
            (
                name,
                email,
                phone,
                status,
                created_at,
                created_by,
                slug,
                website_url
            )
            VALUES
            (
                @Name,
                @Email,
                @Phone,
                'ACTIVE',
                NOW(),
                @CreatedBy,
                @Slug,
                @WebsiteUrl
            )
            RETURNING id;
            """;

                var companyId = await connection.ExecuteScalarAsync<int>(companySql,
                    new
                    {
                        Name = request.Name.Trim(),
                        Email = request.Email.Trim().ToLowerInvariant(),
                        Phone = request.Phone.Trim(),
                        CreatedBy = createdBy,
                        Slug = slug,
                        WebsiteUrl = request.WebsiteUrl?.Trim()
                    },
                    transaction);

                const string userSql = """
            INSERT INTO tbl_users
            (
                company_id,
                name,
                email,
                role,
                is_deleted
            )
            VALUES
            (
                @CompanyId,
                @Name,
                @Email,
                'ADMIN',
                FALSE
            )
            RETURNING id;
            """;

                var adminUserId = await connection.ExecuteScalarAsync<int>(userSql,
                    new
                    {
                        CompanyId = companyId,
                        Name = request.OwnerName.Trim(),
                        Email = request.AdminEmail.Trim().ToLowerInvariant()
                    },
                    transaction);

                const string authSql = """
            INSERT INTO tbl_auth
            (
                user_id,
                password_hash,
                created_at,
                must_change_password
            )
            VALUES
            (
                @UserId,
                @PasswordHash,
                NOW(),
                TRUE
            );
            """;

                await connection.ExecuteAsync(authSql,
                    new
                    {
                        UserId = adminUserId,
                        PasswordHash = passwordHash
                    },
                    transaction);

                transaction.Commit();

                return new CreatedCompanyAdminResult
                {
                    CompanyId = companyId,
                    AdminUserId = adminUserId
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public async Task<bool> CompanyEmailExistsAsync(string email)
        {
            const string sql = """
        SELECT EXISTS
        (
            SELECT 1
            FROM tbl_company
            WHERE LOWER(email) = LOWER(@Email)
        );
        """;

            using var connection = _connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<bool>(
                sql,
                new { Email = email.Trim() });
        }

        public async Task<bool> UserEmailExistsAsync(string email)
        {
            const string sql = """
        SELECT EXISTS
        (
            SELECT 1
            FROM tbl_users
            WHERE LOWER(email) = LOWER(@Email)
              AND is_deleted = FALSE
        );
        """;

            using var connection = _connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<bool>(
                sql,
                new { Email = email.Trim() });
        }

        public async Task<bool> SlugExistsAsync(string slug)
        {
            const string sql = """
        SELECT EXISTS
        (
            SELECT 1
            FROM tbl_company
            WHERE LOWER(slug) = LOWER(@Slug)
        );
        """;

            using var connection = _connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<bool>(
                sql,
                new { Slug = slug });
        }

        public async Task<bool> HasPendingApplicationAsync(string email)
        {
            const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM tbl_company_applications
                WHERE LOWER(email) = LOWER(@Email)
                  AND status = 'PENDING'
            );
            """;

            using var connection = _connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<bool>(
                sql,
                new { Email = email.Trim() });
        }

        public async Task<int> CreateAsync(CreateCompanyApplicationRequest request)
        {
            try
            {
                const string sql = """
            INSERT INTO tbl_company_applications
            (
                company_name,
                owner_name,
                email,
                phone,
                company_description,
                status,
                created_at
            )
            VALUES
            (
                @CompanyName,
                @OwnerName,
                @Email,
                @Phone,
                @CompanyDescription,
                'PENDING',
                NOW()
            )
            RETURNING id;
            """;

                using var connection = _connectionFactory.CreateConnection();

                return await connection.ExecuteScalarAsync<int>(sql,
                    new
                    {
                        CompanyName = request.CompanyName.Trim(),
                        OwnerName = request.OwnerName.Trim(),
                        Email = request.Email.Trim().ToLowerInvariant(),
                        Phone = request.Phone.Trim(),
                        CompanyDescription = request.CompanyDescription?.Trim()
                    });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
