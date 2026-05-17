using Dapper;
using Travella.Application.Interfaces;
using Travella.Infrastructure.Persistence;

namespace Travella.Infrastructure.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ApplicationRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateAsync(string companyName, string email, string phone)
        {
            const string sql = """
                INSERT INTO tbl_company_applications (company_name, email, phone, status, created_at)
                VALUES (@CompanyName, @Email, @Phone, 'PENDING', NOW())
                RETURNING id
            """;

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                CompanyName = companyName,
                Email = email,
                Phone = phone
            });
        }
    }
}
