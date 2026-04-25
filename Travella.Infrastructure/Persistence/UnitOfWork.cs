using System;
using System.Data;
using System.Threading.Tasks;
using Travella.Application.Interfaces;

namespace Travella.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private IDbConnection? _connection;
        private IDbTransaction? _transaction;

        public IDbConnection Connection => _connection ?? throw new InvalidOperationException("Connection is not initialized.");

        public IDbTransaction? CurrentTransaction => _transaction;

        public bool HasActiveTransaction => _transaction != null && _connection != null;

        public IDbTransaction Transaction => _transaction ?? throw new InvalidOperationException("Transaction is not started.");

        public UnitOfWork(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task BeginAsync()
        {
            if (_connection != null)
            {
                return;
            }

            _connection = _connectionFactory.CreateConnection();
            if (_connection.State != ConnectionState.Open)
            {
                if (_connection is Npgsql.NpgsqlConnection npg)
                {
                    await npg.OpenAsync();
                }
                else
                {
                    _connection.Open();
                }
            }

            _transaction = _connection.BeginTransaction();
        }

        public Task CommitAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("Transaction has not been started.");
            }

            _transaction.Commit();
            DisposeTransaction();
            return Task.CompletedTask;
        }

        public Task RollbackAsync()
        {
            if (_transaction == null)
                return Task.CompletedTask;

            try
            {
                _transaction.Rollback();
            }
            catch
            {
            }

            DisposeTransaction();
            return Task.CompletedTask;
        }

        private void DisposeTransaction()
        {
            _transaction?.Dispose();
            _transaction = null;

            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }

        public ValueTask DisposeAsync()
        {
            DisposeTransaction();
            return ValueTask.CompletedTask;
        }
    }
}