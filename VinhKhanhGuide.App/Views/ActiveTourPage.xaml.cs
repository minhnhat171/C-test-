using System.ComponentModel;
using BruTile.Predefined;
using Mapsui;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Xaml;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class ActiveTourPage : ContentPage
{
    private const double RouteFallbackResolution = 6.4;

    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private bool _mapInitialized;
    private bool _isSubscribed;
    private bool _isNavigatingToPoiDetail;

    public ActiveTourPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        EnsureSubscribed();
        InitializeTourMap();
        Dispatcher.Dispatch(() => RefreshTourMap(centerOnRoute: true));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (!_isSubscribed)
        {
            return;
        }

        _viewModel.MapStateChanged -= OnMapStateChanged;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _isSubscribed = false;
    }

    private void EnsureSubscribed()
    {
        if (_isSubscribed)
        {
            return;
        }

        _viewModel.MapStateChanged += OnMapStateChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _isSubscribed = true;
    }

    private void InitializeTourMap()
    {
        if (_mapInitialized)
        {
            return;
        }

        TourRouteMap.Map = CreateMap();
        TourRouteMap.PinClicked += OnTourRoutePinClicked;
        _mapInitialized = true;
    }

    private void RefreshTourMap(bool centerOnRoute)
    {
        if (TourRouteMap.Map is null)
        {
            return;
        }

        TourRouteMap.Pins.Clear();
        TourRouteMap.Drawables.Clear();

        DrawTourRoute();

        foreach (var stop in _viewModel.ActiveTourStops)
        {
            var poi = _viewModel.Pois.FirstOrDefault(item => item.Id == stop.PoiId);
            if (poi is null || !LocationService.IsValidCoordinate(poi.Latitude, poi.Longitude))
            {
                continue;
            }

            TourRouteMap.Pins.Add(CreateTourStopPin(stop, poi));
        }

        if (centerOnRoute && !FocusMapOnActiveRoute())
        {
            CenterOnEntrance();
        }
    }

    private void DrawTourRoute()
    {
        var routePoints = _viewModel.ActiveTourRoutePoints
            .Where(point => LocationService.IsValidCoordinate(point.Latitude, point.Longitude))
            .ToList();

        if (routePoints.Count < 2)
        {
            return;
        }

        var polyline = new Polyline
        {
            StrokeColor = Color.FromArgb("#2F80FF"),
            StrokeWidth = 7
        };

        foreach (var point in routePoints)
        {
            polyline.Positions.Add(new Position(point.Latitude, point.Longitude));
        }

        TourRouteMap.Drawables.Add(polyline);
    }

    private Pin CreateTourStopPin(TourStopProgressItem stop, POI poi)
    {
        var color = stop.IsCurrent
            ? "#102A43"
            : stop.IsCompleted
                ? "#35B8A6"
                : "#2F80FF";

        return new Pin(TourRouteMap)
        {
            Label = $"{stop.Order}. {stop.Name}",
            Address = stop.StatusLabel,
            Position = new Position(poi.Latitude, poi.Longitude),
            Type = PinType.Pin,
            Color = Color.FromArgb(color),
            Scale = stop.IsCurrent ? 0.42f : 0.34f,
            Tag = stop.PoiId
        };
    }

    private bool FocusMapOnActiveRoute()
    {
        var routePoints = _viewModel.ActiveTourRoutePoints
            .Where(point => LocationService.IsValidCoordinate(point.Latitude, point.Longitude))
            .ToList();

        if (routePoints.Count == 0 || TourRouteMap.Map is null)
        {
            return false;
        }

        if (routePoints.Count == 1)
        {
            CenterOnPosition(
                new Position(routePoints[0].Latitude, routePoints[0].Longitude),
                RouteFallbackResolution);
            return true;
        }

        var mapsuiPoints = routePoints
            .Select(point => new Position(point.Latitude, point.Longitude).ToMapsui())
            .ToList();
        var minX = mapsuiPoints.Min(point => point.X);
        var minY = mapsuiPoints.Min(point => point.Y);
        var maxX = mapsuiPoints.Max(point => point.X);
        var maxY = mapsuiPoints.Max(point => point.Y);

        TourRouteMap.Map.Navigator.ZoomToBox(new MRect(minX, minY, maxX, maxY), MBoxFit.Fit, 350);
        return true;
    }

    private void CenterOnEntrance()
    {
        CenterOnPosition(
            new Position(MainViewModel.EntranceLatitude, MainViewModel.EntranceLongitude),
            RouteFallbackResolution);
    }

    private void CenterOnPosition(Position position, double resolution)
    {
        if (TourRouteMap.Map is null)
        {
            return;
        }

        TourRouteMap.Map.Navigator.CenterOnAndZoomTo(position.ToMapsui(), resolution, 350);
    }

    private async void OnTourRoutePinClicked(object? sender, PinClickedEventArgs e)
    {
        e.Handled = true;

        if (e.Pin.Tag is not Guid poiId)
        {
            return;
        }

        await OpenPoiDetailAsync(poiId);
    }

    private async void OnTourStopTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not TourStopProgressItem item)
        {
            return;
        }

        await OpenPoiDetailAsync(item.PoiId);
    }

    private async Task OpenPoiDetailAsync(Guid poiId)
    {
        if (_isNavigatingToPoiDetail)
        {
            return;
        }

        _viewModel.SelectPoi(poiId);
        _isNavigatingToPoiDetail = true;

        try
        {
            var detailPage = _serviceProvider.GetRequiredService<PoiDetailPage>();
            await Navigation.PushAsync(detailPage);
        }
        finally
        {
            _isNavigatingToPoiDetail = false;
        }
    }

    private async void OnStopActiveTourClicked(object? sender, EventArgs e)
    {
        await _viewModel.StopActiveTourAsync();
        RefreshTourMap(centerOnRoute: false);

        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
    }

    private void OnZoomInClicked(object? sender, EventArgs e)
    {
        ZoomMap(zoomIn: true);
    }

    private void OnZoomOutClicked(object? sender, EventArgs e)
    {
        ZoomMap(zoomIn: false);
    }

    private void ZoomMap(bool zoomIn)
    {
        if (TourRouteMap.Map is null)
        {
            return;
        }

        if (zoomIn)
        {
            TourRouteMap.Map.Navigator.ZoomIn(250);
            return;
        }

        TourRouteMap.Map.Navigator.ZoomOut(250);
    }

    private void OnMapStateChanged(object? sender, EventArgs e)
    {
        Dispatcher.Dispatch(() => RefreshTourMap(centerOnRoute: false));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.HasActiveTour)
            or nameof(MainViewModel.ActiveTourRoutePoints)
            or nameof(MainViewModel.ActiveTourStops)
            or nameof(MainViewModel.SelectedLanguage))
        {
            Dispatcher.Dispatch(() => RefreshTourMap(centerOnRoute: false));
        }
    }

    private static Mapsui.Map CreateMap()
    {
        var map = new Mapsui.Map();
        var tileLayer = CreateBaseTileLayer();
        tileLayer.Name = "VinhKhanh.ActiveTourMap";
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
}
