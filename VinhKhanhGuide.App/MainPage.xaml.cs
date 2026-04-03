using System.ComponentModel;
using Mapsui;
using Mapsui.Tiling.Extensions;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App;

public partial class MainPage : ContentPage
{
    private const string MapPinImageSource = "embedded://VinhKhanhGuide.App.Resources.Images.map_pin.png";
    private readonly MainViewModel _viewModel;
    private bool _isInitializing;
    private bool _mapInitialized;
    private bool _initialViewportSet;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
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
            await _viewModel.InitializeAsync();
            InitializeMapsui();
            RefreshMapPins(centerOnSelection: !_initialViewportSet);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void InitializeMapsui()
    {
        if (_mapInitialized)
        {
            return;
        }

        RestaurantMap.Map = new MapBuilder()
            .WithOpenStreetMapLayer((layer, map) =>
            {
                layer.Name = "VinhKhanh.BaseMap";
            })
            .Build();

        RestaurantMap.PinClicked += OnRestaurantPinClicked;
        RestaurantMap.MapClicked += OnRestaurantMapClicked;
        _mapInitialized = true;
    }

    private async void OnTopBellClicked(object sender, EventArgs e)
    {
        await _viewModel.NarrateSelectedPoiAsync();
    }

    private async void OnNarrateSelectedClicked(object sender, EventArgs e)
    {
        await _viewModel.NarrateSelectedPoiAsync();
    }

    private async void OnOpenMapClicked(object sender, EventArgs e)
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

    private void OnGoToEntranceClicked(object sender, EventArgs e)
    {
        if (!_mapInitialized)
        {
            return;
        }

        CenterOnPosition(
            new Position(MainViewModel.EntranceLatitude, MainViewModel.EntranceLongitude),
            resolution: 8);
    }

    private async void OnRestaurantPinClicked(object? sender, PinClickedEventArgs e)
    {
        e.Handled = true;

        if (e.Pin.Tag is not Guid poiId)
        {
            CenterOnPosition(e.Pin.Position, resolution: 8);
            return;
        }

        _viewModel.SelectPoi(poiId);
        RefreshMapPins(centerOnSelection: true);
        await _viewModel.NarrateSelectedPoiAsync();
    }

    private async void OnRestaurantMapClicked(object? sender, MapClickedEventArgs e)
    {
        e.Handled = true;
        await _viewModel.HandleMapTapAsync(e.Point.Latitude, e.Point.Longitude);
        RefreshMapPins(centerOnSelection: true);
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
        if (!_mapInitialized || RestaurantMap.Map is null)
        {
            return;
        }

        RestaurantMap.Pins.Clear();

        RestaurantMap.Pins.Add(CreateEntrancePin());

        if (_viewModel.LastLocation is not null)
        {
            RestaurantMap.Pins.Add(CreateUserPin(_viewModel.LastLocation));
        }

        Pin? selectedPin = null;

        foreach (var poi in _viewModel.PoiStatuses)
        {
            var pin = CreateRestaurantPin(poi);
            RestaurantMap.Pins.Add(pin);

            if (_viewModel.SelectedPoiId == poi.PoiId)
            {
                selectedPin = pin;
            }
        }

        if (!_initialViewportSet)
        {
            CenterOnPosition(
                new Position(MainViewModel.EntranceLatitude, MainViewModel.EntranceLongitude),
                resolution: 8);
            _initialViewportSet = true;
        }
        else if (centerOnSelection && selectedPin is not null)
        {
            CenterOnPosition(selectedPin.Position, resolution: 6);
        }

        if (selectedPin is not null)
        {
            RestaurantMap.SelectedPin = selectedPin;
            selectedPin.ShowCallout();
        }
    }

    private static Pin CreateEntrancePin()
    {
        return new Pin
        {
            ImageSource = MapPinImageSource,
            Type = PinType.ImageSource,
            Position = new Position(MainViewModel.EntranceLatitude, MainViewModel.EntranceLongitude),
            Label = MainViewModel.EntranceName,
            Address = MainViewModel.EntranceAddress,
            Scale = 0.16F
        };
    }

    private static Pin CreateUserPin(LocationDto location)
    {
        return new Pin
        {
            Type = PinType.Pin,
            Color = Colors.Black,
            Position = new Position(location.Latitude, location.Longitude),
            Label = "Bạn đang ở đây",
            Address = $"Sai số khoảng {location.AccuracyMeters?.ToString("F0") ?? "?"}m",
            Scale = 0.82F
        };
    }

    private static Pin CreateRestaurantPin(PoiStatusItem poi)
    {
        var pin = new Pin
        {
            ImageSource = MapPinImageSource,
            Type = PinType.ImageSource,
            Position = new Position(poi.Latitude, poi.Longitude),
            Label = poi.Name,
            Address = poi.Address,
            Scale = poi.IsNearest ? 0.17F : 0.14F,
            Tag = poi.PoiId
        };

        return pin;
    }

    private void CenterOnPosition(Position position, double resolution)
    {
        RestaurantMap.Map?.Navigator.CenterOnAndZoomTo(position.ToMapsui(), resolution, 350);
    }
}
