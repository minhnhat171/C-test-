using Microsoft.AspNetCore.Http;

namespace CTest.WebAdmin.Services;

public sealed class PoiImageStorageService
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly IWebHostEnvironment _environment;

    public PoiImageStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<PoiImageSaveResult> SaveAsync(
        IFormFile? image,
        CancellationToken cancellationToken = default)
    {
        if (image is null || image.Length == 0)
        {
            return PoiImageSaveResult.NoImage();
        }

        if (image.Length > MaxImageBytes)
        {
            return PoiImageSaveResult.Failure("Anh POI toi da 5 MB.");
        }

        var extension = Path.GetExtension(image.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return PoiImageSaveResult.Failure("Chi ho tro anh .jpg, .jpeg, .png hoac .webp.");
        }

        if (!string.IsNullOrWhiteSpace(image.ContentType) &&
            !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return PoiImageSaveResult.Failure("File duoc chon khong phai anh hop le.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var imageBytes = await ReadImageBytesAsync(image, cancellationToken);

        foreach (var webRoot in GetImageWebRoots().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var uploadDirectory = Path.Combine(webRoot, "uploads", "pois");
            Directory.CreateDirectory(uploadDirectory);

            var filePath = Path.Combine(uploadDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
        }

        return PoiImageSaveResult.Success($"/uploads/pois/{fileName}");
    }

    private static async Task<byte[]> ReadImageBytesAsync(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        await using var input = image.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private IEnumerable<string> GetImageWebRoots()
    {
        yield return ResolveWebRoot(_environment.ContentRootPath);

        var repositoryRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, ".."));
        yield return Path.Combine(repositoryRoot, "VKFoodAPI", "wwwroot");
    }

    private string ResolveWebRoot(string contentRoot)
    {
        return string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(contentRoot, "wwwroot")
            : _environment.WebRootPath;
    }
}

public sealed record PoiImageSaveResult(
    bool Succeeded,
    string? PublicPath,
    string? ErrorMessage)
{
    public static PoiImageSaveResult Success(string publicPath)
        => new(true, publicPath, null);

    public static PoiImageSaveResult NoImage()
        => new(true, null, null);

    public static PoiImageSaveResult Failure(string message)
        => new(false, null, message);
}
