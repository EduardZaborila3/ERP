using System.ComponentModel.DataAnnotations.Schema;

namespace ErpServer.Models;

[Table("stocks")]
public class Stock
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public StockCategory Category { get; set; }
    public string? SerialNumber { get; set; }
    public string? Code { get; set; }
    public required string UnitOfMeasurement { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Supplier { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? DeliveryNoteDate { get; set; }
    public string? DeliveryNoteNumber { get; set; }
    public StockCondition Condition { get; set; } = StockCondition.New;

    public int SiteId { get; set; }
    public ConstructionSite? Site { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}
