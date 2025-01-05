using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Drom.WPF.DAL;
using Drom.WPF.DAL.Models;
using Drom.WPF.Infrastructure;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Drom.WPF.ViewModels;

public partial class EditAdViewModel : ObservableObject
{
    public const string DialogId = "EditAdDialog";

    public ObservableCollection<EditAdImageViewModel> Images { get; } = [];

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _carModelName;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _carBrandName;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private int? _carYear;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _description;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private decimal? _price;

    private Guid _adId;
    
    public async Task Initialize(Guid adId)
    {
        _adId = adId;
        using var scope = App.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DromDbContext>>();
        await using var mainDbContext = await dbContextFactory.CreateDbContextAsync();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        
        var ad = await mainDbContext
            .Ads
            .FirstAsync(e => e.Id == adId);
        
        CarModelName = ad.CarModelName;
        CarBrandName = ad.CarBrandName;
        CarYear = ad.CarYear;
        Description = ad.Description;
        Price = ad.Price;
        
        var adImages = await mainDbContext
            .AdImages
            .Where(e => e.AdId == adId)
            .Select(e => new { e.Id, e.IsMain })
            .ToListAsync();
        
        var resultImages = new ConcurrentBag<EditAdImageViewModel>();
        var loadImgTasks = adImages
            .Select(async item =>
            {
                var img = await cache.GetOrCreateAsync<BitmapImage>($"{ad.Id}{item.Id}", async _ =>
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(); 
                    var entity = await dbContext.AdImages.FirstAsync(e => e.Id == item.Id);
                    var bt = new BitmapImage();
                    using var stream = new MemoryStream(entity.Bytes);
                    bt.BeginInit();
                    bt.StreamSource = stream;
                    bt.CacheOption = BitmapCacheOption.OnLoad;
                    bt.EndInit();
                    return bt;
                });
                
                resultImages.Add(new EditAdImageViewModel()
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
            
            if (Images.Count == CreateAdViewModel.MaxImagesCount)
            {
                return;
            }
            
            var value = new BitmapImage(new Uri(file));
            // value.BeginInit();
            // value.CacheOption = BitmapCacheOption.OnLoad;
            // value.EndInit();
            
            Images.Add(new EditAdImageViewModel()
            {
                Id = Guid.NewGuid(),
                IsMain = false,
                Value = value,
                IsNew = true,
            });
        }
    }

    [RelayCommand]
    private void RemoveImage(EditAdImageViewModel image)
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
        var ad = await dbContext.Ads.FirstAsync(e => e.Id == _adId);

        ad.Description = Description!;
        ad.CarBrandName = CarBrandName!;
        ad.CarModelName = CarModelName!;
        ad.Price = (decimal)Price!;
        ad.CarYear = (int)CarYear!;
        
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var imagesIds = await dbContext.AdImages.Where(e => e.AdId == _adId).Select(e => e.Id).ToListAsync();
            await dbContext.AdImages.Where(e => e.AdId == _adId).ExecuteDeleteAsync();
            
            var newMainImage = Images.FirstOrDefault(e => e.IsMain) ?? Images.First();

            var newImages = Images.Select(async e => new AdImage
            {
                Id = e.Id,
                AdId = _adId,
                Bytes = e.IsNew ? await File.ReadAllBytesAsync(e.Value.UriSource.AbsolutePath) : StreamToBytes(e.Value.StreamSource),
                IsMain = newMainImage.Id == e.Id,
            }).ToList();
            
            var newImagesArr = await Task.WhenAll(newImages);

            dbContext.AdImages.AddRange(newImagesArr);
            await dbContext.SaveChangesAsync();
            
            await transaction.CommitAsync();
            
            foreach (var id in imagesIds.Where(e => !newImagesArr.Select(a => a.Id).Contains(e)))
            {
                cache.Remove($"{ad.Id}{id}");
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

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(CarBrandName) &&
                                 !string.IsNullOrWhiteSpace(CarModelName) &&
                                 CarYear > 0 &&
                                 Price > 0 &&
                                 !string.IsNullOrWhiteSpace(Description);
}

public partial class EditAdImageViewModel : ObservableObject
{
    public required Guid Id { get; init; }

    public required BitmapImage Value { get; init; }

    [ObservableProperty] 
    private bool _isMain;
    
    public required bool IsNew { get; init; }
}