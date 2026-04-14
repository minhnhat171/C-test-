using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.Maui.Storage;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public sealed class AudioAssetCacheService : IAudioAssetCacheService
{
    private const string LastPreparedPreferenceKey = "vinhkhanh.offline.audio.lastPreparedUtc";

    private readonly SemaphoreSlim _ioGate = new(1, 1);
    private readonly HttpClient _httpClient;

    public AudioAssetCacheService()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };

        _httpClient = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VinhKhanhGuideApp/1.0");
    }

    public async Task<string> ResolveAsync(string audioAssetPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioAssetPath))
        {
            throw new ArgumentException("Audio asset path is required.", nameof(audioAssetPath));
        }

        var normalizedPath = audioAssetPath.Trim();
        var cachedFilePath = GetCachedFilePath(normalizedPath);
        if (File.Exists(cachedFilePath))
        {
            return cachedFilePath;
        }

        if (Uri.TryCreate(normalizedPath, UriKind.Absolute, out var sourceUri))
        {
            if (sourceUri.Scheme == Uri.UriSchemeFile)
            {
                var localPath = sourceUri.LocalPath;
                if (File.Exists(localPath))
                {
                    return localPath;
                }

                throw new FileNotFoundException("Không tìm thấy file audio cục bộ đã cấu hình.", localPath);
            }

            if (sourceUri.Scheme == Uri.UriSchemeHttp || sourceUri.Scheme == Uri.UriSchemeHttps)
            {
                await DownloadRemoteAudioAsync(sourceUri, cachedFilePath, cancellationToken);
                return cachedFilePath;
            }
        }

        if (Path.IsPathRooted(normalizedPath) && File.Exists(normalizedPath))
        {
            return normalizedPath;
        }

        await CopyPackagedAudioAsync(normalizedPath, cachedFilePath, cancellationToken);
        return cachedFilePath;
    }

    public Task<AudioCacheStatus> GetStatusAsync(
        IEnumerable<POI> pois,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var assetPaths = GetDistinctAssetPaths(pois);
        var cachedAssetCount = 0;
        long cachedBytes = 0;

        foreach (var assetPath in assetPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cachedFilePath = GetCachedFilePath(assetPath);
            if (!File.Exists(cachedFilePath))
            {
                continue;
            }

            cachedAssetCount++;
            cachedBytes += new FileInfo(cachedFilePath).Length;
        }

        return Task.FromResult(new AudioCacheStatus
        {
            AvailableAssetCount = assetPaths.Count,
            CachedAssetCount = cachedAssetCount,
            CachedBytes = cachedBytes,
            LastPreparedAtUtc = ReadTimestampPreference(LastPreparedPreferenceKey)
        });
    }

    public async Task<AudioCachePrefetchResult> PrefetchAsync(
        IEnumerable<POI> pois,
        CancellationToken cancellationToken = default)
    {
        var assetPaths = GetDistinctAssetPaths(pois);
        if (assetPaths.Count == 0)
        {
            Preferences.Default.Remove(LastPreparedPreferenceKey);
            return new AudioCachePrefetchResult();
        }

        var downloadedAssetCount = 0;
        var failedAssetCount = 0;

        foreach (var assetPath in assetPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var cachedFilePath = GetCachedFilePath(assetPath);
                var existedBefore = File.Exists(cachedFilePath);
                await ResolveAsync(assetPath, cancellationToken);

                if (!existedBefore && File.Exists(cachedFilePath))
                {
                    downloadedAssetCount++;
                }
            }
            catch
            {
                failedAssetCount++;
            }
        }

        var status = await GetStatusAsync(pois, cancellationToken);
        if (status.HasCachedAssets)
        {
            Preferences.Default.Set(LastPreparedPreferenceKey, DateTimeOffset.UtcNow.ToString("O"));
            status = await GetStatusAsync(pois, cancellationToken);
        }

        return new AudioCachePrefetchResult
        {
            AvailableAssetCount = status.AvailableAssetCount,
            CachedAssetCount = status.CachedAssetCount,
            DownloadedAssetCount = downloadedAssetCount,
            FailedAssetCount = failedAssetCount,
            CachedBytes = status.CachedBytes
        };
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _ioGate.WaitAsync(cancellationToken);

        try
        {
            var cacheRoot = GetCacheRootDirectory();
            if (Directory.Exists(cacheRoot))
            {
                Directory.Delete(cacheRoot, recursive: true);
            }

            Preferences.Default.Remove(LastPreparedPreferenceKey);
        }
        finally
        {
            _ioGate.Release();
        }
    }

    private static IReadOnlyList<string> GetDistinctAssetPaths(IEnumerable<POI> pois)
    {
        return pois
            .Select(item => item.AudioAssetPath?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    private async Task DownloadRemoteAudioAsync(
        Uri sourceUri,
        string cachedFilePath,
        CancellationToken cancellationToken)
    {
        await _ioGate.WaitAsync(cancellationToken);

        try
        {
            if (File.Exists(cachedFilePath))
            {
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, sourceUri);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await WriteFileAsync(cachedFilePath, bytes, cancellationToken);
        }
        finally
        {
            _ioGate.Release();
        }
    }

    private async Task CopyPackagedAudioAsync(
        string packagePath,
        string cachedFilePath,
        CancellationToken cancellationToken)
    {
        await _ioGate.WaitAsync(cancellationToken);

        try
        {
            if (File.Exists(cachedFilePath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(cachedFilePath)!);

            await using var sourceStream = await FileSystem.OpenAppPackageFileAsync(packagePath);
            await using var destinationStream = File.Create(cachedFilePath);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }
        finally
        {
            _ioGate.Release();
        }
    }

    private static async Task WriteFileAsync(
        string cachedFilePath,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(cachedFilePath)!);

        var tempFilePath = $"{cachedFilePath}.tmp";
        await File.WriteAllBytesAsync(tempFilePath, bytes, cancellationToken);

        if (File.Exists(cachedFilePath))
        {
            File.Delete(cachedFilePath);
        }

        File.Move(tempFilePath, cachedFilePath);
    }

    private static string GetCachedFilePath(string audioAssetPath)
    {
        var extension = DetermineExtension(audioAssetPath);
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(audioAssetPath.Trim())));
        return Path.Combine(GetCacheRootDirectory(), $"{hash}{extension}");
    }

    private static string DetermineExtension(string audioAssetPath)
    {
        if (Uri.TryCreate(audioAssetPath, UriKind.Absolute, out var sourceUri))
        {
            var extensionFromUri = Path.GetExtension(sourceUri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(extensionFromUri))
            {
                return extensionFromUri;
            }
        }

        var extension = Path.GetExtension(audioAssetPath);
        return string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
    }

    private static string GetCacheRootDirectory()
    {
        return Path.Combine(FileSystem.AppDataDirectory, "offline-cache", "audio");
    }

    private static DateTimeOffset? ReadTimestampPreference(string preferenceKey)
    {
        var rawValue = Preferences.Default.Get(preferenceKey, string.Empty);
        return DateTimeOffset.TryParse(rawValue, out var parsedValue)
            ? parsedValue
            : null;
    }
}
