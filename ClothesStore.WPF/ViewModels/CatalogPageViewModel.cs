using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ClothesStore.WPF.DAL;
using ClothesStore.WPF.DAL.Models;
using ClothesStore.WPF.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using MaterialDesignThemes.Wpf.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ClothesStore.WPF.ViewModels;

public partial class CatalogPageViewModel : ObservableObject, IHasPageIndex, IRefreshable
{
    public int PageIndex => 0;

    private bool _isRefreshingRunning;
    
    [ObservableProperty]
    private IEnumerable<ProductOverviewViewModel>? _catalogItems;
    
    [ObservableProperty]
    private ICollectionView? _catalogItemsView;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditSelectedProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedProductCommand))]
    private ProductFullInfoViewModel? _selectedProduct;
    
    [ObservableProperty]
    private string? _searchText;

    public async Task RefreshAsync()
    {
        if (_isRefreshingRunning)
        {
            return;
        }
        
        _isRefreshingRunning = true;
        SelectedProduct = null;
        using var scope = App.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DromDbContext>>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        
        await using var mainDbContext = await dbContextFactory.CreateDbContextAsync();
        
        var items = await mainDbContext
            .Products
            .OrderBy(e => e.Title)
            .Select(e => new ProductOverviewViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Price = e.Price,
                MainImageId = e.Images.First(i => i.IsMain).Id,
            })
            .ToListAsync();
        
        var loadMainImages = items
            .Select(async item =>
            {
                var img = await cache.GetOrCreateAsync<BitmapImage>($"{item.Id}{item.MainImageId}", async _ =>
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(); 
                    var bytes = (await dbContext.ProductImages.FirstAsync(e => e.Id == item.MainImageId)).Bytes;
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

        return obj is ProductOverviewViewModel ad && ad.Title.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase);
    }
    
    [RelayCommand]
    private void Search()
    {
        CatalogItemsView?.Refresh();
    }

    partial void OnSearchTextChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            CatalogItemsView?.Refresh();
        }
    }

    [RelayCommand]
    private async Task OnProductSelected(ProductOverviewViewModel? product)
    {
        if (product is null)
        {
            return;
        }
        
        using var scope = App.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DromDbContext>>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        await using var mainDbContext = await dbContextFactory.CreateDbContextAsync();
        
        var target = await mainDbContext.Products.Include(e => e.AvailableSizes).FirstAsync(e => e.Id == product.Id);

        var vm = new ProductFullInfoViewModel
        {
            Id = target.Id,
            Title = product.Title,
            Price = target.Price,
            Description = target.Description,
            ClothingSizes = target.AvailableSizes
                .Select(e => e.Size)
                .ToList(),
        };
        
        var ids = await mainDbContext.ProductImages.Where(e => e.ProductId == product.Id).Select(e => e.Id).ToListAsync();

        var res = new ConcurrentBag<BitmapImage>();
        
        var loadTasks = ids
            .Select(async item =>
            {
                var img = await cache.GetOrCreateAsync<BitmapImage>($"{product.Id}{item}", async _ =>
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(); 
                    var bytes = (await dbContext.ProductImages.FirstAsync(e => e.Id == item)).Bytes;
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
        SelectedProduct = vm;
    }

    [RelayCommand(CanExecute = nameof(CanBack))]
    private void Back()
    {
        SelectedProduct = null;
    }
    
    private bool CanBack() => SelectedProduct is not null;

    [RelayCommand(CanExecute = nameof(CanBack))]
    private async Task EditSelectedProduct()
    {
        using var scope = App.Services.CreateScope();
        var dialogContent = scope.ServiceProvider.GetRequiredService<IDialogContent<EditProductViewModel>>();
        await dialogContent.ViewModel.Initialize(SelectedProduct!.Id);
        var result = await DialogHost.Show(dialogContent, EditProductViewModel.DialogId);

        if (result is true)
        {
            await RefreshAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanBack))]
    private async Task DeleteSelectedProduct()
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
        
        await dbContext.Products.Where(e => e.Id == SelectedProduct!.Id).ExecuteDeleteAsync();
        
        queue.Enqueue("Объявление снято с публикации.");
        await RefreshAsync();
    }


    [RelayCommand]
    private void SelectSize(ClothingSize selectedSize)
    {
        SelectedProduct!.SelectedClothingSize = selectedSize;
    }
    
    [RelayCommand]
    private async Task AddToBasket()
    {
        // TODO: 
    }
}

public partial class ProductFullInfoViewModel : ObservableObject
{
    public required Guid Id { get; init; }
    
    public required string Title { get; init; }
    
    public required string Description { get; init; }
    
    public required decimal Price { get; init; }
    
    public required IEnumerable<ClothingSize> ClothingSizes { get; init; }
    
    [ObservableProperty]
    private ClothingSize? _selectedClothingSize;
    
    [ObservableProperty]
    private BitmapImage? _selectedImage;
    
    [ObservableProperty]
    private IEnumerable<BitmapImage>? _images;
}

public partial class ProductOverviewViewModel : ObservableObject
{
    public required Guid Id { get; init; }
    
    public required string Title { get; init; }
    
    public required decimal Price { get; init; }
    
    public required Guid MainImageId { get; init; }
    
    [ObservableProperty]
    private BitmapImage? _mainImage;
}
