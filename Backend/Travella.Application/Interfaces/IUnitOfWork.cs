using System;
using System.Data;
using System.Threading.Tasks;

namespace Travella.Application.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task BeginAsync();

        Task CommitAsync();

        Task RollbackAsync();

        bool HasActiveTransaction { get; }

        IDbConnection Connection { get; }

        IDbTransaction? CurrentTransaction { get; }
    }
}