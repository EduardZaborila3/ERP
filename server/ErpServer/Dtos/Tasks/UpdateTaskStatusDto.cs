using TaskStatus = ErpServer.Models.TaskStatus;

namespace ErpServer.Dtos.Tasks;

public record UpdateTaskStatusDto(
    TaskStatus Status
);
