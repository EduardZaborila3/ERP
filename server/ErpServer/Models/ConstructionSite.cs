using System.ComponentModel.DataAnnotations.Schema;

namespace ErpServer.Models;

[Table("construction_sites")]
public class ConstructionSite
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public bool IsBase { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
