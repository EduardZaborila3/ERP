using ErpServer.Models;

namespace ErpServer.Dtos.Cars;

public record CarDto(
    int Id,
    string Brand,
    string Model,
    int FabricationYear,
    string VIN,
    string RegistrationNumber,
    CarStatus Status,
    int? ResponsibleUserId = null,
    string? ResponsibleUserName = null
);
