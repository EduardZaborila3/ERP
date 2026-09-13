using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Auth;

public record ForgotPasswordDto(
    [Required][EmailAddress] string Email,
    string? RedirectUrl = null
);
