using SharedKernel.Bl.Constants;
using SharedKernel.Core.Resources;
using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Bl.Dtos.User;

public class RegisterDto
{
    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "EmailRequired")]
    [EmailAddress(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "InvalidEmail")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "UserNameRequired")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "PasswordRequired")]
    [MinLength(8, ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "PasswordMinLength")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "ConfirmPasswordRequired")]
    [Compare("Password", ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "PasswordMismatch")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "FirstNameRequired")]
    [StringLength(50, ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "FirstNameMaxLength")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "LastNameRequired")]
    [StringLength(50, ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "LastNameMaxLength")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "PhoneRequired")]
    [Phone(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "InvalidPhone")]
    public string Phone { get; set; } = null!;

    public List<string> Roles { get; set; } = new List<string> { SystemRoles.User };

}