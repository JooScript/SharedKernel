using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Da.Test.Infrastructure;

public class TestEntity : SharedKernelBaseTable
{
    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }
}

public class TestView
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }
}

public class SharedKernelDaTestDbContext(DbContextOptions<SharedKernelDaTestDbContext> options)
    : DbContext(options)
{
    public DbSet<TestEntity> Entities => Set<TestEntity>();

    public DbSet<TestView> Views => Set<TestView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<TestEntity>().Property(x => x.Name).HasMaxLength(200);

        modelBuilder.Entity<TestView>().HasKey(x => x.Id);
        modelBuilder.Entity<TestView>().Property(x => x.Name).HasMaxLength(200);
    }
}
