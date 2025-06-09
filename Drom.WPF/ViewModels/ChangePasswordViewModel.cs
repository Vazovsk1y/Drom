using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace Drom.WPF.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    public const string DialogId = "ChangePasswordViewModel";
    
    public string ActualCode { get; set; }
    
    [ObservableProperty]
    private bool _isCodeSubmitted;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(ConfirmCodeCommand))]
    private string? _code;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    private string? _newPassword;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    private string? _confirmPassword;

    public bool IsOkEnabled => NewPassword?.Length >= 6 && NewPassword == ConfirmPassword;

    [RelayCommand(CanExecute = nameof(CanConfirmCode))]
    private void ConfirmCode()
    {
        IsCodeSubmitted = true;
    }

    private bool CanConfirmCode() => ActualCode == Code;

    [RelayCommand]
    private void ResendCode()
    {
        ActualCode = string.Join(string.Empty, Enumerable.Range(0, 6).Select(_ => Random.Shared.Next(1, 9)));
        var queue = App.Services.GetRequiredService<ISnackbarMessageQueue>();
        queue.Enqueue(ActualCode);
    }
}