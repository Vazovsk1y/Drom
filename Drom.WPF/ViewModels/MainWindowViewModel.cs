using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Drom.WPF.Infrastructure;
using MaterialDesignThemes.Wpf;
using MaterialDesignThemes.Wpf.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Drom.WPF.ViewModels;

public partial class MainWindowViewModel(
    ISnackbarMessageQueue snackbarMessageQueue, 
    CatalogPageViewModel catalogPageViewModel,
    FavoritesPageViewModel favoritesPageViewModel,
    MyAdsPageViewModel myAdsPageViewModel,
    NewsPageViewModel newsPageViewModel) : ObservableObject
{
    public static NullableToVisibilityConverter InverseNullableToVisibilityConverter { get; } = new()
    {
        NullValue = Visibility.Visible,
        NotNullValue = Visibility.Collapsed
    };
    
    [ObservableProperty]
    private CurrentUser? _currentUser;
    
    [ObservableProperty]
    private int? _selectedPageIndex;
    
    [ObservableProperty]
    private ObservableObject? _selectedPage;

    public ISnackbarMessageQueue SnackbarMessageQueue { get; } = snackbarMessageQueue;
    
    public CatalogPageViewModel CatalogPageViewModel { get; } = catalogPageViewModel;
    
    public FavoritesPageViewModel FavoritesPageViewModel { get; } = favoritesPageViewModel;

    public MyAdsPageViewModel MyAdsPageViewModel { get; } = myAdsPageViewModel;

    public NewsPageViewModel NewsPageViewModel { get; } = newsPageViewModel;

    [RelayCommand]
    private async Task OpenAuthDialog()
    {
        var content = App.Services.GetRequiredService<IDialogContent<AuthViewModel>>();
        var result = await DialogHost.Show(content, AuthViewModel.DialogId);

        if (result is true)
        {
            CurrentUser = App.Services.GetRequiredService<ICurrentUserService>().Get();
            if (SelectedPage is IRefreshable refreshable)
            {
                await refreshable.RefreshAsync();
            }
        }
    }

    [RelayCommand]
    private async Task SignOut()
    {
        CurrentUser = null;
        var currentUserService = App.Services.GetRequiredService<ICurrentUserService>();
        currentUserService.Set(null);
        if (SelectedPage is IRefreshable refreshable)
        {
            await refreshable.RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task OpenCreateAdDialog()
    {
        var currentUserService = App.Services.GetRequiredService<ICurrentUserService>();
        if (currentUserService.Get() is null)
        {
            SnackbarMessageQueue.Enqueue("Необходимо войти в аккаунт.");
            return;
        }
        
        var content = App.Services.GetRequiredService<IDialogContent<CreateAdViewModel>>();
        var result = await DialogHost.Show(content, CreateAdViewModel.DialogId);

        if (result is true && SelectedPage is IRefreshable refreshable)
        {
            await refreshable.RefreshAsync();
        }
    }
    
    [RelayCommand]
    private async Task OpenAddNewsItemDialog()
    {
        var content = App.Services.GetRequiredService<IDialogContent<NewsItemAddViewModel>>();
        var result = await DialogHost.Show(content, NewsItemAddViewModel.DialogId);

        if (result is true && SelectedPage is IRefreshable refreshable)
        {
            await refreshable.RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task OnPageChanged(ObservableObject selectedPage)
    {
        if (selectedPage is IRefreshable refreshable)
        {
            await refreshable.RefreshAsync();
        }
        
        if (selectedPage is IHasPageIndex page)
        {
            SelectedPageIndex = page.PageIndex;
        }
        
        SelectedPage = selectedPage;
    }
}