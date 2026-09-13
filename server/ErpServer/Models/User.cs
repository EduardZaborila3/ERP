using System.ComponentModel.DataAnnotations.Schema;

namespace ErpServer.Models;

[Table("users", Schema = "public")]
public class User
{  
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required int AppRoleId { get; set; }
    public required int JobTitleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
    public string? PasswordHash { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    public ICollection<AppTask> AssignedTasks { get; set; } = new List<AppTask>();
}