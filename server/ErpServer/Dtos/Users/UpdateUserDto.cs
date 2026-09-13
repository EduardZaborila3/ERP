using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Users;

public record UpdateUserDto
(
    [Required][StringLength(25)] string FirstName,
    [Required][StringLength(25)] string LastName,
    [Required] string Email,
    [Required] int AppRoleId,
    [Required] int JobTitleId,
    [Required] bool IsActive
);
