using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Dtos.User;
using SharedKernel.Bl.Services.Auth;
using SharedKernel.Da.Contracts;
using SharedKernel.Domains.Models.RefreshToken;
using SharedKernel.Domains.Types;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Test.Services.Auth;

public class RefreshTokenServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TokenId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly Mock<ISharedKernelTableQryRepo<TbRefreshToken, DbContext>> _qryRepo = new();
    private readonly Mock<ISharedKernelTableCmdRepo<TbRefreshToken, DbContext>> _cmdRepo = new();
    private readonly Mock<IUserQry> _userQry = new();

    private RefreshTokenService<DbContext> CreateSut()
        => new(_qryRepo.Object, _cmdRepo.Object, _userQry.Object);

    private static RefreshTokenDto CreateDto() => new()
    {
        Id = TokenId,
        Token = "token",
        CSRFToken = Guid.NewGuid(),
        UserId = UserId,
        SessionId = SessionId,
        Expires = DateTime.UtcNow.AddDays(7),
        CurrentState = CurrentStateType.Active
    };

    [Fact]
    public void ToDto_WhenEntityIsNull_Throws()
    {
        var sut = CreateSut();

        Assert.Throws<Exception>(() => sut.ToDto(null!));
    }

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var sut = CreateSut();
        var entity = new TbRefreshToken
        {
            Id = TokenId,
            Token = "token",
            CSRFToken = Guid.NewGuid(),
            Expires = DateTime.UtcNow.AddDays(2),
            SessionId = SessionId,
            CreatedBy = UserId,
            CurrentState = CurrentStateType.InActive
        };

        var result = sut.ToDto(entity);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.Token, result.Token);
        Assert.Equal(entity.CSRFToken, result.CSRFToken);
        Assert.Equal(entity.Expires, result.Expires);
        Assert.Equal(entity.SessionId, result.SessionId);
        Assert.Equal(entity.CreatedBy, result.UserId);
        Assert.Equal(entity.CurrentState, result.CurrentState);
    }

    [Fact]
    public void ToEntity_WhenDtoIsNull_Throws()
    {
        var sut = CreateSut();

        Assert.Throws<Exception>(() => sut.ToEntity(null!));
    }

    [Fact]
    public void ToEntity_MapsAllProperties()
    {
        var sut = CreateSut();
        var dto = CreateDto();

        var result = sut.ToEntity(dto);

        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.Token, result.Token);
        Assert.Equal(dto.CSRFToken, result.CSRFToken);
        Assert.Equal(dto.Expires, result.Expires);
        Assert.Equal(dto.SessionId, result.SessionId);
        Assert.Equal(dto.UserId, result.CreatedBy);
        Assert.Equal(dto.CurrentState, result.CurrentState);
    }

    [Fact]
    public void GenerateToken_ReturnsUniqueBase64Tokens()
    {
        var sut = CreateSut();

        var first = sut.GenerateToken();
        var second = sut.GenerateToken();

        Assert.NotEqual(first, second);
        Assert.Equal(44, first.Length);
        Assert.True(Convert.TryFromBase64String(first, new byte[32], out _));
    }

    [Fact]
    public void GenerateRefreshToken_SetsExpectedDefaults()
    {
        var sut = CreateSut();

        var result = sut.GenerateRefreshToken(UserId, SessionId);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(SessionId, result.SessionId);
        Assert.NotEqual(Guid.Empty, result.CSRFToken);
        Assert.Equal(CurrentStateType.Active, result.CurrentState);
        Assert.True(result.Expires > DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task GetByToken_WhenFound_ReturnsDto()
    {
        var sut = CreateSut();
        var entity = new TbRefreshToken
        {
            Id = TokenId,
            Token = "token",
            CSRFToken = Guid.NewGuid(),
            Expires = DateTime.UtcNow.AddDays(1),
            SessionId = SessionId,
            CreatedBy = UserId
        };

        _qryRepo
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TbRefreshToken, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await sut.GetByToken("token", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result!.Id);
        Assert.Equal("token", result.Token);
        Assert.Equal(UserId, result.UserId);
    }

    [Fact]
    public async Task GetByToken_WhenMissing_ReturnsNull()
    {
        var sut = CreateSut();

        _qryRepo
            .Setup(r => r.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<TbRefreshToken, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TbRefreshToken?)null);

        var result = await sut.GetByToken("missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshAsync_WhenDeactivationAndAddSucceed_ReturnsTrue()
    {
        var sut = CreateSut();
        var dto = CreateDto();
        TbRefreshToken? captured = null;

        _qryRepo
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<TbRefreshToken, bool>>>(),
                It.IsAny<Expression<Func<TbRefreshToken, Guid>>>(),
                o => o.CreatedAt,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Guid.NewGuid() });

        _cmdRepo
            .Setup(c => c.DeactivateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cmdRepo
            .Setup(c => c.AddAsync(It.IsAny<TbRefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<TbRefreshToken, CancellationToken>((entity, _) => captured = entity)
            .ReturnsAsync(SaveResultType.Ok(dto.Id!.Value));

        var result = await sut.RefreshAsync(dto, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(captured);
        Assert.Equal(UserId, captured!.CreatedBy);
        Assert.Equal(dto.Token, captured.Token);
        Assert.Equal(dto.SessionId, captured.SessionId);
    }

    [Fact]
    public async Task RefreshAsync_WhenDeactivationFails_Throws()
    {
        var sut = CreateSut();
        var dto = CreateDto();

        _qryRepo
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<TbRefreshToken, bool>>>(),
                It.IsAny<Expression<Func<TbRefreshToken, Guid>>>(),
                o => o.CreatedAt,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Guid.NewGuid() });

        _cmdRepo
            .Setup(c => c.DeactivateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<Exception>(
            () => sut.RefreshAsync(dto, CancellationToken.None));

        Assert.Equal("Error while token refreshing", exception.Message);
        Assert.IsType<Exception>(exception.InnerException);
        _cmdRepo.Verify(
            c => c.AddAsync(It.IsAny<TbRefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_WhenAddFails_ReturnsFalse()
    {
        var sut = CreateSut();
        var dto = CreateDto();

        _qryRepo
            .Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<TbRefreshToken, bool>>>(),
                It.IsAny<Expression<Func<TbRefreshToken, Guid>>>(),
                o => o.CreatedAt,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        _cmdRepo
            .Setup(c => c.DeactivateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cmdRepo
            .Setup(c => c.AddAsync(It.IsAny<TbRefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SaveResultType.Fail("failed"));

        var result = await sut.RefreshAsync(dto, CancellationToken.None);

        Assert.False(result);
    }
}
