using SharedKernel.Core.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static SharedKernel.Core.Enums.Status.Status;

namespace SharedKernel.Bl.Dtos.User;

public class UserDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    /// <example>"3fa85f64-5717-4562-b3fc-2c963f66afa6"</example>
    public Guid Id { get; set; }

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "EmailRequired")]
    [EmailAddress(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "InvalidEmail")]
    public required string Email { get; set; }

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "UserNameRequired")]
    public required string UserName { get; set; }

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "PasswordRequired")]
    [MinLength(8, ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "PasswordMinLength")]
    public string? Password { get; set; }

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "ConfirmPasswordRequired")]
    [Compare("Password", ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "PasswordMismatch")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "FirstNameRequired")]
    [StringLength(50, ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "FirstNameMaxLength")]
    public required string FirstName { get; set; }

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "LastNameRequired")]
    [StringLength(50, ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "LastNameMaxLength")]
    public required string LastName { get; set; }

    [Required(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "PhoneRequired")]
    [Phone(ErrorMessageResourceType = typeof(ResMessages), ErrorMessageResourceName = "InvalidPhone")]
    public string? Phone { get; set; }

    public enCurrentState CurrentState { get; set; }

    public List<string>? Roles { get; set; }

}