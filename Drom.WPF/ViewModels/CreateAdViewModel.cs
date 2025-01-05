using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Drom.WPF.DAL;
using Drom.WPF.DAL.Models;
using Drom.WPF.Infrastructure;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace Drom.WPF.ViewModels;

public partial class CreateAdViewModel : ObservableObject
{
    public const string DialogId = "CreateAdDialog";
    public const int MaxImagesCount = 6;

    public static readonly IEnumerable<int> Years = Enumerable
        .Range(1980, (DateTime.Now.Year - 1980) + 1)
        .ToList();

    public ObservableCollection<string> Images { get; } = [];

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
            if (Images.Contains(file))
            {
                continue;
            }
            
            if (Images.Count == MaxImagesCount)
            {
                return;
            }
            
            Images.Add(file);
        }
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task Confirm(string? selectedImagePath)
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
        
        selectedImagePath = string.IsNullOrWhiteSpace(selectedImagePath) ? Images.First() : selectedImagePath;
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        
        var adId = Guid.NewGuid();
        var images = new ConcurrentBag<AdImage>();
        
        var loadImages = Images.Select(async path =>
        {
            var image = new AdImage
            {
                Id = Guid.NewGuid(),
                AdId = adId,
                Bytes = await File.ReadAllBytesAsync(path),
                IsMain = path == selectedImagePath,
            };
            images.Add(image);
        });
        
        await Task.WhenAll(loadImages);
        
        var ad = new Ad
        {
            Id = adId,
            UserId = currentUser.Id,
            CreationDateTime = DateTimeOffset.UtcNow,
            CarModelName = CarModelName!,
            CarBrandName = CarBrandName!,
            CarYear = (int)CarYear!,
            Description = Description!,
            Price = (decimal)Price!,
        };
        
        dbContext.Ads.Add(ad);
        dbContext.AdImages.AddRange(images);
        await dbContext.SaveChangesAsync();
        
        DialogHost.Close(DialogId, true);
        snackBarQueue.Enqueue("Объявление успешно опубликовано.");
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(CarBrandName) &&
                                 !string.IsNullOrWhiteSpace(CarModelName) &&
                                 CarYear > 0 &&
                                 Price > 0 &&
                                 !string.IsNullOrWhiteSpace(Description);
}