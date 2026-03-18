using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ILocationService _locationService;
    private readonly IPoiProvider _poiProvider;
    private readonly INarrationService _narrationService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly Dictionary<Guid, DateTimeOffset> _lastNarratedAt = new();

    private IReadOnlyList<POI> _pois = [];

    private bool _isTracking;
    private string _statusText = "Chưa bắt đầu tracking";
    private string _locationText = "Chưa có dữ liệu";
    private string _nearestPoiText = "Chưa xác định";

    public MainViewModel(
        ILocationService locationService,
        IPoiProvider poiProvider,
        INarrationService narrationService,
        GeofenceEngine geofenceEngine)
    {
        _locationService = locationService;
        _poiProvider = poiProvider;
        _narrationService = narrationService;
        _geofenceEngine = geofenceEngine;

        _locationService.LocationUpdated += OnLocationUpdated;

        StartTrackingCommand = new Command(async () => await StartAsync(), () => !IsTracking);
        StopTrackingCommand = new Command(async () => await StopAsync(), () => IsTracking);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PoiStatusItem> PoiStatuses { get; } = [];
    public ObservableCollection<string> EventLogs { get; } = [];

    public ICommand StartTrackingCommand { get; }
    public ICommand StopTrackingCommand { get; }

    public bool IsTracking
    {
        get => _isTracking;
        private set
        {
            if (_isTracking == value)
            {
                return;
            }

            _isTracking = value;
            OnPropertyChanged();
            (StartTrackingCommand as Command)?.ChangeCanExecute();
            (StopTrackingCommand as Command)?.ChangeCanExecute();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string LocationText
    {
        get => _locationText;
        private set
        {
            _locationText = value;
            OnPropertyChanged();
        }
    }

    public string NearestPoiText
    {
        get => _nearestPoiText;
        private set
        {
            _nearestPoiText = value;
            OnPropertyChanged();
        }
    }

    public async Task StartAsync()
    {
        if (IsTracking)
        {
            return;
        }

        _pois = await _poiProvider.GetPoisAsync();
        RefreshPoiList([]);

        try
        {
            await _locationService.StartListeningAsync();
            IsTracking = true;
            StatusText = "Đang tracking GPS";
            AddLog($"{NowLabel()} Bắt đầu tracking GPS");
        }
        catch (Exception ex)
        {
            StatusText = $"Không thể bắt đầu: {ex.Message}";
            AddLog($"{NowLabel()} Lỗi khởi động GPS: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        await _locationService.StopListeningAsync();
        IsTracking = false;
        StatusText = "Đã dừng tracking";
        AddLog($"{NowLabel()} Dừng tracking GPS");
    }

    private async void OnLocationUpdated(object? sender, LocationDto location)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            LocationText =
                $"Lat: {location.Latitude:F6}, Lng: {location.Longitude:F6}, Acc: {location.AccuracyMeters?.ToString("F0") ?? "?"}m";

            var results = _geofenceEngine.Evaluate(location, _pois);
            RefreshPoiList(results);

            var nearest = results.FirstOrDefault();
            if (nearest.Poi is not null)
            {
                NearestPoiText = $"{nearest.Poi.Name} ({nearest.DistanceMeters:F0}m)";
            }

            var candidate = results
                .Where(r => r.IsInside)
                .OrderByDescending(r => r.Poi.Priority)
                .ThenBy(r => r.DistanceMeters)
                .FirstOrDefault();

            if (candidate.Poi is null)
            {
                return;
            }

            if (!CanNarrate(candidate.Poi))
            {
                return;
            }

            _lastNarratedAt[candidate.Poi.Id] = DateTimeOffset.UtcNow;
            StatusText = $"Đang thuyết minh: {candidate.Poi.Name}";
            AddLog($"{NowLabel()} Trigger POI: {candidate.Poi.Name} ({candidate.DistanceMeters:F0}m)");

            try
            {
                await _narrationService.NarrateAsync(candidate.Poi);
                StatusText = "Đang tracking GPS";
            }
            catch (Exception ex)
            {
                StatusText = $"Lỗi TTS: {ex.Message}";
                AddLog($"{NowLabel()} Lỗi TTS: {ex.Message}");
            }
        });
    }

    private void RefreshPoiList(IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> evaluated)
    {
        PoiStatuses.Clear();

        if (evaluated.Count == 0)
        {
            foreach (var poi in _pois)
            {
                PoiStatuses.Add(new PoiStatusItem
                {
                    Name = poi.Name,
                    DistanceMeters = double.NaN,
                    TriggerRadiusMeters = poi.TriggerRadiusMeters,
                    IsInsideRadius = false,
                    Priority = poi.Priority
                });
            }

            return;
        }

        foreach (var item in evaluated)
        {
            PoiStatuses.Add(new PoiStatusItem
            {
                Name = item.Poi.Name,
                DistanceMeters = item.DistanceMeters,
                TriggerRadiusMeters = item.Poi.TriggerRadiusMeters,
                IsInsideRadius = item.IsInside,
                Priority = item.Poi.Priority
            });
        }
    }

    private bool CanNarrate(POI poi)
    {
        if (!_lastNarratedAt.TryGetValue(poi.Id, out var lastNarratedAt))
        {
            return true;
        }

        return DateTimeOffset.UtcNow - lastNarratedAt >= TimeSpan.FromMinutes(poi.CooldownMinutes);
    }

    private void AddLog(string message)
    {
        EventLogs.Insert(0, message);

        while (EventLogs.Count > 20)
        {
            EventLogs.RemoveAt(EventLogs.Count - 1);
        }
    }

    private static string NowLabel()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
