using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Da.Identity;
using SharedKernel.Domains.Models.RefreshToken;

namespace SharedKernel.Da.Config;

public class UserSessionConfig<TUser>
    : IEntityTypeConfiguration<TbUserSession>
    where TUser : SharedKernelAppUser
{
    public void Configure(EntityTypeBuilder<TbUserSession> builder)
    {
        builder
            .HasOne<TUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}