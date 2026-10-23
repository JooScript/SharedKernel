using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Dtos.AuditLog;
using SharedKernel.Bl.Services.AuditLog;
using SharedKernel.Da.Contracts;
using SharedKernel.Domains.Models.Logging;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Test.Services.AuditLog;

public class AuditLogServiceTests
{
    private readonly Mock<ISharedKernelTableQryRepo<TbAuditLog, DbContext>> _qryRepo = new();
    private readonly Mock<ISharedKernelTableCmdRepo<TbAuditLog, DbContext>> _cmdRepo = new();
    private readonly Mock<IUserQry> _userQry = new();

    private AuditLogService<DbContext> CreateSut()
        => new(_qryRepo.Object, _cmdRepo.Object, _userQry.Object);

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var sut = CreateSut();
        var entity = new TbAuditLog
        {
            Id = Guid.NewGuid(),
            Action = "Create",
            Entity = "Invoice",
            EntityId = Guid.NewGuid(),
            Details = "details",
            CurrentState = CurrentStateType.InActive
        };

        var result = sut.ToDto(entity);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.Action, result.Action);
        Assert.Equal(entity.Entity, result.Entity);
        Assert.Equal(entity.EntityId, result.EntityId);
        Assert.Equal(entity.Details, result.Details);
        Assert.Equal(entity.CurrentState, result.CurrentState);
    }

    [Fact]
    public void ToEntity_MapsAllProperties()
    {
        var sut = CreateSut();
        var id = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var dto = new AuditLogDto
        {
            Id = id,
            Action = "Update",
            Entity = "Invoice",
            EntityId = entityId,
            Details = "details",
            CurrentState = CurrentStateType.Deleted
        };

        var result = sut.ToEntity(dto);

        Assert.Equal(id, result.Id);
        Assert.Equal("Update", result.Action);
        Assert.Equal("Invoice", result.Entity);
        Assert.Equal(entityId, result.EntityId);
        Assert.Equal("details", result.Details);
        Assert.Equal(CurrentStateType.Deleted, result.CurrentState);
    }

    [Fact]
    public void ToEntity_WhenIdIsNull_UsesEmptyGuid()
    {
        var sut = CreateSut();
        var dto = new AuditLogDto
        {
            Id = null,
            Action = "Delete",
            Entity = "Invoice",
            EntityId = Guid.NewGuid(),
            CurrentState = CurrentStateType.Active
        };

        var result = sut.ToEntity(dto);

        Assert.Equal(Guid.Empty, result.Id);
    }
}
