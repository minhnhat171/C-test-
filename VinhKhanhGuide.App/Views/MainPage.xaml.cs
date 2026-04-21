using System.ComponentModel;
using System.Diagnostics;
using BruTile.Predefined;
using Mapsui;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Xaml;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class MainPage : ContentPage
{
    private static readonly TimeSpan PreviewMapDoubleTapThreshold = TimeSpan.FromMilliseconds(450);
    private const double PreviewEntranceResolution = 6.8;
    private const double PreviewCurrentLocationResolution = 5.8;
    private const double FullScreenEntranceResolution = 6.2;
    private const double FullScreenCurrentLocationResolution = 5.2;

    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private bool _isInitializing;
    private bool _isNavigatingToPoiDetail;
    private bool _previewMapInitialized;
    private bool _fullScreenMapInitialized;
    private bool _isFullScreenMapVisible;
    private bool _initialViewportSet;
    private bool _hasCenteredOnFirstLiveLocation;
    private bool _ignoreNextPreviewMapClick;
    private bool _ignoreNextFullScreenMapClick;
    private DateTimeOffset _lastPreviewMapTapAt;

    public MainPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
        _viewModel.MapStateChanged += OnMapStateChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isInitializing)
        {
            return;
        }

        _isInitializing = true;

        try
        {
            InitializeMapsui();
            await _viewModel.InitializeAsync();
            RefreshMapPins(centerOnSelection: !_initialViewportSet);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainPage] OnAppearing initialization failed: {ex}");
            await DisplayAlert(
                "Không thể mở trang chính",
                $"Có lỗi khi tải dữ liệu hoặc bản đồ: {ex.Message}",
                "OK");
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void InitializeMapsui()
    {
        if (_previewMapInitialized)
        {
            return;
        }

        RestaurantMap.Map = CreateMap();
        RestaurantMap.PinClicked += OnPreviewMapPinClicked;
        RestaurantMap.MapClicked += OnPreviewMapClicked;
        _previewMapInitialized = true;
    }

    private async void OnNarrateSelectedClicked(object? sender, EventArgs e)
    {
        await _viewModel.ToggleSelectedPoiNarrationAsync();
    }

    private async void OnOpenSelectedPoiDetailClicked(object? sender, EventArgs e)
    {
        await OpenPoiDetailAsync();
    }

    private async void OnOpenPoiDetailClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not PoiStatusItem item)
        {
            return;
        }

        await OpenPoiDetailAsync(item.PoiId);
    }

    private async void OnPoiNarrationClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not PoiStatusItem item)
        {
            return;
        }

        await _viewModel.TogglePoiNarrationAsync(item.PoiId);
        RefreshMapPins(centerOnSelection: false);
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        RestaurantSearchEntry.Unfocus();
        await _viewModel.ResetHomeViewAsync();
        await MainScrollView.ScrollToAsync(0, 0, true);

        if (_isFullScreenMapVisible)
        {
            _isFullScreenMapVisible = false;
            FullScreenMapOverlay.IsVisible = false;
        }

        RefreshMapPins(centerOnSelection: false);
        FocusPreviewMapDefault();
    }

    private void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        OnHomeClicked(sender, EventArgs.Empty);
    }

    private async void OnOpenAccountPageClicked(object? sender, EventArgs e)
    {
        if (_isFullScreenMapVisible)
        {
            _isFullScreenMapVisible = false;
            FullScreenMapOverlay.IsVisible = false;
        }

        var accountPage = _serviceProvider.GetRequiredService<AccountPage>();
        await Navigation.PushAsync(accountPage);
    }

    private void OnOpenAccountPageTapped(object? sender, TappedEventArgs e)
    {
        OnOpenAccountPageClicked(sender, EventArgs.Empty);
    }

    private void OnSearchFocused(object? sender, FocusEventArgs e)
    {
        _viewModel.ShowSearchSuggestions();
    }

    private async void OnSearchUnfocused(object? sender, FocusEventArgs e)
    {
        await Task.Delay(150);

        if (!RestaurantSearchEntry.IsFocused)
        {
            _viewModel.HideSearchSuggestions();
        }
    }

    private void OnSearchCompleted(object? sender, EventArgs e)
    {
        _viewModel.ExecuteSearch();
        _viewModel.HideSearchSuggestions();
        RefreshMapPins(centerOnSelection: false);
        RestaurantSearchEntry.Unfocus();
    }

    private void OnSearchSuggestionTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not SearchSuggestionItem suggestion)
        {
            return;
        }

        _viewModel.ApplySearchSuggestion(suggestion);
        _viewModel.HideSearchSuggestions();
        RefreshMapPins(centerOnSelection: false);
        RestaurantSearchEntry.Unfocus();
    }

    private void OnClearSearchClicked(object? sender, EventArgs e)
    {
        _viewModel.ClearSearch();
        _viewModel.ShowSearchSuggestions();
        RestaurantSearchEntry.Focus();
    }

    private void OnSearchGuideClicked(object? sender, EventArgs e)
    {
        RestaurantSearchEntry.Focus();
    }

    private async void OnSearchPreviewDetailClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not PoiStatusItem item)
        {
            return;
        }

        await OpenPoiDetailAsync(item.PoiId);
    }

    private async void OnSearchPreviewNarrationClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not PoiStatusItem item)
        {
            return;
        }

        await _viewModel.TogglePoiNarrationAsync(item.PoiId);
        RefreshMapPins(centerOnSelection: false);
    }

    private async void OnTourSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TourPackageItem item)
        {
            return;
        }

        await _viewModel.ActivateTourAsync(item.TourId);
        RefreshMapPins(centerOnSelection: true);
        FocusMapOnActiveRoute(RestaurantMap, PreviewEntranceResolution);

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private async void OnStopActiveTourClicked(object? sender, EventArgs e)
    {
        await _viewModel.StopActiveTourAsync();
        _hasCenteredOnFirstLiveLocation = false;
        RefreshMapPins(centerOnSelection: false);
        FocusActiveMapOnGpsOrigin();
    }

    private async void OnTourShortcutClicked(object? sender, EventArgs e)
    {
        await OpenTourPageAsync();
    }

    private void OnTourShortcutTapped(object? sender, TappedEventArgs e)
    {
        OnTourShortcutClicked(sender, EventArgs.Empty);
    }

    private async void OnHistoryShortcutTapped(object? sender, TappedEventArgs e)
    {
        await OpenHistoryPageAsync();
    }

    private async void OnOpenTourPageClicked(object? sender, EventArgs e)
    {
        await OpenTourPageAsync();
    }

    private async void OnOpenHistoryPageClicked(object? sender, EventArgs e)
    {
        await OpenHistoryPageAsync();
    }

    private async void OnOpenPoiBrowsePageClicked(object? sender, EventArgs e)
    {
        var poiBrowsePage = _serviceProvider.GetRequiredService<PoiBrowsePage>();
        await Navigation.PushAsync(poiBrowsePage);
    }

    private void OnOpenFullScreenMapClicked(object? sender, EventArgs e)
    {
        OpenFullScreenMap(
            focusOnRoute: _viewModel.HasActiveTour,
            focusOnVisiblePois: !_viewModel.HasActiveTour);
    }

    private void OnOpenFullScreenMapTapped(object? sender, TappedEventArgs e)
    {
        OnOpenFullScreenMapClicked(sender, EventArgs.Empty);
    }

    private void OnZoomInClicked(object? sender, EventArgs e)
    {
        ZoomMap(GetActiveMapView(), zoomIn: true);
    }

    private void OnZoomOutClicked(object? sender, EventArgs e)
    {
        ZoomMap(GetActiveMapView(), zoomIn: false);
    }

    private void OnCenterCurrentLocationClicked(object? sender, EventArgs e)
    {
        CenterMapOnCurrentLocation(RestaurantMap, PreviewCurrentLocationResolution, PreviewEntranceResolution);
    }

    private async void OnStartTrackingClicked(object? sender, EventArgs e)
    {
        _hasCenteredOnFirstLiveLocation = false;
        await _viewModel.StartAsync();
        RefreshMapPins(centerOnSelection: false);
        CenterMapOnCurrentLocation(RestaurantMap, PreviewCurrentLocationResolution, PreviewEntranceResolution);
    }

    private async void OnStopTrackingClicked(object? sender, EventArgs e)
    {
        await _viewModel.StopAsync();
        _hasCenteredOnFirstLiveLocation = false;
        RefreshMapPins(centerOnSelection: false);
        FocusActiveMapOnGpsOrigin();
    }

    private void OnGpsOriginClicked(object? sender, EventArgs e)
    {
        FocusActiveMapOnGpsOrigin();
    }

    private void OnGpsOriginTapped(object? sender, TappedEventArgs e)
    {
        OnGpsOriginClicked(sender, EventArgs.Empty);
    }

    private void OnFullScreenZoomInClicked(object? sender, EventArgs e)
    {
        ZoomMap(FullScreenRestaurantMap, zoomIn: true);
    }

    private void OnFullScreenZoomOutClicked(object? sender, EventArgs e)
    {
        ZoomMap(FullScreenRestaurantMap, zoomIn: false);
    }

    private void OnFullScreenCenterCurrentLocationClicked(object? sender, EventArgs e)
    {
        CenterMapOnCurrentLocation(
            FullScreenRestaurantMap,
            FullScreenCurrentLocationResolution,
            FullScreenEntranceResolution);
    }

    private MapView GetActiveMapView()
    {
        if (_isFullScreenMapVisible && _fullScreenMapInitialized)
        {
            return FullScreenRestaurantMap;
        }

        return RestaurantMap;
    }

    private static void ZoomMap(MapView? mapView, bool zoomIn)
    {
        if (mapView?.Map is null)
        {
            return;
        }

        if (zoomIn)
        {
            mapView.Map.Navigator.ZoomIn(250);
            return;
        }

        mapView.Map.Navigator.ZoomOut(250);
    }

    private async void OnPreviewMapPinClicked(object? sender, PinClickedEventArgs e)
    {
        e.Handled = true;
        _ignoreNextPreviewMapClick = true;

        if (e.Pin.Tag is not Guid poiId)
        {
            return;
        }

        _viewModel.SelectPoi(poiId);
        RefreshMapPins(centerOnSelection: false);
        await OpenPoiDetailAsync(poiId);
    }

    private void OnPreviewMapClicked(object? sender, MapClickedEventArgs e)
    {
        e.Handled = true;

        if (_ignoreNextPreviewMapClick)
        {
            _ignoreNextPreviewMapClick = false;
            return;
        }

        if (IsPreviewMapDoubleTap())
        {
            OpenFullScreenMap();
        }
    }

    private async void OnFullScreenMapPinClicked(object? sender, PinClickedEventArgs e)
    {
        e.Handled = true;
        _ignoreNextFullScreenMapClick = true;

        if (e.Pin.Tag is not Guid poiId)
        {
            return;
        }

        _viewModel.SelectPoi(poiId);
        RefreshMapPins(centerOnSelection: false);
        await OpenPoiDetailAsync(poiId);
    }

    private async void OnFullScreenMapClicked(object? sender, MapClickedEventArgs e)
    {
        e.Handled = true;

        if (_ignoreNextFullScreenMapClick)
        {
            _ignoreNextFullScreenMapClick = false;
            return;
        }

        await _viewModel.HandleMapTapAsync(e.Point.Latitude, e.Point.Longitude);
        RefreshMapPins(centerOnSelection: true);
    }

    private void OnCloseFullScreenMapClicked(object? sender, EventArgs e)
    {
        _isFullScreenMapVisible = false;
        FullScreenMapOverlay.IsVisible = false;
        RefreshMapPins(centerOnSelection: false);
        FocusPreviewMapDefault();
    }

    private void OnMapStateChanged(object? sender, EventArgs e)
    {
        RefreshMapPins(centerOnSelection: false);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.SelectedLanguage) or nameof(MainViewModel.GpsOriginLocation))
        {
            RefreshMapPins(centerOnSelection: false);
        }
    }

    private void RefreshMapPins(bool centerOnSelection)
    {
        RefreshMapPins(RestaurantMap, centerOnSelection, isPreviewMap: true);

        if (_fullScreenMapInitialized)
        {
            RefreshMapPins(
                FullScreenRestaurantMap,
                centerOnSelection && _isFullScreenMapVisible,
                isPreviewMap: false);
        }
    }

    private void RefreshMapPins(MapView mapView, bool centerOnSelection, bool isPreviewMap)
    {
        if (mapView.Map is null)
        {
            return;
        }

        mapView.Pins.Clear();
        mapView.Drawables.Clear();

        mapView.Pins.Add(CreateEntrancePin());

        if (_viewModel.LastLocation is not null)
        {
            mapView.Pins.Add(CreateUserPin(_viewModel.LastLocation));
        }

        RefreshTourRoute(mapView);

        Pin? selectedPin = null;
        var mapPois = isPreviewMap
            ? _viewModel.PreviewMapPoiStatuses
            : _viewModel.VisibleMapPoiStatuses;

        foreach (var poi in mapPois)
        {
            var pin = CreateRestaurantPin(poi);
            mapView.Pins.Add(pin);

            if (_viewModel.SelectedPoiId == poi.PoiId)
            {
                selectedPin = pin;
            }
        }

        if (isPreviewMap && !_initialViewportSet)
        {
            if (!FocusMapOnPoiCollection(mapView, mapPois, PreviewEntranceResolution))
            {
                CenterMapOnCurrentLocation(mapView, PreviewCurrentLocationResolution, PreviewEntranceResolution);
            }

            _initialViewportSet = true;
        }
        else if (isPreviewMap && !_hasCenteredOnFirstLiveLocation && _viewModel.LastLocation is not null)
        {
            CenterMapOnCurrentLocation(mapView, PreviewCurrentLocationResolution, PreviewEntranceResolution);
            _hasCenteredOnFirstLiveLocation = true;
        }
        else if (centerOnSelection && selectedPin is not null)
        {
            CenterOnPosition(mapView, selectedPin.Position, resolution: isPreviewMap ? 6.2 : 5.4);
        }

        if (selectedPin is not null)
        {
            mapView.SelectedPin = selectedPin;
        }
    }

    private void RefreshTourRoute(MapView mapView)
    {
        var routePoints = _viewModel.ActiveTourRoutePoints;
        if (routePoints.Count < 2)
        {
            return;
        }

        var polyline = new Polyline
        {
            StrokeColor = Color.FromArgb("#2F80FF"),
            StrokeWidth = 6
        };

        foreach (var point in routePoints)
        {
            polyline.Positions.Add(new Position(point.Latitude, point.Longitude));
        }

        mapView.Drawables.Add(polyline);
    }

    private void EnsureFullScreenMapInitialized()
    {
        if (_fullScreenMapInitialized)
        {
            return;
        }

        FullScreenRestaurantMap.Map = CreateMap();
        FullScreenRestaurantMap.PinClicked += OnFullScreenMapPinClicked;
        FullScreenRestaurantMap.MapClicked += OnFullScreenMapClicked;
        _fullScreenMapInitialized = true;
    }

    private void OpenFullScreenMap(bool focusOnRoute = false, bool focusOnVisiblePois = false)
    {
        EnsureFullScreenMapInitialized();
        _isFullScreenMapVisible = true;
        FullScreenMapOverlay.IsVisible = true;
        RefreshMapPins(centerOnSelection: false);

        Dispatcher.Dispatch(() =>
        {
            if (focusOnRoute && FocusMapOnActiveRoute(FullScreenRestaurantMap, FullScreenEntranceResolution))
            {
                return;
            }

            if (focusOnVisiblePois &&
                FocusMapOnPoiCollection(FullScreenRestaurantMap, _viewModel.VisibleMapPoiStatuses, FullScreenEntranceResolution))
            {
                return;
            }

            CenterMapOnCurrentLocation(
                FullScreenRestaurantMap,
                FullScreenCurrentLocationResolution,
                FullScreenEntranceResolution);
        });
    }

    public void OpenMapForFeaturedCategory(string categoryKey)
    {
        _viewModel.SetMapCategoryFilter(categoryKey, updateStatus: false);
        OpenFullScreenMap(focusOnVisiblePois: true);
    }

    public void OpenMapForActiveTour()
    {
        OpenFullScreenMap(focusOnRoute: true);
    }

    private async Task OpenPoiDetailAsync(Guid? poiId = null)
    {
        if (poiId.HasValue &&
            _viewModel.SelectedPoiId != poiId.Value &&
            _viewModel.PoiStatuses.Any(item => item.PoiId == poiId.Value))
        {
            _viewModel.SelectPoi(poiId.Value);
        }

        if (_viewModel.SelectedPoiId is null || _isNavigatingToPoiDetail)
        {
            return;
        }

        _isNavigatingToPoiDetail = true;

        try
        {
            if (_isFullScreenMapVisible)
            {
                _isFullScreenMapVisible = false;
                FullScreenMapOverlay.IsVisible = false;
            }

            var detailPage = _serviceProvider.GetRequiredService<PoiDetailPage>();
            await Navigation.PushAsync(detailPage);
        }
        finally
        {
            _isNavigatingToPoiDetail = false;
        }
    }

    private async Task OpenTourPageAsync()
    {
        var tourPage = _serviceProvider.GetRequiredService<TourPage>();
        await Navigation.PushAsync(tourPage);
    }

    private async Task OpenHistoryPageAsync()
    {
        var historyPage = _serviceProvider.GetRequiredService<ListeningHistoryPage>();
        await Navigation.PushAsync(historyPage);
    }

    private async void OnFeaturedDishCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not FoodCategoryItem item)
        {
            return;
        }

        _viewModel.ShowFeaturedDishCategory(item.Key);
        var featuredPage = _serviceProvider.GetRequiredService<FeaturedDishCategoryPage>();
        await Navigation.PushAsync(featuredPage);
    }

    private async void OnListeningHistoryPreviewTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not ListeningHistoryDisplayItem item)
        {
            return;
        }

        var canOpen = await _viewModel.OpenListeningHistoryDetailAsync(item.Id);
        if (!canOpen)
        {
            await DisplayAlert(
                _viewModel.ListeningHistorySectionTitle,
                GetLocalizedCommonMessage(
                    "Không thể mở lại nội dung từ lịch sử nghe này.",
                    "Could not reopen this listening history item.",
                    "无法重新打开这条收听记录。",
                    "이 청취 기록을 다시 열 수 없습니다.",
                    "Impossible de rouvrir cet élément d'historique."),
                GetLocalizedCommonMessage("OK", "OK", "好的", "확인", "OK"));
            return;
        }

        await OpenPoiDetailAsync();
    }

    private static Mapsui.Map CreateMap()
    {
        var map = new Mapsui.Map();
        var tileLayer = CreateBaseTileLayer();
        tileLayer.Name = "VinhKhanh.BaseMap";
        map.Layers.Add(tileLayer);
        return map;
    }

    private static TileLayer CreateBaseTileLayer()
    {
#if ANDROID
        var tileSource = KnownTileSources.Create(
            KnownTileSource.OpenStreetMap,
            persistentCache: OpenStreetMap.DefaultCache,
            configureHttpRequestMessage: MapTileHttpClientFactory.ConfigureRequest);

        return new TileLayer(tileSource, httpClient: MapTileHttpClientFactory.SharedClient);
#else
        return OpenStreetMap.CreateTileLayer(MapTileHttpClientFactory.UserAgent);
#endif
    }

    private bool IsPreviewMapDoubleTap()
    {
        var now = DateTimeOffset.UtcNow;
        var isDoubleTap = now - _lastPreviewMapTapAt <= PreviewMapDoubleTapThreshold;
        _lastPreviewMapTapAt = now;
        return isDoubleTap;
    }

    private void CenterMapOnEntrance(MapView mapView, double resolution)
    {
        CenterOnPosition(
            mapView,
            new Position(MainViewModel.EntranceLatitude, MainViewModel.EntranceLongitude),
            resolution);
    }

    private void CenterMapOnCurrentLocation(
        MapView mapView,
        double currentLocationResolution,
        double fallbackResolution)
    {
        if (_viewModel.LastLocation is not null)
        {
            CenterOnPosition(
                mapView,
                new Position(_viewModel.LastLocation.Latitude, _viewModel.LastLocation.Longitude),
                currentLocationResolution);
            return;
        }

        CenterMapOnEntrance(mapView, fallbackResolution);
    }

    private void FocusPreviewMapDefault()
    {
        if (!FocusMapOnPoiCollection(RestaurantMap, _viewModel.PreviewMapPoiStatuses, PreviewEntranceResolution))
        {
            CenterMapOnEntrance(RestaurantMap, PreviewEntranceResolution);
        }
    }

    private void FocusMapOnGpsOrigin(
        MapView mapView,
        double originResolution,
        double fallbackResolution)
    {
        if (_viewModel.GpsOriginLocation is not null)
        {
            CenterOnPosition(
                mapView,
                new Position(_viewModel.GpsOriginLocation.Latitude, _viewModel.GpsOriginLocation.Longitude),
                originResolution);
            return;
        }

        CenterMapOnEntrance(mapView, fallbackResolution);
    }

    private void FocusActiveMapOnGpsOrigin()
    {
        if (_isFullScreenMapVisible && _fullScreenMapInitialized)
        {
            FocusMapOnGpsOrigin(
                FullScreenRestaurantMap,
                FullScreenCurrentLocationResolution,
                FullScreenEntranceResolution);
            return;
        }

        FocusMapOnGpsOrigin(
            RestaurantMap,
            PreviewCurrentLocationResolution,
            PreviewEntranceResolution);
    }

    private bool FocusMapOnActiveRoute(MapView mapView, double fallbackResolution)
    {
        var routePoints = _viewModel.ActiveTourRoutePoints;
        if (routePoints.Count == 0 || mapView.Map is null)
        {
            return false;
        }

        if (routePoints.Count == 1)
        {
            CenterOnPosition(
                mapView,
                new Position(routePoints[0].Latitude, routePoints[0].Longitude),
                fallbackResolution);
            return true;
        }

        var mapsuiPoints = routePoints
            .Select(point => new Position(point.Latitude, point.Longitude).ToMapsui())
            .ToList();
        var minX = mapsuiPoints.Min(point => point.X);
        var minY = mapsuiPoints.Min(point => point.Y);
        var maxX = mapsuiPoints.Max(point => point.X);
        var maxY = mapsuiPoints.Max(point => point.Y);

        mapView.Map.Navigator.ZoomToBox(new MRect(minX, minY, maxX, maxY), MBoxFit.Fit, 350);
        return true;
    }

    private bool FocusMapOnPoiCollection(
        MapView mapView,
        IEnumerable<PoiStatusItem> poiStatuses,
        double singlePoiResolution)
    {
        if (mapView.Map is null)
        {
            return false;
        }

        var positions = poiStatuses
            .Select(item => new Position(item.Latitude, item.Longitude))
            .ToList();

        if (positions.Count == 0)
        {
            return false;
        }

        if (positions.Count == 1)
        {
            CenterOnPosition(mapView, positions[0], singlePoiResolution);
            return true;
        }

        var mapsuiPoints = positions
            .Select(position => position.ToMapsui())
            .ToList();
        var minX = mapsuiPoints.Min(point => point.X);
        var minY = mapsuiPoints.Min(point => point.Y);
        var maxX = mapsuiPoints.Max(point => point.X);
        var maxY = mapsuiPoints.Max(point => point.Y);

        mapView.Map.Navigator.ZoomToBox(new MRect(minX, minY, maxX, maxY), MBoxFit.Fit, 350);
        return true;
    }

    private static Pin CreateEntrancePin()
    {
        return new Pin
        {
            Type = PinType.Pin,
            Color = Color.FromArgb("#102A43"),
            Position = new Position(MainViewModel.EntranceLatitude, MainViewModel.EntranceLongitude),
            Label = string.Empty,
            Address = string.Empty,
            Scale = 0.42F
        };
    }

    private static Pin CreateUserPin(LocationDto location)
    {
        return new Pin
        {
            Type = PinType.Pin,
            Color = Color.FromArgb("#2F80FF"),
            Position = new Position(location.Latitude, location.Longitude),
            Label = string.Empty,
            Address = string.Empty,
            Scale = 0.48F
        };
    }

    private Pin CreateRestaurantPin(PoiStatusItem poi)
    {
        var isSelected = _viewModel.SelectedPoiId == poi.PoiId;
        return new Pin
        {
            Type = PinType.Pin,
            Color = isSelected
                ? Color.FromArgb("#102A43")
                : poi.IsActiveTourStop
                    ? Color.FromArgb("#2F80FF")
                    : poi.IsCompletedTourStop
                        ? Color.FromArgb("#60A5FA")
                        : poi.IsInsideRadius
                            ? Color.FromArgb("#1D4ED8")
                            : poi.IsNearest
                                ? Color.FromArgb("#3B82F6")
                                : Color.FromArgb("#94A3B8"),
            Position = new Position(poi.Latitude, poi.Longitude),
            Label = string.Empty,
            Address = string.Empty,
            Scale = isSelected
                ? 0.52F
                : poi.IsActiveTourStop
                    ? 0.48F
                    : poi.IsCompletedTourStop
                        ? 0.42F
                        : 0.38F,
            Tag = poi.PoiId
        };
    }

    private void CenterOnPosition(MapView mapView, Position position, double resolution)
    {
        mapView.Map?.Navigator.CenterOnAndZoomTo(position.ToMapsui(), resolution, 350);
    }

    private string GetLocalizedCommonMessage(string vi, string en, string zh, string ko, string fr)
    {
        return _viewModel.SelectedLanguage switch
        {
            "en" => en,
            "zh" => zh,
            "ko" => ko,
            "fr" => fr,
            _ => vi
        };
    }
}
