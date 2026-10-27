using System.ComponentModel.DataAnnotations.Schema;

namespace ErpServer.Models;

public enum CarDocumentType
{
    Insurance,              // Asigurare (RCA / CASCO)
    TechnicalBook,          // CIV / Cartea Tehnica
    RegistrationCertificate // Talon / Certificat de Inmatriculare
}

[Table("car_documents")]
public class CarDocument
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public CarDocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Car? Car { get; set; }
}
