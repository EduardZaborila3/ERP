namespace ErpServer.Dtos.Users;

public record UserDetailsDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    int AppRoleId,
    int JobTitleId,
    bool IsActive
);