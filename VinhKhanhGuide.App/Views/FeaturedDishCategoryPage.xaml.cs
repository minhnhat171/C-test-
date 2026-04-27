using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

public partial class FeaturedDishCategoryPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private bool _isOpeningActiveTour;

    public FeaturedDishCategoryPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    private async void OnOpenFilteredMapClicked(object? sender, EventArgs e)
    {
        if (Navigation.NavigationStack.OfType<MainPage>().FirstOrDefault() is not MainPage mainPage ||
            !Navigation.NavigationStack.Contains(this))
        {
            return;
        }

        mainPage.OpenMapForFeaturedCategory(_viewModel.SelectedFeaturedDishCategoryKey);
        await Navigation.PopAsync();
    }

    private async void OnStartMiniTourClicked(object? sender, EventArgs e)
    {
        if (_isOpeningActiveTour)
        {
            return;
        }

        var started = await _viewModel.StartFeaturedCategoryTourAsync(_viewModel.SelectedFeaturedDishCategoryKey);
        if (!started)
        {
            return;
        }

        _isOpeningActiveTour = true;

        try
        {
            var activeTourPage = _serviceProvider.GetRequiredService<ActiveTourPage>();

            if (Navigation.NavigationStack.OfType<MainPage>().FirstOrDefault() is MainPage &&
                Navigation.NavigationStack.Contains(this))
            {
                Navigation.InsertPageBefore(activeTourPage, this);
                await Navigation.PopAsync();
                return;
            }

            await Navigation.PushAsync(activeTourPage);
        }
        finally
        {
            _isOpeningActiveTour = false;
        }
    }
}
