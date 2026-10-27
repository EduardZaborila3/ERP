namespace ErpServer.Services;

public interface ISupabaseStorageService
{
    Task<string> UploadFileAsync(string bucketName, string filePath, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string bucketName, string filePath, CancellationToken cancellationToken = default);
    string GetPublicUrl(string bucketName, string filePath);
}
