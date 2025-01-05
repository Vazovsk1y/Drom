using System.Windows.Controls;
using Drom.WPF.Infrastructure;
using Drom.WPF.ViewModels;

namespace Drom.WPF.Views;

public partial class EditAdControl : UserControl, IDialogContent<EditAdViewModel>
{
    public EditAdControl(EditAdViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }

    public EditAdViewModel ViewModel { get; }
}