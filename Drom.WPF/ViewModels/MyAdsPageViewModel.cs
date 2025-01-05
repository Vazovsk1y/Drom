using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Drom.WPF.DAL;
using Drom.WPF.Infrastructure;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Drom.WPF.ViewModels;

public partial class MyAdsPageViewModel : ObservableObject, IHasPageIndex, IRefreshable
{
    public int PageIndex { get; } = 1;
    
    private bool _isRefreshingRunning;
    
    [ObservableProperty]
    private ObservableCollection<MyAdOverviewViewModel>? _myAds;
    
    public async Task RefreshAsync()
    {
        if (_isRefreshingRunning)
        {
            return;
        }
        
        _isRefreshingRunning = true;
        using var scope = App.Services.CreateScope();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        
        if (currentUserService.Get() is not { } user)
        {
            MyAds = null;
            _isRefreshingRunning = false;
            return;
        }
        
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DromDbContext>>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        await using var mainDbContext = await dbContextFactory.CreateDbContextAsync();
        
        var items = await mainDbContext
            .Ads
            .Where(a => a.UserId == user.Id)
            .OrderBy(e => e.CreationDateTime)
            .Select(e => new MyAdOverviewViewModel
            {
                Id = e.Id,
                UserId = e.UserId,
                Title = $"{e.CarBrandName} {e.CarModelName}, {e.CarYear}",
                CreationDateTime = e.CreationDateTime.ToLocalTime(),
                Price = e.Price,
                MainImageId = e.AdImages.First(i => i.IsMain).Id,
            })
            .ToListAsync();
        
        var loadMainImages = items
            .Select(async item =>
            {
                var img = await cache.GetOrCreateAsync<BitmapImage>($"{item.Id}{item.MainImageId}", async _ =>
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(); 
                    var bytes = (await dbContext.AdImages.FirstAsync(e => e.Id == item.MainImageId)).Bytes;
                    var bt = new BitmapImage();
                    using var stream = new MemoryStream(bytes);
                    bt.BeginInit();
                    bt.StreamSource = stream;
                    bt.CacheOption = BitmapCacheOption.OnLoad;
                    bt.EndInit();
                    return bt;
                });
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    item.MainImage = img;
                });
            });
        
        await Task.WhenAll(loadMainImages);
        
        MyAds = new ObservableCollection<MyAdOverviewViewModel>(items);
        _isRefreshingRunning = false;
    }

    [RelayCommand]
    private async Task DeleteAd(MyAdOverviewViewModel? ad)
    {
        if (ad is null)
        {
            return;
        }

        using var scope = App.Services.CreateScope();
        var dialogContent = scope.ServiceProvider.GetRequiredService<IDialogContent<OkCancelDialogViewModel>>();
        dialogContent.ViewModel.Message = "Вы уверены, что хотите снять с публикации(удалить) объявление?";

        var result = await DialogHost.Show(dialogContent, OkCancelDialogViewModel.DialogId);
        if (result is not true)
        {
            return;
        }
        
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var queue = scope.ServiceProvider.GetRequiredService<ISnackbarMessageQueue>();
        
        await dbContext.Ads.Where(e => e.Id == ad.Id).ExecuteDeleteAsync();
        
        queue.Enqueue("Объявление снято с публикации.");
        MyAds?.Remove(ad);
    }

    [RelayCommand]
    private async Task EditAd(MyAdOverviewViewModel? ad)
    {
        if (ad is null)
        {
            return;
        }
        
        using var scope = App.Services.CreateScope();
        var dialogContent = scope.ServiceProvider.GetRequiredService<IDialogContent<EditAdViewModel>>();
        await dialogContent.ViewModel.Initialize(ad.Id);
        var result = await DialogHost.Show(dialogContent, EditAdViewModel.DialogId);

        if (result is true)
        {
            await RefreshAsync();
        }
    }
}

public partial class MyAdOverviewViewModel : ObservableObject
{
    public required Guid Id { get; init; }
    
    public required Guid UserId { get; init; }
    
    public required string Title { get; init; }
    
    public required DateTimeOffset CreationDateTime { get; init; }
    
    public required decimal Price { get; init; }
    
    public required Guid MainImageId { get; init; }
    
    [ObservableProperty]
    private BitmapImage? _mainImage;
}