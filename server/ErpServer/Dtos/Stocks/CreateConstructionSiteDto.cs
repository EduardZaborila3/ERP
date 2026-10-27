using System.ComponentModel.DataAnnotations;

namespace ErpServer.Dtos.Stocks;

public record CreateConstructionSiteDto(
    [Required] string Name,
    string? Description = null,
    string? Location = null
);
