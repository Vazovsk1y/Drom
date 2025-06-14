using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using ClothesStore.WPF.DAL;
using ClothesStore.WPF.DAL.Models;
using ClothesStore.WPF.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace ClothesStore.WPF.ViewModels;

public partial class CreateProductViewModel : ObservableObject
{
    public const string DialogId = "CreateProductDialog";
    public const int MaxImagesCount = 6;
    
    public static readonly IEnumerable<EnumViewModel<ProductCategory>> Categories = Enum
        .GetValues<ProductCategory>()
        .Select(c => new EnumViewModel<ProductCategory>(c))
        .ToList();
    
    public ObservableCollection<string> Images { get; } = [];

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

    public IEnumerable<CreateProductSizeViewModel> Sizes { get; } = Enum
            .GetValues<ClothingSize>()
            .Select(e => new CreateProductSizeViewModel() { Size = e, Count = 1})
            .ToList();
    
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
        var images = new ConcurrentBag<ProductImage>();
        
        var loadImages = Images.Select(async path =>
        {
            var image = new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = adId,
                Bytes = await File.ReadAllBytesAsync(path),
                IsMain = path == selectedImagePath,
            };
            images.Add(image);
        });
        
        await Task.WhenAll(loadImages);
        
        var ad = new Product
        {
            Id = adId,
            Title = Title!,
            Description = Description!,
            Price = (decimal)Price!,
            Category = SelectedCategory!.Value,
        };

        var sizes = Sizes.Select(e => new SizeOption
        {
            Id = Guid.NewGuid(),
            Size = e.Size,
            QuantityInStock = (int)e.Count,
            ProductId = ad.Id,
        });
        
        dbContext.Products.Add(ad);
        dbContext.ProductImages.AddRange(images);
        dbContext.SizeOptions.AddRange(sizes);
        
        await dbContext.SaveChangesAsync();
        
        DialogHost.Close(DialogId, true);
        snackBarQueue.Enqueue("Объявление успешно опубликовано.");
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(Title) &&
                                 Price > 0 &&
                                 !string.IsNullOrWhiteSpace(Description) &&
                                 SelectedCategory is not null;
}

public partial class CreateProductSizeViewModel : ObservableObject
{
    public required ClothingSize Size { get; init; }

    [ObservableProperty]
    private uint _count;
}