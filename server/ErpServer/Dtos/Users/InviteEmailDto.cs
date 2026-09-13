using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Users;

public record InviteEmailDto
(
    [Required] string Email,
    [Required] string FirstName,
    [Required] string LastName,
    string? RedirectUrl = null,
    string? TempPassword = null
);
