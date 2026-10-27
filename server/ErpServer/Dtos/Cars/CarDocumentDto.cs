namespace ErpServer.Dtos.Cars;

public record CarDocumentDto(
    int Id,
    int CarId,
    string DocumentType,
    string FileName,
    string StoragePath,
    string FileUrl,
    long FileSizeBytes,
    DateTime UploadedAt
);
