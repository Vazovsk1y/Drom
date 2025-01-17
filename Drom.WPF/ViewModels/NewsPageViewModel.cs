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

public partial class NewsPageViewModel : ObservableObject, IHasPageIndex, IRefreshable
{
    public int PageIndex { get; } = 0;
    
    private bool _isRefreshingRunning;
    
    [ObservableProperty]
    private ObservableCollection<NewsItemViewModel>? _news;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedItemCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditSelectedItemCommand))]
    private NewsItemViewModel? _selectedItem;
    
    public async Task RefreshAsync()
    {
        if (_isRefreshingRunning)
        {
            return;
        }
        
        _isRefreshingRunning = true;
        SelectedItem = null;
        using var scope = App.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DromDbContext>>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        await using var mainDbContext = await dbContextFactory.CreateDbContextAsync();
        
        var items = await mainDbContext
            .News
            .OrderBy(e => e.PublicationDateTime)
            .Select(e => new NewsItemViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Content = e.Content,
                PublicationDateTime = e.PublicationDateTime.ToLocalTime(),
            })
            .ToListAsync();
        
        var imgs = items
            .Select(async item =>
            {
                var img = await cache.GetOrCreateAsync<BitmapImage>($"{item.Id}", async _ =>
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
                    var bytes = await dbContext.News.Where(e => e.Id == item.Id).Select(e => e.CoverImage).FirstAsync();
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
                    item.CoverImage = img;
                });
            });
        
        await Task.WhenAll(imgs);
        
        News = new ObservableCollection<NewsItemViewModel>(items);
        _isRefreshingRunning = false;
    }
    
    
    [RelayCommand(CanExecute = nameof(CanBack))]
    private void Back() => SelectedItem = null;

    private bool CanBack() => SelectedItem is not null;
    
    [RelayCommand]
    private void OnNewsItemSelected(NewsItemViewModel? ni)
    {
        if (ni is null)
        {
            return;
        }

        SelectedItem = ni;
    }
    
    [RelayCommand(CanExecute = nameof(CanBack))]
    private async Task DeleteSelectedItem()
    {
        using var scope = App.Services.CreateScope();
        var dialogContent = scope.ServiceProvider.GetRequiredService<IDialogContent<OkCancelDialogViewModel>>();
        dialogContent.ViewModel.Message = "Вы уверены, что хотите удалить статью?";

        var result = await DialogHost.Show(dialogContent, OkCancelDialogViewModel.DialogId);
        if (result is not true)
        {
            return;
        }
        
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var queue = scope.ServiceProvider.GetRequiredService<ISnackbarMessageQueue>();
        
        await dbContext.News.Where(e => e.Id == SelectedItem!.Id).ExecuteDeleteAsync();
        
        queue.Enqueue("Статья успешно удалена.");
        await RefreshAsync();
    }
    
    [RelayCommand(CanExecute = nameof(CanBack))]
    private async Task EditSelectedItem()
    {
        using var scope = App.Services.CreateScope();
        var dialogContent = scope.ServiceProvider.GetRequiredService<IDialogContent<NewsItemEditViewModel>>();
        await dialogContent.ViewModel.Initialize(SelectedItem!.Id);
        var result = await DialogHost.Show(dialogContent, NewsItemEditViewModel.DialogId);

        if (result is true)
        {
            await RefreshAsync();
        }
    }
}

public partial class NewsItemViewModel : ObservableObject
{
    public required Guid Id { get; init; }
    
    public required string Title { get; init; }
    
    public required string Content { get; init; }
    
    public required DateTimeOffset PublicationDateTime { get; init; }
    
    [ObservableProperty]
    private BitmapImage? _coverImage;
}