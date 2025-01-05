using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Drom.WPF.DAL;
using Drom.WPF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Drom.WPF.ViewModels;

public partial class FavoritesPageViewModel : ObservableObject, IHasPageIndex, IRefreshable
{
    public int PageIndex { get; } = 2;
    
    private bool _isRefreshingRunning;
    
    [ObservableProperty]
    private ObservableCollection<MyAdOverviewViewModel>? _favorites;
    
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
            Favorites = null;
            _isRefreshingRunning = false;
            return;
        }
        
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DromDbContext>>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        await using var mainDbContext = await dbContextFactory.CreateDbContextAsync();
        
        var items = await mainDbContext
            .FavoriteAds
            .Where(a => a.UserId == user.Id)
            .OrderBy(e => e.Ad.CreationDateTime)
            .Select(e => new MyAdOverviewViewModel
            {
                Id = e.Ad.Id,
                UserId = e.UserId,
                Title = $"{e.Ad.CarBrandName} {e.Ad.CarModelName}, {e.Ad.CarYear}",
                CreationDateTime = e.Ad.CreationDateTime.ToLocalTime(),
                Price = e.Ad.Price,
                MainImageId = e.Ad.AdImages.First(i => i.IsMain).Id,
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
        
        Favorites = new ObservableCollection<MyAdOverviewViewModel>(items);
        _isRefreshingRunning = false;
    }

    [RelayCommand]
    private async Task RemoveFromFavorites(MyAdOverviewViewModel? ad)
    {
        if (ad is null)
        {
            return;
        }
        
        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var currentUserId = scope.ServiceProvider.GetRequiredService<ICurrentUserService>().Get()!.Id;

        await dbContext.FavoriteAds.Where(e => e.AdId == ad.Id && e.UserId == currentUserId).ExecuteDeleteAsync();
        await RefreshAsync();
    }
}