using Microsoft.Extensions.DependencyInjection;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

public partial class ListeningHistoryPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private bool _isNavigatingToPoiDetail;
    private bool _isDeletingHistoryItem;
    private bool _isClearingHistory;

    public ListeningHistoryPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshListeningHistoryCommand.Execute(null);
    }

    private async void OnHistoryDetailClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not ListeningHistoryDisplayItem item)
        {
            return;
        }

        await OpenHistoryDetailAsync(item.Id);
    }

    private async void OnReplayClicked(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not ListeningHistoryDisplayItem item)
        {
            return;
        }

        await _viewModel.ReplayListeningHistoryAsync(item.Id);
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_isDeletingHistoryItem ||
            sender is not BindableObject bindable ||
            bindable.BindingContext is not ListeningHistoryDisplayItem item)
        {
            return;
        }

        _isDeletingHistoryItem = true;

        try
        {
            await _viewModel.DeleteListeningHistoryEntryAsync(item.Id);
        }
        finally
        {
            _isDeletingHistoryItem = false;
        }
    }

    private async void OnClearAllClicked(object? sender, EventArgs e)
    {
        if (_isClearingHistory || !_viewModel.CanClearListeningHistory)
        {
            return;
        }

        var shouldClear = await DisplayAlert(
            _viewModel.ClearListeningHistoryConfirmTitle,
            _viewModel.ClearListeningHistoryConfirmMessage,
            _viewModel.ClearListeningHistoryConfirmButtonText,
            _viewModel.CancelButtonText);

        if (!shouldClear)
        {
            return;
        }

        _isClearingHistory = true;

        try
        {
            var cleared = await _viewModel.ClearListeningHistoryAsync();
            if (!cleared && !string.IsNullOrWhiteSpace(_viewModel.ListeningHistoryLoadError))
            {
                await DisplayAlert(
                    _viewModel.ListeningHistorySectionTitle,
                    _viewModel.ListeningHistoryLoadError,
                    "OK");
            }
        }
        finally
        {
            _isClearingHistory = false;
        }
    }

    private async Task OpenHistoryDetailAsync(Guid historyId)
    {
        if (_isNavigatingToPoiDetail)
        {
            return;
        }

        var canOpen = await _viewModel.OpenListeningHistoryDetailAsync(historyId);
        if (!canOpen)
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
