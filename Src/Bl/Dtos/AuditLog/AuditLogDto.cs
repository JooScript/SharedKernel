using SharedKernel.Bl.Dtos.Base;

namespace SharedKernel.Bl.Dtos.AuditLog;

/// <summary>
/// Represents an audit log entry used for tracking system activities.
/// Inherits from <see cref="SharedKernelBaseDto"/> for common DTO properties.
/// </summary>
public class AuditLogDto : SharedKernelBaseDto
{
    /// <summary>
    /// Gets or sets the action performed (e.g., "Create", "Update", "Delete").
    /// </summary>
    /// <example>"UpdateUser"</example>
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the entity type that was modified.
    /// </summary>
    /// <example>"User"</example>
    public required string Entity { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the affected entity.
    /// </summary>
    /// <example>"3fa85f64-5717-4562-b3fc-2c963f66afa6"</example>
    public required Guid EntityId { get; set; }

    /// <summary>
    /// Gets or sets additional details about the audit event.
    /// </summary>
    /// <example>"Changed email from old@test.com to new@test.com"</example>
    public string? Details { get; set; }
}