using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;

namespace VKFoodAPI.Services;

public sealed class AuditLogRepository
{
    private const int MaxStoredLogs = 10000;

    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _dataFilePath;
    private List<AuditLogDto> _items;

    public AuditLogRepository(IHostEnvironment environment)
    {
        var dataDirectory = AppDataPathResolver.GetDataDirectory(environment);
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "audit-logs.json");
        _items = LoadItems();
    }

    public IReadOnlyList<AuditLogDto> GetLogs(
        string? entityName = null,
        string? username = null,
        int? limit = null)
    {
        lock (_syncRoot)
        {
            var normalizedEntityName = NormalizeLookup(entityName);
            var normalizedUsername = NormalizeLookup(username);
            var query = _items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(normalizedEntityName))
            {
                query = query.Where(item =>
                    string.Equals(NormalizeLookup(item.EntityName), normalizedEntityName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(normalizedUsername))
            {
                query = query.Where(item =>
                    string.Equals(NormalizeLookup(item.Username), normalizedUsername, StringComparison.OrdinalIgnoreCase));
            }

            query = query.OrderByDescending(item => item.CreatedAtUtc);

            if (limit is > 0)
            {
                query = query.Take(Math.Min(limit.Value, MaxStoredLogs));
            }

            return query.Select(item => item.Clone()).ToList();
        }
    }

    public AuditLogDto Create(AuditLogCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_syncRoot)
        {
            var created = Normalize(new AuditLogDto
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Username = request.Username,
                Action = request.Action,
                EntityName = request.EntityName,
                EntityId = request.EntityId,
                Description = request.Description,
                IpAddress = request.IpAddress,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            _items.Add(created);
            TrimUnsafe();
            SaveUnsafe();
            return created.Clone();
        }
    }

    private List<AuditLogDto> LoadItems()
    {
        if (!File.Exists(_dataFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            return (JsonSerializer.Deserialize<List<AuditLogDto>>(json, _jsonOptions) ?? [])
                .Select(Normalize)
                .OrderByDescending(item => item.CreatedAtUtc)
                .Take(MaxStoredLogs)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private void TrimUnsafe()
    {
        if (_items.Count <= MaxStoredLogs)
        {
            return;
        }

        _items = _items
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(MaxStoredLogs)
            .ToList();
    }

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_items, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private static AuditLogDto Normalize(AuditLogDto item)
    {
        var normalized = item.Clone();
        normalized.Id = normalized.Id == Guid.Empty ? Guid.NewGuid() : normalized.Id;
        normalized.UserId = normalized.UserId?.Trim() ?? string.Empty;
        normalized.Username = string.IsNullOrWhiteSpace(normalized.Username) ? "system" : normalized.Username.Trim();
        normalized.Action = normalized.Action?.Trim() ?? string.Empty;
        normalized.EntityName = normalized.EntityName?.Trim() ?? string.Empty;
        normalized.EntityId = normalized.EntityId?.Trim() ?? string.Empty;
        normalized.Description = normalized.Description?.Trim() ?? string.Empty;
        normalized.IpAddress = normalized.IpAddress?.Trim() ?? string.Empty;
        normalized.CreatedAtUtc = normalized.CreatedAtUtc == default
            ? DateTimeOffset.UtcNow
            : normalized.CreatedAtUtc.ToUniversalTime();
        return normalized;
    }

    private static string NormalizeLookup(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
