using Microsoft.Extensions.DependencyInjection;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

public partial class TourPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private bool _isNavigatingToPoiDetail;

    public TourPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    private async void OnStartTourClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not TourPackageItem item)
        {
            return;
        }

        await _viewModel.ActivateTourAsync(item.TourId);
    }

    private async void OnStopActiveTourClicked(object? sender, EventArgs e)
    {
        await _viewModel.StopActiveTourAsync();
    }

    private async void OnTourStopTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not TourStopProgressItem item ||
            _isNavigatingToPoiDetail)
        {
            return;
        }

        _viewModel.SelectPoi(item.PoiId);
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
}
