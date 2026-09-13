using System.ComponentModel.DataAnnotations.Schema;

namespace ErpServer.Models;

[Table("app_roles", Schema = "public")]
public class AppRole
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
