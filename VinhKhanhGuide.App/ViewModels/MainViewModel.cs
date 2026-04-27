using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.ViewModels;

public partial class MainViewModel : INotifyPropertyChanged
{
    private const string DefaultNarrationMode = "tts";
    private const int MaxRecentSearches = 6;
    private const int MaxSearchSuggestions = 6;
    private const int MaxPinnedRecentSuggestions = 3;
    private static readonly TimeSpan PoiRefreshInterval = TimeSpan.FromSeconds(8);
    private static readonly AutoNarrationDecisionOptions AutoNarrationOptions = new()
    {
        DebounceInterval = TimeSpan.FromSeconds(2),
        SamePrioritySwitchThresholdMeters = 8d
    };

    public const double EntranceLatitude = 10.7614500;
    public const double EntranceLongitude = 106.7028200;
    public const string EntranceName = "Cổng phố ẩm thực Vĩnh Khánh";
    public const string EntranceAddress = "40 Vĩnh Khánh, P. Khánh Hội, Q.4";

    private readonly ILocationService _locationService;
    private readonly IPoiRepository _poiRepository;
    private readonly ITourRepository _tourRepository;
    private readonly IPoiOfflineStore _poiOfflineStore;
    private readonly ISearchService _searchService;
    private readonly INarrationService _narrationService;
    private readonly IAuthService _authService;
    private readonly IUserProfileSyncService _userProfileSyncService;
    private readonly IAudioSettingsService _audioSettingsService;
    private readonly IAudioAssetCacheService _audioAssetCacheService;
    private readonly IAccountProfileValidationService _accountProfileValidationService;
    private readonly IUsageHistoryService _usageHistoryService;
    private readonly IListeningHistorySyncService _listeningHistorySyncService;
    private readonly IMapOfflineTileService _mapOfflineTileService;
    private readonly IAutoPoiSelectionService _autoPoiSelectionService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly SemaphoreSlim _locationUpdateGate = new(1, 1);
    private readonly SemaphoreSlim _listeningHistoryRefreshGate = new(1, 1);
    private readonly object _optimisticListeningHistoryGate = new();
    private readonly List<ListeningHistoryDisplayItem> _optimisticListeningHistoryItems = new();
    private readonly Dictionary<Guid, DateTimeOffset> _lastNarratedAt = new();
    private readonly HashSet<Guid> _insidePoiIds = [];
    private readonly List<string> _recentSearches = [];
    private readonly IReadOnlyList<FeaturedDishItem> _featuredDishCatalog;

    private IReadOnlyList<POI> _pois = Array.Empty<POI>();
    private IReadOnlyList<TourDto> _tours = Array.Empty<TourDto>();
    private Task? _poiRefreshLoopTask;
    private bool _isInitialized;
    private bool _isTracking;
    private bool _isNarrating;
    private bool _isSearchFocused;
    private bool _isSearchSuggestionsVisible;
    private bool _isSearchResultEmpty;
    private bool _isAutoNarrationEnabled = true;
    private bool _isRestoringUserPreferences;
    private int _narrationSessionId;
    private Guid? _lastAutoNarratedPoiId;
    private DateTimeOffset? _lastAutoNarrationEvaluationAtUtc;
    private LocationDto? _lastLocation;
    private LocationDto? _gpsOriginLocation;
    private Guid? _activeNarrationPoiId;
    private bool _hasLocationPermission;
    private bool _hasCheckedLocationPermission;
    private POI? _selectedPoi;
    private bool _hasUserSelectedPoi;
    private string _poiDataSnapshot = string.Empty;
    private string _tourDataSnapshot = string.Empty;
    private int? _activeTourId;
    private int _activeTourStopIndex;
    private string _statusText = "Sẵn sàng khám phá";
    private string _locationText = "Bạn đang xem cổng phố ẩm thực Vĩnh Khánh";
    private string _nearestPoiText = "Chọn tour, chọn quán hoặc mở bản đồ để nghe thuyết minh";
    private string _mapModeBadgeText = "Đang mở bản đồ";
    private string _mapPoiBadgeText = "0 quán";
    private string _selectedPoiName = "Ốc Oanh";
    private string _selectedPoiAddress = string.Empty;
    private string _selectedPoiDescription = string.Empty;
    private string _selectedPoiDishText = string.Empty;
    private string _selectedPoiStatusText = string.Empty;
    private string _selectedPoiPriceRangeText = string.Empty;
    private string _selectedPoiOpeningHoursText = string.Empty;
    private string _selectedPoiFirstDishText = string.Empty;
    private string _selectedPoiTravelEstimateText = string.Empty;
    private string _selectedPoiNarrationPreview = string.Empty;
    private string _selectedPoiMapLink = string.Empty;
    private ImageSource? _selectedPoiImageSource;
    private string _activeMapCategoryKey = string.Empty;
    private string _selectedFeaturedDishCategoryName = "Món nổi bật";
    private string _selectedFeaturedDishCategorySummary =
        "Chạm vào Bò, Lẩu, Ốc hoặc Cua để xem danh sách món nổi bật theo từng nhóm.";
    private string _selectedLanguage = "vi";
    private string _selectedPlaybackMode = DefaultNarrationMode;
    private string _draftSelectedLanguage = "vi";
    private string _draftSelectedPlaybackMode = DefaultNarrationMode;
    private bool _draftIsAutoNarrationEnabled = true;
    private string _searchQuery = string.Empty;
    private string _currentUserDisplayName = "Bạn";
    private string _currentUserStatusLine = "Đang hoạt động";
    private string _currentUserInitials = "VK";
    private string _currentUserAccountLabel = "Chưa cập nhật";
    private string _currentUserPasswordLabel = "Đang hoạt động";
    private string _accountProfileFullName = string.Empty;
    private string _accountProfileEmail = string.Empty;
    private string _accountProfilePhoneNumber = string.Empty;
    private bool _isSavingAccountProfile;
    private string _accountSettingsErrorMessage = string.Empty;
    private string _accountSettingsSuccessMessage = string.Empty;
    private bool _isSavingAudioSettings;
    private bool _isPreviewingAudioSettings;
    private string _audioSettingsErrorMessage = string.Empty;
    private string _audioSettingsSuccessMessage = string.Empty;
    private bool _isSyncingOfflinePackage;
    private string _offlinePackageErrorMessage = string.Empty;
    private string _offlinePackageSuccessMessage = string.Empty;
    private string _searchResultStatusText = string.Empty;
    private OfflineContentStatus _offlineContentStatus = new();
    private OfflineMapStatus _offlineMapStatus = new();
    private AudioCacheStatus _audioCacheStatus = new();

    private bool _isListeningHistoryLoading;
    private bool _isClearingListeningHistory;
    private string _listeningHistoryLoadError = string.Empty;
    private DateTimeOffset? _lastListeningHistorySyncAt;
    private string _selectedListeningHistoryPeriod = "Tất cả";
    private string _selectedListeningHistorySort = "Mới nhất trước";
    private string _selectedListeningHistoryView = "Dòng thời gian";

    public MainViewModel(
        ILocationService locationService,
        IPoiRepository poiRepository,
        ITourRepository tourRepository,
        IPoiOfflineStore poiOfflineStore,
        ISearchService searchService,
        INarrationService narrationService,
        IAuthService authService,
        IUserProfileSyncService userProfileSyncService,
        IAudioSettingsService audioSettingsService,
        IAudioAssetCacheService audioAssetCacheService,
        IAccountProfileValidationService accountProfileValidationService,
        IUsageHistoryService usageHistoryService,
        IListeningHistorySyncService listeningHistorySyncService,
        IMapOfflineTileService mapOfflineTileService,
        IAutoPoiSelectionService autoPoiSelectionService,
        GeofenceEngine geofenceEngine)
    {
        _locationService = locationService;
        _poiRepository = poiRepository;
        _tourRepository = tourRepository;
        _poiOfflineStore = poiOfflineStore;
        _searchService = searchService;
        _narrationService = narrationService;
        _authService = authService;
        _userProfileSyncService = userProfileSyncService;
        _audioSettingsService = audioSettingsService;
        _audioAssetCacheService = audioAssetCacheService;
        _accountProfileValidationService = accountProfileValidationService;
        _usageHistoryService = usageHistoryService;
        _listeningHistorySyncService = listeningHistorySyncService;
        _mapOfflineTileService = mapOfflineTileService;
        _autoPoiSelectionService = autoPoiSelectionService;
        _geofenceEngine = geofenceEngine;
        _featuredDishCatalog = CreateFeaturedDishCatalog();

        _locationService.LocationUpdated += OnLocationUpdated;
        _authService.SessionChanged += OnAuthSessionChanged;

        RebuildFeaturedDishCategories();
        ShowFeaturedDishCategory("bo");

        StartTrackingCommand = new Command(async () => await StartAsync(), () => !IsTracking);
        StopTrackingCommand = new Command(async () => await StopAsync(), () => IsTracking);
        StopNarrationCommand = new Command(async () => await StopNarrationAsync(), () => IsNarrating);
        SignOutCommand = new Command(async () => await SignOutAsync());
        ClearEventLogsCommand = new Command(ClearEventLogs, () => HasEventLogs);
        ClearListeningHistoryCommand = new Command(async () => await ClearListeningHistoryAsync(), () => CanClearListeningHistory);
        ClearViewHistoryCommand = new Command(ClearViewHistory, () => HasViewHistory);
        RefreshListeningHistoryCommand = new Command(async () => await RefreshListeningHistoryAsync());
        SaveAccountProfileCommand = new Command(async () => await SaveAccountProfileAsync(), () => CanSaveAccountProfile);
        ResetAccountProfileCommand = new Command(ResetAccountProfileEditor, () => CanManageAccountProfile && !IsSavingAccountProfile);
        PreviewAudioSettingsCommand = new Command(async () => await PreviewAudioSettingsAsync(), () => CanPreviewAudioSettings);
        SaveAudioSettingsCommand = new Command(async () => await SaveAudioSettingsAsync(), () => CanSaveAudioSettings);
        ResetAudioSettingsCommand = new Command(ResetAudioSettingsDraft, () => CanResetAudioSettings);
        DownloadOfflinePackageCommand = new Command(async () => await DownloadOfflinePackageAsync(), () => CanDownloadOfflinePackage);
        ClearOfflinePackageCommand = new Command(async () => await ClearOfflinePackageAsync(), () => CanClearOfflinePackage);

        SyncCurrentUser();
        LoadPersistedState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? MapStateChanged;

    public ObservableCollection<FoodCategoryItem> FeaturedDishes { get; } = new();
    public ObservableCollection<FeaturedDishItem> SelectedFeaturedDishItems { get; } = new();
    public ObservableCollection<PoiStatusItem> PoiStatuses { get; } = new();
    public ObservableCollection<PoiStatusItem> FilteredPoiStatuses { get; } = new();
    public ObservableCollection<SearchSuggestionItem> SearchSuggestions { get; } = new();
    public ObservableCollection<string> EventLogs { get; } = new();
    public ObservableCollection<TourPackageItem> TourPackages { get; } = new();
    public ObservableCollection<TourStopProgressItem> ActiveTourStops { get; } = new();
    public ObservableCollection<ListeningHistoryDisplayItem> ListeningHistory { get; } = new();
    public ObservableCollection<string> ListeningHistoryLocalEntries { get; } = new();
    public ObservableCollection<ListeningHistoryRankingDisplayItem> ListeningHistoryRanking { get; } = new();
    public ObservableCollection<string> ViewHistory { get; } = new();
    public IReadOnlyList<AudioSettingsOption> SupportedLanguages { get; } =
    [
        new() { Code = "vi", Label = "Tiếng Việt", FlagEmoji = "🇻🇳", Description = "Giọng tiếng Việt chuẩn cho khách nội địa." },
        new() { Code = "en", Label = "English", FlagEmoji = "🇺🇸", Description = "English interface and narration for international visitors." },
        new() { Code = "zh", Label = "中文", FlagEmoji = "🇨🇳", Description = "中文界面和中文讲解。" },
        new() { Code = "ko", Label = "한국어", FlagEmoji = "🇰🇷", Description = "한국어 화면과 음성 안내." },
        new() { Code = "fr", Label = "Français", FlagEmoji = "🇫🇷", Description = "Interface et narration en français." }
    ];
    public IReadOnlyList<AudioSettingsOption> SupportedPlaybackModes { get; } =
    [
        new() { Code = "tts", Label = "Giọng đọc tự động", Description = "Ứng dụng đọc thuyết minh theo ngôn ngữ đã chọn." },
        new() { Code = "audio", Label = "Bản thu sẵn", Description = "Ưu tiên phát bản thu khi quán đã có nội dung." }
    ];
    public IReadOnlyList<string> ListeningHistoryPeriodOptions { get; } = ["Tất cả", "24 giờ qua", "7 ngày qua", "30 ngày qua"];
    public IReadOnlyList<string> ListeningHistorySortOptions { get; } = ["Mới nhất trước", "Cũ nhất trước"];
    public IReadOnlyList<string> ListeningHistoryViewOptions { get; } = ["Dòng thời gian", "Xếp hạng quán"];

    public ICommand StartTrackingCommand { get; }
    public ICommand StopTrackingCommand { get; }
    public ICommand StopNarrationCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand ClearEventLogsCommand { get; }
    public ICommand ClearListeningHistoryCommand { get; }
    public ICommand ClearViewHistoryCommand { get; }
    public ICommand RefreshListeningHistoryCommand { get; }
    public ICommand SaveAccountProfileCommand { get; }
    public ICommand ResetAccountProfileCommand { get; }
    public ICommand PreviewAudioSettingsCommand { get; }
    public ICommand SaveAudioSettingsCommand { get; }
    public ICommand ResetAudioSettingsCommand { get; }
    public ICommand DownloadOfflinePackageCommand { get; }
    public ICommand ClearOfflinePackageCommand { get; }

    public IReadOnlyList<POI> Pois => _pois;
    public IReadOnlyList<TourDto> Tours => _tours;
    public LocationDto? LastLocation => _lastLocation;
    public LocationDto? GpsOriginLocation => EntranceLocation;
    public Guid? SelectedPoiId => _selectedPoi?.Id;
    public bool IsSelectedPoiNarrating => _selectedPoi is not null && IsNarrating && _activeNarrationPoiId == _selectedPoi.Id;
    public string SelectedPoiNarrationActionText =>
        GetLocalizedPoiNarrationActionText(IsSelectedPoiNarrating);
    public string SelectedPoiNarrationStateText =>
        GetLocalizedPoiNarrationStateText(IsSelectedPoiNarrating);
    public bool HasTours => TourPackages.Count > 0;
    public bool HasActiveTour => GetActiveTour() is not null;
    public bool HasActiveTourStops => ActiveTourStops.Count > 0;
    public bool IsActiveTourCompleted => HasActiveTour && GetCurrentActiveTourPoi() is null;
    public IReadOnlyList<PoiStatusItem> VisibleMapPoiStatuses => GetVisibleMapPoiStatuses();
    public IReadOnlyList<PoiStatusItem> PreviewMapPoiStatuses => GetPreviewMapPoiStatuses();
    public IReadOnlyList<LocationDto> ActiveTourRoutePoints => GetActiveTourStopIds()
        .Select(poiId => _pois.FirstOrDefault(item => item.Id == poiId))
        .Where(poi => poi is not null)
        .Cast<POI>()
        .Select(poi => new LocationDto
        {
            Latitude = poi.Latitude,
            Longitude = poi.Longitude
        })
        .ToList();
    public string TourSectionSummary => !HasTours
        ? LocalizeUi("Chưa có tour khả dụng.", "No tours available.", "暂无可用路线。", "이용 가능한 투어가 없습니다.", "Aucun parcours disponible.")
        : HasActiveTour
            ? LocalizeUi(
                $"Đang dẫn {GetActiveTour()!.Name}. Xem điểm hiện tại bên dưới.",
                $"Guiding {GetActiveTour()!.Name}. Check the current stop below.",
                $"正在跟随 {GetActiveTour()!.Name} 路线。",
                $"{GetActiveTour()!.Name} 경로를 추적 중입니다.",
                $"Suivi de l'itinéraire {GetActiveTour()!.Name}.")
            : LocalizeUi(
                $"{TourPackages.Count} tour ngắn sẵn sàng cho khách mới.",
                $"{TourPackages.Count} short tours ready for first-time visitors.",
                $"{TourPackages.Count} 条路线已就绪。",
                $"{TourPackages.Count}개 투어 준비 완료.",
                $"{TourPackages.Count} parcours prêts.");
    public string ActiveTourName => GetActiveTour()?.Name ?? LocalizeUi(
        "Chưa chọn tour",
        "No tour selected",
        "尚未选择路线",
        "선택된 투어 없음",
        "Aucun parcours sélectionné");
    public string ActiveTourSummary
    {
        get
        {
            var activeTour = GetActiveTour();
            if (activeTour is null)
            {
                return LocalizeUi(
                    "Chọn một tour để bắt đầu hành trình.",
                    "Pick a tour to begin.",
                    "请选择一条路线开始。",
                    "투어를 선택해 시작하세요.",
                    "Choisissez un parcours pour commencer.");
            }

            var stopIds = GetActiveTourStopIds();
            if (stopIds.Count == 0)
            {
                return LocalizeUi(
                    "Tour hiện tại không còn điểm dừng hợp lệ.",
                    "This tour no longer has valid stops.",
                    "请选择一条路线开始。",
                    "현재 투어에 유효한 경유지가 없습니다.",
                    "Ce parcours n'a plus d'étapes valides.");
            }

            if (GetCurrentActiveTourPoi() is null)
            {
                return LocalizeUi(
                    $"Hoàn tất {stopIds.Count}/{stopIds.Count} điểm dừng.",
                    $"Completed {stopIds.Count}/{stopIds.Count} stops.",
                    $"已完成 {stopIds.Count}/{stopIds.Count} 个站点。",
                    $"{stopIds.Count}/{stopIds.Count}개 경유지 완료.",
                    $"{stopIds.Count}/{stopIds.Count} étapes terminées.");
            }

            return LocalizeUi(
                $"Đang chạy {Math.Min(_activeTourStopIndex + 1, stopIds.Count)}/{stopIds.Count} điểm dừng.",
                $"Running stop {Math.Min(_activeTourStopIndex + 1, stopIds.Count)}/{stopIds.Count}.",
                $"当前进行到第 {Math.Min(_activeTourStopIndex + 1, stopIds.Count)}/{stopIds.Count} 站。",
                $"{Math.Min(_activeTourStopIndex + 1, stopIds.Count)}/{stopIds.Count}번째 경유지 진행 중.",
                $"Étape {Math.Min(_activeTourStopIndex + 1, stopIds.Count)}/{stopIds.Count} en cours.");
        }
    }
    public string ActiveTourCurrentStopText
    {
        get
        {
            var currentPoi = GetCurrentActiveTourPoi();
            if (currentPoi is null)
            {
                return HasActiveTour
                    ? LocalizeUi("Điểm hiện tại: đã hoàn tất.", "Current stop: completed.", "当前站点：已完成。", "현재 경유지: 완료됨.", "Étape actuelle : terminée.")
                    : LocalizeUi("Điểm hiện tại: chưa chọn.", "Current stop: none selected.", "当前站点：未选择。", "현재 경유지: 선택 안 됨.", "Étape actuelle : non définie.");
            }

            return LocalizeUi(
                $"Điểm hiện tại: {currentPoi.Name}",
                $"Current stop: {currentPoi.Name}",
                $"当前站点：{currentPoi.Name}",
                $"현재 경유지: {currentPoi.Name}",
                $"Étape actuelle : {currentPoi.Name}");
        }
    }
    public string ActiveTourNextStopText
    {
        get
        {
            var nextPoi = GetNextActiveTourPoi();
            if (nextPoi is null)
            {
                return HasActiveTour
                    ? LocalizeUi("Điểm kế tiếp: không còn.", "Next stop: none left.", "下一站：没有了。", "다음 경유지: 없음.", "Étape suivante : aucune.")
                    : LocalizeUi("Điểm kế tiếp: chọn tour.", "Next stop: choose a tour.", "下一站：请先选择路线。", "다음 경유지: 투어를 선택하세요.", "Étape suivante : choisissez un parcours.");
            }

            return LocalizeUi(
                $"Điểm kế tiếp: {nextPoi.Name}",
                $"Next stop: {nextPoi.Name}",
                $"下一站：{nextPoi.Name}",
                $"다음 경유지: {nextPoi.Name}",
                $"Étape suivante : {nextPoi.Name}");
        }
    }
    public string HomeNarrationSummary
    {
        get
        {
            var activeTour = GetActiveTour();
            if (activeTour is not null)
            {
                var currentPoi = GetCurrentActiveTourPoi();
            if (currentPoi is null)
            {
                return $"Tour {activeTour.Name} đã hoàn tất. Bạn có thể chọn tour khác hoặc tự do khám phá các quán gần đây.";
            }

            var totalStops = GetActiveTourStopIds().Count;
            return $"Tour {activeTour.Name} đang ở điểm {Math.Min(_activeTourStopIndex + 1, totalStops)}/{totalStops}: {currentPoi.Name}. Khi bạn đến gần, app sẽ phát thuyết minh và chuyển sang điểm tiếp theo.";
        }

            var activePoi = _activeNarrationPoiId.HasValue
                ? _pois.FirstOrDefault(item => item.Id == _activeNarrationPoiId.Value)
                : null;

            if (IsNarrating && activePoi is not null)
            {
                return $"Đang phát thuyết minh cho {activePoi.Name}. Bấm lại nút nghe để dừng.";
            }

            if (_pois.Count == 0)
            {
                return "Khi có dữ liệu quán, bạn sẽ thấy danh sách địa điểm và có thể nghe thuyết minh ngay tại trang chủ.";
            }

            return $"{CurrentUserDisplayName} có thể bấm \"Nghe thuyết minh\" trên từng quán để tìm hiểu trước khi ghé.";
        }
    }

    public string HomeNarrationAvailabilityText => _pois.Count == 0
        ? LocalizeUi(
            "Chưa có danh sách quán để hiển thị.",
            "No places are available yet.",
            "正在等待管理后台的地点列表。",
            "관리 시스템의 매장 목록을 기다리는 중입니다.",
            "En attente de la liste des lieux depuis l'admin.")
        : LocalizeUi(
            $"{_pois.Count} quán sẵn sàng nghe thuyết minh • Ngôn ngữ: {SelectedLanguageDisplayName}",
            $"{_pois.Count} places ready to listen • Language: {SelectedLanguageDisplayName}",
            $"{_pois.Count} 个地点可播放讲解 • 当前语言：{SelectedLanguageDisplayName}",
            $"{_pois.Count}개 매장에서 안내 재생 가능 • 현재 언어: {SelectedLanguageDisplayName}",
            $"{_pois.Count} lieux prêts pour la narration • Langue actuelle : {SelectedLanguageDisplayName}");

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
            OnPropertyChanged(nameof(CanStartTracking));
            (StartTrackingCommand as Command)?.ChangeCanExecute();
            (StopTrackingCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool CanStartTracking => !IsTracking;

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
            OnPropertyChanged(nameof(IsSelectedPoiNarrating));
            OnPropertyChanged(nameof(SelectedPoiNarrationActionText));
            OnPropertyChanged(nameof(SelectedPoiNarrationStateText));
            OnPropertyChanged(nameof(HomeNarrationSummary));
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
        private set
        {
            if (!SetProperty(ref _selectedPoiDishText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedPoiDish));
        }
    }

    public string SelectedPoiStatusText
    {
        get => _selectedPoiStatusText;
        private set => SetProperty(ref _selectedPoiStatusText, value);
    }

    public string SelectedPoiPriceRangeText
    {
        get => _selectedPoiPriceRangeText;
        private set
        {
            if (!SetProperty(ref _selectedPoiPriceRangeText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedPoiPriceRange));
        }
    }

    public string SelectedPoiOpeningHoursText
    {
        get => _selectedPoiOpeningHoursText;
        private set
        {
            if (!SetProperty(ref _selectedPoiOpeningHoursText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedPoiOpeningHours));
        }
    }

    public string SelectedPoiFirstDishText
    {
        get => _selectedPoiFirstDishText;
        private set
        {
            if (!SetProperty(ref _selectedPoiFirstDishText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedPoiFirstDish));
        }
    }

    public string SelectedPoiTravelEstimateText
    {
        get => _selectedPoiTravelEstimateText;
        private set
        {
            if (!SetProperty(ref _selectedPoiTravelEstimateText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedPoiTravelEstimate));
        }
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

    public ImageSource? SelectedPoiImageSource
    {
        get => _selectedPoiImageSource;
        private set => SetProperty(ref _selectedPoiImageSource, value);
    }

    public bool HasSelectedPoiDish => !string.IsNullOrWhiteSpace(SelectedPoiDishText);
    public bool HasSelectedPoiPriceRange => !string.IsNullOrWhiteSpace(SelectedPoiPriceRangeText);
    public bool HasSelectedPoiOpeningHours => !string.IsNullOrWhiteSpace(SelectedPoiOpeningHoursText);
    public bool HasSelectedPoiFirstDish => !string.IsNullOrWhiteSpace(SelectedPoiFirstDishText);
    public bool HasSelectedPoiTravelEstimate => !string.IsNullOrWhiteSpace(SelectedPoiTravelEstimateText);
    public bool HasSelectedPoiPracticalInfo =>
        HasSelectedPoiPriceRange || HasSelectedPoiOpeningHours || HasSelectedPoiFirstDish || HasSelectedPoiTravelEstimate;

    public bool HasMapCategoryFilter => !string.IsNullOrWhiteSpace(_activeMapCategoryKey);

    public string ActiveMapCategoryFilterText => !HasMapCategoryFilter
        ? string.Empty
        : LocalizeUi(
            $"Đang lọc theo nhóm {GetFeaturedCategoryDisplayName(_activeMapCategoryKey)}",
            $"Filtered for {GetFeaturedCategoryDisplayName(_activeMapCategoryKey)}",
            $"按 {GetFeaturedCategoryDisplayName(_activeMapCategoryKey)} 分组筛选",
            $"{GetFeaturedCategoryDisplayName(_activeMapCategoryKey)} 그룹으로 필터 중",
            $"Filtré sur {GetFeaturedCategoryDisplayName(_activeMapCategoryKey)}");

    public IReadOnlyList<PoiStatusItem> SearchPreviewItems => FilteredPoiStatuses.Take(3).ToList();

    public bool HasSearchPreviewResults => HasSearchQuery && SearchPreviewItems.Count > 0;

    public string SearchPreviewSummaryText => LocalizeUi(
        $"{FilteredPoiStatuses.Count} quán khớp với \"{SearchQuery.Trim()}\"",
        $"{FilteredPoiStatuses.Count} places match \"{SearchQuery.Trim()}\"",
        $"有 {FilteredPoiStatuses.Count} 家店铺匹配 “{SearchQuery.Trim()}”",
        $"\"{SearchQuery.Trim()}\"에 맞는 매장 {FilteredPoiStatuses.Count}곳",
        $"{FilteredPoiStatuses.Count} lieux correspondent à \"{SearchQuery.Trim()}\"");

    public string SelectedFeaturedDishCategoryName
    {
        get => _selectedFeaturedDishCategoryName;
        private set => SetProperty(ref _selectedFeaturedDishCategoryName, value);
    }

    public string SelectedFeaturedDishCategoryKey => _selectedFeaturedDishCategoryKey;

    public string SelectedFeaturedDishCategorySummary
    {
        get => _selectedFeaturedDishCategorySummary;
        private set => SetProperty(ref _selectedFeaturedDishCategorySummary, value);
    }

    public string SelectedFeaturedDishResultsText =>
        SelectedFeaturedDishItems.Count == 0
            ? LocalizeUi(
                "Chưa có món nổi bật trong nhóm này.",
                "No featured dishes in this group yet.",
                "该分组暂时没有招牌菜。",
                "이 그룹에는 아직 대표 메뉴가 없습니다.",
                "Aucun plat phare dans cette catégorie pour le moment.")
            : LocalizeUi(
                $"{SelectedFeaturedDishItems.Count} món đã được sắp xếp theo nhóm {SelectedFeaturedDishCategoryName}.",
                $"{SelectedFeaturedDishItems.Count} dishes arranged under {SelectedFeaturedDishCategoryName}.",
                $"{SelectedFeaturedDishItems.Count} 道菜已归入 {SelectedFeaturedDishCategoryName} 分组。",
                $"{SelectedFeaturedDishItems.Count}개 메뉴가 {SelectedFeaturedDishCategoryName} 그룹에 정리되었습니다.",
                $"{SelectedFeaturedDishItems.Count} plats sont classés dans {SelectedFeaturedDishCategoryName}.");

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        private set
        {
            var normalizedLanguage = NormalizeLanguageCode(value);
            if (!SetProperty(ref _selectedLanguage, normalizedLanguage))
            {
                return;
            }

            UpdateSelectedPoiDetails();
            RefreshNarrationPresentation();
            RefreshFeaturedDishLocalization();
            SyncCurrentUser();
            RaiseLocalizedUiChanged();
            if (!IsNarrating)
            {
                StatusText = BuildIdleStatusText();
            }

            OnPropertyChanged(nameof(SelectedLanguageDisplayName));
            OnPropertyChanged(nameof(SelectedLanguageDisplayLabel));
            OnPropertyChanged(nameof(AudioSettingsSummary));
            OnPropertyChanged(nameof(HomeNarrationAvailabilityText));
            OnPropertyChanged(nameof(CurrentAudioSettingsSummary));
            OnPropertyChanged(nameof(AudioSettingsQuickActionText));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            TriggerListeningHistoryRefresh();
            UpdateAudioSettingsCommandStates();
        }
    }

    public string SelectedPlaybackMode
    {
        get => _selectedPlaybackMode;
        private set
        {
            var normalizedPlaybackMode = NormalizePlaybackMode(value);
            if (!SetProperty(ref _selectedPlaybackMode, normalizedPlaybackMode))
            {
                return;
            }

            OnPropertyChanged(nameof(NarrationModeDisplay));
            OnPropertyChanged(nameof(NarrationModeDescription));
            OnPropertyChanged(nameof(AudioSettingsSummary));
            OnPropertyChanged(nameof(HomeNarrationAvailabilityText));
            OnPropertyChanged(nameof(CurrentAudioSettingsSummary));
            OnPropertyChanged(nameof(AudioSettingsQuickActionText));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            UpdateAudioSettingsCommandStates();
        }
    }

    public bool IsAutoNarrationEnabled
    {
        get => _isAutoNarrationEnabled;
        private set
        {
            if (!SetProperty(ref _isAutoNarrationEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(AudioSettingsSummary));
            OnPropertyChanged(nameof(CurrentAudioSettingsSummary));
            OnPropertyChanged(nameof(AudioSettingsQuickActionText));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            UpdateAudioSettingsCommandStates();
        }
    }

    public string DraftSelectedLanguage
    {
        get => _draftSelectedLanguage;
        set
        {
            var normalizedLanguage = NormalizeLanguageCode(value);
            if (!SetProperty(ref _draftSelectedLanguage, normalizedLanguage))
            {
                return;
            }

            OnPropertyChanged(nameof(DraftSelectedLanguageDisplayName));
            OnPropertyChanged(nameof(DraftSelectedLanguageDisplayLabel));
            OnPropertyChanged(nameof(DraftSelectedLanguageOption));
            OnPropertyChanged(nameof(DraftAudioSettingsSummary));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            ClearAudioSettingsFeedback();
            UpdateAudioSettingsCommandStates();
        }
    }

    public string DraftSelectedPlaybackMode
    {
        get => _draftSelectedPlaybackMode;
        set
        {
            var normalizedPlaybackMode = NormalizePlaybackMode(value);
            if (!SetProperty(ref _draftSelectedPlaybackMode, normalizedPlaybackMode))
            {
                return;
            }

            OnPropertyChanged(nameof(DraftPlaybackModeDisplay));
            OnPropertyChanged(nameof(DraftSelectedPlaybackModeOption));
            OnPropertyChanged(nameof(DraftPlaybackModeDescription));
            OnPropertyChanged(nameof(DraftAudioSettingsSummary));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            ClearAudioSettingsFeedback();
            UpdateAudioSettingsCommandStates();
        }
    }

    public bool DraftIsAutoNarrationEnabled
    {
        get => _draftIsAutoNarrationEnabled;
        set
        {
            if (!SetProperty(ref _draftIsAutoNarrationEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(DraftAudioSettingsSummary));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            ClearAudioSettingsFeedback();
            UpdateAudioSettingsCommandStates();
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (!SetProperty(ref _searchQuery, value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSearchQuery));
            UpdateFilteredPoiStatuses();
            RefreshSearchSuggestions();
        }
    }

    public bool HasSearchQuery => !string.IsNullOrWhiteSpace(SearchQuery);

    public bool IsSearchSuggestionsVisible
    {
        get => _isSearchSuggestionsVisible;
        private set => SetProperty(ref _isSearchSuggestionsVisible, value);
    }

    public bool IsSearchResultEmpty
    {
        get => _isSearchResultEmpty;
        private set => SetProperty(ref _isSearchResultEmpty, value);
    }

    public string SearchResultStatusText
    {
        get => _searchResultStatusText;
        private set => SetProperty(ref _searchResultStatusText, value);
    }

    public string CurrentUserDisplayName
    {
        get => _currentUserDisplayName;
        private set => SetProperty(ref _currentUserDisplayName, value);
    }

    public string CurrentUserStatusLine
    {
        get => _currentUserStatusLine;
        private set => SetProperty(ref _currentUserStatusLine, value);
    }

    public string CurrentUserInitials
    {
        get => _currentUserInitials;
        private set => SetProperty(ref _currentUserInitials, value);
    }

    public string CurrentUserAccountLabel
    {
        get => _currentUserAccountLabel;
        private set => SetProperty(ref _currentUserAccountLabel, value);
    }

    public string CurrentUserPasswordLabel
    {
        get => _currentUserPasswordLabel;
        private set => SetProperty(ref _currentUserPasswordLabel, value);
    }

    public string AccountProfileFullName
    {
        get => _accountProfileFullName;
        set
        {
            if (SetProperty(ref _accountProfileFullName, value ?? string.Empty))
            {
                ClearAccountSettingsFeedback();
            }
        }
    }

    public string AccountProfileEmail
    {
        get => _accountProfileEmail;
        set
        {
            if (SetProperty(ref _accountProfileEmail, value ?? string.Empty))
            {
                ClearAccountSettingsFeedback();
            }
        }
    }

    public string AccountProfilePhoneNumber
    {
        get => _accountProfilePhoneNumber;
        set
        {
            if (SetProperty(ref _accountProfilePhoneNumber, value ?? string.Empty))
            {
                ClearAccountSettingsFeedback();
            }
        }
    }

    public bool IsSavingAccountProfile
    {
        get => _isSavingAccountProfile;
        private set
        {
            if (!SetProperty(ref _isSavingAccountProfile, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanSaveAccountProfile));
            (SaveAccountProfileCommand as Command)?.ChangeCanExecute();
            (ResetAccountProfileCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool CanManageAccountProfile => _authService.CurrentSession is not null;

    public bool CanSaveAccountProfile => CanManageAccountProfile && !IsSavingAccountProfile;

    public string AccountSettingsErrorMessage
    {
        get => _accountSettingsErrorMessage;
        private set
        {
            if (!SetProperty(ref _accountSettingsErrorMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasAccountSettingsError));
        }
    }

    public bool HasAccountSettingsError => !string.IsNullOrWhiteSpace(AccountSettingsErrorMessage);

    public string AccountSettingsSuccessMessage
    {
        get => _accountSettingsSuccessMessage;
        private set
        {
            if (!SetProperty(ref _accountSettingsSuccessMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasAccountSettingsSuccess));
        }
    }

    public bool HasAccountSettingsSuccess => !string.IsNullOrWhiteSpace(AccountSettingsSuccessMessage);

    public string AccountSettingsAccessMessage => GetLocalizedAccountAccessMessage(CanManageAccountProfile);

    public bool IsSavingAudioSettings
    {
        get => _isSavingAudioSettings;
        private set
        {
            if (!SetProperty(ref _isSavingAudioSettings, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanSaveAudioSettings));
            OnPropertyChanged(nameof(CanPreviewAudioSettings));
            OnPropertyChanged(nameof(CanResetAudioSettings));
            UpdateAudioSettingsCommandStates();
        }
    }

    public bool IsPreviewingAudioSettings
    {
        get => _isPreviewingAudioSettings;
        private set
        {
            if (!SetProperty(ref _isPreviewingAudioSettings, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanSaveAudioSettings));
            OnPropertyChanged(nameof(CanPreviewAudioSettings));
            OnPropertyChanged(nameof(CanResetAudioSettings));
            UpdateAudioSettingsCommandStates();
        }
    }

    public string AudioSettingsErrorMessage
    {
        get => _audioSettingsErrorMessage;
        private set
        {
            if (!SetProperty(ref _audioSettingsErrorMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasAudioSettingsError));
        }
    }

    public bool HasAudioSettingsError => !string.IsNullOrWhiteSpace(AudioSettingsErrorMessage);

    public string AudioSettingsSuccessMessage
    {
        get => _audioSettingsSuccessMessage;
        private set
        {
            if (!SetProperty(ref _audioSettingsSuccessMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasAudioSettingsSuccess));
        }
    }

    public bool HasAudioSettingsSuccess => !string.IsNullOrWhiteSpace(AudioSettingsSuccessMessage);

    public bool IsSyncingOfflinePackage
    {
        get => _isSyncingOfflinePackage;
        private set
        {
            if (!SetProperty(ref _isSyncingOfflinePackage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanDownloadOfflinePackage));
            OnPropertyChanged(nameof(CanClearOfflinePackage));
            UpdateOfflinePackageCommandStates();
        }
    }

    public string OfflinePackageErrorMessage
    {
        get => _offlinePackageErrorMessage;
        private set
        {
            if (!SetProperty(ref _offlinePackageErrorMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasOfflinePackageError));
        }
    }

    public bool HasOfflinePackageError => !string.IsNullOrWhiteSpace(OfflinePackageErrorMessage);

    public string OfflinePackageSuccessMessage
    {
        get => _offlinePackageSuccessMessage;
        private set
        {
            if (!SetProperty(ref _offlinePackageSuccessMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasOfflinePackageSuccess));
        }
    }

    public bool HasOfflinePackageSuccess => !string.IsNullOrWhiteSpace(OfflinePackageSuccessMessage);

    public bool CanDownloadOfflinePackage => !IsSyncingOfflinePackage;

    public bool CanClearOfflinePackage =>
        !IsSyncingOfflinePackage &&
        (_offlineContentStatus.HasSnapshot || _offlineMapStatus.HasCachedTiles || _audioCacheStatus.HasCachedAssets);

    public string OfflineContentSummary =>
        _offlineContentStatus.HasSnapshot
            ? $"{_offlineContentStatus.PoiCount} quán đã lưu • {FormatTimestamp(_offlineContentStatus.LastSyncedAtUtc, "chưa có mốc cập nhật")}"
            : "Chưa có dữ liệu quán để dùng khi mất mạng.";

    public string OfflineMapSummary =>
        $"{_offlineMapStatus.CachedTileCount}/{_offlineMapStatus.PlannedTileCount} phần bản đồ Vĩnh Khánh • {FormatFileSize(_offlineMapStatus.CachedBytes)}";

    public string OfflineMapDetail =>
        _offlineMapStatus.IsReady
            ? "Bản đồ khu Vĩnh Khánh đã sẵn sàng để xem khi mạng yếu."
            : "Một số phần bản đồ chưa tải xong, nên có thể chưa hiện khi mất mạng.";

    public string OfflineAudioSummary =>
        !_audioCacheStatus.HasPublishedAssets
            ? "Hiện các quán chưa có bản thu sẵn."
            : $"{_audioCacheStatus.CachedAssetCount}/{_audioCacheStatus.AvailableAssetCount} bản thu • {FormatFileSize(_audioCacheStatus.CachedBytes)}";

    public string OfflineAudioDetail =>
        _audioCacheStatus.HasPublishedAssets
            ? $"Bản thu gần nhất: {FormatTimestamp(_audioCacheStatus.LastPreparedAtUtc, "chưa có mốc tải")}."
            : "Khi có bản thu, app sẽ có thể lưu trước để nghe ổn định hơn.";

    public bool HasPendingAudioSettingsChanges =>
        !string.Equals(DraftSelectedLanguage, SelectedLanguage, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(DraftSelectedPlaybackMode, SelectedPlaybackMode, StringComparison.OrdinalIgnoreCase) ||
        DraftIsAutoNarrationEnabled != IsAutoNarrationEnabled;

    public bool CanSaveAudioSettings => HasPendingAudioSettingsChanges && !IsSavingAudioSettings && !IsPreviewingAudioSettings;

    public bool CanPreviewAudioSettings => !IsSavingAudioSettings && !IsPreviewingAudioSettings;

    public bool CanResetAudioSettings => HasPendingAudioSettingsChanges && !IsSavingAudioSettings && !IsPreviewingAudioSettings;

    public string NarrationModeDisplay => GetPlaybackModeLabel(SelectedPlaybackMode);

    public string NarrationModeDescription =>
        string.Equals(SelectedPlaybackMode, "audio", StringComparison.OrdinalIgnoreCase)
            ? LocalizeUi(
                "Ứng dụng sẽ ưu tiên phát bản thu sẵn. Nếu quán chưa có bản thu, app sẽ dùng giọng đọc để không bị gián đoạn.",
                "The app will prefer recorded audio. If one is missing, it will use voice narration automatically.",
                "应用会优先播放预录音频；若没有音频文件，则会自动切换为语音讲解。",
                "앱은 녹음된 오디오를 우선 재생하고, 없으면 자동 음성 안내로 전환합니다.",
                "L'application privilégie l'audio enregistré puis utilise une voix de narration si besoin.")
            : LocalizeUi(
                "Ứng dụng đang dùng giọng đọc tự động cho phần thuyết minh.",
                "The app is using automatic voice narration.",
                "应用会优先播放预录音频；若没有音频文件，则会自动切换为语音讲解。",
                "앱은 현재 자동 음성 안내를 기본 모드로 사용합니다.",
                "L'application utilise actuellement une voix de narration automatique.");

    public string DraftSelectedLanguageDisplayName => GetLanguageDisplayName(DraftSelectedLanguage);

    public string DraftSelectedLanguageDisplayLabel =>
        DraftSelectedLanguageOption?.DisplayLabel ?? DraftSelectedLanguageDisplayName;

    public AudioSettingsOption? DraftSelectedLanguageOption
    {
        get => SupportedLanguages.FirstOrDefault(item =>
            string.Equals(item.Code, DraftSelectedLanguage, StringComparison.OrdinalIgnoreCase));
        set => DraftSelectedLanguage = value?.Code ?? "vi";
    }

    public string DraftPlaybackModeDisplay => GetPlaybackModeLabel(DraftSelectedPlaybackMode);

    public AudioSettingsOption? DraftSelectedPlaybackModeOption
    {
        get => SupportedPlaybackModes.FirstOrDefault(item =>
            string.Equals(item.Code, DraftSelectedPlaybackMode, StringComparison.OrdinalIgnoreCase));
        set => DraftSelectedPlaybackMode = value?.Code ?? DefaultNarrationMode;
    }

    public string DraftPlaybackModeDescription =>
        string.Equals(DraftSelectedPlaybackMode, "audio", StringComparison.OrdinalIgnoreCase)
            ? LocalizeUi(
                "Nghe bằng bản thu sẵn nếu quán đã có nội dung.",
                "Use recorded audio when the place has it.",
                "应用会优先播放预录音频；若没有音频文件，则会自动切换为语音讲解。",
                "매장에 오디오 파일이 있으면 녹음된 음성을 재생합니다.",
                "Utilise l'audio enregistré lorsque le lieu dispose d'un fichier.")
            : LocalizeUi(
                "Nghe bằng giọng đọc máy theo ngôn ngữ bạn chọn.",
                "Use synthesized speech in the selected language.",
                "应用会优先播放预录音频；若没有音频文件，则会自动切换为语音讲解。",
                "선택한 언어의 자동 음성으로 재생합니다.",
                "Utilise une voix de synthèse dans la langue choisie.");

    public string CurrentAudioSettingsSummary =>
        $"{NarrationModeDisplay} • {SelectedLanguageDisplayName} • {(IsAutoNarrationEnabled
            ? LocalizeUi("Tự động phát", "Auto play", "自动播放", "자동 재생", "Lecture auto")
            : LocalizeUi("Chỉ phát thủ công", "Manual only", "仅手动播放", "수동 재생만", "Manuel uniquement"))}";

    public string DraftAudioSettingsSummary =>
        $"{DraftPlaybackModeDisplay} • {DraftSelectedLanguageDisplayName} • {(DraftIsAutoNarrationEnabled
            ? LocalizeUi("Tự động phát", "Auto play", "自动播放", "자동 재생", "Lecture auto")
            : LocalizeUi("Chỉ phát thủ công", "Manual only", "仅手动播放", "수동 재생만", "Manuel uniquement"))}";

    public string AudioSettingsQuickActionText =>
        $"{NarrationModeDisplay} • {SelectedLanguageDisplayName}";

    public string MapModeBadgeText
    {
        get => _mapModeBadgeText;
        private set => SetProperty(ref _mapModeBadgeText, value);
    }

    public string MapPoiBadgeText
    {
        get => _mapPoiBadgeText;
        private set => SetProperty(ref _mapPoiBadgeText, value);
    }

    public bool HasEventLogs => EventLogs.Count > 0;

    public bool IsEventLogEmpty => EventLogs.Count == 0;

    public bool HasListeningHistory => ListeningHistory.Count > 0;

    public bool IsListeningHistoryEmpty => ListeningHistory.Count == 0;

    public bool HasListeningHistoryLocalEntries => ListeningHistoryLocalEntries.Count > 0;

    public bool IsListeningHistoryFallbackVisible => !HasListeningHistory && HasListeningHistoryLocalEntries;

    public bool IsListeningHistoryDisplayEmpty => !HasListeningHistory && !HasListeningHistoryLocalEntries;

    public bool HasListeningHistoryRanking => ListeningHistoryRanking.Count > 0;

    public bool HasViewHistory => ViewHistory.Count > 0;

    public bool IsViewHistoryEmpty => ViewHistory.Count == 0;

    public bool CanClearListeningHistory =>
        !IsListeningHistoryLoading &&
        !IsClearingListeningHistory &&
        (HasListeningHistory || HasListeningHistoryLocalEntries || HasListeningHistoryRanking);

    public string EventLogSummary => HasEventLogs
        ? $"{EventLogs.Count} hoạt động gần nhất của tài khoản hiện tại"
        : "Chưa có hoạt động nào được ghi lại";

    public string ListeningHistorySummary =>
        GetLocalizedListeningHistorySummaryText(ListeningHistory.Count, ListeningHistoryLocalEntries.Count);

    public string ListeningHistoryFallbackSummary =>
        GetLocalizedListeningHistoryFallbackSummaryText(ListeningHistoryLocalEntries.Count);

    public string ViewHistorySummary => HasViewHistory
        ? $"{ViewHistory.Count} lượt xem gần nhất"
        : "Chưa có lượt xem nào";

    public string SelectedLanguageDisplayName => GetLanguageDisplayName(SelectedLanguage);

    public string SelectedLanguageDisplayLabel =>
        SupportedLanguages.FirstOrDefault(item =>
            string.Equals(item.Code, SelectedLanguage, StringComparison.OrdinalIgnoreCase))?.DisplayLabel ??
        SelectedLanguageDisplayName;

    public string AudioSettingsSummary =>
        $"{(IsAutoNarrationEnabled
            ? LocalizeUi("Tự động phát khi vào vùng", "Auto play in range", "进入范围时自动播放", "범위 진입 시 자동 재생", "Lecture auto à l'approche")
            : LocalizeUi("Chỉ phát thủ công", "Manual only", "仅手动播放", "수동 재생만", "Manuel uniquement"))} • {SelectedLanguageDisplayName} • {NarrationModeDisplay}";

    public bool IsListeningHistoryLoading
    {
        get => _isListeningHistoryLoading;
        private set
        {
            if (!SetProperty(ref _isListeningHistoryLoading, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanClearListeningHistory));
            (ClearListeningHistoryCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool IsClearingListeningHistory
    {
        get => _isClearingListeningHistory;
        private set
        {
            if (!SetProperty(ref _isClearingListeningHistory, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanClearListeningHistory));
            (ClearListeningHistoryCommand as Command)?.ChangeCanExecute();
        }
    }

    public string ListeningHistoryLoadError
    {
        get => _listeningHistoryLoadError;
        private set
        {
            if (!SetProperty(ref _listeningHistoryLoadError, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasListeningHistoryLoadError));
        }
    }

    public bool HasListeningHistoryLoadError => !string.IsNullOrWhiteSpace(ListeningHistoryLoadError);

    public string ListeningHistorySyncStatus =>
        GetLocalizedListeningHistorySyncStatus(_lastListeningHistorySyncAt);

    public string SelectedListeningHistoryPeriod
    {
        get => _selectedListeningHistoryPeriod;
        set
        {
            if (!SetProperty(ref _selectedListeningHistoryPeriod, value))
            {
                return;
            }

            TriggerListeningHistoryRefresh();
        }
    }

    public string SelectedListeningHistorySort
    {
        get => _selectedListeningHistorySort;
        set
        {
            if (!SetProperty(ref _selectedListeningHistorySort, value))
            {
                return;
            }

            TriggerListeningHistoryRefresh();
        }
    }

    public string SelectedListeningHistoryView
    {
        get => _selectedListeningHistoryView;
        set
        {
            if (!SetProperty(ref _selectedListeningHistoryView, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsListeningHistoryTimelineVisible));
            OnPropertyChanged(nameof(IsListeningHistoryRankingVisible));
        }
    }

    public bool IsListeningHistoryTimelineVisible => string.Equals(
        SelectedListeningHistoryView,
        "Dòng thời gian",
        StringComparison.Ordinal);

    public bool IsListeningHistoryRankingVisible => !IsListeningHistoryTimelineVisible;

    public async Task InitializeAsync(bool enableLocationFlow = true)
    {
        EnsurePoiRefreshLoopStarted();

        if (_isInitialized)
        {
            await RefreshPoisIfChangedAsync();
            await RefreshOfflinePackageStatusAsync();
            return;
        }

        StatusText = LocalizeUi(
            "Đang tải danh sách quán...",
            "Loading places...",
            "正在加载地点列表...",
            "매장 목록을 불러오는 중...",
            "Chargement des lieux...");
        await RefreshPoisIfChangedAsync(forceRefresh: true);

        if (enableLocationFlow)
        {
            await InitializeMapFlowAsync();
        }
        else
        {
            await ApplyFallbackMapStateAsync(
                LocalizeUi(
                    "Đã mở nội dung từ mã QR",
                    "QR content is ready",
                    "QR 模式：已准备好播放讲解",
                    "QR 모드: 오디오 안내 준비 완료",
                    "Mode QR : narration prête"),
                LocalizeUi(
                    "Bạn có thể nghe thuyết minh ngay, không cần bật vị trí.",
                    "You can listen now without turning on location.",
                    "您可以立即收听讲解，无需开启定位。",
                    "위치를 켜지 않아도 바로 안내를 들을 수 있습니다.",
                    "Vous pouvez écouter tout de suite, sans activer la position."));
        }

        await RefreshOfflinePackageStatusAsync();
        _isInitialized = true;
        OnPropertyChanged(nameof(HasOfflineSnapshotNotice));
        OnPropertyChanged(nameof(OfflineSnapshotNoticeText));
        OnPropertyChanged(nameof(HasManualLocationNotice));
        OnPropertyChanged(nameof(ManualLocationNoticeText));
        OnPropertyChanged(nameof(HasNoTourNotice));
        OnPropertyChanged(nameof(NoTourNoticeText));
    }

    public async Task StartAsync(bool autoStart = false)
    {
        if (IsTracking)
        {
            return;
        }

        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        if (IsTracking)
        {
            return;
        }

        await StartTrackingCoreAsync(autoStart, requestPermissionIfNeeded: !autoStart);
    }

    public async Task StopAsync()
    {
        await _locationService.StopListeningAsync();
        IsTracking = false;
        _insidePoiIds.Clear();
        _lastAutoNarratedPoiId = null;
        _lastAutoNarrationEvaluationAtUtc = null;
        await ReturnToEntranceAsync(clearLiveLocation: true);
        AddLog($"{NowLabel()} Dừng cập nhật vị trí");
    }

    private async Task InitializeMapFlowAsync()
    {
        var hasPermission = await EnsureLocationPermissionAsync(requestIfNeeded: false);
        if (!hasPermission)
        {
            await ApplyFallbackMapStateAsync(
                LocalizeUi(
                    "Bản đồ đang mở tại đầu phố",
                    "Map is centered at the entrance",
                    "您可以立即收听讲解，无需开启定位。",
                    "지도가 기본 위치를 사용 중입니다",
                    "La carte utilise la position par défaut"),
                LocalizeUi(
                    "Bạn chưa bật quyền vị trí. Vẫn có thể xem bản đồ và chọn quán thủ công.",
                    "Location is not enabled yet. You can still view the map and choose places manually.",
                    "您可以立即收听讲解，无需开启定位。",
                    "위치 권한이 없어도 지도와 매장을 직접 선택할 수 있습니다.",
                    "La position n'est pas activée. Vous pouvez tout de même choisir un lieu sur la carte."));
            return;
        }

        var currentLocation = await _locationService.GetCurrentLocationAsync();
        if (currentLocation is null)
        {
            await ApplyFallbackMapStateAsync(
                LocalizeUi(
                    "Đang tìm vị trí của bạn",
                    "Finding your location",
                    "您可以立即收听讲解，无需开启定位。",
                    "내 위치를 찾는 중",
                    "Recherche de votre position"),
                LocalizeUi(
                    "Quá trình định vị có thể mất vài giây. Bạn vẫn có thể chọn quán trên bản đồ.",
                    "This can take a few seconds. You can still choose a place on the map.",
                    "您可以立即收听讲解，无需开启定位。",
                    "위치를 찾는 데 몇 초 걸릴 수 있습니다. 지도에서 매장을 선택할 수 있습니다.",
                    "Cela peut prendre quelques secondes. Vous pouvez choisir un lieu sur la carte."));
            await StartTrackingCoreAsync(autoStart: true, requestPermissionIfNeeded: false);
            return;
        }

        await ApplyLocationSnapshotAsync(currentLocation, allowAutoNarrate: false);
        await StartTrackingCoreAsync(autoStart: true, requestPermissionIfNeeded: false);
    }

    private async Task<bool> EnsureLocationPermissionAsync(bool requestIfNeeded)
    {
        var hasPermission = await _locationService.EnsurePermissionAsync(requestIfNeeded);
        _hasCheckedLocationPermission = true;
        _hasLocationPermission = hasPermission;

        if (!hasPermission)
        {
            _lastLocation = null;
        }

        await MainThread.InvokeOnMainThreadAsync(UpdateMapBadges);
        return hasPermission;
    }

    private async Task StartTrackingCoreAsync(bool autoStart, bool requestPermissionIfNeeded)
    {
        var hasPermission = _hasLocationPermission;
        if (!hasPermission && requestPermissionIfNeeded)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page ?? Application.Current?.MainPage;
            var shouldRequest = page is null || await page.DisplayAlert(
                LocalizeUi(
                    "Cần dùng vị trí",
                    "Location needed",
                    "需要使用位置",
                    "위치 권한 필요",
                    "Position requise"),
                LocalizeUi(
                    "Ứng dụng dùng vị trí để tìm quán gần bạn và tự phát thuyết minh đúng điểm. Bạn vẫn có thể xem thủ công nếu không cấp quyền.",
                    "The app uses location to find nearby places and play the right narration. You can still browse manually without permission.",
                    "应用会用位置查找附近店铺并播放对应讲解。若不授权，仍可手动浏览。",
                    "앱은 위치로 가까운 매장을 찾고 알맞은 안내를 재생합니다. 권한 없이도 수동으로 볼 수 있습니다.",
                    "L'application utilise la position pour trouver les lieux proches et lancer le bon récit. Vous pouvez aussi parcourir manuellement."),
                LocalizeUi("Cho phép", "Allow", "允许", "허용", "Autoriser"),
                LocalizeUi("Để sau", "Later", "稍后", "나중에", "Plus tard"));

            if (!shouldRequest)
            {
                await ApplyFallbackMapStateAsync(
                    LocalizeUi(
                        "Bản đồ đang mở tại đầu phố",
                        "Map is centered at the entrance",
                        "地图正在使用默认位置",
                        "지도가 기본 위치를 사용 중입니다",
                        "La carte utilise la position par défaut"),
                    LocalizeUi(
                        "Bạn chưa bật quyền vị trí. Vẫn có thể xem các quán và nghe thuyết minh thủ công.",
                        "Location is not enabled yet. You can still view places and listen manually.",
                        "尚未开启定位权限，但仍可查看店铺并手动收听。",
                        "위치 권한이 없어도 매장을 보고 직접 들을 수 있습니다.",
                        "La position n'est pas activée. Vous pouvez voir les lieux et écouter manuellement."));
                return;
            }
        }

        if (!hasPermission)
        {
            hasPermission = await EnsureLocationPermissionAsync(requestPermissionIfNeeded);
        }

        if (!hasPermission)
        {
            await ApplyFallbackMapStateAsync(
                LocalizeUi(
                    "Bản đồ đang mở tại đầu phố",
                    "Map is centered at the entrance",
                    "地图正在使用默认位置",
                    "지도가 기본 위치를 사용 중입니다",
                    "La carte utilise la position par défaut"),
                requestPermissionIfNeeded
                    ? LocalizeUi(
                        "Bạn chưa bật quyền vị trí. Vẫn có thể xem các quán và nghe thuyết minh thủ công.",
                        "Location is not enabled yet. You can still view places and listen manually.",
                        "尚未开启定位权限，但仍可查看店铺并手动收听。",
                        "위치 권한이 없어도 매장을 보고 직접 들을 수 있습니다.",
                        "La position n'est pas activée. Vous pouvez voir les lieux et écouter manuellement.")
                    : LocalizeUi(
                        "Bạn chưa bật quyền vị trí. Vẫn có thể xem bản đồ và chọn quán thủ công.",
                        "Location is not enabled yet. You can still view the map and choose places manually.",
                        "尚未开启定位权限，但仍可查看店铺并手动收听。",
                        "위치 권한이 없어도 매장을 보고 직접 들을 수 있습니다.",
                        "La position n'est pas activée. Vous pouvez tout de même choisir un lieu sur la carte."));
            return;
        }

        try
        {
            StatusText = LocalizeUi(
                "Đang tìm vị trí của bạn...",
                "Finding your location...",
                "正在查找您的位置...",
                "내 위치를 찾는 중...",
                "Recherche de votre position...");

            if (_lastLocation is null)
            {
                var currentLocation = await _locationService.GetCurrentLocationAsync();
                if (currentLocation is not null)
                {
                    await ApplyLocationSnapshotAsync(currentLocation, allowAutoNarrate: false);
                }
            }

            await _locationService.StartListeningAsync();
            IsTracking = true;
            UpdateMapBadges();
            StatusText = _lastLocation is null
                ? LocalizeUi(
                    "Đã bật vị trí, đang chờ tín hiệu đầu tiên",
                    "Location is on, waiting for the first signal",
                    "尚未开启定位权限，但仍可查看地图并手动选择店铺。",
                    "위치가 켜졌고 첫 신호를 기다리는 중입니다",
                    "La position est activée, en attente du premier signal")
                : LocalizeUi(
                    "Đang dùng vị trí của bạn",
                    "Using your location",
                    "正在查找您的位置",
                    "내 위치를 찾는 중",
                    "Utilisation de votre position");
            AddLog($"{NowLabel()} {(autoStart ? "Khởi động" : "Bật")} cập nhật vị trí");
        }
        catch (Exception ex)
        {
            if (!await EnsureLocationPermissionAsync(requestIfNeeded: false))
            {
                await ApplyFallbackMapStateAsync(
                    LocalizeUi(
                        "Bản đồ đang mở tại đầu phố",
                        "Map is centered at the entrance",
                        "尚未开启定位权限，但仍可查看店铺并手动收听。",
                        "지도가 기본 위치를 사용 중입니다",
                        "La carte utilise la position par défaut"),
                    LocalizeUi(
                        "Chưa thể dùng vị trí. Hãy bật quyền vị trí trong cài đặt thiết bị rồi thử lại.",
                        "Location is not ready yet. Enable location permission in device settings and try again.",
                        "尚未开启定位权限，但仍可查看店铺并手动收听。",
                        "아직 위치를 사용할 수 없습니다. 기기 설정에서 위치 권한을 켜고 다시 시도하세요.",
                        "La position n'est pas prête. Activez l'autorisation dans les réglages puis réessayez."));
                AddLog($"{NowLabel()} Lỗi vị trí: {ex.Message}");
                return;
            }

            StatusText = LocalizeUi(
                "Chưa thể bật vị trí. Hãy kiểm tra quyền vị trí rồi thử lại.",
                "Could not start location. Check location permission and try again.",
                "暂时无法开启定位。请检查定位权限后重试。",
                "위치를 켤 수 없습니다. 위치 권한을 확인하고 다시 시도하세요.",
                "Impossible d'activer la position. Vérifiez l'autorisation puis réessayez.");
            UpdateMapBadges();
            AddLog($"{NowLabel()} Lỗi vị trí: {ex.Message}");
        }
    }

    public void ShowSearchSuggestions()
    {
        _isSearchFocused = true;
        RefreshSearchSuggestions();
    }

    public void HideSearchSuggestions()
    {
        _isSearchFocused = false;
        IsSearchSuggestionsVisible = false;
    }

    public void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    public async Task ResetHomeViewAsync()
    {
        if (IsNarrating)
        {
            await StopNarrationAsync();
        }

        _hasUserSelectedPoi = false;
        ClearMapCategoryFilterInternal();
        HideSearchSuggestions();
        ClearSearch();

        if (_pois.Count == 0)
        {
            LocationText = GetDefaultEntranceLocationText();
            NearestPoiText = HasActiveTour
                ? ActiveTourCurrentStopText
                : GetDefaultPoiPromptText();
            StatusText = BuildIdleStatusText();
            UpdateMapBadges();
            RaiseMapStateChanged();
            return;
        }

        RefreshPoiList(Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>(), null);
        SetSelectedPoi(GetCurrentActiveTourPoi() ?? _pois.First(), false, null);

        LocationText = GetDefaultEntranceLocationText();
        NearestPoiText = HasActiveTour
            ? ActiveTourCurrentStopText
            : GetDefaultPoiPromptText();
        StatusText = BuildIdleStatusText();

        UpdateMapBadges();
        RaiseMapStateChanged();
    }

    public async Task ActivateTourAsync(int tourId)
    {
        var tour = _tours.FirstOrDefault(item => item.Id == tourId && item.IsActive);
        if (tour is null)
        {
            StatusText = LocalizeUi(
                "Không tìm thấy tour đang hoạt động",
                "Could not find an active tour",
                "暂时无法开启定位。请检查定位权限后重试。",
                "활성 투어를 찾지 못했습니다",
                "Impossible de trouver un parcours actif");
            return;
        }

        if (IsNarrating)
        {
            await StopNarrationAsync();
        }

        ClearMapCategoryFilterInternal();
        _activeTourId = tour.Id;
        _activeTourStopIndex = 0;
        _hasUserSelectedPoi = false;
        _insidePoiIds.Clear();
        _lastAutoNarratedPoiId = null;
        _lastAutoNarrationEvaluationAtUtc = null;

        NormalizeActiveTourState();
        RefreshTourState();

        var currentPoi = GetCurrentActiveTourPoi();
        if (currentPoi is not null)
        {
            SetSelectedPoi(currentPoi, false, EvaluateCurrentPoiStatuses());
        }

            StatusText = currentPoi is null
                ? LocalizeUi(
                $"Tour {tour.Name} chưa có điểm dừng khả dụng",
                $"{tour.Name} does not have an available stop yet.",
                $"{tour.Name} 暂时没有可跟踪的有效站点。",
                $"{tour.Name} 투어에는 아직 유효한 경유지가 없습니다.",
                $"{tour.Name} ne dispose pas encore d'étape valide à suivre.")
            : LocalizeUi(
                $"Đã bắt đầu {tour.Name}. Điểm đầu tiên: {currentPoi.Name}.",
                $"{tour.Name} started. First stop: {currentPoi.Name}.",
                $"已开始 {tour.Name}。第一站：{currentPoi.Name}。",
                $"{tour.Name} 투어를 시작했습니다. 첫 지점: {currentPoi.Name}.",
                $"{tour.Name} commencé. Première étape : {currentPoi.Name}.");

        AddLog($"{NowLabel()} Kích hoạt tour {tour.Name}");
        await NarrateTourActivationAsync(tour);

        if (_lastLocation is not null)
        {
            await ApplyLocationSnapshotAsync(_lastLocation, allowAutoNarrate: false);
            return;
        }

        RefreshNarrationPresentation();
    }

    public async Task StopActiveTourAsync()
    {
        var activeTour = GetActiveTour();
        if (activeTour is null)
        {
            return;
        }

        if (IsNarrating)
        {
            await StopNarrationAsync();
        }

        _activeTourId = null;
        _activeTourStopIndex = 0;
        RefreshTourState();
        AddLog($"{NowLabel()} Tắt tour {activeTour.Name}");
        await ReturnToEntranceAsync(clearLiveLocation: true);
        StatusText = LocalizeUi(
            "Đã dừng tour. Bạn có thể khám phá tự do.",
            "Tour stopped. You can explore freely.",
            "已返回自由探索模式",
            "자유 탐색 모드로 돌아왔습니다",
            "Retour au mode découverte libre");
    }

    public bool ExecuteSearch(string? query = null)
    {
        if (query is not null)
        {
            SearchQuery = query.Trim();
        }
        else
        {
            SearchQuery = SearchQuery.Trim();
        }

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return false;
        }

        AddRecentSearch(SearchQuery);
        UpdateFilteredPoiStatuses();
        return FilteredPoiStatuses.Count > 0;
    }

    public void SetMapCategoryFilter(string? categoryKey, bool updateStatus = true)
    {
        var normalizedCategory = NormalizeFeaturedDishCategoryKey(categoryKey);
        if (string.Equals(_activeMapCategoryKey, normalizedCategory, StringComparison.OrdinalIgnoreCase))
        {
            if (updateStatus)
            {
                StatusText = LocalizeUi(
                    $"Đang mở bản đồ cho nhóm {GetFeaturedCategoryDisplayName(normalizedCategory)}.",
                    $"Map is already filtered for {GetFeaturedCategoryDisplayName(normalizedCategory)}.",
                    $"地图已经按 {GetFeaturedCategoryDisplayName(normalizedCategory)} 分组显示。",
                    $"{GetFeaturedCategoryDisplayName(normalizedCategory)} 그룹 지도를 이미 표시 중입니다.",
                    $"La carte est déjà filtrée sur {GetFeaturedCategoryDisplayName(normalizedCategory)}.");
            }

            return;
        }

        _activeMapCategoryKey = normalizedCategory;
        OnPropertyChanged(nameof(HasMapCategoryFilter));
        OnPropertyChanged(nameof(ActiveMapCategoryFilterText));

        if (updateStatus)
        {
            StatusText = LocalizeUi(
                $"Đã lọc bản đồ theo nhóm {GetFeaturedCategoryDisplayName(normalizedCategory)}.",
                $"Map filtered for {GetFeaturedCategoryDisplayName(normalizedCategory)}.",
                $"地图已按 {GetFeaturedCategoryDisplayName(normalizedCategory)} 分组筛选。",
                $"{GetFeaturedCategoryDisplayName(normalizedCategory)} 그룹으로 지도를 필터링했습니다.",
                $"Carte filtrée sur {GetFeaturedCategoryDisplayName(normalizedCategory)}.");
        }

        UpdateMapBadges();
        RaiseMapStateChanged();
    }

    public void ClearMapCategoryFilter(bool updateStatus = false)
    {
        ClearMapCategoryFilterInternal();

        if (updateStatus)
        {
            StatusText = BuildIdleStatusText();
        }
    }

    public bool TrySelectNearestPoiForFeaturedCategory(string? categoryKey)
    {
        var poi = GetNearestPoiForFeaturedCategory(categoryKey);
        if (poi is null)
        {
            return false;
        }

        SetSelectedPoi(poi, true, EvaluateCurrentPoiStatuses());
        StatusText = LocalizeUi(
            $"Quán gần nhất cho nhóm {GetFeaturedCategoryDisplayName(categoryKey)} là {poi.Name}.",
            $"Nearest place for {GetFeaturedCategoryDisplayName(categoryKey)} is {poi.Name}.",
            $"{GetFeaturedCategoryDisplayName(categoryKey)} 分组最近的店铺是 {poi.Name}。",
            $"{GetFeaturedCategoryDisplayName(categoryKey)} 그룹에서 가장 가까운 곳은 {poi.Name}입니다.",
            $"Le lieu le plus proche pour {GetFeaturedCategoryDisplayName(categoryKey)} est {poi.Name}.");
        return true;
    }

    public bool TrySelectRecommendedPoiForFeaturedCategory(string? categoryKey)
    {
        var poi = GetRecommendedPoiForFeaturedCategory(categoryKey);
        if (poi is null)
        {
            return false;
        }

        SetSelectedPoi(poi, true, EvaluateCurrentPoiStatuses());
        StatusText = LocalizeUi(
            $"Quán nên bắt đầu cho nhóm {GetFeaturedCategoryDisplayName(categoryKey)} là {poi.Name}.",
            $"Recommended first stop for {GetFeaturedCategoryDisplayName(categoryKey)} is {poi.Name}.",
            $"{GetFeaturedCategoryDisplayName(categoryKey)} 分组最近的店铺是 {poi.Name}。",
            $"{GetFeaturedCategoryDisplayName(categoryKey)} 그룹에서 가장 가까운 곳은 {poi.Name}입니다.",
            $"Le premier lieu recommandé pour {GetFeaturedCategoryDisplayName(categoryKey)} est {poi.Name}.");
        return true;
    }

    public async Task<bool> StartFeaturedCategoryTourAsync(string? categoryKey)
    {
        var tour = ResolveFeaturedCategoryTour(categoryKey);
        if (tour is null)
        {
            StatusText = LocalizeUi(
                "Nhóm món này chưa có mini tour khả dụng.",
                "This category does not have a mini tour yet.",
                "这个分组暂时没有可用的小路线。",
                "이 그룹에는 아직 미니 투어가 없습니다.",
                "Cette catégorie n'a pas encore de mini parcours.");
            return false;
        }

        await ActivateTourAsync(tour.Id);
        return true;
    }

    public bool ApplySearchSuggestion(SearchSuggestionItem suggestion)
    {
        return ExecuteSearch(suggestion.Text);
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

    public async Task<bool> OpenPoiFromQrAsync(
        Guid poiId,
        bool autoPlay = true,
        CancellationToken cancellationToken = default)
    {
        if (poiId == Guid.Empty)
        {
            StatusText = LocalizeUi(
                "Mã QR này chưa mở được nội dung",
                "This QR code cannot open content yet",
                "这个分组暂时没有可用的小路线。",
                "QR 코드가 올바르지 않습니다",
                "Code QR invalide");
            return false;
        }

        if (!_isInitialized)
        {
            await InitializeAsync(enableLocationFlow: false);
        }
        else if (_pois.All(item => item.Id != poiId))
        {
            await RefreshPoisIfChangedAsync(forceRefresh: true, cancellationToken);
        }

        var poi = _pois.FirstOrDefault(item => item.Id == poiId)
                  ?? await _poiRepository.GetPoiByIdAsync(poiId, cancellationToken);

        if (poi is null)
        {
            StatusText = LocalizeUi(
                "Mã QR này chưa liên kết với quán nào",
                "This QR code is not linked to a place yet",
                "这个分组暂时没有可用的小路线。",
                "이 QR 코드에서 매장을 찾지 못했습니다",
                "Aucun lieu trouvé depuis ce code QR");
            return false;
        }

        if (!poi.IsActive)
        {
            StatusText = LocalizeUi(
                "Quán này hiện đang tạm khóa",
                "This place is temporarily unavailable",
                "这个分组暂时没有可用的小路线。",
                "이 매장은 현재 일시적으로 비활성화되어 있습니다",
                "Ce lieu est temporairement indisponible");
            return false;
        }

        SetSelectedPoi(poi, true, null);
        StatusText = autoPlay
            ? LocalizeUi(
                $"Đang mở thuyết minh của {poi.Name}",
                $"Opening narration for {poi.Name}",
                $"正在打开 {poi.Name} 的二维码内容",
                $"{poi.Name}의 QR 콘텐츠를 여는 중",
                $"Ouverture du QR de {poi.Name}")
            : LocalizeUi(
                $"Đã mở {poi.Name}",
                $"Opened {poi.Name}",
                $"正在打开 {poi.Name} 的二维码内容",
                $"{poi.Name}의 QR 콘텐츠를 열었습니다",
                $"QR de {poi.Name} ouvert");
        AddLog($"{NowLabel()} Mở {poi.Name} từ mã QR");

        if (!IsTracking)
        {
            LocationText = LocalizeUi(
                "Bạn đang mở nội dung từ mã QR. Không cần bật vị trí.",
                "You opened this from QR. Location is not required.",
                "这个分组暂时没有可用的小路线。",
                "QR로 연 콘텐츠입니다. 위치는 필요하지 않습니다.",
                "Vous avez ouvert ce contenu par QR. La position n'est pas nécessaire.");
        }

        if (!autoPlay)
        {
            RefreshNarrationPresentation();
            return true;
        }

        if (IsCurrentNarration(poi))
        {
            await StopNarrationAsync();
        }

        await NarratePoiAsync(poi, false, GetDistanceForPoi(poi.Id), syncSelectedPoi: false);
        return true;
    }

    public async Task<bool> OpenTourFromQrAsync(
        int tourId,
        CancellationToken cancellationToken = default)
    {
        if (tourId <= 0)
        {
            StatusText = LocalizeUi(
                "이 QR 코드에서 매장을 찾지 못했습니다",
                "This tour QR code is invalid",
                "二维码无效",
                "이 QR 코드에서 매장을 찾지 못했습니다",
                "Ce code QR de parcours est invalide");
            return false;
        }

        if (!_isInitialized)
        {
            await InitializeAsync(enableLocationFlow: false);
        }
        else if (_tours.All(item => item.Id != tourId))
        {
            await RefreshPoisIfChangedAsync(forceRefresh: true, cancellationToken);
        }

        var tour = _tours.FirstOrDefault(item => item.Id == tourId)
                   ?? await _tourRepository.GetTourByIdAsync(tourId, cancellationToken);

        if (tour is null || !tour.IsActive || !tour.IsQrEnabled)
        {
            StatusText = LocalizeUi(
                "Mã QR này chưa liên kết với quán nào",
                "This QR code is not linked to an active tour",
                "未从此二维码找到地点",
                "이 QR 코드에서 매장을 찾지 못했습니다",
                "Ce code QR n'est pas lié à un parcours actif");
            return false;
        }

        if (_tours.All(item => item.Id != tour.Id))
        {
            _tours = _tours
                .Concat([tour.Clone()])
                .Where(item => item.IsActive)
                .Select(item => item.Clone())
                .ToList();
            RefreshTourState();
        }

        await ActivateTourAsync(tour.Id);

        return true;
    }

    public async Task NarrateSelectedPoiAsync()
    {
        if (_selectedPoi is null)
        {
            StatusText = LocalizeUi(
                "Chưa có quán được chọn",
                "No place selected yet",
                "此地点暂时不可用",
                "아직 매장을 선택하지 않았습니다",
                "Aucun lieu sélectionné");
            return;
        }

        if (IsCurrentNarration(_selectedPoi))
        {
            StatusText = LocalizeUi(
                $"Nội dung của {_selectedPoi.Name} đang được phát",
                $"{_selectedPoi.Name} is already playing",
                $"{_selectedPoi.Name} 正在播放",
                $"{_selectedPoi.Name} 안내가 이미 재생 중입니다",
                $"{_selectedPoi.Name} est déjà en lecture");
            AddLog($"{NowLabel()} Bỏ qua phát lại {_selectedPoi.Name} vì nội dung đang phát");
            return;
        }

        await NarratePoiAsync(_selectedPoi, false, GetDistanceForPoi(_selectedPoi.Id));
    }

    public async Task ToggleSelectedPoiNarrationAsync()
    {
        if (_selectedPoi is null)
        {
            StatusText = LocalizeUi(
                "Chưa có quán được chọn",
                "No place selected yet",
                "您通过二维码打开此内容，无需开启定位。",
                "아직 매장을 선택하지 않았습니다",
                "Aucun lieu sélectionné");
            return;
        }

        if (IsCurrentNarration(_selectedPoi))
        {
            await StopNarrationAsync();
            return;
        }

        await NarratePoiAsync(_selectedPoi, false, GetDistanceForPoi(_selectedPoi.Id));
    }

    public async Task TogglePoiNarrationAsync(Guid poiId)
    {
        var poi = _pois.FirstOrDefault(item => item.Id == poiId);
        if (poi is null)
        {
            return;
        }

        if (IsCurrentNarration(poi))
        {
            await StopNarrationAsync();
            return;
        }

        await NarratePoiAsync(
            poi,
            false,
            GetDistanceForPoi(poi.Id),
            syncSelectedPoi: false);
    }

    public async Task StopNarrationAsync()
    {
        var hadActiveNarration = IsNarrating;

        Interlocked.Increment(ref _narrationSessionId);
        await _narrationService.StopAsync();

        SetActiveNarrationPoiId(null);
        IsNarrating = false;
        StatusText = hadActiveNarration
            ? LocalizeUi(
                "Đã dừng thuyết minh",
                "Narration stopped",
                "尚未选择地点",
                "오디오 안내가 중지되었습니다",
                "Narration arrêtée")
            : BuildIdleStatusText();
        await MainThread.InvokeOnMainThreadAsync(() => RefreshNarrationPresentation());

        if (hadActiveNarration)
        {
            AddLog($"{NowLabel()} Dừng thuyết minh");
        }
    }

    public async Task<bool> OpenListeningHistoryDetailAsync(Guid historyId)
    {
        var item = FindListeningHistoryItem(historyId);
        if (item is null)
        {
            StatusText = LocalizeUi(
                "Không tìm thấy bản ghi lịch sử nghe",
                "Listening history entry not found",
                "讲解已停止",
                "청취 기록을 찾지 못했습니다",
                "Enregistrement d'écoute introuvable");
            return false;
        }

        var poi = await ResolvePoiForListeningHistoryAsync(item);
        if (poi is null)
        {
            StatusText = LocalizeUi(
                "Không thể mở chi tiết quán từ bản ghi này",
                "Could not open place details from this record",
                "讲解已停止",
                "이 기록으로 장소 상세를 열 수 없습니다",
                "Impossible d'ouvrir les détails du lieu depuis cet enregistrement");
            return false;
        }

        SetSelectedPoi(poi, true, null);
        StatusText = LocalizeUi(
            $"Đang xem lại lịch sử nghe của {item.PoiName}",
            $"Reviewing listening history for {item.PoiName}",
            $"正在查看 {item.PoiName} 的收听记录",
            $"{item.PoiName} 청취 기록을 확인하는 중입니다",
            $"Consultation de l'historique d'écoute de {item.PoiName}");
        return true;
    }

    public async Task ReplayListeningHistoryAsync(Guid historyId)
    {
        var item = FindListeningHistoryItem(historyId);
        if (item is null)
        {
            StatusText = LocalizeUi(
                "Không tìm thấy bản ghi lịch sử nghe",
                "Listening history entry not found",
                "讲解已停止",
                "청취 기록을 찾지 못했습니다",
                "Enregistrement d'écoute introuvable");
            return;
        }

        if (string.IsNullOrWhiteSpace(item.NarrationSnapshot) &&
            string.IsNullOrWhiteSpace(item.AudioAssetPath))
        {
            StatusText = LocalizeUi(
                "Bản ghi này chưa có nội dung để phát lại",
                "This record does not have replayable content yet",
                "未找到该收听记录",
                "이 기록으로 장소 상세를 열 수 없습니다",
                "Cet enregistrement ne contient pas encore de contenu à rejouer");
            return;
        }

        var playbackRequest = ResolvePlaybackRequest(
            item.PlaybackMode,
            item.AudioAssetPath,
            allowAudioFallback: true);
        var narrationSessionId = Interlocked.Increment(ref _narrationSessionId);
        var hasReplayError = false;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SetActiveNarrationPoiId(item.PoiId == Guid.Empty ? null : item.PoiId);
            IsNarrating = true;
            StatusText = LocalizeUi(
                $"Phát lại lịch sử: {item.PoiName}",
                $"Replaying history: {item.PoiName}",
                $"正在重播：{item.PoiName}",
                $"기록 재생 중: {item.PoiName}",
                $"Relecture de l'historique : {item.PoiName}");
            RefreshNarrationPresentation();
        });

        AddLog($"{NowLabel()} Phát lại lịch sử nghe {item.PoiName} [{item.PlaybackModeLabel}]");
        if (!string.IsNullOrWhiteSpace(playbackRequest.FallbackMessage))
        {
            AddLog($"{NowLabel()} {playbackRequest.FallbackMessage}");
        }

        try
        {
            await _narrationService.SpeakAsync(
                item.NarrationSnapshot,
                item.Language,
                playbackRequest.PlaybackMode,
                playbackRequest.AudioAssetPath);
        }
        catch (Exception ex)
        {
            hasReplayError = true;
            AddLog($"{NowLabel()} Lỗi phát lại lịch sử: {ex.Message}");

            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                StatusText = LocalizeUi(
                    "Chưa thể nghe lại mục này. Hãy thử lại sau.",
                    "Could not replay this item. Please try again later.",
                    "暂时无法重播此项目，请稍后再试。",
                    "이 항목을 다시 재생할 수 없습니다. 나중에 다시 시도하세요.",
                    "Impossible de relire cet élément. Réessayez plus tard.");
            }
        }
        finally
        {
            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetActiveNarrationPoiId(null);
                    IsNarrating = false;
                    RefreshNarrationPresentation();

                    if (!hasReplayError)
                    {
                        StatusText = BuildIdleStatusText();
                    }
                });
            }
        }
    }

    public async Task<bool> DeleteListeningHistoryEntryAsync(Guid historyId)
    {
        var deletedItem = FindListeningHistoryItem(historyId);
        var deleted = await _listeningHistorySyncService.DeleteAsync(historyId);
        if (!deleted)
        {
            ListeningHistoryLoadError = LocalizeUi(
                "Không xóa được bản ghi lịch sử nghe này.",
                "Could not delete this listening history entry.",
                "无法删除这条收听记录。",
                "이 청취 기록을 삭제할 수 없습니다.",
                "Impossible de supprimer cet historique d'écoute.");
            return false;
        }

        RemoveOptimisticListeningHistoryItem(historyId);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var item = ListeningHistory.FirstOrDefault(entry => entry.Id == historyId);
            if (item is not null)
            {
                ListeningHistory.Remove(item);
            }

            ListeningHistoryLoadError = string.Empty;
            RaiseListeningHistoryStateChanged();
        });

        await RefreshListeningHistoryAsync();
        StatusText = LocalizeUi(
            deletedItem is null
                ? "Đã xóa bản ghi khỏi lịch sử nghe."
                : $"Đã xóa {deletedItem.PoiName} khỏi lịch sử nghe.",
            deletedItem is null
                ? "Listening history entry deleted."
                : $"{deletedItem.PoiName} removed from listening history.",
            "收听记录已更新",
            "청취 기록이 업데이트되었습니다",
            "L'historique d'écoute a été mis à jour");
        return true;
    }

    public async Task SignOutAsync()
    {
        if (IsNarrating)
        {
            await StopNarrationAsync();
        }

        if (IsTracking)
        {
            await StopAsync();
        }

        _hasUserSelectedPoi = false;
        _insidePoiIds.Clear();
        _lastAutoNarratedPoiId = null;
        _lastAutoNarrationEvaluationAtUtc = null;
        _activeTourId = null;
        _activeTourStopIndex = 0;
        _gpsOriginLocation = null;
        HideSearchSuggestions();
        ClearSearch();
        OnPropertyChanged(nameof(GpsOriginLocation));

        await _authService.SignOutAsync();
    }

    public void ShowFeaturedDishCategory(string? categoryKey)
    {
        var normalizedCategory = NormalizeFeaturedDishCategoryKey(categoryKey);
        _selectedFeaturedDishCategoryKey = normalizedCategory;
        OnPropertyChanged(nameof(SelectedFeaturedDishCategoryKey));
        var category = FeaturedDishes.FirstOrDefault(item =>
                           string.Equals(item.Key, normalizedCategory, StringComparison.OrdinalIgnoreCase))
                       ?? FeaturedDishes.First();
        var dishes = _featuredDishCatalog
            .Where(item => string.Equals(item.CategoryKey, category.Key, StringComparison.OrdinalIgnoreCase))
            .Select(LocalizeFeaturedDishItem)
            .ToList();

        ReplaceCollection(SelectedFeaturedDishItems, dishes);
        SelectedFeaturedDishCategoryName = category.Name;
        SelectedFeaturedDishCategorySummary = category.Description;
        OnPropertyChanged(nameof(SelectedFeaturedDishResultsText));
        OnPropertyChanged(nameof(SelectedFeaturedDishCategoryHeaderText));
        OnPropertyChanged(nameof(SelectedFeaturedDishNearestPoiText));
        OnPropertyChanged(nameof(SelectedFeaturedDishRecommendedPoiText));
        OnPropertyChanged(nameof(SelectedFeaturedDishMapFilterText));
        OnPropertyChanged(nameof(SelectedFeaturedDishMiniTourText));
    }

    private void EnsurePoiRefreshLoopStarted()
    {
        if (_poiRefreshLoopTask is not null)
        {
            return;
        }

        _poiRefreshLoopTask = Task.Run(RunPoiRefreshLoopAsync);
    }

    private async Task RunPoiRefreshLoopAsync()
    {
        using var timer = new PeriodicTimer(PoiRefreshInterval);

        while (await timer.WaitForNextTickAsync())
        {
            await RefreshPoisIfChangedAsync();
        }
    }

    private async Task RefreshPoisIfChangedAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var latestPoisTask = _poiRepository.GetPoisAsync(cancellationToken);
        var latestToursTask = _tourRepository.GetToursAsync(cancellationToken);

        await Task.WhenAll(latestPoisTask, latestToursTask);

        var latestPois = latestPoisTask.Result;
        var latestTours = latestToursTask.Result;
        var poiSnapshot = CreatePoiSnapshot(latestPois);
        var tourSnapshot = CreateTourSnapshot(latestTours);

        if (!forceRefresh &&
            string.Equals(_poiDataSnapshot, poiSnapshot, StringComparison.Ordinal) &&
            string.Equals(_tourDataSnapshot, tourSnapshot, StringComparison.Ordinal))
        {
            return;
        }

        await _locationUpdateGate.WaitAsync(cancellationToken);

        try
        {
            var previousSelectedPoiId = _selectedPoi?.Id;
            var previousSnapshot = _poiDataSnapshot;
            var previousTourSnapshot = _tourDataSnapshot;

            _pois = latestPois.ToList();
            _tours = latestTours
                .Where(item => item.IsActive)
                .Select(item => item.Clone())
                .ToList();
            _poiDataSnapshot = poiSnapshot;
            _tourDataSnapshot = tourSnapshot;
            CleanupPoiState();
            NormalizeActiveTourState();

            var nearestPoi = default(POI);
            IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> evaluated =
                Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>();

            if (_lastLocation is not null && _pois.Count > 0)
            {
                evaluated = _geofenceEngine.Evaluate(_lastLocation, _pois);
                nearestPoi = evaluated.FirstOrDefault().Poi;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RefreshTourState();
                RefreshPoiList(evaluated, nearestPoi?.Id);

                if (_lastLocation is not null)
                {
                    UpdateLocationSummary(
                        _lastLocation,
                        nearestPoi,
                        nearestPoi is null ? null : GetDistanceForPoi(nearestPoi.Id));
                }
                else if (_pois.Count == 0)
                {
                    NearestPoiText = LocalizeUi(
                        "Chưa có quán nào sẵn sàng hiển thị",
                        "No places are ready to show yet",
                        "暂时没有可显示的店铺",
                        "아직 표시할 매장이 없습니다",
                        "Aucun lieu prêt à afficher");
                }
                else
                {
                    NearestPoiText = GetDefaultPoiPromptText();
                }

                ApplySelectedPoiAfterRefresh(
                    previousSelectedPoiId,
                    GetCurrentActiveTourPoi() ?? nearestPoi,
                    evaluated);
                UpdateMapBadges();
                RaiseMapStateChanged();
            });

            if (_isInitialized &&
                (!string.IsNullOrWhiteSpace(previousSnapshot) || !string.IsNullOrWhiteSpace(previousTourSnapshot)))
            {
                AddLog($"{NowLabel()} Cập nhật dữ liệu mới ({_pois.Count} quán, {_tours.Count} tour)");
            }
        }
        finally
        {
            _locationUpdateGate.Release();
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
            StatusText = LocalizeUi(
                $"Bạn vừa chạm vùng của {candidate.Poi.Name}",
                $"You tapped inside {candidate.Poi.Name}'s area",
                $"您刚点击了 {candidate.Poi.Name} 的范围内",
                $"{candidate.Poi.Name} 영역 안을 눌렀습니다",
                $"Vous avez touché la zone de {candidate.Poi.Name}");
            AddLog($"{NowLabel()} Chạm Mapsui trong bán kính {candidate.Poi.Name}");
            await NarratePoiAsync(candidate.Poi, false, candidate.DistanceMeters);
            return;
        }

        var nearest = results.FirstOrDefault();
        if (nearest.Poi is not null)
        {
            SetSelectedPoi(nearest.Poi, true, results);
            StatusText = LocalizeUi(
                $"Bạn vừa chạm ngoài vùng quán. Gần nhất là {nearest.Poi.Name}",
                $"You tapped outside a place area. Nearest place: {nearest.Poi.Name}",
                $"您点击的位置不在店铺范围内。最近地点：{nearest.Poi.Name}",
                $"매장 영역 밖을 눌렀습니다. 가장 가까운 곳: {nearest.Poi.Name}",
                $"Vous avez touché hors zone. Lieu le plus proche : {nearest.Poi.Name}");
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

    private void OnAuthSessionChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SyncCurrentUser();
            LoadPersistedState();
            TriggerListeningHistoryRefresh();
            _ = SyncCurrentUserProfileToAdminAsync();
        });
    }

    private async Task HandleLocationUpdatedAsync(LocationDto location)
    {
        await ApplyLocationSnapshotAsync(location, allowAutoNarrate: true);
    }

    private async Task ApplyLocationSnapshotAsync(LocationDto location, bool allowAutoNarrate)
    {
        if (!LocationService.IsValidCoordinate(location.Latitude, location.Longitude))
        {
            return;
        }

        _lastLocation = location;
        _hasCheckedLocationPermission = true;
        _hasLocationPermission = true;
        _gpsOriginLocation ??= new LocationDto
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AccuracyMeters = location.AccuracyMeters,
            TimestampUtc = location.TimestampUtc
        };

        var results = _pois.Count == 0
            ? Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>()
            : _geofenceEngine.Evaluate(location, _pois);
        var nearest = results.FirstOrDefault();
        var autoCandidates = CreateAutoNarrationCandidates(results);
        var currentTourPoi = GetCurrentActiveTourPoi();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            RefreshPoiList(results, nearest.Poi?.Id);
            UpdateLocationSummary(location, nearest.Poi, nearest.Poi is null ? null : nearest.DistanceMeters);

            if (_pois.Count == 0)
            {
                NearestPoiText = LocalizeUi(
                    "Chưa có quán nào sẵn sàng hiển thị",
                    "No places are ready to show yet",
                    "暂时没有可显示的店铺",
                    "아직 표시할 매장이 없습니다",
                    "Aucun lieu prêt à afficher");
            }
            else if (!_hasUserSelectedPoi)
            {
                SetSelectedPoi(currentTourPoi ?? nearest.Poi ?? _selectedPoi ?? _pois.FirstOrDefault(), false, results);
            }
            else
            {
                UpdateSelectedPoiDetails(results);
            }

            UpdateMapBadges();
        });

        if (allowAutoNarrate && IsAutoNarrationEnabled)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var decision = ResolveAutoNarrationDecision(autoCandidates, nowUtc);

            if (decision.ShouldUpdateEvaluationTimestamp)
            {
                _lastAutoNarrationEvaluationAtUtc = nowUtc;
            }

            if (decision.ShouldNarrate && decision.Poi is not null)
            {
                await NarratePoiAsync(decision.Poi, true, decision.DistanceMeters);
            }
        }

        _insidePoiIds.Clear();
        foreach (var poiId in autoCandidates.Select(candidate => candidate.Poi.Id))
        {
            _insidePoiIds.Add(poiId);
        }

        if (_insidePoiIds.Count == 0)
        {
            _lastAutoNarratedPoiId = null;
        }
        else if (_lastAutoNarratedPoiId.HasValue &&
                 !_insidePoiIds.Contains(_lastAutoNarratedPoiId.Value))
        {
            _lastAutoNarratedPoiId = null;
        }

        RaiseMapStateChanged();
        OnPropertyChanged(nameof(GpsOriginLocation));
    }

    private async Task ApplyFallbackMapStateAsync(string statusText, string locationText)
    {
        _lastLocation = null;
        _insidePoiIds.Clear();
        _lastAutoNarratedPoiId = null;
        _lastAutoNarrationEvaluationAtUtc = null;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            RefreshTourState();
            RefreshPoiList(Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>(), null);

            if (_pois.Count == 0)
            {
                _selectedPoi = null;
                _hasUserSelectedPoi = false;
                ClearSelectedPoiDetails();
                NearestPoiText = LocalizeUi(
                    "Chưa có quán nào sẵn sàng hiển thị",
                    "No places are ready to show yet",
                    "暂时没有可显示的店铺",
                    "아직 표시할 매장이 없습니다",
                    "Aucun lieu prêt à afficher");
            }
            else
            {
                NearestPoiText = HasActiveTour
                    ? ActiveTourCurrentStopText
                    : GetDefaultPoiPromptText();

                if (_selectedPoi is null)
                {
                    SetSelectedPoi(GetCurrentActiveTourPoi() ?? _pois.FirstOrDefault(), false, null);
                }
                else
                {
                    UpdateSelectedPoiDetails();
                }
            }

            StatusText = statusText;
            LocationText = locationText;
            UpdateMapBadges();
            RaiseMapStateChanged();
        });
    }

    public async Task ReturnToEntranceAsync(bool clearLiveLocation)
    {
        if (clearLiveLocation)
        {
            _lastLocation = null;
            _insidePoiIds.Clear();
            _lastAutoNarratedPoiId = null;
            _lastAutoNarrationEvaluationAtUtc = null;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var evaluated = clearLiveLocation
                ? Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>()
                : EvaluateCurrentPoiStatuses();
            var nearest = evaluated.FirstOrDefault().Poi;

            RefreshPoiList(evaluated, nearest?.Id);

            if (_pois.Count == 0)
            {
                _selectedPoi = null;
                ClearSelectedPoiDetails();
                NearestPoiText = LocalizeUi(
                    "Chưa có quán nào sẵn sàng hiển thị",
                    "No places are ready to show yet",
                    "暂时没有可显示的店铺",
                    "아직 표시할 매장이 없습니다",
                    "Aucun lieu prêt à afficher");
            }
            else
            {
                SetSelectedPoi(GetCurrentActiveTourPoi() ?? _pois.FirstOrDefault(), false, evaluated);
                NearestPoiText = HasActiveTour ? ActiveTourCurrentStopText : GetDefaultPoiPromptText();
            }

            LocationText = GetDefaultEntranceLocationText();
            StatusText = BuildIdleStatusText();
            UpdateMapBadges();
            RaiseMapStateChanged();
        });
    }

    private void CleanupPoiState()
    {
        var activePoiIds = _pois
            .Select(poi => poi.Id)
            .ToHashSet();

        _insidePoiIds.RemoveWhere(poiId => !activePoiIds.Contains(poiId));

        if (_lastAutoNarratedPoiId.HasValue && !activePoiIds.Contains(_lastAutoNarratedPoiId.Value))
        {
            _lastAutoNarratedPoiId = null;
            _lastAutoNarrationEvaluationAtUtc = null;
        }

        foreach (var stalePoiId in _lastNarratedAt.Keys.Where(poiId => !activePoiIds.Contains(poiId)).ToList())
        {
            _lastNarratedAt.Remove(stalePoiId);
        }
    }

    private void NormalizeActiveTourState()
    {
        var activeTour = GetActiveTour();
        if (activeTour is null)
        {
            _activeTourId = null;
            _activeTourStopIndex = 0;
            return;
        }

        var activeStopIds = GetTourStopIds(activeTour);
        if (activeStopIds.Count == 0)
        {
            _activeTourId = null;
            _activeTourStopIndex = 0;
            return;
        }

        if (_activeTourStopIndex < 0)
        {
            _activeTourStopIndex = 0;
            return;
        }

        if (_activeTourStopIndex > activeStopIds.Count)
        {
            _activeTourStopIndex = activeStopIds.Count;
        }
    }

    private void RefreshTourState()
    {
        TourPackages.Clear();

        foreach (var tour in _tours.OrderBy(item => item.Name))
        {
            var stopIds = GetTourStopIds(tour);
            if (stopIds.Count == 0)
            {
                continue;
            }

            var stopNames = stopIds
                .Select(poiId => _pois.FirstOrDefault(item => item.Id == poiId)?.Name ?? "Điểm dừng chưa xác định")
                .ToList();
            var isSelected = _activeTourId == tour.Id;

            TourPackages.Add(new TourPackageItem
            {
                TourId = tour.Id,
                Code = tour.Code,
                Name = tour.Name,
                Description = tour.Description,
                EstimatedMinutes = tour.EstimatedMinutes,
                StopCount = stopIds.Count,
                StopsSummary = string.Join(" • ", stopNames.Take(3)) + (stopNames.Count > 3 ? " ..." : string.Empty),
                IsSelected = isSelected,
                IsCompleted = isSelected && GetCurrentActiveTourPoi() is null,
                MetaLabel = GetLocalizedTourMetaLabel(stopIds.Count, tour.EstimatedMinutes),
                StatusLabel = GetLocalizedTourPackageStatusLabel(isSelected, isSelected && GetCurrentActiveTourPoi() is null),
                ActionLabel = GetLocalizedTourPackageActionLabel(isSelected)
            });
        }

        ActiveTourStops.Clear();

        var activeTour = GetActiveTour();
        if (activeTour is null)
        {
            RaiseTourStateChanged();
            return;
        }

        var activeStopIds = GetActiveTourStopIds();
        for (var index = 0; index < activeStopIds.Count; index++)
        {
            var poiId = activeStopIds[index];
            var poi = _pois.FirstOrDefault(item => item.Id == poiId);
            if (poi is null)
            {
                continue;
            }

            ActiveTourStops.Add(new TourStopProgressItem
            {
                Order = index + 1,
                PoiId = poi.Id,
                Name = poi.Name,
                Address = poi.Address,
                IsCompleted = index < _activeTourStopIndex,
                IsCurrent = index == _activeTourStopIndex && _activeTourStopIndex < activeStopIds.Count,
                IsUpcoming = index > _activeTourStopIndex,
                OrderLabel = GetLocalizedTourStopOrderLabel(index + 1),
                StatusLabel = GetLocalizedTourStopStatusLabel(
                    index < _activeTourStopIndex,
                    index == _activeTourStopIndex && _activeTourStopIndex < activeStopIds.Count)
            });
        }

        RaiseTourStateChanged();
    }

    private IReadOnlyList<PoiStatusItem> GetVisibleMapPoiStatuses()
    {
        var filteredItems = GetCategoryFilteredPoiStatuses(PoiStatuses);
        var activeTourPoiIds = GetActiveTourStopIds().ToHashSet();
        if (activeTourPoiIds.Count == 0)
        {
            return SortPoiStatusesForNearby(filteredItems);
        }

        return filteredItems
            .Where(item => activeTourPoiIds.Contains(item.PoiId))
            .ToList();
    }

    private IReadOnlyList<PoiStatusItem> GetPreviewMapPoiStatuses()
    {
        var fullItems = GetVisibleMapPoiStatuses();
        return fullItems.ToList();
    }

    private TourDto? GetActiveTour()
    {
        return _activeTourId.HasValue
            ? _tours.FirstOrDefault(item => item.Id == _activeTourId.Value)
            : null;
    }

    private IReadOnlyList<Guid> GetTourStopIds(TourDto? tour)
    {
        if (tour is null)
        {
            return Array.Empty<Guid>();
        }

        var activePoiIds = _pois
            .Select(item => item.Id)
            .ToHashSet();

        return tour.PoiIds
            .Where(poiId => poiId != Guid.Empty && activePoiIds.Contains(poiId))
            .Distinct()
            .ToList();
    }

    private IReadOnlyList<Guid> GetActiveTourStopIds()
    {
        return GetTourStopIds(GetActiveTour());
    }

    private Guid? GetCurrentActiveTourPoiId()
    {
        var stopIds = GetActiveTourStopIds();
        return _activeTourStopIndex >= 0 && _activeTourStopIndex < stopIds.Count
            ? stopIds[_activeTourStopIndex]
            : null;
    }

    private POI? GetCurrentActiveTourPoi()
    {
        var currentPoiId = GetCurrentActiveTourPoiId();
        return currentPoiId.HasValue
            ? _pois.FirstOrDefault(item => item.Id == currentPoiId.Value)
            : null;
    }

    private POI? GetNextActiveTourPoi()
    {
        var stopIds = GetActiveTourStopIds();
        var nextIndex = _activeTourStopIndex + 1;

        if (nextIndex < 0 || nextIndex >= stopIds.Count)
        {
            return null;
        }

        return _pois.FirstOrDefault(item => item.Id == stopIds[nextIndex]);
    }

    private HashSet<Guid> GetCompletedActiveTourPoiIds()
    {
        return GetActiveTourStopIds()
            .Take(Math.Max(0, _activeTourStopIndex))
            .ToHashSet();
    }

    private Dictionary<Guid, int> GetActiveTourOrderLookup()
    {
        return GetActiveTourStopIds()
            .Select((poiId, index) => new { PoiId = poiId, Order = index + 1 })
            .ToDictionary(item => item.PoiId, item => item.Order);
    }

    private bool TryAdvanceActiveTourAfterNarration(Guid poiId, out string completedTourName)
    {
        completedTourName = string.Empty;

        var activeTour = GetActiveTour();
        if (activeTour is null)
        {
            return false;
        }

        var currentPoiId = GetCurrentActiveTourPoiId();
        if (!currentPoiId.HasValue || currentPoiId.Value != poiId)
        {
            return false;
        }

        var stopIds = GetActiveTourStopIds();
        if (_activeTourStopIndex < stopIds.Count)
        {
            _activeTourStopIndex++;
        }

        if (_activeTourStopIndex >= stopIds.Count)
        {
            completedTourName = activeTour.Name;
        }

        if (!_hasUserSelectedPoi)
        {
            _selectedPoi = GetCurrentActiveTourPoi() ?? _selectedPoi;
        }

        return true;
    }

    private static bool ShouldAdvanceActiveTourAfterNarration(
        POI poi,
        bool autoTriggered,
        double? distanceMeters)
    {
        return autoTriggered ||
               (distanceMeters.HasValue && distanceMeters.Value <= poi.TriggerRadiusMeters);
    }

    private IReadOnlyList<AutoNarrationCandidate> CreateAutoNarrationCandidates(
        IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> results)
    {
        IEnumerable<(POI Poi, double DistanceMeters, bool IsInside)> scopedResults = results;

        if (HasActiveTour)
        {
            var currentTourPoiId = GetCurrentActiveTourPoiId();
            if (!currentTourPoiId.HasValue)
            {
                return Array.Empty<AutoNarrationCandidate>();
            }

            scopedResults = results.Where(item => item.Poi.Id == currentTourPoiId.Value);
        }

        var evaluations = scopedResults
            .Select(item => new AutoNarrationPoiEvaluation(
                item.Poi,
                item.DistanceMeters,
                item.IsInside))
            .ToList();

        return _autoPoiSelectionService.CreateCandidates(evaluations, SelectedLanguage);
    }

    private AutoNarrationDecisionResult ResolveAutoNarrationDecision(
        IReadOnlyList<AutoNarrationCandidate> candidates,
        DateTimeOffset nowUtc)
    {
        return _autoPoiSelectionService.Decide(new AutoNarrationDecisionInput
        {
            Candidates = candidates,
            CurrentPoiId = _lastAutoNarratedPoiId,
            ActiveNarrationPoiId = _activeNarrationPoiId,
            LastAutoNarratedPoiId = _lastAutoNarratedPoiId,
            IsNarrationInProgress = IsNarrating,
            LastEvaluationAtUtc = _lastAutoNarrationEvaluationAtUtc,
            NowUtc = nowUtc,
            PreviousInsidePoiIds = _insidePoiIds.ToList(),
            LastNarratedAtUtc = new Dictionary<Guid, DateTimeOffset>(_lastNarratedAt),
            Options = AutoNarrationOptions
        });
    }

    private string BuildIdleStatusText()
    {
        var activeTour = GetActiveTour();
        if (activeTour is not null)
        {
            var currentPoi = GetCurrentActiveTourPoi();
            if (currentPoi is null)
            {
                return LocalizeUi(
                    $"Tour {activeTour.Name} đã hoàn tất",
                    $"{activeTour.Name} tour completed",
                    $"{activeTour.Name} 路线已完成",
                    $"{activeTour.Name} 투어 완료",
                    $"Parcours {activeTour.Name} terminé");
            }

            return IsTracking
                ? LocalizeUi(
                    $"Tour {activeTour.Name}: đang chờ {currentPoi.Name}",
                    $"{activeTour.Name}: waiting for {currentPoi.Name}",
                    $"{activeTour.Name}：正在等待 {currentPoi.Name}",
                    $"{activeTour.Name}: {currentPoi.Name} 대기 중",
                    $"{activeTour.Name} : en attente de {currentPoi.Name}")
                : LocalizeUi(
                    $"Tour {activeTour.Name}: sẵn sàng theo dõi {currentPoi.Name}",
                    $"{activeTour.Name}: ready to track {currentPoi.Name}",
                    $"{activeTour.Name}：正在等待 {currentPoi.Name}",
                    $"{activeTour.Name}: {currentPoi.Name} 추적 준비 완료",
                    $"{activeTour.Name} : prêt à suivre {currentPoi.Name}");
        }

        return IsTracking
            ? LocalizeUi("Đang dùng vị trí của bạn", "Using your location", "正在使用您的位置", "내 위치를 사용하는 중", "Utilisation de votre position")
            : LocalizeUi("Sẵn sàng khám phá", "Ready to explore", "准备开始探索", "탐색 준비 완료", "Prêt à explorer");
    }

    private async Task NarrateTourActivationAsync(TourDto tour)
    {
        var sessionId = Interlocked.Increment(ref _narrationSessionId);
        var narrationText = BuildTourActivationNarrationText(tour, SelectedLanguage);
        string errorMessage = string.Empty;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SetActiveNarrationPoiId(null);
            IsNarrating = true;
            StatusText = LocalizeUi(
                $"Đang giới thiệu tour: {tour.Name}",
                $"Introducing tour: {tour.Name}",
                $"正在介绍路线：{tour.Name}",
                $"투어 소개 중: {tour.Name}",
                $"Présentation du parcours : {tour.Name}");
            RefreshNarrationPresentation();
        });

        try
        {
            await _narrationService.SpeakAsync(
                narrationText,
                SelectedLanguage,
                playbackMode: "tts");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            AddLog($"{NowLabel()} Lỗi phát giới thiệu tour: {ex.Message}");

            if (sessionId == Volatile.Read(ref _narrationSessionId))
            {
                StatusText = LocalizeUi(
                    "Chưa thể phát giới thiệu tour. Bạn vẫn có thể xem lộ trình bên dưới.",
                    "Could not play the tour intro. You can still view the route below.",
                    "暂时无法播放路线介绍，您仍可查看下方路线。",
                    "투어 소개를 재생할 수 없습니다. 아래에서 경로를 볼 수 있습니다.",
                    "Impossible de lire l'introduction. Vous pouvez toujours voir le parcours ci-dessous.");
            }
        }
        finally
        {
            if (sessionId == Volatile.Read(ref _narrationSessionId))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetActiveNarrationPoiId(null);
                    IsNarrating = false;
                    RefreshNarrationPresentation();

                    if (string.IsNullOrWhiteSpace(errorMessage))
                    {
                        StatusText = BuildIdleStatusText();
                    }
                });
            }

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                AddLog($"{NowLabel()} Phát giới thiệu tour {tour.Name}");
            }
        }
    }

    private string BuildTourActivationNarrationText(TourDto tour, string languageCode)
    {
        var stopPois = GetTourStopIds(tour)
            .Select(poiId => _pois.FirstOrDefault(item => item.Id == poiId))
            .Where(poi => poi is not null)
            .Cast<POI>()
            .ToList();
        var firstStop = stopPois.FirstOrDefault();
        var nextStop = stopPois.Skip(1).FirstOrDefault();
        var finalStop = stopPois.Count > 1 ? stopPois.LastOrDefault() : null;
        var stopSummary = stopPois.Count == 0
            ? string.Empty
            : string.Join(", ", stopPois.Take(4).Select(item => item.Name));

        return NormalizeLanguageCode(languageCode) switch
        {
            "en" =>
                $"You selected {tour.Name}. {tour.Description} " +
                $"This tour includes {stopPois.Count} featured stops and should take about {tour.EstimatedMinutes} minutes. " +
                $"출발 지점은 {firstStop?.Name ?? "설정되지 않았습니다"} 입니다. " +
                $"{(nextStop is null ? "There is no next stop yet." : $"After that, the route continues to {nextStop.Name}.")} " +
                $"{(finalStop is null || ReferenceEquals(finalStop, nextStop) ? string.Empty : $"마지막 지점은 {finalStop.Name} 입니다. ")}" +
                $"{(string.IsNullOrWhiteSpace(stopSummary) ? string.Empty : $"이번 경로의 주요 지점은 {stopSummary} 입니다.")}",
            "zh" =>
                $"您已选择 {tour.Name}。{tour.Description}" +
                $" 此路线共有 {stopPois.Count} 个重点停靠点，预计约 {tour.EstimatedMinutes} 分钟。" +
                $" 第一站是 {firstStop?.Name ?? "暂未设置"}。" +
                $"{(nextStop is null ? "目前还没有下一站。" : $" 接下来会前往 {nextStop.Name}。")}" +
                $"{(finalStop is null || ReferenceEquals(finalStop, nextStop) ? string.Empty : $" 最后一站是 {finalStop.Name}。")}" +
                $"{(string.IsNullOrWhiteSpace(stopSummary) ? string.Empty : $" 本次路线包括：{stopSummary}。")}",
            "ko" =>
                $"{tour.Name} 투어를 선택했습니다. {tour.Description} " +
                $"이 투어는 주요 정차 지점 {stopPois.Count}곳으로 구성되어 있으며 예상 소요 시간은 약 {tour.EstimatedMinutes}분입니다. " +
                $"출발 지점은 {firstStop?.Name ?? "설정되지 않았습니다"} 입니다. " +
                $"{(nextStop is null ? "다음 지점은 아직 없습니다." : $"그다음에는 {nextStop.Name}(으)로 이동합니다.")} " +
                $"{(finalStop is null || ReferenceEquals(finalStop, nextStop) ? string.Empty : $"마지막 지점은 {finalStop.Name} 입니다. ")}" +
                $"{(string.IsNullOrWhiteSpace(stopSummary) ? string.Empty : $"이번 경로의 주요 지점은 {stopSummary} 입니다.")}",
            "fr" =>
                $"Vous avez choisi {tour.Name}. {tour.Description} " +
                $"Ce parcours comprend {stopPois.Count} étapes principales pour environ {tour.EstimatedMinutes} minutes. " +
                $"La première étape est {firstStop?.Name ?? "non définie"}. " +
                $"{(nextStop is null ? "Il n'y a pas encore d'étape suivante." : $"Ensuite, l'application vous guidera vers {nextStop.Name}.")} " +
                $"{(finalStop is null || ReferenceEquals(finalStop, nextStop) ? string.Empty : $"La dernière étape est {finalStop.Name}. ")}" +
                $"{(string.IsNullOrWhiteSpace(stopSummary) ? string.Empty : $"이번 경로의 주요 지점은 {stopSummary} 입니다.")}",
            _ =>
                $"Bạn đã chọn {tour.Name}. {tour.Description} " +
                $"Tour gồm {stopPois.Count} điểm dừng nổi bật, thời lượng khoảng {tour.EstimatedMinutes} phút. " +
                $"Điểm mở đầu là {firstStop?.Name ?? "chưa xác định"}. " +
                $"{(nextStop is null ? "Hiện chưa có chặng kế tiếp." : $"Sau đó, ứng dụng sẽ dẫn bạn tới {nextStop.Name}.")} " +
                $"{(finalStop is null || ReferenceEquals(finalStop, nextStop) ? string.Empty : $"Điểm kết của hành trình là {finalStop.Name}. ")}" +
                $"{(string.IsNullOrWhiteSpace(stopSummary) ? string.Empty : $"Lộ trình hôm nay gồm {stopSummary}.")}"
        };
    }

    private void ApplySelectedPoiAfterRefresh(
        Guid? previousSelectedPoiId,
        POI? nearestPoi,
        IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> evaluated)
    {
        if (_pois.Count == 0)
        {
            _selectedPoi = null;
            _hasUserSelectedPoi = false;
            ClearSelectedPoiDetails();
            RaiseMapStateChanged();
            return;
        }

        var preservedSelection = previousSelectedPoiId.HasValue
            ? _pois.FirstOrDefault(item => item.Id == previousSelectedPoiId.Value)
            : null;

        if (preservedSelection is null && previousSelectedPoiId.HasValue)
        {
            _hasUserSelectedPoi = false;
        }

        var nextSelectedPoi = preservedSelection
            ?? (!_hasUserSelectedPoi ? nearestPoi : null)
            ?? _pois.FirstOrDefault();

        SetSelectedPoi(nextSelectedPoi, false, evaluated.Count > 0 ? evaluated : null);
    }

    private void UpdateLocationSummary(LocationDto location, POI? nearestPoi, double? nearestDistanceMeters)
    {
        LocationText = location.AccuracyMeters.HasValue
            ? LocalizeUi(
                $"Đã tìm thấy vị trí của bạn • sai số khoảng {location.AccuracyMeters.Value:F0}m",
                $"Your location is available • accuracy about {location.AccuracyMeters.Value:F0}m",
                $"当前位置：{location.Latitude:F5}, {location.Longitude:F5} • 误差约 {location.AccuracyMeters.Value:F0} 米",
                $"현재 위치: {location.Latitude:F5}, {location.Longitude:F5} • 오차 약 {location.AccuracyMeters.Value:F0}m",
                $"Position actuelle : {location.Latitude:F5}, {location.Longitude:F5} • précision d'environ {location.AccuracyMeters.Value:F0} m")
            : LocalizeUi(
                "Đã tìm thấy vị trí của bạn",
                "Your location is available",
                $"当前位置：{location.Latitude:F5}, {location.Longitude:F5}",
                $"현재 위치: {location.Latitude:F5}, {location.Longitude:F5}",
                $"Position actuelle : {location.Latitude:F5}, {location.Longitude:F5}");

        var activeTour = GetActiveTour();
        var currentTourPoi = GetCurrentActiveTourPoi();
        if (activeTour is not null && currentTourPoi is not null)
        {
            var tourDistance = GetDistanceForPoi(currentTourPoi.Id);
            var totalStops = GetActiveTourStopIds().Count;
            NearestPoiText =
                LocalizeUi(
                    $"Tour {activeTour.Name}: chặng {Math.Min(_activeTourStopIndex + 1, totalStops)}/{totalStops} - {currentTourPoi.Name} ({tourDistance?.ToString("F0") ?? "?"}m)",
                    $"{activeTour.Name}: {Math.Min(_activeTourStopIndex + 1, totalStops)}/{totalStops}번 - {currentTourPoi.Name} ({tourDistance?.ToString("F0") ?? "?"}m)",
                    $"{activeTour.Name}：第 {Math.Min(_activeTourStopIndex + 1, totalStops)}/{totalStops} 站 - {currentTourPoi.Name} ({tourDistance?.ToString("F0") ?? "?"} 米)",
                    $"{activeTour.Name}: {Math.Min(_activeTourStopIndex + 1, totalStops)}/{totalStops}번 - {currentTourPoi.Name} ({tourDistance?.ToString("F0") ?? "?"}m)",
                    $"{activeTour.Name} : étape {Math.Min(_activeTourStopIndex + 1, totalStops)}/{totalStops} - {currentTourPoi.Name} ({tourDistance?.ToString("F0") ?? "?"} m)");
            return;
        }

        if (activeTour is not null)
        {
            NearestPoiText = LocalizeUi(
                $"Tour {activeTour.Name} đã hoàn tất",
                $"{activeTour.Name} tour completed",
                $"{activeTour.Name} 路线已完成",
                $"{activeTour.Name} 투어 완료",
                $"Parcours {activeTour.Name} terminé");
            return;
        }

        NearestPoiText = nearestPoi is null
            ? LocalizeUi(
                "Chưa xác định điểm gần nhất",
                "Nearest place not available yet",
                "暂未识别最近地点",
                "가장 가까운 장소를 아직 확인하지 못했습니다",
                "Le lieu le plus proche n'est pas encore disponible")
            : LocalizeUi(
                $"Quán gần nhất: {nearestPoi.Name} ({nearestDistanceMeters?.ToString("F0") ?? "?"}m)",
                $"Nearest place: {nearestPoi.Name} ({nearestDistanceMeters?.ToString("F0") ?? "?"}m)",
                $"最近地点：{nearestPoi.Name}（{nearestDistanceMeters?.ToString("F0") ?? "?"} 米）",
                $"가장 가까운 장소: {nearestPoi.Name} ({nearestDistanceMeters?.ToString("F0") ?? "?"}m)",
                $"Lieu le plus proche : {nearestPoi.Name} ({nearestDistanceMeters?.ToString("F0") ?? "?"}m)");
    }

    private void UpdateMapBadges()
    {
        var previewMapPoiCount = PreviewMapPoiStatuses.Count;
        var visibleMapPoiCount = VisibleMapPoiStatuses.Count;

        MapPoiBadgeText = visibleMapPoiCount == 0
            ? LocalizeUi("Chưa có quán", "No places yet", "暂无店铺", "매장 없음", "Aucun lieu")
            : HasActiveTour
                ? LocalizeUi(
                    $"{visibleMapPoiCount} điểm trong tour",
                    $"{visibleMapPoiCount} stops in tour",
                    $"路线中 {visibleMapPoiCount} 个站点",
                    $"투어 내 지점 {visibleMapPoiCount}개",
                    $"{visibleMapPoiCount} étapes dans le parcours")
                : HasMapCategoryFilter
                    ? LocalizeUi(
                        $"{visibleMapPoiCount} quán trong nhóm {GetFeaturedCategoryDisplayName(_activeMapCategoryKey)}",
                        $"{visibleMapPoiCount} places in {GetFeaturedCategoryDisplayName(_activeMapCategoryKey)}",
                        $"{GetFeaturedCategoryDisplayName(_activeMapCategoryKey)} 分组 {visibleMapPoiCount} 家店铺",
                        $"{GetFeaturedCategoryDisplayName(_activeMapCategoryKey)} 그룹 {visibleMapPoiCount}개 매장",
                        $"{visibleMapPoiCount} lieux dans {GetFeaturedCategoryDisplayName(_activeMapCategoryKey)}")
                : LocalizeUi(
                    $"{previewMapPoiCount} quán nổi bật đang hiển thị",
                    $"{previewMapPoiCount} nearby places shown",
                    $"显示 {previewMapPoiCount} 家附近店铺",
                    $"가까운 매장 {previewMapPoiCount}개 표시 중",
                    $"{previewMapPoiCount} lieux proches affichés");

        MapModeBadgeText = IsTracking
            ? HasActiveTour
                ? LocalizeUi("Đang dẫn tour", "Guiding tour", "正在引导路线", "투어 안내 중", "Guidage du parcours")
                : HasMapCategoryFilter
                    ? LocalizeUi("Đang lọc theo món", "Dish filter on", "正在按菜品筛选", "메뉴 필터 적용 중", "Filtre plat actif")
                : LocalizeUi("Đang dùng vị trí", "Using location", "正在使用定位", "위치 사용 중", "Position active")
            : HasMapCategoryFilter
                ? LocalizeUi("Đang lọc theo món", "Dish filter on", "正在按菜品筛选", "메뉴 필터 적용 중", "Filtre plat actif")
            : _lastLocation is not null
                ? LocalizeUi("Đã có vị trí", "Location ready", "已有定位", "위치 확보됨", "Position captée")
                : _hasCheckedLocationPermission
                    ? (!_hasLocationPermission
                        ? LocalizeUi("Đầu phố", "Entrance", "默认位置", "기본 위치", "Position par défaut")
                        : IsTracking
                        ? LocalizeUi("Đang tìm vị trí", "Finding location", "正在查找位置", "위치 찾는 중", "Recherche de position")
                        : LocalizeUi("Đầu phố", "Entrance", "默认位置", "기본 위치", "Position par défaut"))
                    : LocalizeUi("Đang chuẩn bị bản đồ", "Preparing map", "正在准备地图", "지도 준비 중", "Préparation de la carte");

        OnPropertyChanged(nameof(HasOfflineSnapshotNotice));
        OnPropertyChanged(nameof(OfflineSnapshotNoticeText));
        OnPropertyChanged(nameof(HasManualLocationNotice));
        OnPropertyChanged(nameof(ManualLocationNoticeText));
        OnPropertyChanged(nameof(ActiveMapCategoryFilterText));
    }

    private async Task NarratePoiAsync(
        POI poi,
        bool autoTriggered,
        double? distanceMeters,
        bool syncSelectedPoi = true)
    {
        if (IsCurrentNarration(poi))
        {
            StatusText = LocalizeUi(
                $"Nội dung của {poi.Name} đang được phát",
                $"{poi.Name} is already playing",
                $"{poi.Name} 正在播放",
                $"{poi.Name} 안내가 이미 재생 중입니다",
                $"{poi.Name} est déjà en lecture");
            return;
        }

        var listenStopwatch = Stopwatch.StartNew();
        var narrationSessionId = Interlocked.Increment(ref _narrationSessionId);
        var historyTask = Task.FromResult<Guid?>(null);
        string errorMessage = string.Empty;
        var playbackRequest = ResolvePlaybackRequest(
            SelectedPlaybackMode,
            poi.AudioAssetPath,
            allowAudioFallback: true);
        _lastNarratedAt[poi.Id] = DateTimeOffset.UtcNow;
        if (autoTriggered)
        {
            _lastAutoNarratedPoiId = poi.Id;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SetActiveNarrationPoiId(poi.Id);
            IsNarrating = true;
            StatusText = autoTriggered
                ? LocalizeUi(
                    $"Tự động phát: {poi.Name}",
                    $"Auto playing: {poi.Name}",
                    $"自动播放：{poi.Name}",
                    $"자동 재생: {poi.Name}",
                    $"Lecture automatique : {poi.Name}")
                : LocalizeUi(
                    $"Đang phát: {poi.Name}",
                    $"Playing: {poi.Name}",
                    $"自动播放：{poi.Name}",
                    $"자동 재생: {poi.Name}",
                    $"Lecture : {poi.Name}");

            if (syncSelectedPoi && !_hasUserSelectedPoi)
            {
                SetSelectedPoi(poi, false, null);
            }

            RefreshNarrationPresentation();
        });

        AddLog(
            $"{NowLabel()} {(autoTriggered ? "Tự động nghe" : "Nghe thủ công")} {poi.Name}" +
            (distanceMeters.HasValue ? $" ({distanceMeters.Value:F0}m)" : string.Empty));
        AddListeningHistory(
            $"{NowLabel()} {(autoTriggered ? "Tự động nghe" : "Nghe thủ công")} {poi.Name}" +
            (distanceMeters.HasValue ? $" ({distanceMeters.Value:F0}m)" : string.Empty));
        var optimisticHistoryId = AddOptimisticListeningHistory(
            poi,
            SelectedLanguage,
            playbackRequest.PlaybackMode,
            autoTriggered);

        if (!string.IsNullOrWhiteSpace(playbackRequest.FallbackMessage))
        {
            AddLog($"{NowLabel()} {playbackRequest.FallbackMessage}");
        }

        historyTask = _listeningHistorySyncService.BeginAsync(
            poi,
            SelectedLanguage,
            playbackRequest.PlaybackMode,
            autoTriggered);
        _ = RefreshListeningHistoryAfterCreateAsync(historyTask, optimisticHistoryId);

        try
        {
            await _narrationService.NarrateAsync(
                poi,
                SelectedLanguage,
                playbackRequest.PlaybackMode);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            AddLog($"{NowLabel()} Lỗi âm thanh: {ex.Message}");

            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                StatusText = LocalizeUi(
                    "Chưa thể phát thuyết minh. Hãy kiểm tra âm lượng hoặc thử lại.",
                    "Could not play narration. Check volume or try again.",
                    "暂时无法播放讲解，请检查音量或稍后再试。",
                    "안내를 재생할 수 없습니다. 음량을 확인하거나 다시 시도하세요.",
                    "Impossible de lire la narration. Vérifiez le volume ou réessayez.");
            }
        }
        finally
        {
            listenStopwatch.Stop();
            var narrationCompleted = string.IsNullOrWhiteSpace(errorMessage)
                && narrationSessionId == Volatile.Read(ref _narrationSessionId);

            var historyId = await historyTask;
            if (historyId.HasValue)
            {
                await _listeningHistorySyncService.CompleteAsync(
                    historyId.Value,
                    (int)Math.Round(listenStopwatch.Elapsed.TotalSeconds),
                    narrationCompleted,
                    narrationCompleted ? string.Empty : errorMessage);

                await RefreshListeningHistoryAsync();
            }

            if (narrationCompleted &&
                ShouldAdvanceActiveTourAfterNarration(poi, autoTriggered, distanceMeters) &&
                TryAdvanceActiveTourAfterNarration(poi.Id, out var completedTourName))
            {
                AddLog($"{NowLabel()} Hoan tat chặng tour tại {poi.Name}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    RefreshTourState();
                    RefreshNarrationPresentation();
                    StatusText = string.IsNullOrWhiteSpace(completedTourName)
                        ? BuildIdleStatusText()
                        : LocalizeUi(
                            $"Đã hoàn tất tour {completedTourName}",
                            $"{completedTourName} tour completed",
                            $"{completedTourName} 路线已完成",
                            $"{completedTourName} 투어 완료",
                            $"Parcours {completedTourName} terminé");
                });
            }

            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetActiveNarrationPoiId(null);
                    IsNarrating = false;
                    RefreshNarrationPresentation();

                    if (string.IsNullOrWhiteSpace(errorMessage))
                    {
                        StatusText = BuildIdleStatusText();
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
        var currentTourPoiId = GetCurrentActiveTourPoiId();
        var completedTourPoiIds = GetCompletedActiveTourPoiIds();
        var tourOrders = GetActiveTourOrderLookup();

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
                NarrationPreview = BuildNarrationPreview(poi.GetNarrationText(SelectedLanguage)),
                MapLink = poi.MapLink,
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                DistanceMeters = hasDistance ? evaluatedItem.DistanceMeters : double.NaN,
                TriggerRadiusMeters = poi.TriggerRadiusMeters,
                IsInsideRadius = hasDistance && evaluatedItem.IsInside,
                IsNearest = nearestPoiId == poi.Id,
                IsNarrationActive = IsNarrating && _activeNarrationPoiId == poi.Id,
                IsActiveTourStop = currentTourPoiId == poi.Id,
                IsCompletedTourStop = completedTourPoiIds.Contains(poi.Id),
                TourOrder = tourOrders.TryGetValue(poi.Id, out var tourOrder) ? tourOrder : null,
                Priority = poi.Priority,
                PriorityLabel = $"P{poi.Priority}",
                CodeLabel = GetLocalizedPoiCodeLabel(poi.Code),
                StatusLabel = GetLocalizedPoiStatusLabel(hasDistance && evaluatedItem.IsInside),
                NearestLabel = GetLocalizedPoiNearestLabel(nearestPoiId == poi.Id),
                InRadiusBadge = GetLocalizedPoiInRadiusBadge(hasDistance && evaluatedItem.IsInside),
                SpecialDishLabel = GetLocalizedSpecialDishLabel(poi.SpecialDish),
                NarrationActionText = GetLocalizedPoiNarrationActionText(IsNarrating && _activeNarrationPoiId == poi.Id),
                NarrationStateText = GetLocalizedPoiNarrationStateText(IsNarrating && _activeNarrationPoiId == poi.Id),
                NarrationGuideText = GetLocalizedPoiNarrationGuideText(IsNarrating && _activeNarrationPoiId == poi.Id),
                TourBadgeText = GetLocalizedTourBadgeText(
                    currentTourPoiId == poi.Id || completedTourPoiIds.Contains(poi.Id),
                    completedTourPoiIds.Contains(poi.Id),
                    tourOrders.TryGetValue(poi.Id, out var resolvedTourOrder) ? resolvedTourOrder : null),
                DistanceLabel = GetLocalizedPoiDistanceLabel(
                    hasDistance ? evaluatedItem.DistanceMeters : double.NaN,
                    poi.TriggerRadiusMeters)
            });
        }

        UpdateFilteredPoiStatuses();
        RefreshSearchSuggestions();
        RaiseTourStateChanged();
        OnPropertyChanged(nameof(HomeNarrationSummary));
        OnPropertyChanged(nameof(HomeNarrationAvailabilityText));
        OnPropertyChanged(nameof(SelectedFeaturedDishNearestPoiText));
        OnPropertyChanged(nameof(SelectedFeaturedDishRecommendedPoiText));
        OnPropertyChanged(nameof(SelectedFeaturedDishMapFilterText));
        OnPropertyChanged(nameof(SelectedFeaturedDishMiniTourText));
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
        OnPropertyChanged(nameof(IsSelectedPoiNarrating));
        OnPropertyChanged(nameof(SelectedPoiNarrationActionText));
        OnPropertyChanged(nameof(SelectedPoiNarrationStateText));

        if (userInitiated)
        {
            AddViewHistory($"{NowLabel()} Xem thông tin quán {poi.Name}");
        }

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
            ? LocalizeUi(
                $"Khoảng cách hiện tại: {distanceMeters.Value:F0}m",
                $"Current distance: {distanceMeters.Value:F0}m",
                $"当前距离：{distanceMeters.Value:F0} 米",
                $"현재 거리: {distanceMeters.Value:F0}m",
                $"Distance actuelle : {distanceMeters.Value:F0} m")
            : LocalizeUi(
                "Khoảng cách: chưa xác định",
                "Distance: not available yet",
                "当前距离：暂未确定",
                "현재 거리: 아직 알 수 없음",
                "Distance : non disponible");

        var referenceLocation = _lastLocation ?? EntranceLocation;
        var effectiveDistance = distanceMeters ??
            GeoMath.DistanceMeters(
                referenceLocation.Latitude,
                referenceLocation.Longitude,
                _selectedPoi.Latitude,
                _selectedPoi.Longitude);
        var walkMinutes = Math.Max(1, (int)Math.Round(effectiveDistance / 78d, MidpointRounding.AwayFromZero));
        var travelEstimateLabel = LocalizeUi(
            $"{effectiveDistance:F0}m • khoảng {walkMinutes} phút đi bộ",
            $"{effectiveDistance:F0}m • about {walkMinutes} min walk",
            $"{effectiveDistance:F0} 米 • 步行约 {walkMinutes} 分钟",
            $"{effectiveDistance:F0}m • 도보 약 {walkMinutes}분",
            $"{effectiveDistance:F0} m • environ {walkMinutes} min à pied");

        SelectedPoiName = _selectedPoi.Name;
        SelectedPoiAddress = _selectedPoi.Address;
        SelectedPoiDescription = _selectedPoi.Description;
        SelectedPoiDishText = GetLocalizedSpecialDishLabel(_selectedPoi.SpecialDish);
        SelectedPoiStatusText =
            $"{distanceLabel} • {LocalizeUi(
                $"Tự phát khi ở gần khoảng {_selectedPoi.TriggerRadiusMeters:F0}m",
                $"Auto plays within about {_selectedPoi.TriggerRadiusMeters:F0}m",
                $"触发半径 {_selectedPoi.TriggerRadiusMeters:F0} 米",
                $"트리거 반경 {_selectedPoi.TriggerRadiusMeters:F0}m",
                $"Rayon de déclenchement {_selectedPoi.TriggerRadiusMeters:F0} m")}";
        SelectedPoiPriceRangeText = _selectedPoi.PriceRange;
        SelectedPoiOpeningHoursText = _selectedPoi.OpeningHours;
        SelectedPoiFirstDishText = _selectedPoi.FirstDishSuggestion;
        SelectedPoiTravelEstimateText = travelEstimateLabel;
        SelectedPoiNarrationPreview = _selectedPoi.GetNarrationText(SelectedLanguage);
        SelectedPoiMapLink = _selectedPoi.MapLink;
        SelectedPoiImageSource = AppImageSourceResolver.Resolve(_selectedPoi.ImageSource);
    }

    private void ClearSelectedPoiDetails()
    {
        SelectedPoiName = LocalizeUi(
            "Chưa có quán nào để hiển thị",
            "No places to show yet",
            "暂无可显示的店铺",
            "표시할 매장이 없습니다",
            "Aucun lieu à afficher");
        SelectedPoiAddress = LocalizeUi(
            "Vui lòng thử lại sau hoặc kiểm tra kết nối mạng.",
            "Please try again later or check your connection.",
            "暂无可显示的店铺",
            "나중에 다시 시도하거나 네트워크 연결을 확인하세요.",
            "Réessayez plus tard ou vérifiez la connexion.");
        SelectedPoiDescription = string.Empty;
        SelectedPoiDishText = string.Empty;
        SelectedPoiStatusText = string.Empty;
        SelectedPoiPriceRangeText = string.Empty;
        SelectedPoiOpeningHoursText = string.Empty;
        SelectedPoiFirstDishText = string.Empty;
        SelectedPoiTravelEstimateText = string.Empty;
        SelectedPoiNarrationPreview = string.Empty;
        SelectedPoiMapLink = string.Empty;
        SelectedPoiImageSource = null;
        OnPropertyChanged(nameof(IsSelectedPoiNarrating));
        OnPropertyChanged(nameof(SelectedPoiNarrationActionText));
        OnPropertyChanged(nameof(SelectedPoiNarrationStateText));
    }

    private double? GetDistanceForPoi(Guid poiId)
    {
        var status = PoiStatuses.FirstOrDefault(item => item.PoiId == poiId);
        return status is null || double.IsNaN(status.DistanceMeters)
            ? null
            : status.DistanceMeters;
    }

    private bool IsCurrentNarration(POI poi) => IsNarrating && _activeNarrationPoiId == poi.Id;

    private void SetActiveNarrationPoiId(Guid? poiId)
    {
        if (_activeNarrationPoiId == poiId)
        {
            return;
        }

        _activeNarrationPoiId = poiId;
        OnPropertyChanged(nameof(IsSelectedPoiNarrating));
        OnPropertyChanged(nameof(SelectedPoiNarrationActionText));
        OnPropertyChanged(nameof(SelectedPoiNarrationStateText));
        OnPropertyChanged(nameof(HomeNarrationSummary));
    }

    private IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> EvaluateCurrentPoiStatuses()
    {
        if (_lastLocation is null || _pois.Count == 0)
        {
            return Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>();
        }

        return _geofenceEngine.Evaluate(_lastLocation, _pois);
    }

    private void RefreshNarrationPresentation()
    {
        var evaluated = EvaluateCurrentPoiStatuses();
        var nearestPoi = evaluated.FirstOrDefault().Poi;

        RefreshTourState();
        RefreshPoiList(evaluated, nearestPoi?.Id);

        if (_selectedPoi is not null)
        {
            UpdateSelectedPoiDetails(evaluated.Count > 0 ? evaluated : null);
        }

        RaiseMapStateChanged();
    }

    private void AddLog(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            EventLogs.Insert(0, message);
            _usageHistoryService.AppendEntry(UsageHistoryCategory.Activity, message);

            while (EventLogs.Count > 15)
            {
                EventLogs.RemoveAt(EventLogs.Count - 1);
            }

            RaiseEventLogStateChanged();
        });
    }

    private void RaiseMapStateChanged()
    {
        MainThread.BeginInvokeOnMainThread(() => MapStateChanged?.Invoke(this, EventArgs.Empty));
    }

    private void RaiseTourStateChanged()
    {
        OnPropertyChanged(nameof(HasTours));
        OnPropertyChanged(nameof(HasActiveTour));
        OnPropertyChanged(nameof(HasActiveTourStops));
        OnPropertyChanged(nameof(IsActiveTourCompleted));
        OnPropertyChanged(nameof(SelectedPoiSectionTitle));
        OnPropertyChanged(nameof(TourSectionSummary));
        OnPropertyChanged(nameof(ActiveTourName));
        OnPropertyChanged(nameof(ActiveTourSummary));
        OnPropertyChanged(nameof(ActiveTourCurrentStopText));
        OnPropertyChanged(nameof(ActiveTourNextStopText));
        OnPropertyChanged(nameof(ActiveTourRoutePoints));
        OnPropertyChanged(nameof(ActiveTourStops));
        OnPropertyChanged(nameof(IsTourPackageListVisible));
        OnPropertyChanged(nameof(TourPackagesHeight));
        OnPropertyChanged(nameof(ActiveTourStopsHeight));
        OnPropertyChanged(nameof(HomeNarrationSummary));
        OnPropertyChanged(nameof(HomePrimaryCtaText));
        OnPropertyChanged(nameof(HasNoTourNotice));
        OnPropertyChanged(nameof(NoTourNoticeText));
    }

    private void UpdateFilteredPoiStatuses()
    {
        var searchResult = _searchService.Search(SearchQuery);
        var visibleItems = searchResult.HasKeyword
            ? searchResult.Results
                .Select(result => PoiStatuses.FirstOrDefault(item => item.PoiId == result.Id))
                .Where(item => item is not null)
                .Cast<PoiStatusItem>()
                .ToList()
            : SortPoiStatusesForNearby(PoiStatuses);

        FilteredPoiStatuses.Clear();

        foreach (var item in visibleItems)
        {
            FilteredPoiStatuses.Add(item);
        }

        IsSearchResultEmpty = searchResult.HasKeyword && FilteredPoiStatuses.Count == 0;
        SearchResultStatusText = IsSearchResultEmpty
            ? GetSearchEmptyStateMessage(searchResult.Keyword)
            : string.Empty;
        OnPropertyChanged(nameof(SearchPreviewItems));
        OnPropertyChanged(nameof(HasSearchPreviewResults));
        OnPropertyChanged(nameof(SearchPreviewSummaryText));
    }

    private void RefreshSearchSuggestions()
    {
        SearchSuggestions.Clear();

        if (!_isSearchFocused || PoiStatuses.Count == 0 || HasSearchQuery)
        {
            IsSearchSuggestionsVisible = false;
            return;
        }

        var suggestions = _searchService.BuildSuggestions(
            SearchQuery,
            _recentSearches,
            MaxSearchSuggestions,
            MaxPinnedRecentSuggestions);

        foreach (var suggestion in suggestions)
        {
        SearchSuggestions.Add(suggestion);
        }

        IsSearchSuggestionsVisible = SearchSuggestions.Count > 0;
    }

    private IReadOnlyList<PoiStatusItem> GetCategoryFilteredPoiStatuses(IEnumerable<PoiStatusItem> source)
    {
        if (!HasMapCategoryFilter)
        {
            return source.ToList();
        }

        var normalizedCategory = NormalizeFeaturedDishCategoryKey(_activeMapCategoryKey);
        return source
            .Where(item => _pois.FirstOrDefault(poi => poi.Id == item.PoiId)?
                .FeaturedCategories
                .Any(category => string.Equals(category, normalizedCategory, StringComparison.OrdinalIgnoreCase)) == true)
            .ToList();
    }

    private List<PoiStatusItem> SortPoiStatusesForNearby(IEnumerable<PoiStatusItem> source)
    {
        return source
            .OrderBy(item => double.IsNaN(item.DistanceMeters) ? 1 : 0)
            .ThenBy(item => double.IsNaN(item.DistanceMeters) ? double.MaxValue : item.DistanceMeters)
            .ThenByDescending(item => item.IsNearest)
            .ThenByDescending(item => item.Priority)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private void ClearMapCategoryFilterInternal()
    {
        if (string.IsNullOrWhiteSpace(_activeMapCategoryKey))
        {
            return;
        }

        _activeMapCategoryKey = string.Empty;
        OnPropertyChanged(nameof(HasMapCategoryFilter));
        OnPropertyChanged(nameof(ActiveMapCategoryFilterText));
        UpdateMapBadges();
        RaiseMapStateChanged();
    }

    private IReadOnlyList<POI> GetPoisForFeaturedCategory(string? categoryKey)
    {
        var normalizedCategory = NormalizeFeaturedDishCategoryKey(categoryKey);
        return _pois
            .Where(poi => poi.FeaturedCategories.Any(item =>
                string.Equals(item, normalizedCategory, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private POI? GetNearestPoiForFeaturedCategory(string? categoryKey)
    {
        var candidates = GetPoisForFeaturedCategory(categoryKey);
        if (candidates.Count == 0)
        {
            return null;
        }

        var referenceLocation = _lastLocation ?? EntranceLocation;
        return candidates
            .OrderBy(poi => GeoMath.DistanceMeters(
                referenceLocation.Latitude,
                referenceLocation.Longitude,
                poi.Latitude,
                poi.Longitude))
            .ThenByDescending(poi => poi.Priority)
            .FirstOrDefault();
    }

    private POI? GetRecommendedPoiForFeaturedCategory(string? categoryKey)
    {
        return GetPoisForFeaturedCategory(categoryKey)
            .OrderByDescending(poi => poi.Priority)
            .ThenBy(poi => NormalizeForSearch(poi.Name))
            .FirstOrDefault();
    }

    private TourDto? ResolveFeaturedCategoryTour(string? categoryKey)
    {
        var normalizedCategory = NormalizeFeaturedDishCategoryKey(categoryKey);
        var codeFragment = normalizedCategory switch
        {
            "bo" => "BO",
            "lau" => "LAU",
            "oc" => "OC",
            "cua" => "CUA",
            _ => string.Empty
        };

        return _tours.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(codeFragment) &&
            item.Code.Contains(codeFragment, StringComparison.OrdinalIgnoreCase));
    }

    private void AddRecentSearch(string query)
    {
        var trimmedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return;
        }

        var existingIndex = _recentSearches.FindIndex(item =>
            string.Equals(item, trimmedQuery, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            _recentSearches.RemoveAt(existingIndex);
        }

        _recentSearches.Insert(0, trimmedQuery);

        while (_recentSearches.Count > MaxRecentSearches)
        {
            _recentSearches.RemoveAt(_recentSearches.Count - 1);
        }

        RefreshSearchSuggestions();
    }

    private void TriggerListeningHistoryRefresh()
    {
        _ = RefreshListeningHistoryAsync();
    }

    private async Task RefreshListeningHistoryAsync(CancellationToken cancellationToken = default)
    {
        await _listeningHistoryRefreshGate.WaitAsync(cancellationToken);

        try
        {
            IsListeningHistoryLoading = true;
            ListeningHistoryLoadError = string.Empty;
            OnPropertyChanged(nameof(ListeningHistorySummary));

            var sortBy = MapListeningHistorySortToApi(SelectedListeningHistorySort);
            var period = MapListeningHistoryPeriodToApi(SelectedListeningHistoryPeriod);

            var timelineTask = _listeningHistorySyncService.GetCurrentUserHistoryAsync(
                sortBy,
                period,
                15,
                cancellationToken);
            var rankingTask = _listeningHistorySyncService.GetCurrentUserRankingAsync(period, cancellationToken);

            await Task.WhenAll(timelineTask, rankingTask);

            var timelineItems = MergeOptimisticListeningHistoryItems(
                await BuildListeningHistoryDisplayItemsAsync(
                    timelineTask.Result,
                    cancellationToken));
            var rankingItems = rankingTask.Result
                .Select((item, index) => ToListeningHistoryRankingDisplayItem(item, index))
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ReplaceCollection(ListeningHistory, timelineItems);
                ReplaceCollection(ListeningHistoryRanking, rankingItems);
                _lastListeningHistorySyncAt = DateTimeOffset.Now;

                RaiseListeningHistoryStateChanged();
                OnPropertyChanged(nameof(HasListeningHistoryRanking));
                OnPropertyChanged(nameof(ListeningHistorySyncStatus));
            });
        }
        catch (Exception ex)
        {
            ListeningHistoryLoadError = LocalizeUi(
                $"Không tải được lịch sử nghe: {ex.Message}",
                $"Could not load listening history: {ex.Message}",
                $"无法加载收听记录：{ex.Message}",
                $"청취 기록을 불러올 수 없습니다: {ex.Message}",
                $"Impossible de charger l'historique d'écoute : {ex.Message}");
        }
        finally
        {
            IsListeningHistoryLoading = false;
            OnPropertyChanged(nameof(ListeningHistorySummary));
            _listeningHistoryRefreshGate.Release();
        }
    }

    private async Task RefreshListeningHistoryAfterCreateAsync(Task<Guid?> historyTask, Guid optimisticHistoryId)
    {
        try
        {
            var historyId = await historyTask;
            if (historyId.HasValue)
            {
                RemoveOptimisticListeningHistoryItem(optimisticHistoryId);
                await RefreshListeningHistoryAsync();
            }
        }
        catch
        {
            // Ignore background refresh failures and keep narration flow responsive.
        }
    }

    private async Task<List<ListeningHistoryDisplayItem>> BuildListeningHistoryDisplayItemsAsync(
        IReadOnlyList<ListeningHistoryEntryDto> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return [];
        }

        var poiLookup = _pois.ToDictionary(item => item.Id);
        var missingPoiIds = entries
            .Select(item => item.PoiId)
            .Where(poiId => poiId != Guid.Empty && !poiLookup.ContainsKey(poiId))
            .Distinct()
            .ToList();

        if (missingPoiIds.Count > 0)
        {
            var lookupTasks = missingPoiIds
                .Select(async poiId => new
                {
                    PoiId = poiId,
                    Poi = await _poiRepository.GetPoiByIdAsync(poiId, cancellationToken)
                })
                .ToList();

            var results = await Task.WhenAll(lookupTasks);
            foreach (var result in results)
            {
                if (result.Poi is not null)
                {
                    poiLookup[result.PoiId] = result.Poi;
                }
            }
        }

        return entries
            .Select(item => ToListeningHistoryDisplayItem(
                item,
                poiLookup.TryGetValue(item.PoiId, out var poi) ? poi : null))
            .ToList();
    }

    private List<ListeningHistoryDisplayItem> MergeOptimisticListeningHistoryItems(
        IReadOnlyList<ListeningHistoryDisplayItem> syncedItems)
    {
        List<ListeningHistoryDisplayItem> pendingItems;
        lock (_optimisticListeningHistoryGate)
        {
            pendingItems = _optimisticListeningHistoryItems.ToList();
        }

        if (pendingItems.Count == 0)
        {
            return syncedItems.ToList();
        }

        var syncedIds = syncedItems
            .Select(item => item.Id)
            .ToHashSet();

        return pendingItems
            .Where(item => !syncedIds.Contains(item.Id))
            .Concat(syncedItems)
            .Take(15)
            .ToList();
    }

    private ListeningHistoryDisplayItem ToListeningHistoryDisplayItem(
        ListeningHistoryEntryDto item,
        POI? poi)
    {
        var startedAtLocal = item.StartedAtUtc.ToLocalTime();
        var triggerLabel = item.AutoTriggered || string.Equals(item.TriggerType, "GPS", StringComparison.OrdinalIgnoreCase)
            ? GetLocalizedHistoryTriggerLabel(true)
            : GetLocalizedHistoryTriggerLabel(false);
        var durationLabel = GetLocalizedHistoryDurationLabel(item.ListenSeconds);
        var languageLabel = GetLanguageDisplayName(item.Language);
        var playbackModeLabel = GetPlaybackModeLabel(item.PlaybackMode);

        var statusLabel = GetLocalizedHistoryStatusLabel(
            item.Completed,
            !string.IsNullOrWhiteSpace(item.ErrorMessage));

        var statusAccentColor = item.Completed
            ? "#15803D"
            : string.IsNullOrWhiteSpace(item.ErrorMessage)
                ? "#C2410C"
                : "#B91C1C";

        var summaryParts = new List<string>
        {
            languageLabel,
            playbackModeLabel,
            durationLabel
        };

        var detailParts = new List<string>
        {
            triggerLabel
        };

        if (!string.IsNullOrWhiteSpace(item.DevicePlatform))
        {
            detailParts.Add(item.DevicePlatform);
        }

        var address = string.IsNullOrWhiteSpace(item.PoiAddress)
            ? poi?.Address ?? string.Empty
            : item.PoiAddress;
        var description = string.IsNullOrWhiteSpace(item.PoiDescription)
            ? poi?.Description ?? string.Empty
            : item.PoiDescription;
        var specialDish = string.IsNullOrWhiteSpace(item.PoiSpecialDish)
            ? poi?.SpecialDish ?? string.Empty
            : item.PoiSpecialDish;
        var imageSource = string.IsNullOrWhiteSpace(item.PoiImageSource)
            ? poi?.ImageSource ?? string.Empty
            : item.PoiImageSource;
        var mapLink = string.IsNullOrWhiteSpace(item.PoiMapLink)
            ? poi?.MapLink ?? string.Empty
            : item.PoiMapLink;
        var narrationSnapshot = string.IsNullOrWhiteSpace(item.NarrationSnapshot)
            ? poi?.GetNarrationText(item.Language) ?? string.Empty
            : item.NarrationSnapshot;

        return new ListeningHistoryDisplayItem
        {
            Id = item.Id,
            PoiId = item.PoiId,
            PoiCode = item.PoiCode,
            PoiName = item.PoiName,
            Address = address,
            Description = string.IsNullOrWhiteSpace(description)
                ? GetLocalizedHistoryDescriptionFallback()
                : description,
            SpecialDish = specialDish,
            ImageSource = imageSource,
            MapLink = mapLink,
            Language = item.Language,
            LanguageLabel = languageLabel,
            PlaybackMode = item.PlaybackMode,
            PlaybackModeLabel = playbackModeLabel,
            NarrationSnapshot = narrationSnapshot,
            AudioAssetPath = item.AudioAssetPath,
            NarrationPreview = BuildNarrationPreview(narrationSnapshot),
            StartedAtLabel = startedAtLocal.ToString("dd/MM/yyyy HH:mm:ss"),
            StartedAtShortLabel = startedAtLocal.ToString("HH:mm"),
            DetailSummaryLabel = string.Join(" • ", summaryParts.Where(part => !string.IsNullOrWhiteSpace(part))),
            DetailLabel = string.Join(" • ", detailParts.Where(part => !string.IsNullOrWhiteSpace(part))),
            StatusLabel = statusLabel,
            StatusAccentColor = statusAccentColor,
            ErrorMessage = item.ErrorMessage
        };
    }

    private static ListeningHistoryRankingDisplayItem ToListeningHistoryRankingDisplayItem(
        PoiListeningCountDto item,
        int index)
    {
        var completionRate = item.ListenCount == 0
            ? 0
            : (int)Math.Round(item.CompletedCount * 100.0 / item.ListenCount);

        return new ListeningHistoryRankingDisplayItem
        {
            Rank = index + 1,
            PoiCode = item.PoiCode,
            PoiName = item.PoiName,
            SummaryLabel = $"{item.ListenCount} lượt nghe • {item.TotalListenSeconds} giây • {completionRate}% hoàn tất",
            LastStartedAtLabel = item.LastStartedAtUtc.HasValue
                ? item.LastStartedAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : "--"
        };
    }

    private static string MapListeningHistorySortToApi(string? selectedSort)
    {
        return string.Equals(selectedSort, "Cũ nhất trước", StringComparison.Ordinal)
            ? "time_asc"
            : "time_desc";
    }

    private static string MapListeningHistoryPeriodToApi(string? selectedPeriod)
    {
        return selectedPeriod switch
        {
            "24 giờ qua" => "day",
            "7 ngày qua" => "week",
            "30 ngày qua" => "month",
            _ => "all"
        };
    }

    private string GetPlaybackModeLabel(string? playbackMode)
    {
        return playbackMode?.Trim().ToLowerInvariant() switch
        {
            "audio" => LocalizeUi("Bản thu sẵn", "Recorded audio", "录音", "녹음 음성", "Audio enregistré"),
            _ => LocalizeUi("Giọng đọc", "Voice narration", "语音讲解", "음성 안내", "Voix de narration")
        };
    }

    private static void ReplaceCollection<T>(
        ObservableCollection<T> collection,
        IReadOnlyList<T> items)
    {
        collection.Clear();

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private void SyncCurrentUser()
    {
        var session = _authService.CurrentSession;
        var loginId = session?.LoginId ?? "guest";
        var isGuestAccess = IsGuestAccess(session);
        var hasPersonalName = !string.IsNullOrWhiteSpace(session?.FullName);

        CurrentUserDisplayName = hasPersonalName
            ? session!.FullName
            : LocalizeUi("Bạn", "Guest", "访客", "방문객", "Visiteur");
        CurrentUserInitials = hasPersonalName ? session!.Initials : "VK";
        CurrentUserAccountLabel = hasPersonalName
            ? session!.FullName
            : LocalizeUi("Chưa cập nhật tên", "Name not updated", "未更新姓名", "이름 미등록", "Nom non renseigné");
        CurrentUserPasswordLabel = isGuestAccess
            ? GetRoleLabel("guest")
            : string.IsNullOrWhiteSpace(loginId)
                ? AccountStatusValue
                : $"@{loginId}";
        CurrentUserStatusLine = isGuestAccess
            ? $"{AccountStatusValue} • {GetRoleLabel("guest")}"
            : $"{AccountStatusValue} • {GetRoleLabel(session!.Role)}";
        LoadAccountProfileEditor(clearFeedback: true);
        OnPropertyChanged(nameof(CanManageAccountProfile));
        OnPropertyChanged(nameof(CanSaveAccountProfile));
        OnPropertyChanged(nameof(AccountSettingsAccessMessage));
        OnPropertyChanged(nameof(CurrentAudioSettingsSummary));
        OnPropertyChanged(nameof(AudioSettingsQuickActionText));
        OnPropertyChanged(nameof(NarrationModeDisplay));
        OnPropertyChanged(nameof(NarrationModeDescription));
        (SaveAccountProfileCommand as Command)?.ChangeCanExecute();
        (ResetAccountProfileCommand as Command)?.ChangeCanExecute();
        UpdateAudioSettingsCommandStates();
        OnPropertyChanged(nameof(HomeNarrationSummary));
        RaiseLocalizedUiChanged();
    }

    public void ResetAccountProfileEditor()
    {
        LoadAccountProfileEditor(clearFeedback: true);
    }

    public void ResetAudioSettingsDraft()
    {
        _draftSelectedLanguage = SelectedLanguage;
        _draftSelectedPlaybackMode = SelectedPlaybackMode;
        _draftIsAutoNarrationEnabled = IsAutoNarrationEnabled;

        OnPropertyChanged(nameof(DraftSelectedLanguage));
        OnPropertyChanged(nameof(DraftSelectedLanguageDisplayName));
        OnPropertyChanged(nameof(DraftSelectedLanguageDisplayLabel));
        OnPropertyChanged(nameof(DraftSelectedLanguageOption));
        OnPropertyChanged(nameof(DraftSelectedPlaybackMode));
        OnPropertyChanged(nameof(DraftPlaybackModeDisplay));
        OnPropertyChanged(nameof(DraftSelectedPlaybackModeOption));
        OnPropertyChanged(nameof(DraftPlaybackModeDescription));
        OnPropertyChanged(nameof(DraftIsAutoNarrationEnabled));
        OnPropertyChanged(nameof(DraftAudioSettingsSummary));
        OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));

        ClearAudioSettingsFeedback();
        UpdateAudioSettingsCommandStates();
    }

    private async Task SaveAccountProfileAsync()
    {
        if (IsSavingAccountProfile)
        {
            return;
        }

        ClearAccountSettingsFeedback();

        var request = new AccountProfileUpdateRequest
        {
            FullName = AccountProfileFullName,
            Email = AccountProfileEmail,
            PhoneNumber = AccountProfilePhoneNumber
        };

        var validationResult = _accountProfileValidationService.Validate(request);
        if (!validationResult.IsValid)
        {
            AccountSettingsErrorMessage = validationResult.ErrorMessage;
            return;
        }

        IsSavingAccountProfile = true;

        try
        {
            var result = await _authService.UpdateCurrentUserProfileAsync(request);
            if (!result.Succeeded)
            {
                AccountSettingsErrorMessage = result.Message;
                return;
            }

            var synced = await _userProfileSyncService.SyncCurrentUserAsync(GetPreferredLanguageLocaleCode(SelectedLanguage));
            AccountSettingsSuccessMessage = synced
                ? LocalizeUi(
                    "Đã lưu thông tin khách.",
                    "Visitor info saved.",
                    "访客信息已保存。",
                    "방문객 정보가 저장되었습니다.",
                    "Informations visiteur enregistrées.")
                : LocalizeUi(
                    "Đã lưu trên thiết bị. Khi có mạng ổn định, app sẽ cập nhật lại.",
                    "Saved on this device. The app will update again when the connection is stable.",
                    "访客信息已保存。",
                    "기기에 저장되었습니다. 연결이 안정되면 다시 업데이트됩니다.",
                    "Enregistré sur cet appareil. L'app mettra à jour quand la connexion sera stable.");
            AddLog($"{NowLabel()} Cập nhật hồ sơ tài khoản");
        }
        finally
        {
            IsSavingAccountProfile = false;
        }
    }

    private async Task PreviewAudioSettingsAsync()
    {
        if (!CanPreviewAudioSettings)
        {
            return;
        }

        ClearAudioSettingsFeedback();
        IsPreviewingAudioSettings = true;

        try
        {
            await StopNarrationAsync();

            var previewPoi = GetAudioPreviewPoi();
            var playbackRequest = ResolvePlaybackRequest(
                DraftSelectedPlaybackMode,
                previewPoi?.AudioAssetPath,
                allowAudioFallback: false);

            if (!string.IsNullOrWhiteSpace(playbackRequest.FallbackMessage))
            {
                AudioSettingsErrorMessage = playbackRequest.FallbackMessage;
                return;
            }

            var previewText = BuildAudioSettingsPreviewText(DraftSelectedLanguage, previewPoi);
            await _narrationService.SpeakAsync(
                previewText,
                DraftSelectedLanguage,
                playbackRequest.PlaybackMode,
                playbackRequest.AudioAssetPath);

            AudioSettingsSuccessMessage =
                $"Đã phát nghe thử bằng {DraftSelectedLanguageDisplayName}.";
        }
        catch (Exception ex)
        {
            AudioSettingsErrorMessage = "Chưa thể nghe thử lúc này. Hãy kiểm tra âm lượng hoặc thử lại sau.";
            AddLog($"{NowLabel()} Lỗi nghe thử: {ex.Message}");
        }
        finally
        {
            IsPreviewingAudioSettings = false;
        }
    }

    private async Task SaveAudioSettingsAsync()
    {
        if (!CanSaveAudioSettings)
        {
            return;
        }

        ClearAudioSettingsFeedback();
        IsSavingAudioSettings = true;

        try
        {
            var settings = new AudioSettingsState
            {
                LanguageCode = DraftSelectedLanguage,
                PlaybackMode = DraftSelectedPlaybackMode,
                AutoNarrationEnabled = DraftIsAutoNarrationEnabled
            };

            ApplyAudioSettings(settings, shouldLog: true);
            PersistUserPreferences();
            var synced = await _userProfileSyncService.SyncCurrentUserAsync(GetPreferredLanguageLocaleCode(DraftSelectedLanguage));
            AudioSettingsSuccessMessage = synced
                ? GetLocalizedLanguageSavedMessage()
                : $"{GetLocalizedLanguageSavedMessage()} {LocalizeUi("App sẽ cập nhật lại khi kết nối ổn định.", "The app will update again when the connection is stable.", "连接稳定后会再次更新。", "연결이 안정되면 다시 업데이트됩니다.", "L'app mettra à jour quand la connexion sera stable.")}";
        }
        finally
        {
            IsSavingAudioSettings = false;
        }

    }

    private void LoadAccountProfileEditor(bool clearFeedback)
    {
        var session = _authService.CurrentSession;

        _accountProfileFullName = session?.FullName ?? string.Empty;
        _accountProfileEmail = session?.Email ?? string.Empty;
        _accountProfilePhoneNumber = session?.PhoneNumber ?? string.Empty;

        OnPropertyChanged(nameof(AccountProfileFullName));
        OnPropertyChanged(nameof(AccountProfileEmail));
        OnPropertyChanged(nameof(AccountProfilePhoneNumber));

        if (clearFeedback)
        {
            ClearAccountSettingsFeedback();
        }
    }

    private void ClearAccountSettingsFeedback()
    {
        AccountSettingsErrorMessage = string.Empty;
        AccountSettingsSuccessMessage = string.Empty;
    }

    private void ClearAudioSettingsFeedback()
    {
        AudioSettingsErrorMessage = string.Empty;
        AudioSettingsSuccessMessage = string.Empty;
    }

    private async Task DownloadOfflinePackageAsync()
    {
        if (!CanDownloadOfflinePackage)
        {
            return;
        }

        ClearOfflinePackageFeedback();
        IsSyncingOfflinePackage = true;

        try
        {
            var pois = await _poiRepository.GetPoisAsync();
            var mapResult = await _mapOfflineTileService.PrefetchAsync();
            var audioResult = await _audioAssetCacheService.PrefetchAsync(pois);

            await RefreshOfflinePackageStatusAsync();

            var successMessage =
                $"Đã lưu dữ liệu offline: {_offlineContentStatus.PoiCount} quán, bản đồ {mapResult.CachedTileCount}/{mapResult.PlannedTileCount}, bản thu {audioResult.CachedAssetCount}/{audioResult.AvailableAssetCount}.";

            var notes = new List<string>();
            if (mapResult.FailedTileCount > 0)
            {
                notes.Add($"Còn {mapResult.FailedTileCount} phần bản đồ chưa tải được");
            }

            if (audioResult.FailedAssetCount > 0)
            {
                notes.Add($"Còn {audioResult.FailedAssetCount} bản thu chưa tải được");
            }

            if (audioResult.AvailableAssetCount == 0)
            {
                notes.Add("Các quán hiện chưa có bản thu sẵn để tải");
            }

            OfflinePackageSuccessMessage = notes.Count == 0
                ? successMessage
                : $"{successMessage} {string.Join(". ", notes)}.";

            AddLog($"{NowLabel()} Cập nhật gói offline trên thiết bị");
        }
        catch (Exception ex)
        {
            OfflinePackageErrorMessage = "Chưa thể cập nhật dữ liệu offline. Hãy kiểm tra kết nối rồi thử lại.";
            AddLog($"{NowLabel()} Lỗi cập nhật offline: {ex.Message}");
        }
        finally
        {
            IsSyncingOfflinePackage = false;
        }
    }

    private async Task ClearOfflinePackageAsync()
    {
        if (!CanClearOfflinePackage)
        {
            return;
        }

        ClearOfflinePackageFeedback();
        IsSyncingOfflinePackage = true;

        try
        {
            await _mapOfflineTileService.ClearAsync();
            await _audioAssetCacheService.ClearAsync();
            await _poiOfflineStore.ClearAsync();
            await RefreshOfflinePackageStatusAsync();

            OfflinePackageSuccessMessage = LocalizeUi(
                "Đã xóa gói offline trên thiết bị.",
                "Offline package cleared on this device.",
                "已清除此设备上的离线包。",
                "이 기기의 오프라인 패키지를 삭제했습니다.",
                "Le pack hors ligne a été supprimé de cet appareil.");
            AddLog($"{NowLabel()} Xóa gói offline trên thiết bị");
        }
        catch (Exception ex)
        {
            OfflinePackageErrorMessage = "Chưa thể xóa dữ liệu offline. Vui lòng thử lại.";
            AddLog($"{NowLabel()} Lỗi xóa offline: {ex.Message}");
        }
        finally
        {
            IsSyncingOfflinePackage = false;
        }
    }

    private async Task RefreshOfflinePackageStatusAsync(CancellationToken cancellationToken = default)
    {
        var offlinePois = await _poiOfflineStore.GetPoisAsync(cancellationToken);
        var lastSyncedAt = await _poiOfflineStore.GetLastSyncedAtAsync(cancellationToken);
        var mapStatus = await _mapOfflineTileService.GetStatusAsync(cancellationToken);
        var audioStatus = await _audioAssetCacheService.GetStatusAsync(
            _pois.Count > 0 ? _pois : offlinePois,
            cancellationToken);

        _offlineContentStatus = new OfflineContentStatus
        {
            PoiCount = offlinePois.Count,
            LastSyncedAtUtc = lastSyncedAt
        };
        _offlineMapStatus = mapStatus;
        _audioCacheStatus = audioStatus;

        RaiseOfflinePackageStateChanged();
    }

    private async Task RefreshOfflinePackageStatusSafeAsync()
    {
        try
        {
            await RefreshOfflinePackageStatusAsync();
        }
        catch
        {
        }
    }

    private void ClearOfflinePackageFeedback()
    {
        OfflinePackageErrorMessage = string.Empty;
        OfflinePackageSuccessMessage = string.Empty;
    }

    private void LoadPersistedState()
    {
        LoadUserPreferences();
        LoadPersistedHistory(EventLogs, UsageHistoryCategory.Activity);
        LoadPersistedHistory(ListeningHistoryLocalEntries, UsageHistoryCategory.Listening);
        LoadPersistedHistory(ViewHistory, UsageHistoryCategory.Viewing);
        RaiseEventLogStateChanged();
        RaiseListeningHistoryStateChanged();
        RaiseViewHistoryStateChanged();
        TriggerListeningHistoryRefresh();
        _ = RefreshOfflinePackageStatusSafeAsync();
    }

    private ListeningHistoryDisplayItem? FindListeningHistoryItem(Guid historyId)
    {
        return ListeningHistory.FirstOrDefault(item => item.Id == historyId);
    }

    private async Task<POI?> ResolvePoiForListeningHistoryAsync(
        ListeningHistoryDisplayItem item,
        CancellationToken cancellationToken = default)
    {
        POI? basePoi = null;

        if (item.PoiId != Guid.Empty)
        {
            basePoi = _pois.FirstOrDefault(poi => poi.Id == item.PoiId)
                ?? await _poiRepository.GetPoiByIdAsync(item.PoiId, cancellationToken);
        }

        return BuildPoiFromListeningHistoryItem(item, basePoi);
    }

    private static POI? BuildPoiFromListeningHistoryItem(
        ListeningHistoryDisplayItem item,
        POI? basePoi)
    {
        var poiId = item.PoiId != Guid.Empty
            ? item.PoiId
            : basePoi?.Id ?? Guid.Empty;

        if (poiId == Guid.Empty &&
            string.IsNullOrWhiteSpace(item.PoiName) &&
            string.IsNullOrWhiteSpace(basePoi?.Name))
        {
            return null;
        }

        var narrationText = string.IsNullOrWhiteSpace(item.NarrationSnapshot)
            ? basePoi?.GetNarrationText(item.Language) ?? string.Empty
            : item.NarrationSnapshot;

        return new POI
        {
            Id = poiId == Guid.Empty ? Guid.NewGuid() : poiId,
            Name = string.IsNullOrWhiteSpace(item.PoiName) ? basePoi?.Name ?? "Quán đã nghe" : item.PoiName,
            Category = basePoi?.Category ?? "Ẩm thực",
            Description = string.IsNullOrWhiteSpace(item.Description)
                ? basePoi?.Description ?? string.Empty
                : item.Description,
            SpecialDish = string.IsNullOrWhiteSpace(item.SpecialDish)
                ? basePoi?.SpecialDish ?? string.Empty
                : item.SpecialDish,
            ImageSource = string.IsNullOrWhiteSpace(item.ImageSource)
                ? basePoi?.ImageSource ?? string.Empty
                : item.ImageSource,
            MapLink = string.IsNullOrWhiteSpace(item.MapLink)
                ? basePoi?.MapLink ?? string.Empty
                : item.MapLink,
            NarrationText = narrationText,
            AudioAssetPath = string.IsNullOrWhiteSpace(item.AudioAssetPath)
                ? basePoi?.AudioAssetPath ?? string.Empty
                : item.AudioAssetPath,
            Priority = basePoi?.Priority ?? 1,
            Latitude = basePoi?.Latitude ?? 0,
            Longitude = basePoi?.Longitude ?? 0,
            TriggerRadiusMeters = basePoi?.TriggerRadiusMeters ?? 50,
            CooldownMinutes = basePoi?.CooldownMinutes ?? 5,
            IsActive = basePoi?.IsActive ?? true,
            NarrationTranslations = !string.IsNullOrWhiteSpace(item.Language) &&
                !string.IsNullOrWhiteSpace(narrationText)
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [item.Language] = narrationText
                }
                : new Dictionary<string, string>(
                    basePoi?.NarrationTranslations ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase)
        };
    }

    private void ClearEventLogs()
    {
        EventLogs.Clear();
        _usageHistoryService.ClearEntries(UsageHistoryCategory.Activity);
        RaiseEventLogStateChanged();
    }

    public async Task<bool> ClearListeningHistoryAsync()
    {
        if (IsClearingListeningHistory)
        {
            return false;
        }

        IsClearingListeningHistory = true;

        try
        {
            var needsRemoteDelete = HasListeningHistory || HasListeningHistoryRanking;
            var cleared = !needsRemoteDelete || await _listeningHistorySyncService.DeleteCurrentUserHistoryAsync();
            if (!cleared)
            {
                ListeningHistoryLoadError = LocalizeUi(
                    "Không xóa được toàn bộ lịch sử nghe lúc này.",
                    "Could not clear all listening history right now.",
                    "暂时无法清除全部收听记录。",
                    "지금은 청취 기록을 모두 삭제할 수 없습니다.",
                    "Impossible d'effacer tout l'historique pour le moment.");
                return false;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ListeningHistory.Clear();
                ListeningHistoryLocalEntries.Clear();
                ListeningHistoryRanking.Clear();
                lock (_optimisticListeningHistoryGate)
                {
                    _optimisticListeningHistoryItems.Clear();
                }

                _usageHistoryService.ClearEntries(UsageHistoryCategory.Listening);
                ListeningHistoryLoadError = string.Empty;
                _lastListeningHistorySyncAt = null;
                RaiseListeningHistoryStateChanged();
                OnPropertyChanged(nameof(HasListeningHistoryRanking));
                OnPropertyChanged(nameof(ListeningHistorySyncStatus));
            });

            StatusText = LocalizeUi(
                "Đã xóa toàn bộ lịch sử nghe.",
                "All listening history has been cleared.",
                "已清除全部收听记录。",
                "청취 기록을 모두 삭제했습니다.",
                "Tout l'historique d'écoute a été effacé.");
            return true;
        }
        finally
        {
            IsClearingListeningHistory = false;
        }
    }

    private void ClearViewHistory()
    {
        ViewHistory.Clear();
        _usageHistoryService.ClearEntries(UsageHistoryCategory.Viewing);
        RaiseViewHistoryStateChanged();
    }

    private void RaiseEventLogStateChanged()
    {
        OnPropertyChanged(nameof(HasEventLogs));
        OnPropertyChanged(nameof(IsEventLogEmpty));
        OnPropertyChanged(nameof(EventLogSummary));
        (ClearEventLogsCommand as Command)?.ChangeCanExecute();
    }

    private void RaiseListeningHistoryStateChanged()
    {
        OnPropertyChanged(nameof(HasListeningHistory));
        OnPropertyChanged(nameof(IsListeningHistoryEmpty));
        OnPropertyChanged(nameof(HasListeningHistoryLocalEntries));
        OnPropertyChanged(nameof(IsListeningHistoryFallbackVisible));
        OnPropertyChanged(nameof(IsListeningHistoryDisplayEmpty));
        OnPropertyChanged(nameof(HasListeningHistoryRanking));
        OnPropertyChanged(nameof(ListeningHistoryFallbackSummary));
        OnPropertyChanged(nameof(ListeningHistorySummary));
        OnPropertyChanged(nameof(ListeningHistoryPreviewItems));
        OnPropertyChanged(nameof(HasListeningHistoryPreview));
        OnPropertyChanged(nameof(CanClearListeningHistory));
        (ClearListeningHistoryCommand as Command)?.ChangeCanExecute();
        (RefreshListeningHistoryCommand as Command)?.ChangeCanExecute();
    }

    private void RaiseViewHistoryStateChanged()
    {
        OnPropertyChanged(nameof(HasViewHistory));
        OnPropertyChanged(nameof(IsViewHistoryEmpty));
        OnPropertyChanged(nameof(ViewHistorySummary));
        (ClearViewHistoryCommand as Command)?.ChangeCanExecute();
    }

    private void AddListeningHistory(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        AddHistoryEntry(
            ListeningHistoryLocalEntries,
            UsageHistoryCategory.Listening,
            message,
            RaiseListeningHistoryStateChanged);
    }

    private Guid AddOptimisticListeningHistory(
        POI poi,
        string language,
        string playbackMode,
        bool autoTriggered)
    {
        var startedAt = DateTimeOffset.Now;
        var narrationSnapshot = poi.GetNarrationText(language);
        var languageLabel = GetLanguageDisplayName(language);
        var playbackModeLabel = GetPlaybackModeLabel(playbackMode);
        var detailSummaryParts = new[]
        {
            languageLabel,
            playbackModeLabel,
            GetLocalizedHistoryDurationLabel(0)
        };

        var displayItem = new ListeningHistoryDisplayItem
        {
            Id = Guid.NewGuid(),
            PoiId = poi.Id,
            PoiCode = poi.Code,
            PoiName = poi.Name,
            Address = poi.Address,
            Description = string.IsNullOrWhiteSpace(poi.Description)
                ? GetLocalizedHistoryDescriptionFallback()
                : poi.Description,
            SpecialDish = poi.SpecialDish,
            ImageSource = poi.ImageSource,
            MapLink = poi.MapLink,
            Language = language,
            LanguageLabel = languageLabel,
            PlaybackMode = playbackMode,
            PlaybackModeLabel = playbackModeLabel,
            NarrationSnapshot = narrationSnapshot,
            AudioAssetPath = poi.AudioAssetPath,
            NarrationPreview = BuildNarrationPreview(narrationSnapshot),
            StartedAtLabel = startedAt.ToString("dd/MM/yyyy HH:mm:ss"),
            StartedAtShortLabel = startedAt.ToString("HH:mm"),
            DetailSummaryLabel = string.Join(" • ", detailSummaryParts.Where(part => !string.IsNullOrWhiteSpace(part))),
            DetailLabel = GetLocalizedHistoryTriggerLabel(autoTriggered),
            StatusLabel = LocalizeUi(
                "Đang đồng bộ",
                "Syncing",
                "正在同步",
                "동기화 중",
                "Synchronisation"),
            StatusAccentColor = "#2F80FF"
        };

        lock (_optimisticListeningHistoryGate)
        {
            _optimisticListeningHistoryItems.Insert(0, displayItem);

            while (_optimisticListeningHistoryItems.Count > 15)
            {
                _optimisticListeningHistoryItems.RemoveAt(_optimisticListeningHistoryItems.Count - 1);
            }
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ListeningHistoryLoadError = string.Empty;

            if (ListeningHistory.All(item => item.Id != displayItem.Id))
            {
                ListeningHistory.Insert(0, displayItem);
            }

            while (ListeningHistory.Count > 15)
            {
                ListeningHistory.RemoveAt(ListeningHistory.Count - 1);
            }

            RaiseListeningHistoryStateChanged();
        });

        return displayItem.Id;
    }

    private void RemoveOptimisticListeningHistoryItem(Guid historyId)
    {
        lock (_optimisticListeningHistoryGate)
        {
            _optimisticListeningHistoryItems.RemoveAll(item => item.Id == historyId);
        }
    }

    private void AddViewHistory(string message)
    {
        AddHistoryEntry(ViewHistory, UsageHistoryCategory.Viewing, message, RaiseViewHistoryStateChanged);
    }

    private void AddHistoryEntry(
        ObservableCollection<string> targetCollection,
        UsageHistoryCategory category,
        string message,
        Action stateChangedCallback)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            targetCollection.Insert(0, message);
            _usageHistoryService.AppendEntry(category, message);

            while (targetCollection.Count > 15)
            {
                targetCollection.RemoveAt(targetCollection.Count - 1);
            }

            stateChangedCallback();
        });
    }

    private void LoadPersistedHistory(
        ObservableCollection<string> targetCollection,
        UsageHistoryCategory category)
    {
        targetCollection.Clear();

        foreach (var entry in _usageHistoryService.LoadEntries(category).Take(15))
        {
            targetCollection.Add(entry);
        }
    }

    private void LoadUserPreferences()
    {
        _isRestoringUserPreferences = true;

        try
        {
            var settings = _audioSettingsService.Load(GetCurrentUserPreferencePrefix());
            ApplyAudioSettings(settings, shouldLog: false);
        }
        finally
        {
            _isRestoringUserPreferences = false;
        }

        ResetAudioSettingsDraft();
        RefreshNarrationPresentation();
        OnPropertyChanged(nameof(SelectedLanguageDisplayName));
        OnPropertyChanged(nameof(AudioSettingsSummary));
        OnPropertyChanged(nameof(HomeNarrationAvailabilityText));
    }

    private void PersistUserPreferences()
    {
        if (_isRestoringUserPreferences)
        {
            return;
        }

        _audioSettingsService.Save(
            GetCurrentUserPreferencePrefix(),
            new AudioSettingsState
            {
                LanguageCode = SelectedLanguage,
                PlaybackMode = SelectedPlaybackMode,
                AutoNarrationEnabled = IsAutoNarrationEnabled
            });
    }

    private string GetCurrentUserPreferencePrefix()
    {
        return UserPreferenceScope.BuildAudioSettingsPrefix(_authService.CurrentSession?.ScopeKey);
    }

    private void ApplyAudioSettings(AudioSettingsState settings, bool shouldLog)
    {
        var normalizedLanguage = NormalizeLanguageCode(settings.LanguageCode);
        var normalizedPlaybackMode = NormalizePlaybackMode(settings.PlaybackMode);
        var languageChanged = !string.Equals(_selectedLanguage, normalizedLanguage, StringComparison.OrdinalIgnoreCase);
        var playbackModeChanged = !string.Equals(_selectedPlaybackMode, normalizedPlaybackMode, StringComparison.OrdinalIgnoreCase);
        var autoNarrationChanged = _isAutoNarrationEnabled != settings.AutoNarrationEnabled;

        SelectedLanguage = normalizedLanguage;
        SelectedPlaybackMode = normalizedPlaybackMode;
        IsAutoNarrationEnabled = settings.AutoNarrationEnabled;

        if (!shouldLog || _isRestoringUserPreferences)
        {
            return;
        }

        if (languageChanged)
        {
            AddLog($"{NowLabel()} Chuyển ngôn ngữ sang {SelectedLanguageDisplayName}");
        }

        if (playbackModeChanged)
        {
            AddLog($"{NowLabel()} Chuyển chế độ phát sang {NarrationModeDisplay}");
        }

        if (autoNarrationChanged)
        {
            AddLog($"{NowLabel()} {(IsAutoNarrationEnabled ? "Bật" : "Tắt")} tự động phát khi vào vùng");
        }
    }

    private void UpdateAudioSettingsCommandStates()
    {
        OnPropertyChanged(nameof(CanSaveAudioSettings));
        OnPropertyChanged(nameof(CanPreviewAudioSettings));
        OnPropertyChanged(nameof(CanResetAudioSettings));
        (PreviewAudioSettingsCommand as Command)?.ChangeCanExecute();
        (SaveAudioSettingsCommand as Command)?.ChangeCanExecute();
        (ResetAudioSettingsCommand as Command)?.ChangeCanExecute();
    }

    private void RaiseOfflinePackageStateChanged()
    {
        OnPropertyChanged(nameof(CanClearOfflinePackage));
        OnPropertyChanged(nameof(OfflineContentSummary));
        OnPropertyChanged(nameof(OfflineMapSummary));
        OnPropertyChanged(nameof(OfflineMapDetail));
        OnPropertyChanged(nameof(OfflineAudioSummary));
        OnPropertyChanged(nameof(OfflineAudioDetail));
        OnPropertyChanged(nameof(HasOfflineSnapshotNotice));
        OnPropertyChanged(nameof(OfflineSnapshotNoticeText));
        UpdateOfflinePackageCommandStates();
    }

    private void UpdateOfflinePackageCommandStates()
    {
        OnPropertyChanged(nameof(CanDownloadOfflinePackage));
        OnPropertyChanged(nameof(CanClearOfflinePackage));
        (DownloadOfflinePackageCommand as Command)?.ChangeCanExecute();
        (ClearOfflinePackageCommand as Command)?.ChangeCanExecute();
    }

    private POI? GetAudioPreviewPoi()
    {
        return _selectedPoi ?? _pois.FirstOrDefault();
    }

    private string BuildAudioSettingsPreviewText(string languageCode, POI? previewPoi)
    {
        if (previewPoi is not null)
        {
            return previewPoi.GetNarrationText(languageCode);
        }

        return languageCode.Trim().ToLowerInvariant() switch
        {
            "en" => "Hello. This is an English voice preview for Vinh Khanh Food Street.",
            "zh" => "您好，這是永慶美食街的中文語音試聽。",
            "ko" => "안녕하세요. 빈칸 음식 거리의 한국어 음성 미리 듣기입니다.",
            "fr" => "Bonjour. Ceci est un aperçu audio en français pour la rue gastronomique Vinh Khanh.",
            _ => "Xin chào. Đây là bản nghe thử âm thanh cho phố ẩm thực Vĩnh Khánh."
        };
    }

    private PlaybackRequest ResolvePlaybackRequest(
        string? preferredPlaybackMode,
        string? audioAssetPath,
        bool allowAudioFallback)
    {
        var normalizedPlaybackMode = NormalizePlaybackMode(preferredPlaybackMode);
        if (!string.Equals(normalizedPlaybackMode, "audio", StringComparison.OrdinalIgnoreCase))
        {
            return new PlaybackRequest("tts", null, null);
        }

        if (!string.IsNullOrWhiteSpace(audioAssetPath))
        {
            return new PlaybackRequest("audio", audioAssetPath.Trim(), null);
        }

        return allowAudioFallback
            ? new PlaybackRequest(
                "tts",
                null,
                "Quán này chưa có bản thu sẵn, app sẽ dùng giọng đọc để tiếp tục.")
            : new PlaybackRequest(
                "audio",
                null,
                "Quán này chưa có bản thu sẵn để nghe thử. Hãy chọn giọng đọc hoặc thử quán khác.");
    }

    private bool IsSupportedLanguage(string? languageCode)
    {
        return SupportedLanguages.Any(item =>
            string.Equals(item.Code, languageCode, StringComparison.OrdinalIgnoreCase));
    }

    private int CountFeaturedDishes(string categoryKey)
    {
        return _featuredDishCatalog.Count(item =>
            string.Equals(item.CategoryKey, categoryKey, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<FeaturedDishItem> CreateFeaturedDishCatalog()
    {
        return
        [
            new()
            {
                CategoryKey = "bo",
                CategoryName = "Bò",
                Name = "Sườn bò nướng",
                StartingPrice = "89,000 VND",
                ImageSource = "suon_bo_nuong.jpg",
                ShortDescription = "Phần sườn bò nướng đậm vị, phù hợp cho khách thích món nướng."
            },
            new()
            {
                CategoryKey = "bo",
                CategoryName = "Bò",
                Name = "Bò nướng miếng",
                StartingPrice = "79,000 VND",
                ImageSource = "bo_nuong_mieng.jpg",
                ShortDescription = "Bò nướng cắt miếng dễ dùng, hợp để gọi chia sẻ theo nhóm."
            },
            new()
            {
                CategoryKey = "bo",
                CategoryName = "Bò",
                Name = "Bò nướng lá lốt",
                StartingPrice = "79,000 VND",
                ImageSource = "bo_nuong_la_lot.jpg",
                ShortDescription = "Món bò cuốn lá lốt thơm mùi đặc trưng, phù hợp khách thích vị truyền thống."
            },
            new()
            {
                CategoryKey = "lau",
                CategoryName = "Lẩu",
                Name = "Lẩu Thái",
                StartingPrice = "199,000 VND",
                ImageSource = "lau_thai.jpg",
                ShortDescription = "Nước lẩu chua cay kiểu Thái, hợp nhóm khách thích vị đậm và nóng."
            },
            new()
            {
                CategoryKey = "lau",
                CategoryName = "Lẩu",
                Name = "Lẩu Hàn Quốc",
                StartingPrice = "199,000 VND",
                ImageSource = "lau_han_quoc.jpg",
                ShortDescription = "Lẩu cay phong cách Hàn Quốc với topping phong phú và dễ gọi theo nhóm."
            },
            new()
            {
                CategoryKey = "oc",
                CategoryName = "Ốc",
                Name = "Ốc hương sốt trứng muối",
                StartingPrice = "79,000 VND",
                ImageSource = "oc_huong_sot_trung_muoi.jpg",
                ShortDescription = "Ốc hương phủ sốt trứng muối béo mặn, là món nổi bật dễ thu hút khách mới."
            },
            new()
            {
                CategoryKey = "oc",
                CategoryName = "Ốc",
                Name = "Ốc nướng mỡ hành",
                StartingPrice = "69,000 VND",
                ImageSource = "oc_nuong_mo_hanh.jpg",
                ShortDescription = "Ốc nướng mỡ hành quen vị, dễ ăn và có mức giá khởi điểm nhẹ hơn."
            },
            new()
            {
                CategoryKey = "cua",
                CategoryName = "Cua",
                Name = "Cua Hoàng đế",
                StartingPrice = "299,000 VND",
                ImageSource = "cua_hoang_de.jpg",
                ShortDescription = "Món cua cao cấp nổi bật, phù hợp nhóm khách muốn trải nghiệm đặc biệt."
            },
            new()
            {
                CategoryKey = "cua",
                CategoryName = "Cua",
                Name = "Cua Cà Mau",
                StartingPrice = "99,000 VND",
                ImageSource = "cua_ca_mau.jpg",
                ShortDescription = "Cua Cà Mau là lựa chọn dễ tiếp cận hơn với mức giá mở đầu rõ ràng."
            }
        ];
    }

    private static string NormalizeFeaturedDishCategoryKey(string? categoryKey)
    {
        return categoryKey?.Trim().ToLowerInvariant() switch
        {
            "lau" => "lau",
            "oc" => "oc",
            "cua" => "cua",
            _ => "bo"
        };
    }

    private string NormalizeLanguageCode(string? languageCode)
    {
        return IsSupportedLanguage(languageCode)
            ? languageCode!.Trim().ToLowerInvariant()
            : "vi";
    }

    private string NormalizePlaybackMode(string? playbackMode)
    {
        return SupportedPlaybackModes.Any(item =>
            string.Equals(item.Code, playbackMode, StringComparison.OrdinalIgnoreCase))
            ? playbackMode!.Trim().ToLowerInvariant()
            : DefaultNarrationMode;
    }

    private bool IsGuestAccess(AuthSession? session = null)
    {
        var currentSession = session ?? _authService.CurrentSession;
        return currentSession is null ||
               string.Equals(currentSession.Role, "guest", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLanguageDisplayName(string? languageCode) => languageCode?.ToLowerInvariant() switch
    {
        "vi" => "Tiếng Việt",
        "en" => "English",
        "zh" => "中文",
        "ko" => "한국어",
        "fr" => "Français",
        _ => "Tiếng Việt"
    };

    private static string NormalizeForSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string BuildNarrationPreview(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Chưa có nội dung thuyết minh cho quán này.";
        }

        var normalized = string.Join(
            ' ',
            text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return normalized.Length <= 150
            ? normalized
            : $"{normalized[..147].TrimEnd()}...";
    }

    private static string FormatTimestamp(DateTimeOffset? timestamp, string emptyText)
    {
        return timestamp.HasValue
            ? $"cập nhật {timestamp.Value.ToLocalTime():HH:mm dd/MM}"
            : emptyText;
    }

    private static string FormatFileSize(long byteCount)
    {
        if (byteCount <= 0)
        {
            return "0 B";
        }

        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)byteCount;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }

    private readonly record struct PlaybackRequest(
        string PlaybackMode,
        string? AudioAssetPath,
        string? FallbackMessage);

    private static string CreatePoiSnapshot(IEnumerable<POI> pois)
    {
        var builder = new StringBuilder();

        foreach (var poi in pois.OrderBy(item => item.Id))
        {
            builder
                .Append(poi.Id).Append('|')
                .Append(poi.Code).Append('|')
                .Append(poi.Name).Append('|')
                .Append(poi.Category).Append('|')
                .Append(poi.ImageSource).Append('|')
                .Append(poi.Address).Append('|')
                .Append(poi.Description).Append('|')
                .Append(poi.SpecialDish).Append('|')
                .Append(poi.NarrationText).Append('|')
                .Append(poi.MapLink).Append('|')
                .Append(poi.AudioAssetPath).Append('|')
                .Append(poi.Priority).Append('|')
                .Append(poi.Latitude).Append('|')
                .Append(poi.Longitude).Append('|')
                .Append(poi.TriggerRadiusMeters).Append('|')
                .Append(poi.CooldownMinutes).Append('|')
                .Append(poi.IsActive);

            foreach (var translation in poi.NarrationTranslations.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder
                    .Append('|')
                    .Append(translation.Key)
                    .Append('=')
                    .Append(translation.Value);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string CreateTourSnapshot(IEnumerable<TourDto> tours)
    {
        var builder = new StringBuilder();

        foreach (var tour in tours.OrderBy(item => item.Id))
        {
            builder
                .Append(tour.Id).Append('|')
                .Append(tour.Code).Append('|')
                .Append(tour.Name).Append('|')
                .Append(tour.Description).Append('|')
                .Append(tour.EstimatedMinutes).Append('|')
                .Append(tour.IsActive).Append('|')
                .Append(tour.IsQrEnabled);

            foreach (var poiId in tour.PoiIds)
            {
                builder
                    .Append('|')
                    .Append(poiId);
            }

            builder.AppendLine();
        }

        return builder.ToString();
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
