using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Stocks;

public record BatchMoveItemDto(
    [Required] int StockId,
    [Required] decimal Quantity
);

public record BatchMoveStockDto(
    [Required] int FromSiteId,
    [Required] int ToSiteId,
    [Required] List<BatchMoveItemDto> Items,
    int? DriverId = null,
    string? DriverName = null,
    string? CarPlate = null,
    string? Notes = null,
    DateTime? MovedAt = null
);
