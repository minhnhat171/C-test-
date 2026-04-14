using System.Text.Json;
using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Microsoft.Maui.ApplicationModel;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Mappings;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public sealed class PoiOfflineStore : IPoiOfflineStore
{
    private const string PoiDatabaseFileName = "vinhkhanh_guide.db";
    private const int DatabaseVersion = 2;
    private const string PoiTable = "poi_cache";
    private const string MetadataTable = "sync_metadata";
    private const string LastSyncKey = "poi_last_sync_utc";

    private readonly Lazy<PoiOfflineDatabaseHelper> _databaseHelper;

    public PoiOfflineStore()
    {
        _databaseHelper = new Lazy<PoiOfflineDatabaseHelper>(() =>
            new PoiOfflineDatabaseHelper(
                Platform.AppContext ?? throw new InvalidOperationException("AppContext chưa sẵn sàng để mở SQLite.")));
    }

    public Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<POI>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var database = _databaseHelper.Value.ReadableDatabase
                ?? throw new InvalidOperationException("Không thể mở SQLite readable database.");
            using var cursor = database.Query(
                PoiTable,
                null,
                null,
                null,
                null,
                null,
                "name COLLATE NOCASE")
                ?? throw new InvalidOperationException("Không thể truy vấn bảng POI offline.");

            var pois = new List<POI>();
            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                pois.Add(ReadPoi(cursor));
            }

            return pois;
        }, cancellationToken);
    }

    public Task<POI?> GetPoiByIdAsync(Guid poiId, CancellationToken cancellationToken = default)
    {
        if (poiId == Guid.Empty)
        {
            return Task.FromResult<POI?>(null);
        }

        return Task.Run<POI?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var database = _databaseHelper.Value.ReadableDatabase
                ?? throw new InvalidOperationException("Không thể mở SQLite readable database.");
            using var cursor = database.Query(
                PoiTable,
                null,
                "id = ?",
                [poiId.ToString()],
                null,
                null,
                null,
                "1")
                ?? throw new InvalidOperationException("Không thể truy vấn bảng POI offline theo ID.");

            return cursor.MoveToFirst()
                ? ReadPoi(cursor)
                : null;
        }, cancellationToken);
    }

    public Task ReplacePoisAsync(
        IReadOnlyList<POI> pois,
        DateTimeOffset syncedAtUtc,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var database = _databaseHelper.Value.WritableDatabase
                ?? throw new InvalidOperationException("Không thể mở SQLite writable database.");
            database.BeginTransaction();

            try
            {
                database.Delete(PoiTable, null, null);

                foreach (var poi in pois)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var values = BuildPoiValues(poi);
                    database.Replace(
                        PoiTable,
                        null,
                        values);
                }

                using var metadataValues = new ContentValues();
                metadataValues.Put("metadata_key", LastSyncKey);
                metadataValues.Put("metadata_value", syncedAtUtc.ToString("O"));
                database.Replace(
                    MetadataTable,
                    null,
                    metadataValues);

                database.SetTransactionSuccessful();
            }
            finally
            {
                database.EndTransaction();
            }
        }, cancellationToken);
    }

    public Task<DateTimeOffset?> GetLastSyncedAtAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<DateTimeOffset?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var database = _databaseHelper.Value.ReadableDatabase
                ?? throw new InvalidOperationException("Không thể mở SQLite readable database.");
            using var cursor = database.Query(
                MetadataTable,
                ["metadata_value"],
                "metadata_key = ?",
                [LastSyncKey],
                null,
                null,
                null,
                "1")
                ?? throw new InvalidOperationException("Không thể truy vấn metadata sync của SQLite.");

            if (!cursor.MoveToFirst())
            {
                return null;
            }

            var rawValue = cursor.GetString(0);
            return DateTimeOffset.TryParse(rawValue, out var parsedValue)
                ? parsedValue
                : null;
        }, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var database = _databaseHelper.Value.WritableDatabase
                ?? throw new InvalidOperationException("Không thể mở SQLite writable database.");
            database.BeginTransaction();

            try
            {
                database.Delete(PoiTable, null, null);
                database.Delete(MetadataTable, null, null);
                database.SetTransactionSuccessful();
            }
            finally
            {
                database.EndTransaction();
            }
        }, cancellationToken);
    }

    private static ContentValues BuildPoiValues(POI poi)
    {
        var values = new ContentValues();
        values.Put("id", poi.Id.ToString());
        values.Put("code", poi.Code);
        values.Put("name", poi.Name);
        values.Put("category", poi.Category);
        values.Put("image_source", poi.ImageSource);
        values.Put("address", poi.Address);
        values.Put("description", poi.Description);
        values.Put("special_dish", poi.SpecialDish);
        values.Put("narration_text", poi.NarrationText);
        values.Put("map_link", poi.MapLink);
        values.Put("audio_asset_path", poi.AudioAssetPath);
        values.Put("priority", poi.Priority);
        values.Put("latitude", poi.Latitude);
        values.Put("longitude", poi.Longitude);
        values.Put("trigger_radius_meters", poi.TriggerRadiusMeters);
        values.Put("cooldown_minutes", poi.CooldownMinutes);
        values.Put("is_active", poi.IsActive ? 1 : 0);
        values.Put(
            "narration_translations_json",
            JsonSerializer.Serialize(
                poi.NarrationTranslations ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));
        return values;
    }

    private static POI ReadPoi(ICursor cursor)
    {
        var dto = new PoiDto
        {
            Id = Guid.Parse(cursor.GetString(cursor.GetColumnIndexOrThrow("id"))
                ?? throw new InvalidOperationException("POI offline không có ID hợp lệ.")),
            Code = cursor.GetString(cursor.GetColumnIndexOrThrow("code")) ?? string.Empty,
            Name = cursor.GetString(cursor.GetColumnIndexOrThrow("name")) ?? string.Empty,
            Category = cursor.GetString(cursor.GetColumnIndexOrThrow("category")) ?? "Ẩm thực",
            ImageSource = cursor.GetString(cursor.GetColumnIndexOrThrow("image_source")) ?? string.Empty,
            Address = cursor.GetString(cursor.GetColumnIndexOrThrow("address")) ?? string.Empty,
            Description = cursor.GetString(cursor.GetColumnIndexOrThrow("description")) ?? string.Empty,
            SpecialDish = cursor.GetString(cursor.GetColumnIndexOrThrow("special_dish")) ?? string.Empty,
            NarrationText = cursor.GetString(cursor.GetColumnIndexOrThrow("narration_text")) ?? string.Empty,
            MapLink = cursor.GetString(cursor.GetColumnIndexOrThrow("map_link")) ?? string.Empty,
            AudioAssetPath = cursor.GetString(cursor.GetColumnIndexOrThrow("audio_asset_path")) ?? string.Empty,
            Priority = cursor.GetInt(cursor.GetColumnIndexOrThrow("priority")),
            Latitude = cursor.GetDouble(cursor.GetColumnIndexOrThrow("latitude")),
            Longitude = cursor.GetDouble(cursor.GetColumnIndexOrThrow("longitude")),
            TriggerRadiusMeters = cursor.GetDouble(cursor.GetColumnIndexOrThrow("trigger_radius_meters")),
            CooldownMinutes = cursor.GetInt(cursor.GetColumnIndexOrThrow("cooldown_minutes")),
            IsActive = cursor.GetInt(cursor.GetColumnIndexOrThrow("is_active")) == 1,
            NarrationTranslations = JsonSerializer.Deserialize<Dictionary<string, string>>(
                cursor.GetString(cursor.GetColumnIndexOrThrow("narration_translations_json")) ?? "{}")
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        return dto.ToDomain();
    }

    private sealed class PoiOfflineDatabaseHelper(Context context)
        : SQLiteOpenHelper(context, PoiDatabaseFileName, null, DatabaseVersion)
    {
        public override void OnCreate(SQLiteDatabase? db)
        {
            ArgumentNullException.ThrowIfNull(db);

            db.ExecSQL(
                $"""
                CREATE TABLE IF NOT EXISTS {PoiTable} (
                    id TEXT PRIMARY KEY,
                    code TEXT NOT NULL,
                    name TEXT NOT NULL,
                    category TEXT NOT NULL,
                    image_source TEXT NOT NULL,
                    address TEXT NOT NULL,
                    description TEXT NOT NULL,
                    special_dish TEXT NOT NULL,
                    narration_text TEXT NOT NULL,
                    map_link TEXT NOT NULL,
                    audio_asset_path TEXT NOT NULL,
                    priority INTEGER NOT NULL,
                    latitude REAL NOT NULL,
                    longitude REAL NOT NULL,
                    trigger_radius_meters REAL NOT NULL,
                    cooldown_minutes INTEGER NOT NULL,
                    is_active INTEGER NOT NULL,
                    narration_translations_json TEXT NOT NULL
                );
                """);

            db.ExecSQL(
                $"""
                CREATE TABLE IF NOT EXISTS {MetadataTable} (
                    metadata_key TEXT PRIMARY KEY,
                    metadata_value TEXT NOT NULL
                );
                """);
        }

        public override void OnUpgrade(SQLiteDatabase? db, int oldVersion, int newVersion)
        {
            ArgumentNullException.ThrowIfNull(db);

            db.ExecSQL($"DROP TABLE IF EXISTS {PoiTable};");
            db.ExecSQL($"DROP TABLE IF EXISTS {MetadataTable};");
            OnCreate(db);
        }
    }
}
