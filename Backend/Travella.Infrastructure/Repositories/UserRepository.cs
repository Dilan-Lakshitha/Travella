using Dapper;
using System.Data;
using Travella.Domain.Domain;
using Travella.Domain.Interface;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public UserRepository(IDbConnection connection, IDbTransaction? transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = """
            SELECT id, company_id AS CompanyId, name, email, role
            FROM tbl_users
            WHERE email = @Email
        """;

        return await _connection.QueryFirstOrDefaultAsync<User>(
            sql,
            new { Email = email },
            _transaction
        );
    }

    public async Task<int> CreateAsync(User user)
    {
        const string sql = """
            INSERT INTO tbl_users (company_id, name, email, role)
            VALUES (@CompanyId, @Name, @Email, @Role)
            RETURNING id
        """;

        return await _connection.ExecuteScalarAsync<int>(
            sql,
            user,
            _transaction
        );
    }
}
