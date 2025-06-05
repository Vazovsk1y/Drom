using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ClosedXML.Report;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Drom.WPF.DAL;
using Drom.WPF.DAL.Models;
using Drom.WPF.Infrastructure;
using MaterialDesignThemes.Wpf;
using MaterialDesignThemes.Wpf.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Drom.WPF.ViewModels;

public partial class CatalogPageViewModel : ObservableObject, IHasPageIndex, IRefreshable
{
    public int PageIndex { get; } = 1;

    private bool _isRefreshingRunning;

    [ObservableProperty] private IEnumerable<AdOverviewViewModel>? _catalogItems;

    [ObservableProperty] private ICollectionView? _catalogItemsView;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditSelectedAdCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedAdCommand))]
    private AdFullInfoViewModel? _selectedAd;

    [ObservableProperty] private string? _searchText;

    public async Task RefreshAsync()
    {
        if (_isRefreshingRunning)
        {
            return;
        }

        _isRefreshingRunning = true;
        SelectedAd = null;
        using var scope = App.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DromDbContext>>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        var currentUserId = scope.ServiceProvider.GetRequiredService<ICurrentUserService>().Get()?.Id;

        await using var mainDbContext = await dbContextFactory.CreateDbContextAsync();

        var items = await mainDbContext
            .Ads
            .OrderBy(e => e.CreationDateTime)
            .Where(e => !e.Sold)
            .Select(e => new AdOverviewViewModel
            {
                Id = e.Id,
                UserId = e.UserId,
                Title = $"{e.CarBrandName} {e.CarModelName}, {e.CarYear}",
                CreationDateTime = e.CreationDateTime.ToLocalTime(),
                Price = e.Price,
                MainImageId = e.AdImages.First(i => i.IsMain).Id,
                IsAbleAddToFavorites =
                    !mainDbContext.FavoriteAds.Any(i => i.UserId == currentUserId && i.AdId == e.Id) &&
                    currentUserId != null && e.UserId != currentUserId,
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

                Application.Current.Dispatcher.Invoke(() => { item.MainImage = img; });
            });

        await Task.WhenAll(loadMainImages);

        CatalogItems = items;
        CatalogItemsView = CollectionViewSource.GetDefaultView(items);
        CatalogItemsView.Filter = Filter;
        _isRefreshingRunning = false;
    }

    private bool Filter(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return obj is AdOverviewViewModel ad &&
               ad.Title.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase);
    }

    [RelayCommand]
    private void Search()
    {
        CatalogItemsView?.Refresh();
    }

    [RelayCommand]
    private async Task GenerateSellReport()
    {
        using var scope = App.Services.CreateScope();
        var dialogContent = scope.ServiceProvider.GetRequiredService<IDialogContent<DatesDialogViewModel>>();

        var result = await DialogHost.Show(dialogContent, OkCancelDialogViewModel.DialogId);
        if (result is not true)
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var queue = scope.ServiceProvider.GetRequiredService<ISnackbarMessageQueue>();

        var from = dialogContent.ViewModel.From;
        var to = dialogContent.ViewModel.To;

        var soldAds = await dbContext
            .Ads
            .AsNoTracking()
            .Where(ad => ad.Sold
                         && ad.SoldDateTime != null
                         && ad.SoldDateTime.Value.Date >= from!.Value.Date
                         && ad.SoldDateTime.Value.Date <= to!.Value.Date)
            .Select(e => new
            {
                CarInfo = $"{e.CarBrandName} {e.CarModelName}, {e.CarYear}",
                e.Price,
                SoldDateTime = e.SoldDateTime!.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                CreationDateTime = e.CreationDateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                UserInfo = $"{e.User.Username}, {e.User.PhoneNumber}",
            })
            .ToListAsync();

        var d = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        
        using var template = new XLTemplate(Assembly.GetExecutingAssembly().GetManifestResourceStream("Drom.WPF.ОтчетОПродажахШаблон.xlsx"));

        template.AddVariable(new
        {
            From = from.Value.ToString("dd.MM.yyyy"), 
            To = to.Value.ToString("dd.MM.yyyy"), 
            Items = soldAds
        });
        
        template.Generate();
        
        template.Workbook.Worksheets.First().Columns().AdjustToContents();

        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ОтчетОПродажах.xlsx");
        template.SaveAs(path);

        queue.Enqueue($"Отчет успешно сохранен как {path}");
    }

    [RelayCommand]
    private async Task GenerateUsersRegistrationsReport()
    {
        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var queue = scope.ServiceProvider.GetRequiredService<ISnackbarMessageQueue>();
        
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(e => e.Role == Role.User)
            .Select(e => new
            {
                e.PhoneNumber,
                e.Username,
                RegistrationDateTime = e.RegistrationDateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
            })
            .ToListAsync();

        var data = new { TotalUsersCount = users.Count, Items = users };
        
        using var template = new XLTemplate(Assembly.GetExecutingAssembly().GetManifestResourceStream("Drom.WPF.ОтчетОРегистрацииПользователей.xlsx"));

        template.AddVariable(data);
        
        template.Generate();
        
        template.Workbook.Worksheets.First().Columns().AdjustToContents();

        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ОтчетОРегистрацииПользователей.xlsx");
        template.SaveAs(path);

        queue.Enqueue($"Отчет успешно сохранен как {path}");
    }

    partial void OnSearchTextChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            CatalogItemsView?.Refresh();
        }
    }

    [RelayCommand]
    private async Task OnAdSelected(AdOverviewViewModel? ad)
    {
        if (ad is null)
        {
            return;
        }

        using var scope = App.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DromDbContext>>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        await using var mainDbContext = await dbContextFactory.CreateDbContextAsync();
        var currentUserId = scope.ServiceProvider.GetRequiredService<ICurrentUserService>().Get()?.Id;

        var target = await mainDbContext.Ads.Include(e => e.User).FirstAsync(e => e.Id == ad.Id);

        var vm = new AdFullInfoViewModel
        {
            Id = target.Id,
            CreationDateTime = target.CreationDateTime.ToLocalTime(),
            CarBrandName = target.CarBrandName,
            CarModelName = target.CarModelName,
            CarYear = target.CarYear,
            Price = target.Price,
            UserPhoneNumber = target.User.PhoneNumber,
            Description = target.Description,
            IsAbleAddToFavorites =
                !(await mainDbContext.FavoriteAds.AnyAsync(i => i.UserId == currentUserId && i.AdId == target.Id)) &&
                currentUserId != null && target.UserId != currentUserId,
        };

        var ids = await mainDbContext.AdImages.Where(e => e.AdId == ad.Id).Select(e => e.Id).ToListAsync();

        var res = new ConcurrentBag<BitmapImage>();

        var loadTasks = ids
            .Select(async item =>
            {
                var img = await cache.GetOrCreateAsync<BitmapImage>($"{ad.Id}{item}", async _ =>
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
                    var bytes = (await dbContext.AdImages.FirstAsync(e => e.Id == item)).Bytes;
                    var bt = new BitmapImage();
                    using var stream = new MemoryStream(bytes);
                    bt.BeginInit();
                    bt.StreamSource = stream;
                    bt.CacheOption = BitmapCacheOption.OnLoad;
                    bt.EndInit();
                    return bt;
                });

                res.Add(img!);
            });

        await Task.WhenAll(loadTasks);

        vm.Images = res;
        vm.SelectedImage = vm.Images.First();
        SelectedAd = vm;
    }

    [RelayCommand(CanExecute = nameof(CanBack))]
    private void Back()
    {
        SelectedAd = null;
    }

    [RelayCommand]
    private async Task AddToFavorites(ObservableObject? ad)
    {
        if (ad is null)
        {
            return;
        }

        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var currentUserId = scope.ServiceProvider.GetRequiredService<ICurrentUserService>().Get()!.Id;

        if (ad is AdFullInfoViewModel fullAd)
        {
            var entity = new FavoriteAd()
            {
                UserId = currentUserId,
                AdId = fullAd.Id,
            };

            dbContext.FavoriteAds.Add(entity);
            await dbContext.SaveChangesAsync();

            fullAd.IsAbleAddToFavorites = false;
            return;
        }

        if (ad is AdOverviewViewModel overviewAd)
        {
            var entity = new FavoriteAd()
            {
                UserId = currentUserId,
                AdId = overviewAd.Id,
            };

            dbContext.FavoriteAds.Add(entity);
            await dbContext.SaveChangesAsync();

            await RefreshAsync();
        }
    }

    private bool CanBack() => SelectedAd is not null;

    [RelayCommand(CanExecute = nameof(CanBack))]
    private async Task EditSelectedAd()
    {
        using var scope = App.Services.CreateScope();
        var dialogContent = scope.ServiceProvider.GetRequiredService<IDialogContent<EditAdViewModel>>();
        await dialogContent.ViewModel.Initialize(SelectedAd!.Id);
        var result = await DialogHost.Show(dialogContent, EditAdViewModel.DialogId);

        if (result is true)
        {
            await RefreshAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanBack))]
    private async Task DeleteSelectedAd()
    {
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

        await dbContext.Ads.Where(e => e.Id == SelectedAd!.Id).ExecuteDeleteAsync();

        queue.Enqueue("Объявление снято с публикации.");
        await RefreshAsync();
    }
}

public partial class AdFullInfoViewModel : ObservableObject
{
    public required Guid Id { get; init; }

    public required DateTimeOffset CreationDateTime { get; init; }

    public required string CarBrandName { get; init; }

    public required string CarModelName { get; init; }

    public required int CarYear { get; init; }

    public required decimal Price { get; init; }

    public required string UserPhoneNumber { get; init; }

    [ObservableProperty] private bool _isAbleAddToFavorites;

    public required string Description { get; init; }

    [ObservableProperty] private BitmapImage? _selectedImage;

    [ObservableProperty] private IEnumerable<BitmapImage>? _images;
}

public partial class AdOverviewViewModel : ObservableObject
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required string Title { get; init; }

    public required DateTimeOffset CreationDateTime { get; init; }

    public required bool IsAbleAddToFavorites { get; init; }

    public required decimal Price { get; init; }

    public required Guid MainImageId { get; init; }

    [ObservableProperty] private BitmapImage? _mainImage;
}