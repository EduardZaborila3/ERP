namespace ErpServer.Dtos.Stocks;

public record StockMovementDto(
    int Id,
    int StockId,
    string StockName,
    int? FromSiteId,
    string? FromSiteName,
    int ToSiteId,
    string ToSiteName,
    decimal Quantity,
    DateTime MovedAt,
    int? MovedByUserId,
    string? MovedByUserName,
    string? Notes,
    string MovementType,
    int? DriverId = null,
    string? DriverName = null
);
