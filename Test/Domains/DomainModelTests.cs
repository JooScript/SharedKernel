using SharedKernel.Domains.Models.Logging;
using SharedKernel.Domains.Models.RefreshToken;
using SharedKernel.Domains.Types;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Domains.Test;

public class DomainModelTests
{
    private sealed record TestStrEnum : StrEnum
    {
        public TestStrEnum(string value) : base(value) { }
    }

    private sealed class TestTable : SharedKernelBaseTable
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void CurrentStateType_HasExpectedNumericValues()
    {
        Assert.Equal(0, (short)CurrentStateType.Deleted);
        Assert.Equal(1, (short)CurrentStateType.Active);
        Assert.Equal(2, (short)CurrentStateType.InActive);
    }

    [Fact]
    public void StrEnum_ExposesValue()
    {
        var value = new TestStrEnum("Invoice");

        Assert.Equal("Invoice", value.Value);
    }

    [Fact]
    public void BaseTable_HasActiveStateAndEmptyDefaults()
    {
        var table = new TestTable();

        Assert.Equal(Guid.Empty, table.Id);
        Assert.Equal(CurrentStateType.Active, table.CurrentState);
        Assert.Equal(default, table.CreatedAt);
        Assert.Equal(Guid.Empty, table.CreatedBy);
        Assert.Null(table.UpdatedAt);
        Assert.Null(table.UpdatedBy);
    }

    [Fact]
    public void BaseTable_StoresAssignedValues()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(5);

        var table = new TestTable
        {
            Id = id,
            CurrentState = CurrentStateType.Deleted,
            CreatedAt = createdAt,
            CreatedBy = userId,
            UpdatedAt = updatedAt,
            UpdatedBy = userId,
            Name = "Test"
        };

        Assert.Equal(id, table.Id);
        Assert.Equal(CurrentStateType.Deleted, table.CurrentState);
        Assert.Equal(createdAt, table.CreatedAt);
        Assert.Equal(userId, table.CreatedBy);
        Assert.Equal(updatedAt, table.UpdatedAt);
        Assert.Equal(userId, table.UpdatedBy);
        Assert.Equal("Test", table.Name);
    }

    [Fact]
    public void AuditLog_StoresValues()
    {
        var entityId = Guid.NewGuid();
        var auditLog = new TbAuditLog
        {
            Action = "Create",
            Entity = "Invoice",
            EntityId = entityId,
            Details = "Created invoice"
        };

        Assert.Equal("Create", auditLog.Action);
        Assert.Equal("Invoice", auditLog.Entity);
        Assert.Equal(entityId, auditLog.EntityId);
        Assert.Equal("Created invoice", auditLog.Details);
    }

    [Fact]
    public void AuditLog_DetailsCanBeNull()
    {
        var auditLog = new TbAuditLog
        {
            Action = "Delete",
            Entity = "Invoice",
            EntityId = Guid.NewGuid()
        };

        Assert.Null(auditLog.Details);
    }

    [Fact]
    public void UserSession_StartsWithEmptyRefreshTokenCollection()
    {
        var session = new TbUserSession();

        Assert.Empty(session.RefreshTokens);
        Assert.Null(session.LastUsedAt);
        Assert.Null(session.RevokedAt);
    }

    [Fact]
    public void RefreshToken_StoresValues()
    {
        var sessionId = Guid.NewGuid();
        var csrf = Guid.NewGuid();
        var expires = DateTime.UtcNow.AddDays(1);
        var token = new TbRefreshToken
        {
            Token = "token",
            CSRFToken = csrf,
            Expires = expires,
            SessionId = sessionId
        };

        Assert.Equal("token", token.Token);
        Assert.Equal(csrf, token.CSRFToken);
        Assert.Equal(expires, token.Expires);
        Assert.Equal(sessionId, token.SessionId);
    }
}
