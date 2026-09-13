using TaskStatus = ErpServer.Models.TaskStatus;

namespace ErpServer.Dtos.Tasks;

public record TaskDto(
    int Id,
    int OwnerId,
    TaskOwnerDto? Owner,
    string Title,
    string Description,
    TaskStatus Status,
    DateTime CreatedAt,
    DateTime? Deadline,
    List<TaskAssigneeDto> Assignees
);
