using Microsoft.Maui.ApplicationModel;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

public partial class PoiDetailPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public PoiDetailPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnNarrateSelectedClicked(object? sender, EventArgs e)
    {
        await _viewModel.ToggleSelectedPoiNarrationAsync();
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
}
