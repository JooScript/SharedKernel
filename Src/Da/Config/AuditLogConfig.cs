using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domains.Models.Logging;

namespace SharedKernel.Da.Config;

public class AuditLogConfig : IEntityTypeConfiguration<TbAuditLog>
{
    public void Configure(EntityTypeBuilder<TbAuditLog> builder)
    {

        builder.Property(e => e.Details)
            .HasMaxLength(int.MaxValue);

    }
}