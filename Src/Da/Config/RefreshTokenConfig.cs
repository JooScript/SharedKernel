using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Identity.Client;
using SharedKernel.Domains.Models.RefreshToken;

namespace SharedKernel.Da.Config;

public class RefreshTokenConfig : IEntityTypeConfiguration<TbRefreshToken>
{
    public void Configure(EntityTypeBuilder<TbRefreshToken> builder)
    {
        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("UQ_TbRefreshToken_Token");

        builder.Property(e => e.Expires).IsRequired();

    }
}
