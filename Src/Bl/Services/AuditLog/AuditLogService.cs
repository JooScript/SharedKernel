using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.AuditLog;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Contracts.Events;
using SharedKernel.Bl.Dtos.AuditLog;
using SharedKernel.Bl.Services.Base;
using SharedKernel.Domains.Contracts.Generic;
using SharedKernel.Domains.Models.Logging;

namespace SharedKernel.Bl.Services.AuditLog;

public class AuditLogService<TDbContext>(
    ITableQryRepo<TbAuditLog, TDbContext> repoQuery,
    ITableCmdRepo<TbAuditLog, TDbContext> repoCommand,
    IUserQry userServiceQuery,
    ICrudPublisher publisher)
    : BaseService<TbAuditLog, AuditLogDto, TDbContext>(repoQuery, repoCommand, userServiceQuery, publisher),
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