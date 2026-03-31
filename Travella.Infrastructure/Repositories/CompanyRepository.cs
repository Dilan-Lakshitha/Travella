using Dapper;
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

        public async Task<int> CreateAsync(string name, string email, string phone, int createdBy)
        {
            const string sql = """
                INSERT INTO tbl_company (name, email, phone, status, created_by, created_at)
                VALUES (@Name, @Email, @Phone, 'ACTIVE', @CreatedBy, NOW())
                RETURNING id
            """;

            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                Name = name,
                Email = email,
                Phone = phone,
                CreatedBy = createdBy
            });
        }
    }
}
