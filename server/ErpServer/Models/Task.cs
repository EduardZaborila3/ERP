using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mime;

namespace ErpServer.Models;

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed
}

[Table("tasks", Schema = "public")]
public class AppTask
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public ICollection<User> Assignees { get; set; } = new List<User>();
    public required string Title { get; set; }
    public required string Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }
}