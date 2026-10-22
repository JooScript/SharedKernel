using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Da.Config;
using SharedKernel.Da.Identity;
using System.Reflection;

namespace SharedKernel.Da.Context;

public partial class SharedKernelDbContext<TUser, TRole>
    : IdentityDbContext<
        TUser,
        TRole,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>
        >
    where TUser : SharedKernelAppUser
    where TRole : SharedKernelAppRole

{
    public SharedKernelDbContext(DbContextOptions options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.Properties<string>().HaveMaxLength(100);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.ApplyConfiguration(new UserSessionConfig<TUser>());

        #region GlobalConfigs

        foreach (IMutableEntityType entityType in builder.Model.GetEntityTypes())
        {
            EntityTypeBuilder? entity = builder.Entity(entityType.ClrType);

            if (typeof(SharedKernelBaseTable).IsAssignableFrom(entityType.ClrType))
            {
                entity.HasKey(nameof(SharedKernelBaseTable.Id))
                    .HasName($"PK_{entityType.ClrType.Name}");

                entity.Property(nameof(SharedKernelBaseTable.CreatedAt))
                    .IsRequired(true)
                    .Metadata
                    .SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

                entity.Property(nameof(SharedKernelBaseTable.UpdatedAt))
                    .IsRequired(false);

                entity.Property(nameof(SharedKernelBaseTable.CreatedBy))
                    .IsRequired(true)
                    .Metadata
                    .SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

                entity.HasOne(typeof(TUser))
                    .WithMany()
                    .HasForeignKey(nameof(SharedKernelBaseTable.CreatedBy))
                    .IsRequired(true)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(typeof(TUser))
                    .WithMany()
                    .HasForeignKey(nameof(SharedKernelBaseTable.UpdatedBy))
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(nameof(SharedKernelBaseTable.CurrentState))
                    .IsRequired(true);
            }
        }

        #endregion

        OnModelCreatingPartial(builder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}