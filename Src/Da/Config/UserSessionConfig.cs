using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Da.Identity;
using SharedKernel.Domains.Models.RefreshToken;

namespace SharedKernel.Da.Config;

public class UserSessionConfig : IEntityTypeConfiguration<TbUserSession>
{
    public void Configure(EntityTypeBuilder<TbUserSession> builder)
    {
        builder.HasOne(typeof(SharedKernelAppUser))
        .WithMany()
        .HasForeignKey(nameof(TbUserSession.UserId))
        .OnDelete(DeleteBehavior.Restrict);
    }
}
