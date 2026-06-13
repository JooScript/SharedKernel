using SharedKernel.Bl.Contracts.Base;
using SharedKernel.Bl.Dtos.AuditLog;
using SharedKernel.Domains.Models.Logging;

namespace SharedKernel.Bl.Contracts.AuditLog;

public interface IAuditLog : IBaseService<TbAuditLog, AuditLogDto>
{

}