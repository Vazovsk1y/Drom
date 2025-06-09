using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Drom.WPF.DAL;
using Drom.WPF.DAL.Models;
using Drom.WPF.Infrastructure;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drom.WPF.ViewModels;

public partial class AuthViewModel : ObservableObject
{
    public const string DialogId = "AuthDialog";
    
    public const string PhoneNumberMask = "+7 000 000 0000";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    [NotifyCanExecuteChangedFor(nameof(ForgotPasswordCommand))]
    private string? _usernameOrPhoneNumber;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string? _password;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string? _username;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string? _phoneNumber;
    
    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task Register()
    {
        if (RegisterCommand.IsRunning)
        {
            return;
        }

        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

        if (await dbContext.Users.AnyAsync(e => e.Username == Username))
        {
            ErrorMessage = "Логин занят.";
            return;
        }

        if (await dbContext.Users.AnyAsync(e => e.PhoneNumber == PhoneNumber))
        {
            ErrorMessage = "Номер телефона занят.";
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            Username = Username!,
            PhoneNumber = PhoneNumber!,
            Role = Role.User,
            RegistrationDateTime = DateTimeOffset.UtcNow,
        };
        
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        currentUserService.Set(user);
        DialogHost.Close(DialogId, true);
    }
    
    [RelayCommand(CanExecute = nameof(CanForgotPassword))]
    private async Task ForgotPassword()
    {
        using var scope = App.Services.CreateScope();
        var dialogContent = scope.ServiceProvider.GetRequiredService<IDialogContent<ChangePasswordViewModel>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var queue = scope.ServiceProvider.GetRequiredService<ISnackbarMessageQueue>();
        
        var user = await dbContext.Users.FirstOrDefaultAsync(e => e.Username == UsernameOrPhoneNumber) ?? 
                   await dbContext.Users.FirstOrDefaultAsync(e => e.PhoneNumber == UsernameOrPhoneNumber);
        
        if (user is null)
        {
            ErrorMessage = "Неверный логин или пароль.";
            return;
        }

        dialogContent.ViewModel.ActualCode = string.Join(string.Empty, Enumerable.Range(0, 6).Select(_ => Random.Shared.Next(1, 9)));
        
        queue.Enqueue(dialogContent.ViewModel.ActualCode);
        
        DialogHost.Close(DialogId);
        var result = await DialogHost.Show(dialogContent, ChangePasswordViewModel.DialogId);
        
        if (result is not true)
        {
            return;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dialogContent.ViewModel.NewPassword);
        await dbContext.SaveChangesAsync();
        queue.Enqueue("Пароль успешно изменен");

        var vm = scope.ServiceProvider.GetRequiredService<MainWindowViewModel>();
        vm.OpenAuthDialogCommand.Execute(null);
    }

    private bool CanForgotPassword() => !string.IsNullOrWhiteSpace(UsernameOrPhoneNumber);

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignIn()
    {
        if (SignInCommand.IsRunning)
        {
            return;
        }
        
        using var scope = App.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DromDbContext>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

        var user = await dbContext.Users.FirstOrDefaultAsync(e => e.Username == UsernameOrPhoneNumber) ?? 
                   await dbContext.Users.FirstOrDefaultAsync(e => e.PhoneNumber == UsernameOrPhoneNumber);
        
        if (user is null)
        {
            ErrorMessage = "Неверный логин или пароль.";
            return;
        }

        if (!BCrypt.Net.BCrypt.Verify(Password!, user.PasswordHash))
        {
            ErrorMessage = "Неверный логин или пароль.";
            return;
        }
        
        currentUserService.Set(user);
        DialogHost.Close(DialogId, true);
    }

    [RelayCommand]
    private void OnTabChanged()
    {
        Password = null;
        UsernameOrPhoneNumber = null;
        Username = null;
        PhoneNumber = null;
        ErrorMessage = null;
    }
    
    private bool CanSignIn() => !string.IsNullOrWhiteSpace(UsernameOrPhoneNumber) && 
                                !string.IsNullOrWhiteSpace(Password);

    private bool CanRegister() => !string.IsNullOrWhiteSpace(Username) &&
                                  !string.IsNullOrWhiteSpace(PhoneNumber) && IsPhoneNumber().IsMatch(PhoneNumber) &&
                                  !string.IsNullOrWhiteSpace(Password) && Password.Length > 6;

    [GeneratedRegex(@"^\+7 \d{3} \d{3} \d{4}$")]
    private static partial Regex IsPhoneNumber();
}