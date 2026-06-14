using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.Base;
using SharedKernel.Bl.Dtos.AuditLog;
using SharedKernel.Domains.Models.Logging;

namespace SharedKernel.Bl.Contracts.AuditLog;

public interface IAuditLog<TDbContext>
    : IBaseService<TbAuditLog, AuditLogDto>
    where TDbContext : DbContext
{

}