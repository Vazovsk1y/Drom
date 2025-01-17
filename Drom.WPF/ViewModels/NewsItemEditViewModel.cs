using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Drom.WPF.DAL;
using Drom.WPF.DAL.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Drom.WPF.ViewModels;

public partial class NewsItemEditViewModel : ObservableObject
{
    public const string DialogId = "NewsItemEditDialog";
    
    private Guid _newsItemId;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _title;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _content;

    [ObservableProperty]
    private string? _imagePath;

    public async Task Initialize(Guid newsItemId)
    {
        _newsItemId = newsItemId;
        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        
        var target = await dbContext.News.FirstAsync(e => e.Id == newsItemId);
        Title = target.Title;
        Content = target.Content;
    }
    
    [RelayCommand]
    private void SelectFile()
    {
        const string title = "Выберите файлы:";
        const string filter = "Files|*.jpg;*.jpeg;*.png;";

        var fileDialog = new Microsoft.Win32.OpenFileDialog()
        {
            Title = title,
            RestoreDirectory = true,
            Multiselect = false,
            Filter = filter,
        };

        fileDialog.ShowDialog();
        ImagePath = fileDialog.FileName;
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task Confirm()
    {
        if (ConfirmCommand.IsRunning)
        {
            return;
        }

        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var target = await dbContext.News.FirstAsync(e => e.Id == _newsItemId);

        target.Title = Title!;
        target.Content = Content!;

        if (!string.IsNullOrWhiteSpace(ImagePath))
        {
            var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            memoryCache.Remove(target.Id);
            target.CoverImage = await File.ReadAllBytesAsync(ImagePath);
        }
        
        await dbContext.SaveChangesAsync();
        
        DialogHost.Close(DialogId, true);
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(Title) &&
                                 !string.IsNullOrWhiteSpace(Content);
}