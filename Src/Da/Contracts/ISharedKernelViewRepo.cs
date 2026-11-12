using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelViewRepo<Vw, TDbContext> : ISharedKernelQueryRepo<Vw, TDbContext>
    where Vw : class
    where TDbContext : DbContext;
