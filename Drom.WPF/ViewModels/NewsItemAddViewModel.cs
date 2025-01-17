using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Drom.WPF.DAL;
using Drom.WPF.DAL.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace Drom.WPF.ViewModels;

public partial class NewsItemAddViewModel : ObservableObject
{
    public const string DialogId = "NewsItemAddDialog";
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _title;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _content;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _imagePath;
    
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
        var snackBarQueue = scope.ServiceProvider.GetRequiredService<ISnackbarMessageQueue>();

        var item = new NewsItem
        {
            Id = Guid.NewGuid(),
            PublicationDateTime = DateTimeOffset.UtcNow,
            Title = Title!,
            Content = Content!,
            CoverImage = await File.ReadAllBytesAsync(ImagePath!),
        };
        
        dbContext.Add(item);
        await dbContext.SaveChangesAsync();
        
        DialogHost.Close(DialogId, true);
        snackBarQueue.Enqueue("Новость успешно опубликована.");
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(Title) &&
                                 !string.IsNullOrWhiteSpace(Content) &&
                                 !string.IsNullOrWhiteSpace(ImagePath);
}