using System;
using Microsoft.AspNetCore.Identity;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Da.Identity;

public class SharedKernelAppUser : IdentityUser<Guid>

{
    public SharedKernelAppUser() : base() { }

    public SharedKernelAppUser(string userName) : base(userName) { }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public CurrentStateType CurrentState { get; set; }

}