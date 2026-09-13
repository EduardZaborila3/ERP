namespace ErpServer.Dtos.Tasks;

public record TaskAssigneeDto(
    int Id,
    string Email,
    string FirstName,
    string LastName
);
