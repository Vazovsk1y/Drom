using System.Windows.Controls;
using Drom.WPF.Infrastructure;
using Drom.WPF.ViewModels;

namespace Drom.WPF.Views;

public partial class OkCancelDialogControl : UserControl, IDialogContent<OkCancelDialogViewModel>
{
    public OkCancelDialogViewModel ViewModel { get; }

    public OkCancelDialogControl(OkCancelDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }
}