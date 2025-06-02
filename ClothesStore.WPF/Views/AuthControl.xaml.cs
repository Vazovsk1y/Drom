using System.Windows.Controls;
using ClothesStore.WPF.Infrastructure;
using ClothesStore.WPF.ViewModels;

namespace ClothesStore.WPF.Views;

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