namespace ErpServer.Dtos.Stocks;

public record ConstructionSiteDto(
    int Id,
    string Name,
    string? Description,
    string? Location,
    bool IsBase,
    int TotalItemsCount,
    decimal TotalValue,
    DateTime CreatedAt
);
