using Microsoft.Extensions.DependencyInjection;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

public partial class AccountPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public AccountPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ResetAccountProfileEditor();
        _viewModel.ResetAudioSettingsDraft();
        _viewModel.RefreshListeningHistoryCommand.Execute(null);
    }

    private async void OnListeningHistoryItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not Models.ListeningHistoryDisplayItem item)
        {
            return;
        }

        var canOpen = await _viewModel.OpenListeningHistoryDetailAsync(item.Id);
        if (!canOpen)
        {
            await DisplayAlert("Thông báo", "Không thể mở chi tiết từ bản ghi này.", "OK");
            return;
        }

        var detailPage = _serviceProvider.GetRequiredService<PoiDetailPage>();
        await Navigation.PushAsync(detailPage);
    }
}
