using Microsoft.AspNetCore.Identity;

namespace SharedKernel.Da.Identity;

public class SharedKernelAppRole : IdentityRole<Guid>
{
    public SharedKernelAppRole() : base() { }

    public SharedKernelAppRole(string roleName) : base(roleName) { }

}