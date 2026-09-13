using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Auth;

public record RegisterDto(
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password,
    [Required][StringLength(50)] string FirstName,
    [Required][StringLength(50)] string LastName,
    [Required] int AppRoleId = 2,
    [Required] int JobTitleId = 1
);
