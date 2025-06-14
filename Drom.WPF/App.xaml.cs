﻿using System.Windows;
using System.Windows.Threading;
using Drom.WPF.DAL;
using Drom.WPF.Infrastructure;
using Drom.WPF.ViewModels;
using Drom.WPF.Views;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Drom.WPF;

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

                e.AddTransient<CreateAdViewModel>();
                e.AddTransient<IDialogContent<CreateAdViewModel>, CreateAdControl>();

                e.AddTransient<DatesDialogViewModel>();
                e.AddTransient<IDialogContent<DatesDialogViewModel>, DatesDialog>();
                
                e.AddTransient<ChangePasswordViewModel>();
                e.AddTransient<IDialogContent<ChangePasswordViewModel>, ChangePasswordDialog>();

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
                e.AddSingleton<MyAdsPageViewModel>();
                e.AddSingleton<FavoritesPageViewModel>();

                e.AddTransient<OkCancelDialogViewModel>();
                e.AddTransient<IDialogContent<OkCancelDialogViewModel>, OkCancelDialogControl>();

                e.AddTransient<EditAdViewModel>();
                e.AddTransient<IDialogContent<EditAdViewModel>, EditAdControl>();

                e.AddTransient<IDialogContent<NewsItemAddViewModel>, NewsItemAddControl>();
                e.AddTransient<NewsItemAddViewModel>();
                e.AddSingleton<NewsPageViewModel>();
                
                e.AddTransient<IDialogContent<NewsItemEditViewModel>, NewsItemEditControl>();
                e.AddTransient<NewsItemEditViewModel>();

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