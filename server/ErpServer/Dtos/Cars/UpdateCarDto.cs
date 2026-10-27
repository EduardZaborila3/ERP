using ErpServer.Models;

namespace ErpServer.Dtos.Cars;

public record UpdateCarDto
(
    string Brand,
    string Model,
    int FabricationYear,
    string VIN,
    string RegistrationNumber,
    CarStatus Status,
    int? ResponsibleUserId = null
);