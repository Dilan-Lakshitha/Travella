using System;
using System.Collections.Generic;
using System.Text;

namespace Travella.Domain.Interface
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }

}
