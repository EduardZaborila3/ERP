namespace ErpServer.Dtos.Tasks;

public record TaskSummaryDto(
    int TotalTasks,
    int PendingTasks,
    int InProgressTasks,
    int CompletedTasks,
    int OverdueTasks
);
