using System.Windows.Controls;
using Drom.WPF.Infrastructure;
using Drom.WPF.ViewModels;

namespace Drom.WPF.Views;

public partial class ChangePasswordDialog : UserControl, IDialogContent<ChangePasswordViewModel>
{
    public ChangePasswordDialog(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
    }

    public ChangePasswordViewModel ViewModel { get; }
}