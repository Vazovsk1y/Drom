using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using ClothesStore.WPF.DAL;
using ClothesStore.WPF.DAL.Models;
using ClothesStore.WPF.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ClothesStore.WPF.ViewModels;

public partial class EditProductViewModel : ObservableObject
{
    public const string DialogId = "EditProductDialog";

    public ObservableCollection<EditProductImageViewModel> Images { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _title;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _description;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private decimal? _price;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private EnumViewModel<ProductCategory>? _selectedCategory;

    private Guid _productId;
    
    public async Task Initialize(Guid productId)
    {
        _productId = productId;
        using var scope = App.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DromDbContext>>();
        await using var mainDbContext = await dbContextFactory.CreateDbContextAsync();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        
        var product = await mainDbContext
            .Products
            .FirstAsync(e => e.Id == productId);
        
        Title = product.Title;
        SelectedCategory = new EnumViewModel<ProductCategory>(product.Category);
        Description = product.Description;
        Price = product.Price;
        
        var images = await mainDbContext
            .ProductImages
            .Where(e => e.ProductId == productId)
            .Select(e => new { e.Id, e.IsMain })
            .ToListAsync();
        
        var resultImages = new ConcurrentBag<EditProductImageViewModel>();
        var loadImgTasks = images
            .Select(async item =>
            {
                var img = await cache.GetOrCreateAsync<BitmapImage>($"{product.Id}{item.Id}", async _ =>
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(); 
                    var entity = await dbContext.ProductImages.FirstAsync(e => e.Id == item.Id);
                    var bt = new BitmapImage();
                    using var stream = new MemoryStream(entity.Bytes);
                    bt.BeginInit();
                    bt.StreamSource = stream;
                    bt.CacheOption = BitmapCacheOption.OnLoad;
                    bt.EndInit();
                    return bt;
                });
                
                resultImages.Add(new EditProductImageViewModel()
                {
                    Id = item.Id,
                    IsMain = item.IsMain,
                    Value = img!,
                    IsNew = false,
                });
            });
        
        await Task.WhenAll(loadImgTasks);

        foreach (var img in resultImages)
        {
            Images.Add(img);
        }
    }
    
    [RelayCommand]
    private void SelectFiles()
    {
        const string title = "Выберите файлы:";
        const string filter = "Files|*.jpg;*.jpeg;*.png;";

        var fileDialog = new Microsoft.Win32.OpenFileDialog()
        {
            Title = title,
            RestoreDirectory = true,
            Multiselect = true,
            Filter = filter,
        };

        fileDialog.ShowDialog();
        foreach (var file in fileDialog.FileNames)
        {
            if (Images.Any(e => e.Value.UriSource?.AbsoluteUri == file))
            {
                continue;
            }
            
            if (Images.Count == CreateProductViewModel.MaxImagesCount)
            {
                return;
            }
            
            var value = new BitmapImage(new Uri(file));
            Images.Add(new EditProductImageViewModel()
            {
                Id = Guid.NewGuid(),
                IsMain = false,
                Value = value,
                IsNew = true,
            });
        }
    }

    [RelayCommand]
    private void RemoveImage(EditProductImageViewModel image)
    {
        Images.Remove(image);
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task Confirm()
    {
        if (ConfirmCommand.IsRunning || Images.Count == 0)
        {
            return;
        }

        using var scope = App.Services.CreateScope();
        var snackBarQueue = scope.ServiceProvider.GetRequiredService<ISnackbarMessageQueue>();
        
        if (scope.ServiceProvider.GetRequiredService<ICurrentUserService>().Get() is not {} currentUser)
        {
            DialogHost.Close(DialogId);
            snackBarQueue.Enqueue("Необходимо войти в аккаунт.");
            return;
        }
        
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        var product = await dbContext.Products.FirstAsync(e => e.Id == _productId);

        if (currentUser.Role == Role.Admin)
        {
            product.Description = Description!;
            product.Title = Title!;
            product.Price = (decimal)Price!;
            product.Category = SelectedCategory!.Value;
        
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                var imagesIds = await dbContext.ProductImages.Where(e => e.ProductId == _productId).Select(e => e.Id).ToListAsync();
                await dbContext.ProductImages.Where(e => e.ProductId == _productId).ExecuteDeleteAsync();
            
                var newMainImage = Images.FirstOrDefault(e => e.IsMain) ?? Images.First();

                var newImages = Images.Select(async e => new ProductImage
                {
                    Id = e.Id,
                    ProductId = _productId,
                    Bytes = e.IsNew ? await File.ReadAllBytesAsync(e.Value.UriSource.AbsolutePath) : StreamToBytes(e.Value.StreamSource),
                    IsMain = newMainImage.Id == e.Id,
                }).ToList();
            
                var newImagesArr = await Task.WhenAll(newImages);

                dbContext.ProductImages.AddRange(newImagesArr);
                await dbContext.SaveChangesAsync();
            
                await transaction.CommitAsync();
            
                foreach (var id in imagesIds.Where(e => !newImagesArr.Select(a => a.Id).Contains(e)))
                {
                    cache.Remove($"{product.Id}{id}");
                }
            
                DialogHost.Close(DialogId, true);
                snackBarQueue.Enqueue("Объявление успешно отредактировано.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            DialogHost.Close(DialogId);
            snackBarQueue.Enqueue("Недостаточно полномочий на выполнение данной операции.");
        }
    }
    
    private static byte[] StreamToBytes(Stream inputStream)
    {
        if (inputStream is MemoryStream mr)
        {
            return mr.ToArray();
        }

        using var memoryStream = new MemoryStream();
        inputStream.CopyTo(memoryStream);
        inputStream.Seek(0, SeekOrigin.Begin);
        return memoryStream.ToArray();
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(Title) &&
                                 Price > 0 &&
                                 !string.IsNullOrWhiteSpace(Description) &&
                                 SelectedCategory is not null;
}

public partial class EditProductImageViewModel : ObservableObject
{
    public required Guid Id { get; init; }

    public required BitmapImage Value { get; init; }

    [ObservableProperty] 
    private bool _isMain;
    
    public required bool IsNew { get; init; }
}