using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelTableQryRepo<Tb, TDbContext> : ISharedKernelQueryRepo<Tb>
    where Tb : SharedKernelBaseTable
    where TDbContext : DbContext;
