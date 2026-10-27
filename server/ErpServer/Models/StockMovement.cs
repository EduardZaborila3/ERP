using System.ComponentModel.DataAnnotations.Schema;

namespace ErpServer.Models;

[Table("stock_movements")]
public class StockMovement
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public Stock? Stock { get; set; }

    public string StockName { get; set; } = string.Empty;

    public int? FromSiteId { get; set; }
    public ConstructionSite? FromSite { get; set; }

    public int ToSiteId { get; set; }
    public ConstructionSite? ToSite { get; set; }

    public decimal Quantity { get; set; }
    public DateTime MovedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? MovedByUserId { get; set; }
    public User? MovedByUser { get; set; }
    public string? MovedByUserName { get; set; }

    public int? DriverId { get; set; }
    public User? Driver { get; set; }
    public string? DriverName { get; set; }

    public string? Notes { get; set; }
    public string MovementType { get; set; } = "Transfer"; // "InitialEntry", "Transfer", "Return", "Adjustment"
}
