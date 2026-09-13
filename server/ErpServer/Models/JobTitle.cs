using System.ComponentModel.DataAnnotations.Schema;

namespace ErpServer.Models;

[Table("job_titles")]
public class JobTitle
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
