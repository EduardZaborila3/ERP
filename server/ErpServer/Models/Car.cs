using System.ComponentModel.DataAnnotations.Schema;

namespace ErpServer.Models;

public enum CarStatus
{
    Available,
    InUse,
    InService,
    Broken
}

[Table("cars")]
public class Car
{
    public int Id { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }
    public int FabricationYear { get; set; }
    public string VIN { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public CarStatus Status { get; set; } = CarStatus.Available;

    public int? ResponsibleUserId { get; set; }
    public User? ResponsibleUser { get; set; }
}