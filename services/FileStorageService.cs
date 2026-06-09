namespace HotstarApi.Services;

/// <summary>
/// Saves uploaded files to wwwroot/uploads and returns the relative URL path
/// that can be stored in the database and served directly by UseStaticFiles().
/// </summary>
public interface IFileStorageService
{
    /// <param name="file">The uploaded file.</param>
    /// <param name="subfolder">e.g. "posters", "banners", "videos", "avatars"</param>
    /// <returns>Relative URL string, e.g. "/uploads/posters/abc123.jpg"</returns>
    Task<string> SaveFileAsync(IFormFile file, string subfolder);

    /// <summary>Deletes a previously saved file given its relative URL path.</summary>
    void DeleteFile(string? relativeUrl);
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subfolder)
    {
        // Build the absolute directory path inside wwwroot/uploads/<subfolder>
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", subfolder);
        Directory.CreateDirectory(uploadDir);   // idempotent — safe to call always

        // Generate a collision-resistant filename while preserving the original extension
        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Return the relative URL path (served by UseStaticFiles middleware)
        return $"/uploads/{subfolder}/{fileName}";
    }

    public void DeleteFile(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return;

        // Convert "/uploads/posters/foo.jpg" → absolute path
        var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_env.WebRootPath, relativePath);

        if (File.Exists(absolutePath))
            File.Delete(absolutePath);
    }
}
