namespace ErpServer.Dtos.Tasks;

public record TaskOwnerDto(
    int Id,
    string Email,
    string FirstName,
    string LastName
);
