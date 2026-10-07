using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Application.DTOs;

public class AccountDto
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateAccountDto
{
    [Required, MaxLength(100)]
    public required string FirstName { get; set; }

    [Required, MaxLength(100)]
    public required string LastName { get; set; }
}

public class ChangePasswordDto
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required, MinLength(8)]
    public required string NewPassword { get; set; }
}
