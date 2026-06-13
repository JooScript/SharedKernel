namespace SharedKernel.Bl.Constants;

/// <summary>
/// Defines the available system roles used for authorization and access control.
/// </summary>
/// <remarks>
/// These roles are typically used in policies, JWT claims, and authorization attributes
/// such as <c>[Authorize(Roles = SystemRoles.Admin)]</c>.
/// </remarks>
public static class SystemRoles
{
    /// <summary>
    /// Represents the administrator role with full system privileges.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Represents the standard user role with restricted access permissions.
    /// </summary>
    public const string User = "User";
}