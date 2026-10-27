using System.ComponentModel.DataAnnotations;
using ErpServer.Models;

namespace ErpServer.Dtos.Stocks;

public record CreateStockDto(
    [Required] string Name,
    [Required] StockCategory Category,
    string? SerialNumber,
    string? Code,
    [Required] string UnitOfMeasurement,
    [Required] decimal Quantity,
    [Required] decimal UnitPrice,
    string? Supplier,
    DateTime? InvoiceDate,
    string? InvoiceNumber,
    DateTime? DeliveryNoteDate,
    string? DeliveryNoteNumber,
    [Required] StockCondition Condition,
    int? SiteId = null,
    string? InitialNotes = null
);
