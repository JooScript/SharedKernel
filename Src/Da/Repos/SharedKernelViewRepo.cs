using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Da.Contracts;

namespace SharedKernel.Da.Repos;

public class SharedKernelViewRepo<Vw, TDbContext>(
    TDbContext context,
    ILogger<SharedKernelViewRepo<Vw, TDbContext>> logger) :
    SharedKernelQueryRepo<Vw, TDbContext>(context),
    ISharedKernelViewRepo<Vw, TDbContext>
    where Vw : class
    where TDbContext : DbContext;