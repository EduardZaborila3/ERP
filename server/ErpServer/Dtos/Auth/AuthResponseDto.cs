using ErpServer.Dtos.Users;

namespace ErpServer.Dtos.Auth;

public record AuthResponseDto(
    string Token,
    UserDetailsDto User
);
