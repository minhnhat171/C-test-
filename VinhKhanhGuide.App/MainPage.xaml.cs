using System.ComponentModel;
using BruTile.Predefined;
using Mapsui;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App;

public partial class MainPage : ContentPage
{
    private static readonly TimeSpan PreviewMapDoubleTapThreshold = TimeSpan.FromMilliseconds(450);
    private const double PreviewEntranceResolution = 10;
    private const double PreviewCurrentLocationResolution = 8.5;
    private const double FullScreenEntranceResolution = 8.5;
    private const double FullScreenCurrentLocationResolution = 6.5;

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

    private async void OnTopBellClicked(object? sender, EventArgs e)
    {
        await _viewModel.NarrateSelectedPoiAsync();
    }

    private async void OnNarrateSelectedClicked(object? sender, EventArgs e)
    {
        await _viewModel.NarrateSelectedPoiAsync();
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        RestaurantSearchEntry.Unfocus();
        HideSecondaryOverlays();
        await _viewModel.ResetHomeViewAsync();

        if (_isFullScreenMapVisible)
        {
            _isFullScreenMapVisible = false;
            FullScreenMapOverlay.IsVisible = false;
        }

        RefreshMapPins(centerOnSelection: false);
        CenterMapOnEntrance(RestaurantMap, PreviewEntranceResolution);
    }

    private void OnOpenListeningHistoryClicked(object? sender, TappedEventArgs e)
    {
        RestaurantSearchEntry.Unfocus();
        ListeningHistoryOverlay.IsVisible = true;
        UserProfileOverlay.IsVisible = false;
        _viewModel.RefreshListeningHistoryCommand.Execute(null);
    }

    private void OnCloseListeningHistoryClicked(object? sender, EventArgs e)
    {
        ListeningHistoryOverlay.IsVisible = false;
    }

    private void OnOpenUserProfileClicked(object? sender, TappedEventArgs e)
    {
        RestaurantSearchEntry.Unfocus();
        UserProfileOverlay.IsVisible = true;
        ListeningHistoryOverlay.IsVisible = false;
        _viewModel.RefreshListeningHistoryCommand.Execute(null);
    }

    private void OnCloseUserProfileClicked(object? sender, EventArgs e)
    {
        UserProfileOverlay.IsVisible = false;
    }

    private async void OnOpenMapClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.SelectedPoiMapLink))
        {
            await DisplayAlert("Thông báo", "Quán hiện tại chưa có link chỉ đường.", "OK");
            return;
        }

        await Browser.Default.OpenAsync(_viewModel.SelectedPoiMapLink, BrowserLaunchMode.SystemPreferred);
    }

    private void OnPoiSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PoiStatusItem item)
        {
            return;
        }

        _viewModel.SelectPoi(item.PoiId);
        RefreshMapPins(centerOnSelection: true);

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
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
        var foundMatch = _viewModel.ExecuteSearch();

        if (foundMatch)
        {
            _viewModel.HideSearchSuggestions();
            RefreshMapPins(centerOnSelection: true);
            RestaurantSearchEntry.Unfocus();
            return;
        }

        _viewModel.ShowSearchSuggestions();
        RefreshMapPins(centerOnSelection: false);
    }

    private void OnSearchSuggestionTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (sender is not BindableObject bindable ||
                bindable.BindingContext is not SearchSuggestionItem suggestion)
            {
                return;
            }

            var foundMatch = _viewModel.ApplySearchSuggestion(suggestion);
            _viewModel.HideSearchSuggestions();

            if (foundMatch)
            {
                RefreshMapPins(centerOnSelection: true);
            }

            RestaurantSearchEntry.Unfocus();
        }
        catch
        {
            // tránh crash UI khi binding chưa sẵn sàng
        }
    }

    private void OnClearSearchClicked(object? sender, EventArgs e)
    {
        _viewModel.ClearSearch();
        _viewModel.ShowSearchSuggestions();
        RestaurantSearchEntry.Focus();
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
        CenterMapOnEntrance(RestaurantMap, PreviewEntranceResolution);
    }

    private void OnMapStateChanged(object? sender, EventArgs e)
    {
        RefreshMapPins(centerOnSelection: false);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.SelectedLanguage))
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

        mapView.Pins.Add(CreateEntrancePin());

        if (_viewModel.LastLocation is not null)
        {
            mapView.Pins.Add(CreateUserPin(_viewModel.LastLocation));
        }

        Pin? selectedPin = null;

        foreach (var poi in _viewModel.PoiStatuses)
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
            CenterMapOnCurrentLocation(mapView, PreviewCurrentLocationResolution, PreviewEntranceResolution);
            _initialViewportSet = true;
        }
        else if (isPreviewMap && !_hasCenteredOnFirstLiveLocation && _viewModel.LastLocation is not null)
        {
            CenterMapOnCurrentLocation(mapView, PreviewCurrentLocationResolution, PreviewEntranceResolution);
            _hasCenteredOnFirstLiveLocation = true;
        }
        else if (centerOnSelection && selectedPin is not null)
        {
            CenterOnPosition(mapView, selectedPin.Position, resolution: isPreviewMap ? 8 : 6.5);
        }

        if (selectedPin is not null)
        {
            mapView.SelectedPin = selectedPin;
        }
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

    private void OpenFullScreenMap()
    {
        HideSecondaryOverlays();
        EnsureFullScreenMapInitialized();
        _isFullScreenMapVisible = true;
        FullScreenMapOverlay.IsVisible = true;
        RefreshMapPins(centerOnSelection: false);

        Dispatcher.Dispatch(() =>
        {
            CenterMapOnCurrentLocation(
                FullScreenRestaurantMap,
                FullScreenCurrentLocationResolution,
                FullScreenEntranceResolution);
        });
    }

    private async Task OpenPoiDetailAsync(Guid poiId)
    {
        var selectedPoi = _viewModel.PoiStatuses.FirstOrDefault(item => item.PoiId == poiId);
        if (selectedPoi is null)
        {
            return;
        }

        if (_isNavigatingToPoiDetail)
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

            var detailPage = _serviceProvider.GetRequiredService<Views.PoiDetailPage>();
            await Navigation.PushAsync(detailPage);
        }
        finally
        {
            _isNavigatingToPoiDetail = false;
        }
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

    private static Pin CreateEntrancePin()
    {
        return new Pin
        {
            Type = PinType.Pin,
            Color = Colors.Black,
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
            Color = Color.FromArgb("#2563EB"),
            Position = new Position(location.Latitude, location.Longitude),
            Label = string.Empty,
            Address = string.Empty,
            Scale = 0.48F
        };
    }

    private void HideSecondaryOverlays()
    {
        if (ListeningHistoryOverlay is not null)
        {
            ListeningHistoryOverlay.IsVisible = false;
        }

        if (UserProfileOverlay is not null)
        {
            UserProfileOverlay.IsVisible = false;
        }
    }

    private Pin CreateRestaurantPin(PoiStatusItem poi)
    {
        var isSelected = _viewModel.SelectedPoiId == poi.PoiId;
        var pin = new Pin
        {
            Type = PinType.Pin,
            Color = isSelected
                ? Color.FromArgb("#173A43")
                : poi.IsInsideRadius
                ? Color.FromArgb("#EA580C")
                : poi.IsNearest
                    ? Color.FromArgb("#B91C1C")
                    : Color.FromArgb("#6B7280"),
            Position = new Position(poi.Latitude, poi.Longitude),
            Label = string.Empty,
            Address = string.Empty,
            Scale = isSelected
                ? 0.52F
                : poi.IsInsideRadius
                ? 0.44F
                : poi.IsNearest
                    ? 0.40F
                    : 0.34F,
            Tag = poi.PoiId
        };

        return pin;
    }

    private void CenterOnPosition(MapView mapView, Position position, double resolution)
    {
        mapView.Map?.Navigator.CenterOnAndZoomTo(position.ToMapsui(), resolution, 350);
    }
}
