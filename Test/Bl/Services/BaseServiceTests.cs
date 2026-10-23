using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Exceptions;
using SharedKernel.Bl.Test.Infrastructure;
using SharedKernel.Da.Contracts;
using SharedKernel.Domains.Types;
using static SharedKernel.Bl.Enums.ServicesEnums;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Test.Services;

public class BaseServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EntityId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ISharedKernelTableCmdRepo<TestEntity, DbContext>> _cmdRepo = new();
    private readonly Mock<ISharedKernelTableQryRepo<TestEntity, DbContext>> _qryRepo = new();
    private readonly Mock<IUserQry> _userQry = new();

    private TestService CreateSut()
    {
        _userQry.SetupGet(u => u.LoggedInUserId).Returns(UserId);
        _userQry.Setup(u => u.GetLoggedInUserId()).Returns(UserId);

        return new TestService(_cmdRepo.Object, _qryRepo.Object, _userQry.Object);
    }

    private static TestEntity CreateEntity(
        Guid? id = null,
        string name = "Entity",
        int value = 1,
        CurrentStateType state = CurrentStateType.Active)
        => new()
        {
            Id = id ?? EntityId,
            Name = name,
            Value = value,
            CurrentState = state,
            CreatedBy = UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var sut = CreateSut();
        _qryRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateEntity(name: "First"), CreateEntity(name: "Second") });

        var result = await sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "First", "Second" }, result.Select(x => x.Name));
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        var sut = CreateSut();
        _qryRepo
            .Setup(r => r.GetByIdAsync(EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEntity(name: "Found"));

        var result = await sut.GetByIdAsync(EntityId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(EntityId, result!.Id);
        Assert.Equal("Found", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var sut = CreateSut();
        _qryRepo
            .Setup(r => r.GetByIdAsync(EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestEntity?)null);

        var result = await sut.GetByIdAsync(EntityId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CountAsync_CountsOnlyVisibleEntities()
    {
        var sut = CreateSut();
        Expression<Func<TestEntity, bool>>? captured = null;

        _qryRepo
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<TestEntity, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<TestEntity, bool>>?, CancellationToken>((filter, _) => captured = filter)
            .ReturnsAsync(4);

        var result = await sut.CountAsync(CancellationToken.None);

        Assert.Equal(4, result);
        Assert.NotNull(captured);
        Assert.True(captured!.Compile()(CreateEntity(state: CurrentStateType.Active)));
        Assert.False(captured.Compile()(CreateEntity(state: CurrentStateType.Deleted)));
    }

    [Fact]
    public async Task IsExistsAsync_WithFilter_DelegatesToRepo()
    {
        var sut = CreateSut();
        Expression<Func<TestEntity, bool>>? captured = null;

        _qryRepo
            .Setup(r => r.IsExistsAsync(
                It.IsAny<Expression<Func<TestEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<TestEntity, bool>>, CancellationToken>((filter, _) => captured = filter)
            .ReturnsAsync(true);

        var result = await sut.IsExistsAsync(e => e.Value == 5, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(captured);
        Assert.True(captured!.Compile()(CreateEntity(value: 5)));
        Assert.False(captured.Compile()(CreateEntity(value: 6)));
    }

    [Fact]
    public async Task IsExistsAsync_WithId_DelegatesToRepo()
    {
        var sut = CreateSut();

        _qryRepo
            .Setup(r => r.IsExistsAsync(EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.IsExistsAsync(EntityId, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task AddAsync_NormalizesEntityAndReturnsResult()
    {
        var sut = CreateSut();
        TestEntity? captured = null;

        _cmdRepo
            .Setup(r => r.AddAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TestEntity, CancellationToken>((entity, _) => captured = entity)
            .ReturnsAsync(SaveResultType.Ok(EntityId));

        var result = await sut.AddAsync(new TestDto { Id = Guid.NewGuid(), Name = "New", Value = 3 }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(EntityId, result.Id);
        Assert.NotNull(captured);
        Assert.Equal(Guid.Empty, captured!.Id);
        Assert.Equal(UserId, captured.CreatedBy);
        Assert.Equal(CurrentStateType.Active, captured.CurrentState);
        Assert.NotEqual(default, captured.CreatedAt);
        Assert.Equal("New", captured.Name);
    }

    [Fact]
    public async Task AddAsync_WhenEntityIsNull_ThrowsBusinessLogicException()
    {
        var sut = CreateSut();
        sut.ReturnNullEntity = true;

        await Assert.ThrowsAsync<BusinessLogicException>(
            () => sut.AddAsync(new TestDto { Name = "New" }, CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_WhenRepositoryThrows_ThrowsBusinessLogicException()
    {
        var sut = CreateSut();
        _cmdRepo
            .Setup(r => r.AddAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));

        await Assert.ThrowsAsync<BusinessLogicException>(
            () => sut.AddAsync(new TestDto { Name = "New" }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenAllowed_NormalizesAndUpdates()
    {
        var sut = CreateSut();
        TestEntity? captured = null;

        _cmdRepo
            .Setup(r => r.UpdateAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TestEntity, CancellationToken>((entity, _) => captured = entity)
            .ReturnsAsync(true);

        var result = await sut.UpdateAsync(new TestDto { Id = EntityId, Name = "Updated" }, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(captured);
        Assert.Equal(EntityId, captured!.Id);
        Assert.Equal("Updated", captured.Name);
        Assert.Equal(UserId, captured.UpdatedBy);
        Assert.NotNull(captured.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotAllowed_ReturnsFalseWithoutCallingRepo()
    {
        var sut = CreateSut();
        sut.CanUpdate = false;

        var result = await sut.UpdateAsync(new TestDto { Id = EntityId }, CancellationToken.None);

        Assert.False(result);
        _cmdRepo.Verify(
            r => r.UpdateAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenEntityExists_UpdatesState()
    {
        var sut = CreateSut();
        var entity = CreateEntity(state: CurrentStateType.Active);

        _qryRepo
            .Setup(r => r.GetByIdAsync(EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _cmdRepo
            .Setup(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.ChangeStatusAsync(EntityId, CurrentStateType.InActive, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(CurrentStateType.InActive, entity.CurrentState);
        Assert.Equal(UserId, entity.UpdatedBy);
        _cmdRepo.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenEntityMissing_ReturnsFalse()
    {
        var sut = CreateSut();
        _qryRepo
            .Setup(r => r.GetByIdAsync(EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestEntity?)null);

        var result = await sut.ChangeStatusAsync(EntityId, CurrentStateType.InActive, CancellationToken.None);

        Assert.False(result);
        _cmdRepo.Verify(
            r => r.UpdateAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ActivateAsync_SetsActiveState()
    {
        var sut = CreateSut();
        var entity = CreateEntity(state: CurrentStateType.InActive);

        _qryRepo
            .Setup(r => r.GetByIdAsync(EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _cmdRepo
            .Setup(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.ActivateAsync(EntityId, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(CurrentStateType.Active, entity.CurrentState);
    }

    [Fact]
    public async Task DeactivateAsync_SetsInActiveState()
    {
        var sut = CreateSut();
        var entity = CreateEntity(state: CurrentStateType.Active);

        _qryRepo
            .Setup(r => r.GetByIdAsync(EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _cmdRepo
            .Setup(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.DeactivateAsync(EntityId, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(CurrentStateType.InActive, entity.CurrentState);
    }

    [Fact]
    public async Task DeleteAsync_SoftDelete_ChangesState()
    {
        var sut = CreateSut();
        var entity = CreateEntity();

        _qryRepo
            .Setup(r => r.GetByIdAsync(EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _cmdRepo
            .Setup(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.DeleteAsync(EntityId, DeleteType.SoftDelete, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(CurrentStateType.Deleted, entity.CurrentState);
        _cmdRepo.Verify(
            r => r.HardDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_HardDelete_CallsHardDelete()
    {
        var sut = CreateSut();
        _cmdRepo
            .Setup(r => r.HardDeleteAsync(EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.DeleteAsync(EntityId, DeleteType.HardDelete, CancellationToken.None);

        Assert.True(result);
        _cmdRepo.Verify(r => r.HardDeleteAsync(EntityId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotAllowed_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.CanDelete = false;

        var result = await sut.DeleteAsync(EntityId, DeleteType.HardDelete, CancellationToken.None);

        Assert.False(result);
        _cmdRepo.Verify(
            r => r.HardDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Many_ReturnsTrueWhenAllSucceed()
    {
        var sut = CreateSut();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        _cmdRepo
            .Setup(r => r.HardDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.DeleteAsync(
            new List<Guid> { firstId, secondId },
            DeleteType.HardDelete,
            CancellationToken.None);

        Assert.True(result);
        _cmdRepo.Verify(r => r.HardDeleteAsync(firstId, It.IsAny<CancellationToken>()), Times.Once);
        _cmdRepo.Verify(r => r.HardDeleteAsync(secondId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Many_ReturnsFalseWhenAnyFails()
    {
        var sut = CreateSut();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        _cmdRepo
            .Setup(r => r.HardDeleteAsync(firstId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cmdRepo
            .Setup(r => r.HardDeleteAsync(secondId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await sut.DeleteAsync(
            new List<Guid> { firstId, secondId },
            DeleteType.HardDelete,
            CancellationToken.None);

        Assert.False(result);
    }
}
