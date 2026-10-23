using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SharedKernel.Da.Test.Infrastructure;

public static class TestDbContextFactory
{
    public static SharedKernelDaTestDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<SharedKernelDaTestDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new SharedKernelDaTestDbContext(options);
    }
}
