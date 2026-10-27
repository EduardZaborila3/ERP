using ErpServer.Models;

namespace ErpServer.Dtos.Stocks;

public record StockDto(
    int Id,
    string Name,
    StockCategory Category,
    string CategoryName,
    string? SerialNumber,
    string? Code,
    string UnitOfMeasurement,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? Supplier,
    DateTime? InvoiceDate,
    string? InvoiceNumber,
    DateTime? DeliveryNoteDate,
    string? DeliveryNoteNumber,
    StockCondition Condition,
    string ConditionName,
    int SiteId,
    string? SiteName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
