using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel.Api.Services;
using SharedKernel.Da.Contracts;
using SharedKernel.Domains.Models.RefreshToken;
using SharedKernel.Domains.Types;

namespace SharedKernel.Api.Tests.Services;

public class UserSessionServiceTests
{
    private readonly Mock<ISharedKernelTableCmdRepo<TbUserSession, TestDbContext>> _repo = new();
    private readonly UserSessionService<TestDbContext> _sut;

    public UserSessionServiceTests()
    {
        _sut = new UserSessionService<TestDbContext>(_repo.Object);
    }

    [Fact]
    public async Task AddAsync_CreatesSessionWithExpectedValues()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var before = DateTime.UtcNow;
        TbUserSession? captured = null;

        _repo
            .Setup(repo => repo.AddAsync(It.IsAny<TbUserSession>(), It.IsAny<CancellationToken>()))
            .Callback<TbUserSession, CancellationToken>((session, _) => captured = session)
            .ReturnsAsync(SaveResultType.Ok(sessionId));

        var result = await _sut.AddAsync("agent/1.0", "127.0.0.1", userId, CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.True(result.Success);
        Assert.Equal(sessionId, result.Id);
        Assert.NotNull(captured);
        Assert.Equal(userId, captured.UserId);
        Assert.Equal("agent/1.0", captured.UserAgent);
        Assert.Equal("127.0.0.1", captured.IpAddress);
        Assert.Equal(userId, captured.CreatedBy);
        Assert.InRange(captured.CreatedAt.UtcDateTime, before, after);
        Assert.Null(captured.LastUsedAt);
        Assert.Null(captured.RevokedAt);
    }

    [Fact]
    public async Task AddAsync_PassesCancellationTokenToRepository()
    {
        using var source = new CancellationTokenSource();
        var token = source.Token;

        _repo
            .Setup(repo => repo.AddAsync(It.IsAny<TbUserSession>(), token))
            .ReturnsAsync(SaveResultType.Ok(Guid.NewGuid()));

        await _sut.AddAsync("agent", "10.0.0.1", Guid.NewGuid(), token);

        _repo.Verify(repo => repo.AddAsync(It.IsAny<TbUserSession>(), token), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ReturnsRepositoryFailure()
    {
        _repo
            .Setup(repo => repo.AddAsync(It.IsAny<TbUserSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SaveResultType.Fail("database error"));

        var result = await _sut.AddAsync("agent", "10.0.0.1", Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(["database error"], result.Errors);
    }

    public sealed class TestDbContext : DbContext;
}
