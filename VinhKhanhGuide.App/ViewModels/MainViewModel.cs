using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public const double EntranceLatitude = 10.7614500;
    public const double EntranceLongitude = 106.7028200;
    public const string EntranceName = "Cổng phố ẩm thực Vĩnh Khánh";
    public const string EntranceAddress = "40 Vĩnh Khánh, P. Khánh Hội, Q.4";

    private readonly ILocationService _locationService;
    private readonly IPoiProvider _poiProvider;
    private readonly INarrationService _narrationService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly SemaphoreSlim _locationUpdateGate = new(1, 1);
    private readonly Dictionary<Guid, DateTimeOffset> _lastNarratedAt = new();
    private readonly HashSet<Guid> _insidePoiIds = [];

    private IReadOnlyList<POI> _pois = Array.Empty<POI>();
    private bool _isInitialized;
    private bool _isTracking;
    private bool _isNarrating;
    private int _narrationSessionId;
    private Guid? _lastAutoNarratedPoiId;
    private LocationDto? _lastLocation;
    private POI? _selectedPoi;
    private bool _hasUserSelectedPoi;
    private string _statusText = "Đang chờ khởi động GPS";
    private string _locationText = "Bạn đang xem cổng phố ẩm thực Vĩnh Khánh";
    private string _nearestPoiText = "Chọn quán hoặc chạm bản đồ để nghe thuyết minh";
    private string _selectedPoiName = "Ốc Oanh";
    private string _selectedPoiAddress = string.Empty;
    private string _selectedPoiDescription = string.Empty;
    private string _selectedPoiDishText = string.Empty;
    private string _selectedPoiStatusText = string.Empty;
    private string _selectedPoiNarrationPreview = string.Empty;
    private string _selectedPoiMapLink = string.Empty;
    private string _selectedPoiImageSource = string.Empty;
    private string _selectedLanguage = "vi";

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

        FeaturedDishes.Add(new FoodCategoryItem { Icon = "🐚", Name = "Ốc" });
        FeaturedDishes.Add(new FoodCategoryItem { Icon = "🥩", Name = "Bò nướng" });
        FeaturedDishes.Add(new FoodCategoryItem { Icon = "🍲", Name = "Lẩu" });
        FeaturedDishes.Add(new FoodCategoryItem { Icon = "🦀", Name = "Cua" });

        StartTrackingCommand = new Command(async () => await StartAsync(), () => !IsTracking);
        StopTrackingCommand = new Command(async () => await StopAsync(), () => IsTracking);
        StopNarrationCommand = new Command(async () => await StopNarrationAsync(), () => IsNarrating);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? MapStateChanged;

    public ObservableCollection<FoodCategoryItem> FeaturedDishes { get; } = new();
    public ObservableCollection<PoiStatusItem> PoiStatuses { get; } = new();
    public ObservableCollection<string> EventLogs { get; } = new();
    public IReadOnlyList<string> SupportedLanguages { get; } = ["vi", "en", "zh", "ko", "fr"];

    public ICommand StartTrackingCommand { get; }
    public ICommand StopTrackingCommand { get; }
    public ICommand StopNarrationCommand { get; }

    public IReadOnlyList<POI> Pois => _pois;
    public LocationDto? LastLocation => _lastLocation;
    public Guid? SelectedPoiId => _selectedPoi?.Id;

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

    public bool IsNarrating
    {
        get => _isNarrating;
        private set
        {
            if (_isNarrating == value)
            {
                return;
            }

            _isNarrating = value;
            OnPropertyChanged();
            (StopNarrationCommand as Command)?.ChangeCanExecute();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string LocationText
    {
        get => _locationText;
        private set => SetProperty(ref _locationText, value);
    }

    public string NearestPoiText
    {
        get => _nearestPoiText;
        private set => SetProperty(ref _nearestPoiText, value);
    }

    public string SelectedPoiName
    {
        get => _selectedPoiName;
        private set => SetProperty(ref _selectedPoiName, value);
    }

    public string SelectedPoiAddress
    {
        get => _selectedPoiAddress;
        private set => SetProperty(ref _selectedPoiAddress, value);
    }

    public string SelectedPoiDescription
    {
        get => _selectedPoiDescription;
        private set => SetProperty(ref _selectedPoiDescription, value);
    }

    public string SelectedPoiDishText
    {
        get => _selectedPoiDishText;
        private set => SetProperty(ref _selectedPoiDishText, value);
    }

    public string SelectedPoiStatusText
    {
        get => _selectedPoiStatusText;
        private set => SetProperty(ref _selectedPoiStatusText, value);
    }

    public string SelectedPoiNarrationPreview
    {
        get => _selectedPoiNarrationPreview;
        private set => SetProperty(ref _selectedPoiNarrationPreview, value);
    }

    public string SelectedPoiMapLink
    {
        get => _selectedPoiMapLink;
        private set => SetProperty(ref _selectedPoiMapLink, value);
    }

    public string SelectedPoiImageSource
    {
        get => _selectedPoiImageSource;
        private set => SetProperty(ref _selectedPoiImageSource, value);
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!SetProperty(ref _selectedLanguage, value))
            {
                return;
            }

            UpdateSelectedPoiDetails();
        }
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _pois = await _poiProvider.GetPoisAsync();
        RefreshPoiList(Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>(), null);
        SetSelectedPoi(_pois.FirstOrDefault(), false, null);
        _isInitialized = true;
        RaiseMapStateChanged();
    }

    public async Task StartAsync()
    {
        if (IsTracking)
        {
            return;
        }

        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        try
        {
            StatusText = "Đang khởi động GPS...";
            await _locationService.StartListeningAsync();
            IsTracking = true;
            StatusText = "GPS đang hoạt động";
            AddLog($"{NowLabel()} Bật tracking GPS");
        }
        catch (Exception ex)
        {
            StatusText = $"Không thể bật GPS: {ex.Message}";
            AddLog($"{NowLabel()} Lỗi GPS: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        await _locationService.StopListeningAsync();
        IsTracking = false;
        _insidePoiIds.Clear();
        _lastAutoNarratedPoiId = null;
        StatusText = "GPS đã dừng";
        AddLog($"{NowLabel()} Dừng tracking GPS");
    }

    public void SelectPoi(Guid poiId, bool userInitiated = true)
    {
        var poi = _pois.FirstOrDefault(item => item.Id == poiId);
        if (poi is null)
        {
            return;
        }

        SetSelectedPoi(poi, userInitiated, null);
    }

    public async Task NarrateSelectedPoiAsync()
    {
        if (_selectedPoi is null)
        {
            StatusText = "Chưa có quán được chọn";
            return;
        }

        await NarratePoiAsync(_selectedPoi, false, GetDistanceForPoi(_selectedPoi.Id));
    }

    public async Task StopNarrationAsync()
    {
        var hadActiveNarration = IsNarrating;

        Interlocked.Increment(ref _narrationSessionId);
        await _narrationService.StopAsync();

        IsNarrating = false;
        StatusText = hadActiveNarration
            ? "Đã dừng thuyết minh"
            : IsTracking
                ? "GPS đang hoạt động"
                : "Sẵn sàng phát thuyết minh";

        if (hadActiveNarration)
        {
            AddLog($"{NowLabel()} Dừng thuyết minh");
        }
    }

    public async Task HandleMapTapAsync(double latitude, double longitude)
    {
        if (_pois.Count == 0)
        {
            return;
        }

        var tapLocation = new LocationDto
        {
            Latitude = latitude,
            Longitude = longitude
        };

        var results = _geofenceEngine.Evaluate(tapLocation, _pois);
        var candidate = results
            .Where(item => item.IsInside)
            .OrderByDescending(item => item.Poi.Priority)
            .ThenBy(item => item.DistanceMeters)
            .FirstOrDefault();

        if (candidate.Poi is not null)
        {
            SetSelectedPoi(candidate.Poi, true, results);
            StatusText = $"Bạn vừa chạm vùng của {candidate.Poi.Name}";
            AddLog($"{NowLabel()} Chạm Mapsui trong bán kính {candidate.Poi.Name}");
            await NarratePoiAsync(candidate.Poi, false, candidate.DistanceMeters);
            return;
        }

        var nearest = results.FirstOrDefault();
        if (nearest.Poi is not null)
        {
            SetSelectedPoi(nearest.Poi, true, results);
            StatusText = $"Bạn vừa chạm ngoài vùng quán. Gần nhất là {nearest.Poi.Name}";
            AddLog($"{NowLabel()} Chạm bản đồ gần {nearest.Poi.Name}");
        }
    }

    private async void OnLocationUpdated(object? sender, LocationDto location)
    {
        if (!await _locationUpdateGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            await HandleLocationUpdatedAsync(location);
        }
        finally
        {
            _locationUpdateGate.Release();
        }
    }

    private async Task HandleLocationUpdatedAsync(LocationDto location)
    {
        if (_pois.Count == 0)
        {
            return;
        }

        var results = _geofenceEngine.Evaluate(location, _pois);
        var nearest = results.FirstOrDefault();
        var insideNow = results
            .Where(item => item.IsInside)
            .Select(item => item.Poi.Id)
            .ToHashSet();

        _lastLocation = location;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            LocationText =
                $"Lat {location.Latitude:F6} | Lng {location.Longitude:F6} | Sai số {location.AccuracyMeters?.ToString("F0") ?? "?"}m";

            NearestPoiText = nearest.Poi is null
                ? "Chưa xác định quán gần nhất"
                : $"Quán gần nhất: {nearest.Poi.Name} ({nearest.DistanceMeters:F0}m)";

            RefreshPoiList(results, nearest.Poi?.Id);

            if (!_hasUserSelectedPoi)
            {
                SetSelectedPoi(nearest.Poi ?? _selectedPoi ?? _pois.FirstOrDefault(), false, results);
            }
            else
            {
                UpdateSelectedPoiDetails(results);
            }
        });

        var candidate = results
            .Where(item => item.IsInside)
            .OrderByDescending(item => item.Poi.Priority)
            .ThenBy(item => item.DistanceMeters)
            .FirstOrDefault();

        if (candidate.Poi is not null && ShouldAutoNarrate(candidate.Poi))
        {
            await NarratePoiAsync(candidate.Poi, true, candidate.DistanceMeters);
        }

        _insidePoiIds.Clear();
        foreach (var poiId in insideNow)
        {
            _insidePoiIds.Add(poiId);
        }

        if (_insidePoiIds.Count == 0)
        {
            _lastAutoNarratedPoiId = null;
        }

        RaiseMapStateChanged();
    }

    private bool ShouldAutoNarrate(POI poi)
    {
        var isNewEntry = !_insidePoiIds.Contains(poi.Id);
        var candidateChanged = _lastAutoNarratedPoiId != poi.Id;

        if (!isNewEntry && !candidateChanged)
        {
            return false;
        }

        return CanNarrate(poi);
    }

    private async Task NarratePoiAsync(POI poi, bool autoTriggered, double? distanceMeters)
    {
        var narrationSessionId = Interlocked.Increment(ref _narrationSessionId);
        _lastNarratedAt[poi.Id] = DateTimeOffset.UtcNow;
        if (autoTriggered)
        {
            _lastAutoNarratedPoiId = poi.Id;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            IsNarrating = true;
            StatusText = autoTriggered
                ? $"Tự động phát: {poi.Name}"
                : $"Đang phát: {poi.Name}";

            if (!_hasUserSelectedPoi)
            {
                SetSelectedPoi(poi, false, null);
            }
        });

        AddLog(
            $"{NowLabel()} {(autoTriggered ? "Auto" : "Manual")} trigger {poi.Name}" +
            (distanceMeters.HasValue ? $" ({distanceMeters.Value:F0}m)" : string.Empty));

        try
        {
            await _narrationService.NarrateAsync(poi, SelectedLanguage);
        }
        catch (Exception ex)
        {
            AddLog($"{NowLabel()} Lỗi âm thanh: {ex.Message}");

            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                StatusText = $"Lỗi phát âm thanh: {ex.Message}";
            }
        }
        finally
        {
            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsNarrating = false;

                    if (!StatusText.StartsWith("Lỗi phát âm thanh:", StringComparison.Ordinal))
                    {
                        StatusText = IsTracking ? "GPS đang hoạt động" : "Sẵn sàng phát thuyết minh";
                    }
                });
            }
        }
    }

    private void RefreshPoiList(
        IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> evaluated,
        Guid? nearestPoiId)
    {
        var lookup = evaluated.ToDictionary(item => item.Poi.Id);

        PoiStatuses.Clear();

        foreach (var poi in _pois)
        {
            lookup.TryGetValue(poi.Id, out var evaluatedItem);
            var hasDistance = evaluatedItem.Poi is not null;

            PoiStatuses.Add(new PoiStatusItem
            {
                PoiId = poi.Id,
                Code = poi.Code,
                Name = poi.Name,
                Address = poi.Address,
                ImageSource = poi.ImageSource,
                Description = poi.Description,
                SpecialDish = poi.SpecialDish,
                MapLink = poi.MapLink,
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                DistanceMeters = hasDistance ? evaluatedItem.DistanceMeters : double.NaN,
                TriggerRadiusMeters = poi.TriggerRadiusMeters,
                IsInsideRadius = hasDistance && evaluatedItem.IsInside,
                IsNearest = nearestPoiId == poi.Id,
                Priority = poi.Priority
            });
        }
    }

    private void SetSelectedPoi(
        POI? poi,
        bool userInitiated,
        IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)>? evaluated)
    {
        if (poi is null)
        {
            return;
        }

        _selectedPoi = poi;
        _hasUserSelectedPoi = userInitiated || _hasUserSelectedPoi;
        UpdateSelectedPoiDetails(evaluated);
        RaiseMapStateChanged();
    }

    private void UpdateSelectedPoiDetails(
        IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)>? evaluated = null)
    {
        if (_selectedPoi is null)
        {
            return;
        }

        double? distanceMeters = null;

        if (evaluated is not null)
        {
            var matchedEvaluation = evaluated.FirstOrDefault(item => item.Poi.Id == _selectedPoi.Id);
            if (matchedEvaluation.Poi is not null)
            {
                distanceMeters = matchedEvaluation.DistanceMeters;
            }
        }

        if (!distanceMeters.HasValue)
        {
            var status = PoiStatuses.FirstOrDefault(item => item.PoiId == _selectedPoi.Id);
            distanceMeters = status is null || double.IsNaN(status.DistanceMeters)
                ? null
                : status.DistanceMeters;
        }

        var distanceLabel = distanceMeters.HasValue
            ? $"Khoảng cách hiện tại: {distanceMeters.Value:F0}m"
            : "Khoảng cách hiện tại: N/A";

        SelectedPoiName = _selectedPoi.Name;
        SelectedPoiAddress = _selectedPoi.Address;
        SelectedPoiDescription = _selectedPoi.Description;
        SelectedPoiDishText = _selectedPoi.SpecialDish;
        SelectedPoiStatusText =
            $"{distanceLabel} | Bán kính phát: {_selectedPoi.TriggerRadiusMeters:F0}m | Ưu tiên: P{_selectedPoi.Priority}";
        SelectedPoiNarrationPreview = _selectedPoi.GetNarrationText(SelectedLanguage);
        SelectedPoiMapLink = _selectedPoi.MapLink;
        SelectedPoiImageSource = _selectedPoi.ImageSource;
    }

    private double? GetDistanceForPoi(Guid poiId)
    {
        var status = PoiStatuses.FirstOrDefault(item => item.PoiId == poiId);
        return status is null || double.IsNaN(status.DistanceMeters)
            ? null
            : status.DistanceMeters;
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            EventLogs.Insert(0, message);

            while (EventLogs.Count > 15)
            {
                EventLogs.RemoveAt(EventLogs.Count - 1);
            }
        });
    }

    private void RaiseMapStateChanged()
    {
        MainThread.BeginInvokeOnMainThread(() => MapStateChanged?.Invoke(this, EventArgs.Empty));
    }

    private static string NowLabel() => DateTime.Now.ToString("HH:mm:ss");

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
