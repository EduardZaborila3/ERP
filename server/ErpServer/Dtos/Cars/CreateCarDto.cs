using System.ComponentModel.DataAnnotations;
using ErpServer.Models;

namespace ErpServer.Dtos.Cars;

public record CreateCarDto(
    [Required] string Brand,
    [Required] string Model,
    [Required] int FabricationYear,
    [Required] string VIN,
    [Required] string RegistrationNumber,
    [Required] CarStatus Status,
    int? ResponsibleUserId = null
);