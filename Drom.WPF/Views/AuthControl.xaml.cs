using System.Windows.Controls;
using Drom.WPF.Infrastructure;
using Drom.WPF.ViewModels;

namespace Drom.WPF.Views;

public partial class AuthControl : UserControl, IDialogContent<AuthViewModel>
{
    public AuthViewModel ViewModel { get; }

    public AuthControl(AuthViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewModel = viewModel;
    }
}