using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Stocks;

public record MoveStockDto(
    [Required] int ToSiteId,
    [Required] decimal Quantity,
    string? Notes = null,
    int? DriverId = null,
    string? DriverName = null
);
