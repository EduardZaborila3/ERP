using System.Net.Http.Headers;

namespace ErpServer.Services;

public class SupabaseStorageService(
    IHttpClientFactory httpClientFactory, 
    IConfiguration configuration, 
    ILogger<SupabaseStorageService> logger) : ISupabaseStorageService
{
    private readonly string _supabaseUrl = configuration["Supabase:Url"]?.TrimEnd('/') ?? "https://oczljyuubrqijwhbqxgh.supabase.co";
    private readonly string _apiKey = configuration["Supabase:ApiKey"] ?? string.Empty;

    public async Task<string> UploadFileAsync(
        string bucketName, 
        string filePath, 
        Stream fileStream, 
        string contentType, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Cheia Supabase API Key nu este configurată în appsettings.json ('Supabase:ApiKey'). Adăugați cheia anon sau service_role din Supabase Dashboard -> Project Settings -> API.");
        }

        var client = httpClientFactory.CreateClient("SupabaseStorage");
        var normalizedPath = filePath.TrimStart('/');
        var url = $"{_supabaseUrl}/storage/v1/object/{bucketName}/{normalizedPath}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("apikey", _apiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Add("x-upsert", "true");

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = content;

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Supabase Storage upload failed: Status {StatusCode}, Response: {Error}", response.StatusCode, err);
            throw new InvalidOperationException($"Supabase storage upload failed: {response.StatusCode} - {err}");
        }

        return GetPublicUrl(bucketName, normalizedPath);
    }

    public async Task<bool> DeleteFileAsync(
        string bucketName, 
        string filePath, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("Supabase:ApiKey is not configured; skipping remote storage file deletion.");
            return true;
        }

        var client = httpClientFactory.CreateClient("SupabaseStorage");
        var normalizedPath = filePath.TrimStart('/');
        var url = $"{_supabaseUrl}/storage/v1/object/{bucketName}/{normalizedPath}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("apikey", _apiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Supabase Storage delete failed: Status {StatusCode}, Response: {Error}", response.StatusCode, err);
            return false;
        }

        return true;
    }

    public string GetPublicUrl(string bucketName, string filePath)
    {
        var normalizedPath = filePath.TrimStart('/');
        return $"{_supabaseUrl}/storage/v1/object/public/{bucketName}/{normalizedPath}";
    }
}
