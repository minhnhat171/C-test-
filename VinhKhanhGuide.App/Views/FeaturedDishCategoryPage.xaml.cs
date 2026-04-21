using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

public partial class FeaturedDishCategoryPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private bool _isNavigatingToPoiDetail;

    public FeaturedDishCategoryPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    private async void OnOpenNearestPoiClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.TrySelectNearestPoiForFeaturedCategory(_viewModel.SelectedFeaturedDishCategoryKey))
        {
            return;
        }

        await OpenPoiDetailAsync();
    }

    private async void OnOpenRecommendedPoiClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.TrySelectRecommendedPoiForFeaturedCategory(_viewModel.SelectedFeaturedDishCategoryKey))
        {
            return;
        }

        await OpenPoiDetailAsync();
    }

    private async void OnOpenFilteredMapClicked(object? sender, EventArgs e)
    {
        _viewModel.SetMapCategoryFilter(_viewModel.SelectedFeaturedDishCategoryKey);

        if (Navigation.NavigationStack.OfType<MainPage>().FirstOrDefault() is not MainPage mainPage)
        {
            await Navigation.PopAsync();
            return;
        }

        await Navigation.PopAsync();
        mainPage.OpenMapForFeaturedCategory(_viewModel.SelectedFeaturedDishCategoryKey);
    }

    private async void OnStartMiniTourClicked(object? sender, EventArgs e)
    {
        var started = await _viewModel.StartFeaturedCategoryTourAsync(_viewModel.SelectedFeaturedDishCategoryKey);
        if (!started)
        {
            return;
        }

        if (Navigation.NavigationStack.OfType<MainPage>().FirstOrDefault() is not MainPage mainPage)
        {
            await Navigation.PopAsync();
            return;
        }

        await Navigation.PopAsync();
        mainPage.OpenMapForActiveTour();
    }

    private async Task OpenPoiDetailAsync()
    {
        if (_isNavigatingToPoiDetail)
        {
            return;
        }

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
