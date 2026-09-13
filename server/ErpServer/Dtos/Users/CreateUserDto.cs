using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Users;

public record CreateUserDto
(
    [Required][StringLength(25)] string FirstName,
    [Required][StringLength(25)] string LastName,
    [Required] string Email,
    [Required] int AppRoleId = 1,
    [Required] int JobTitleId = 1,
    bool IsActive = true
);
