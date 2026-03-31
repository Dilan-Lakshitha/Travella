using System;
using System.Threading.Tasks;

namespace Travella.Application.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task BeginAsync();

        Task CommitAsync();

        Task RollbackAsync();
    }
}