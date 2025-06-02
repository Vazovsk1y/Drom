using System.Windows;
using ClothesStore.WPF.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using MaterialDesignThemes.Wpf.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace ClothesStore.WPF.ViewModels;

public partial class MainWindowViewModel(
    ISnackbarMessageQueue snackbarMessageQueue, 
    CatalogPageViewModel catalogPageViewModel) : ObservableObject
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
        
        var content = App.Services.GetRequiredService<IDialogContent<CreateProductViewModel>>();
        var result = await DialogHost.Show(content, CreateProductViewModel.DialogId);

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