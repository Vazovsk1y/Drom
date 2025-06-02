using System.Windows;
using System.Windows.Threading;
using ClothesStore.WPF.DAL;
using ClothesStore.WPF.Infrastructure;
using ClothesStore.WPF.ViewModels;
using ClothesStore.WPF.Views;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClothesStore.WPF;

public partial class App : Application
{
    public static IServiceProvider Services { get; }
    
    static App()
    {
        var host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((ctx, e) =>
            {
                e.AddSingleton<MainWindow>();
                e.AddSingleton<MainWindowViewModel>();

                e.AddTransient<AuthViewModel>();
                e.AddTransient<IDialogContent<AuthViewModel>, AuthControl>();

                e.AddTransient<CreateProductViewModel>();
                e.AddTransient<IDialogContent<CreateProductViewModel>, CreateProductControl>();

                e.AddDbContext<DromDbContext>(o =>
                {
                    o.UseNpgsql(ctx.Configuration.GetConnectionString("Database"));
                    
                    // https://github.com/dotnet/efcore/issues/35285
                    o.ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning));
                });
                e.AddDbContextFactory<DromDbContext>();
                
                e.AddSingleton<ICurrentUserService, CurrentUserService>();
                
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    var snackbarMessageQueue = new SnackbarMessageQueue { DiscardDuplicates = true };
                    e.AddSingleton<ISnackbarMessageQueue>(snackbarMessageQueue);
                });

                e.AddSingleton<CatalogPageViewModel>();

                e.AddTransient<OkCancelDialogViewModel>();
                e.AddTransient<IDialogContent<OkCancelDialogViewModel>, OkCancelDialogControl>();

                e.AddTransient<EditProductViewModel>();
                e.AddTransient<IDialogContent<EditProductViewModel>, EditProductControl>();

                e.AddMemoryCache();
            })
            .Build();
        
        Services = host.Services;
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        dbContext.Database.Migrate();
        scope.Dispose();
        
        var mainWindow = Services.GetRequiredService<MainWindow>();
        var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainWindowViewModel;
        mainWindow.Show();
    }
}