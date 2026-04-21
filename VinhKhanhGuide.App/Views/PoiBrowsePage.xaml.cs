using Microsoft.Extensions.DependencyInjection;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

public partial class PoiBrowsePage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private bool _isNavigatingToPoiDetail;

    public PoiBrowsePage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
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
}
