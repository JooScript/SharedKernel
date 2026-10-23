using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Models;
using SharedKernel.Bl.Services.Base;

namespace SharedKernel.Bl.Test.Infrastructure;

public class TestEntity : SharedKernelBaseTable
{
    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }
}

public class TestDto
{
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }
}

public class TestService : SharedKernelBaseService<TestEntity, TestDto, DbContext>
{
    public TestService(
        SharedKernel.Da.Contracts.ISharedKernelTableCmdRepo<TestEntity, DbContext> cmdRepo,
        SharedKernel.Da.Contracts.ISharedKernelTableQryRepo<TestEntity, DbContext> qryRepo,
        SharedKernel.Bl.Contracts.Auth.IUserQry userQry)
        : base(cmdRepo, qryRepo, userQry)
    {
    }

    public bool CanUpdate { get; set; } = true;

    public bool CanDelete { get; set; } = true;

    public bool ReturnNullEntity { get; set; }

    public override TestDto ToDto(TestEntity entity)
        => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Value = entity.Value
        };

    public override TestEntity ToEntity(TestDto dto)
    {
        if (ReturnNullEntity)
            return null!;

        return new TestEntity
        {
            Id = dto.Id ?? Guid.Empty,
            Name = dto.Name,
            Value = dto.Value
        };
    }

    public override Task<EntityValidationResult> CanUpdateAsync(TestDto dto, CancellationToken cancellationToken)
        => Task.FromResult(new EntityValidationResult(CanUpdate, CanUpdate ? string.Empty : "Cannot update"));

    public override Task<EntityValidationResult> CanDeleteAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(new EntityValidationResult(CanDelete, CanDelete ? string.Empty : "Cannot delete"));
}
