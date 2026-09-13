using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Auth;

public record LoginDto(
    [Required][EmailAddress] string Email,
    [Required] string Password
);
