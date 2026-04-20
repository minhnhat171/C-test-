using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;

namespace VKFoodAPI.Services;

public class UserManagementRepository
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(15);

    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _dataFilePath;
    private readonly ListeningHistoryRepository _listeningHistoryRepository;
    private readonly PoiRepository _poiRepository;
    private List<UserProfileRecord> _profiles;

    public UserManagementRepository(
        IHostEnvironment environment,
        ListeningHistoryRepository listeningHistoryRepository,
        PoiRepository poiRepository)
    {
        _listeningHistoryRepository = listeningHistoryRepository;
        _poiRepository = poiRepository;

        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "user-profiles.json");
        _profiles = LoadProfiles();
    }

    public IReadOnlyList<AdminUserSummaryDto> GetAllUsers()
    {
        lock (_syncRoot)
        {
            return BuildSnapshotsUnsafe()
                .Select(snapshot => snapshot.Summary.Clone())
                .ToList();
        }
    }

    public IReadOnlyList<AdminUserSummaryDto> GetUsersByStatus(string? status)
    {
        var normalizedStatus = NormalizeStatus(status);

        lock (_syncRoot)
        {
            return BuildSnapshotsUnsafe()
                .Where(snapshot => normalizedStatus == "all" || snapshot.Summary.Status == normalizedStatus)
                .Select(snapshot => snapshot.Summary.Clone())
                .ToList();
        }
    }

    public IReadOnlyList<AdminUserSummaryDto> SearchUsers(string? keyword)
    {
        var normalizedKeyword = keyword?.Trim() ?? string.Empty;

        lock (_syncRoot)
        {
            var snapshots = BuildSnapshotsUnsafe();
            if (string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                return snapshots.Select(snapshot => snapshot.Summary.Clone()).ToList();
            }

            return snapshots
                .Where(snapshot =>
                    Contains(snapshot.Summary.UserCode, normalizedKeyword) ||
                    Contains(snapshot.Summary.DisplayName, normalizedKeyword) ||
                    Contains(snapshot.Summary.Email, normalizedKeyword) ||
                    Contains(snapshot.Summary.Role, normalizedKeyword) ||
                    Contains(snapshot.Summary.LastPoiName, normalizedKeyword))
                .Select(snapshot => snapshot.Summary.Clone())
                .ToList();
        }
    }

    public AdminUserDetailDto? GetUserDetails(Guid userId)
    {
        lock (_syncRoot)
        {
            return BuildSnapshotsUnsafe()
                .FirstOrDefault(snapshot => snapshot.Summary.Id == userId)?
                .Detail
                .Clone();
        }
    }

    public AdminUserLocationDto? GetUserLocation(Guid userId)
    {
        lock (_syncRoot)
        {
            return BuildSnapshotsUnsafe()
                .FirstOrDefault(snapshot => snapshot.Summary.Id == userId)?
                .Location?
                .Clone();
        }
    }

    public AdminUserDetailDto UpsertProfile(AdminUserProfileUpsertRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_syncRoot)
        {
            var normalizedRequest = NormalizeUpsertRequest(request);
            var existingIndex = _profiles.FindIndex(profile =>
                (normalizedRequest.Id.HasValue && normalizedRequest.Id.Value != Guid.Empty && profile.Id == normalizedRequest.Id.Value) ||
                (!string.IsNullOrWhiteSpace(normalizedRequest.UserCode) &&
                 string.Equals(profile.UserCode, normalizedRequest.UserCode, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(normalizedRequest.Email) &&
                 string.Equals(profile.Email, normalizedRequest.Email, StringComparison.OrdinalIgnoreCase)));
            var existingProfile = _profiles.FirstOrDefault(profile =>
                (normalizedRequest.Id.HasValue && normalizedRequest.Id.Value != Guid.Empty && profile.Id == normalizedRequest.Id.Value) ||
                (!string.IsNullOrWhiteSpace(normalizedRequest.UserCode) &&
                 string.Equals(profile.UserCode, normalizedRequest.UserCode, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(normalizedRequest.Email) &&
                 string.Equals(profile.Email, normalizedRequest.Email, StringComparison.OrdinalIgnoreCase)));

            if (existingProfile is null)
            {
                existingProfile = NormalizeProfile(new UserProfileRecord
                {
                    Id = normalizedRequest.Id ?? Guid.Empty,
                    UserCode = normalizedRequest.UserCode,
                    DisplayName = normalizedRequest.DisplayName,
                    Email = normalizedRequest.Email,
                    Role = normalizedRequest.Role,
                    PhoneNumber = normalizedRequest.PhoneNumber,
                    PreferredLanguage = normalizedRequest.PreferredLanguage,
                    DevicePlatform = normalizedRequest.DevicePlatform,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });

                _profiles.Add(existingProfile);
            }
            else
            {
                existingProfile.UserCode = normalizedRequest.UserCode;
                existingProfile.DisplayName = normalizedRequest.DisplayName;
                existingProfile.Email = normalizedRequest.Email;
                existingProfile.Role = normalizedRequest.Role;
                existingProfile.PhoneNumber = normalizedRequest.PhoneNumber;
                existingProfile.PreferredLanguage = normalizedRequest.PreferredLanguage;
                existingProfile.DevicePlatform = normalizedRequest.DevicePlatform;
            }

            existingProfile = NormalizeProfile(existingProfile);
            if (existingIndex >= 0)
            {
                _profiles[existingIndex] = existingProfile;
            }

            SaveProfilesUnsafe();

            return BuildSnapshotsUnsafe()
                .First(snapshot => snapshot.Summary.Id == existingProfile.Id)
                .Detail
                .Clone();
        }
    }

    private List<UserSnapshot> BuildSnapshotsUnsafe()
    {
        var profiles = _profiles
            .Select(profile => profile.Clone())
            .ToList();
        var history = _listeningHistoryRepository.GetListeningHistory(limit: null);
        var pois = _poiRepository.GetAll();

        var profileByCode = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.UserCode))
            .GroupBy(profile => NormalizeLookup(profile.UserCode))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var profileByEmail = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Email))
            .GroupBy(profile => NormalizeLookup(profile.Email))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var poiById = pois
            .Where(poi => poi.Id != Guid.Empty)
            .GroupBy(poi => poi.Id)
            .ToDictionary(group => group.Key, group => group.First());
        var poiByCode = pois
            .Where(poi => !string.IsNullOrWhiteSpace(poi.Code))
            .GroupBy(poi => NormalizeLookup(poi.Code))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var poiByName = pois
            .Where(poi => !string.IsNullOrWhiteSpace(poi.Name))
            .GroupBy(poi => NormalizeLookup(poi.Name))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var groupedEntries = new Dictionary<string, UserHistoryAggregation>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in history)
        {
            var matchedProfile = ResolveProfile(entry, profileByCode, profileByEmail);
            var groupKey = matchedProfile is not null
                ? matchedProfile.Id.ToString("N")
                : $"dynamic:{ResolveDynamicKey(entry)}";

            if (!groupedEntries.TryGetValue(groupKey, out var aggregation))
            {
                aggregation = new UserHistoryAggregation();
                groupedEntries[groupKey] = aggregation;
            }

            aggregation.Profile ??= matchedProfile;
            aggregation.Entries.Add(entry.Clone());
        }

        var snapshots = new List<UserSnapshot>();
        var seenProfileIds = new HashSet<Guid>();

        foreach (var aggregation in groupedEntries.Values)
        {
            var snapshot = BuildSnapshot(
                aggregation.Profile,
                aggregation.Entries,
                poiById,
                poiByCode,
                poiByName);

            snapshots.Add(snapshot);

            if (aggregation.Profile is not null)
            {
                seenProfileIds.Add(aggregation.Profile.Id);
            }
        }

        foreach (var profile in profiles.Where(profile => !seenProfileIds.Contains(profile.Id)))
        {
            snapshots.Add(BuildSnapshot(
                profile,
                [],
                poiById,
                poiByCode,
                poiByName));
        }

        return snapshots
            .OrderByDescending(snapshot => snapshot.Summary.LastActiveAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(snapshot => snapshot.Summary.DisplayName)
            .ThenBy(snapshot => snapshot.Summary.UserCode)
            .ToList();
    }

    private static UserSnapshot BuildSnapshot(
        UserProfileRecord? profile,
        List<ListeningHistoryEntryDto> entries,
        IReadOnlyDictionary<Guid, PoiDto> poiById,
        IReadOnlyDictionary<string, PoiDto> poiByCode,
        IReadOnlyDictionary<string, PoiDto> poiByName)
    {
        var orderedEntries = entries
            .OrderByDescending(GetActivityAtUtc)
            .ThenByDescending(entry => entry.StartedAtUtc)
            .ToList();
        var latestEntry = orderedEntries.FirstOrDefault();
        var latestPoi = ResolvePoi(latestEntry, poiById, poiByCode, poiByName);
        DateTimeOffset? lastActiveAtUtc = latestEntry is null
            ? null
            : GetActivityAtUtc(latestEntry);
        var isOnline = lastActiveAtUtc.HasValue && lastActiveAtUtc.Value >= DateTimeOffset.UtcNow.Subtract(OnlineThreshold);
        var userCode = profile?.UserCode?.Trim();
        if (string.IsNullOrWhiteSpace(userCode))
        {
            userCode = latestEntry?.UserCode?.Trim();
        }

        var displayName = profile?.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = latestEntry?.UserDisplayName?.Trim();
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = !string.IsNullOrWhiteSpace(userCode)
                ? userCode
                : latestEntry?.UserEmail?.Trim();
        }

        var email = profile?.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            email = latestEntry?.UserEmail?.Trim();
        }

        var role = string.IsNullOrWhiteSpace(profile?.Role) ? "User" : profile!.Role.Trim();
        var preferredLanguage = string.IsNullOrWhiteSpace(profile?.PreferredLanguage)
            ? latestEntry?.Language?.Trim() ?? "vi-VN"
            : profile!.PreferredLanguage.Trim();
        var phoneNumber = profile?.PhoneNumber?.Trim() ?? string.Empty;
        var lastPoiName = latestPoi?.Name ?? latestEntry?.PoiName?.Trim() ?? string.Empty;
        var lastPoiCode = latestPoi?.Code ?? latestEntry?.PoiCode?.Trim() ?? string.Empty;
        var totalSessions = orderedEntries.Count;
        var completedSessions = orderedEntries.Count(entry => entry.Completed);
        var totalListenSeconds = orderedEntries.Sum(entry => Math.Max(0, entry.ListenSeconds));
        var status = isOnline ? "online" : "offline";
        var createdAtUtc = profile?.CreatedAtUtc
            ?? latestEntry?.StartedAtUtc
            ?? DateTimeOffset.UtcNow;
        var userId = profile?.Id ?? CreateDeterministicGuid(
            $"{userCode}|{email}|{displayName}");

        return new UserSnapshot
        {
            Summary = new AdminUserSummaryDto
            {
                Id = userId,
                UserCode = userCode ?? string.Empty,
                DisplayName = displayName ?? "Unknown user",
                Email = email ?? string.Empty,
                PhoneNumber = phoneNumber,
                PreferredLanguage = preferredLanguage,
                Role = role,
                Status = status,
                IsOnline = isOnline,
                LastActiveAtUtc = lastActiveAtUtc,
                LastPoiName = lastPoiName,
                DevicePlatform = !string.IsNullOrWhiteSpace(latestEntry?.DevicePlatform)
                    ? latestEntry!.DevicePlatform.Trim()
                    : profile?.DevicePlatform?.Trim() ?? string.Empty,
                TotalSessions = totalSessions,
                CompletedSessions = completedSessions,
                TotalListenSeconds = totalListenSeconds
            },
            Detail = new AdminUserDetailDto
            {
                Id = userId,
                UserCode = userCode ?? string.Empty,
                DisplayName = displayName ?? "Unknown user",
                Email = email ?? string.Empty,
                Role = role,
                Status = status,
                IsOnline = isOnline,
                CreatedAtUtc = createdAtUtc,
                LastActiveAtUtc = lastActiveAtUtc,
                LastCompletedAtUtc = orderedEntries
                    .Where(entry => entry.CompletedAtUtc.HasValue)
                    .Select(entry => entry.CompletedAtUtc)
                    .Max(),
                PreferredLanguage = preferredLanguage,
                PhoneNumber = phoneNumber,
                DevicePlatform = !string.IsNullOrWhiteSpace(latestEntry?.DevicePlatform)
                    ? latestEntry!.DevicePlatform.Trim()
                    : profile?.DevicePlatform?.Trim() ?? string.Empty,
                LastTriggerType = latestEntry?.TriggerType?.Trim() ?? string.Empty,
                LastSource = latestEntry?.Source?.Trim() ?? string.Empty,
                LastPoiName = lastPoiName,
                LastPoiCode = lastPoiCode,
                TotalSessions = totalSessions,
                CompletedSessions = completedSessions,
                TotalListenSeconds = totalListenSeconds
            },
            Location = latestPoi is null
                ? null
                : new AdminUserLocationDto
                {
                    UserId = userId,
                    UserCode = userCode ?? string.Empty,
                    DisplayName = displayName ?? "Unknown user",
                    PoiId = latestPoi.Id,
                    PoiCode = latestPoi.Code,
                    PoiName = latestPoi.Name,
                    Address = latestPoi.Address,
                    Latitude = latestPoi.Latitude,
                    Longitude = latestPoi.Longitude,
                    MapLink = string.IsNullOrWhiteSpace(latestPoi.MapLink)
                        ? $"https://maps.google.com/?q={latestPoi.Latitude},{latestPoi.Longitude}"
                        : latestPoi.MapLink,
                    UpdatedAtUtc = lastActiveAtUtc
                }
        };
    }

    private List<UserProfileRecord> LoadProfiles()
    {
        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = File.ReadAllText(_dataFilePath);
                var items = JsonSerializer.Deserialize<List<UserProfileRecord>>(json, _jsonOptions);
                if (items is not null)
                {
                    return items.Select(NormalizeProfile).ToList();
                }
            }
            catch
            {
                // Fall back to the seed data if the file is missing or invalid.
            }
        }

        var seeded = CreateSeedProfiles()
            .Select(NormalizeProfile)
            .ToList();

        _profiles = seeded;
        SaveProfilesUnsafe();

        return seeded;
    }

    private void SaveProfilesUnsafe()
    {
        var json = JsonSerializer.Serialize(_profiles, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private static UserProfileRecord? ResolveProfile(
        ListeningHistoryEntryDto entry,
        IReadOnlyDictionary<string, UserProfileRecord> profileByCode,
        IReadOnlyDictionary<string, UserProfileRecord> profileByEmail)
    {
        var userCode = NormalizeLookup(entry.UserCode);
        if (!string.IsNullOrWhiteSpace(userCode) &&
            profileByCode.TryGetValue(userCode, out var profileByUserCode))
        {
            return profileByUserCode;
        }

        var email = NormalizeLookup(entry.UserEmail);
        if (!string.IsNullOrWhiteSpace(email) &&
            profileByEmail.TryGetValue(email, out var profileByUserEmail))
        {
            return profileByUserEmail;
        }

        return null;
    }

    private static PoiDto? ResolvePoi(
        ListeningHistoryEntryDto? entry,
        IReadOnlyDictionary<Guid, PoiDto> poiById,
        IReadOnlyDictionary<string, PoiDto> poiByCode,
        IReadOnlyDictionary<string, PoiDto> poiByName)
    {
        if (entry is null)
        {
            return null;
        }

        if (entry.PoiId != Guid.Empty && poiById.TryGetValue(entry.PoiId, out var poiByIdentifier))
        {
            return poiByIdentifier;
        }

        var poiCode = NormalizeLookup(entry.PoiCode);
        if (!string.IsNullOrWhiteSpace(poiCode) &&
            poiByCode.TryGetValue(poiCode, out var poiByCodeValue))
        {
            return poiByCodeValue;
        }

        var poiName = NormalizeLookup(entry.PoiName);
        if (!string.IsNullOrWhiteSpace(poiName) &&
            poiByName.TryGetValue(poiName, out var poiByNameValue))
        {
            return poiByNameValue;
        }

        return null;
    }

    private static bool Contains(string? source, string keyword)
    {
        return source?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static DateTimeOffset GetActivityAtUtc(ListeningHistoryEntryDto entry)
    {
        return entry.CompletedAtUtc ?? entry.StartedAtUtc;
    }

    private static string NormalizeStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "online" => "online",
            "offline" => "offline",
            _ => "all"
        };
    }

    private static string ResolveDynamicKey(ListeningHistoryEntryDto entry)
    {
        var normalizedUserCode = NormalizeLookup(entry.UserCode);
        if (!string.IsNullOrWhiteSpace(normalizedUserCode))
        {
            return normalizedUserCode;
        }

        var normalizedEmail = NormalizeLookup(entry.UserEmail);
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return normalizedEmail;
        }

        var normalizedDisplayName = NormalizeLookup(entry.UserDisplayName);
        return string.IsNullOrWhiteSpace(normalizedDisplayName)
            ? $"anonymous-{entry.Id:N}"
            : normalizedDisplayName;
    }

    private static UserProfileRecord NormalizeProfile(UserProfileRecord profile)
    {
        var normalized = profile.Clone();
        normalized.Id = normalized.Id == Guid.Empty
            ? CreateDeterministicGuid($"{normalized.UserCode}|{normalized.Email}|{normalized.DisplayName}")
            : normalized.Id;
        normalized.UserCode ??= string.Empty;
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName)
            ? normalized.UserCode
            : normalized.DisplayName;
        normalized.Email ??= string.Empty;
        normalized.Role = string.IsNullOrWhiteSpace(normalized.Role) ? "User" : normalized.Role;
        normalized.PhoneNumber ??= string.Empty;
        normalized.PreferredLanguage = string.IsNullOrWhiteSpace(normalized.PreferredLanguage)
            ? "vi-VN"
            : normalized.PreferredLanguage;
        normalized.DevicePlatform ??= string.Empty;
        normalized.CreatedAtUtc = normalized.CreatedAtUtc == default
            ? DateTimeOffset.UtcNow
            : normalized.CreatedAtUtc;
        return normalized;
    }

    private static AdminUserProfileUpsertRequest NormalizeUpsertRequest(AdminUserProfileUpsertRequest request)
    {
        var userCode = NormalizeLookup(request.UserCode);
        var email = NormalizeLookup(request.Email);
        var displayName = request.DisplayName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userCode))
        {
            userCode = !string.IsNullOrWhiteSpace(email)
                ? email
                : CreateDeterministicGuid($"{displayName}|{request.PhoneNumber}").ToString("N")[..12];
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = userCode;
        }

        return new AdminUserProfileUpsertRequest
        {
            Id = request.Id,
            UserCode = userCode,
            DisplayName = displayName,
            Email = email,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Guest" : request.Role.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
            PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage)
                ? "vi-VN"
                : request.PreferredLanguage.Trim(),
            DevicePlatform = request.DevicePlatform?.Trim() ?? string.Empty
        };
    }

    private static string NormalizeLookup(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant());
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }

    private static IEnumerable<UserProfileRecord> CreateSeedProfiles()
    {
        return
        [
            new UserProfileRecord
            {
                Id = CreateDeterministicGuid("USR001"),
                UserCode = "USR001",
                DisplayName = "Nguyen Minh Anh",
                Email = "usr001@vinhkhanh.app",
                Role = "User",
                PhoneNumber = "0901000001",
                PreferredLanguage = "vi-VN",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-4)
            },
            new UserProfileRecord
            {
                Id = CreateDeterministicGuid("USR002"),
                UserCode = "USR002",
                DisplayName = "Tran Bao Chau",
                Email = "usr002@vinhkhanh.app",
                Role = "User",
                PhoneNumber = "0901000002",
                PreferredLanguage = "en-US",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-3)
            },
            new UserProfileRecord
            {
                Id = CreateDeterministicGuid("USR003"),
                UserCode = "USR003",
                DisplayName = "Le Quoc Dat",
                Email = "usr003@vinhkhanh.app",
                Role = "Contributor",
                PhoneNumber = "0901000003",
                PreferredLanguage = "en-US",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-2)
            },
            new UserProfileRecord
            {
                Id = CreateDeterministicGuid("USR004"),
                UserCode = "USR004",
                DisplayName = "Pham Thu Ha",
                Email = "usr004@vinhkhanh.app",
                Role = "User",
                PhoneNumber = "0901000004",
                PreferredLanguage = "vi-VN",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-1)
            }
        ];
    }

    private sealed class UserHistoryAggregation
    {
        public UserProfileRecord? Profile { get; set; }
        public List<ListeningHistoryEntryDto> Entries { get; } = [];
    }

    private sealed class UserSnapshot
    {
        public AdminUserSummaryDto Summary { get; set; } = new();
        public AdminUserDetailDto Detail { get; set; } = new();
        public AdminUserLocationDto? Location { get; set; }
    }

    private sealed class UserProfileRecord
    {
        public Guid Id { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = string.Empty;
        public string DevicePlatform { get; set; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; set; }

        public UserProfileRecord Clone()
        {
            return new UserProfileRecord
            {
                Id = Id,
                UserCode = UserCode,
                DisplayName = DisplayName,
                Email = Email,
                Role = Role,
                PhoneNumber = PhoneNumber,
                PreferredLanguage = PreferredLanguage,
                DevicePlatform = DevicePlatform,
                CreatedAtUtc = CreatedAtUtc
            };
        }
    }
}
