using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Application.DTOs;

public class RegisterDto
{
    [Required, MaxLength(100)]
    public required string FirstName { get; set; }

    [Required, MaxLength(100)]
    public required string LastName { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required, MinLength(8)]
    public required string Password { get; set; }
}
