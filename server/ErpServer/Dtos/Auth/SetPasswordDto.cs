using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Auth;

public record SetPasswordDto(
    [Required][EmailAddress] string Email,
    [Required] string Token,
    [Required][MinLength(6)] string NewPassword
);
