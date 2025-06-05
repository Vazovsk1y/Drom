using System.Windows.Controls;
using Drom.WPF.Infrastructure;
using Drom.WPF.ViewModels;

namespace Drom.WPF.Views;

public partial class DatesDialog : UserControl, IDialogContent<DatesDialogViewModel>
{
    public DatesDialog(DatesDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public DatesDialogViewModel ViewModel { get; }
}