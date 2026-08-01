using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.AuditLog;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Dtos.AuditLog;
using SharedKernel.Bl.Services.Base;
using SharedKernel.Da.Contracts;
using SharedKernel.Domains.Models.Logging;

namespace SharedKernel.Bl.Services.AuditLog;

public class AuditLogService<TDbContext>(
    ISharedKernelTableQryRepo<TbAuditLog, TDbContext> repoQuery,
    ISharedKernelTableCmdRepo<TbAuditLog, TDbContext> repoCommand,
    IUserQry userServiceQuery)
    : SharedKernelBaseService<TbAuditLog, AuditLogDto, TDbContext>(repoQuery, repoCommand, userServiceQuery),
    IAuditLog<TDbContext>
    where TDbContext : DbContext
{
    public override AuditLogDto ToDto(TbAuditLog entity)
    {
        return new AuditLogDto
        {
            Id = entity.Id,

            Action = entity.Action,
            Entity = entity.Entity,
            EntityId = entity.EntityId,
            Details = entity.Details,
            CurrentState = entity.CurrentState
        };
    }

    public override TbAuditLog ToEntity(AuditLogDto dto)
    {
        return new TbAuditLog
        {
            Id = dto.Id,

            Action = dto.Action,
            Entity = dto.Entity,
            EntityId = dto.EntityId,
            Details = dto.Details,
            CurrentState = dto.CurrentState
        };
    }

}