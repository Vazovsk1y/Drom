using System.Windows.Controls;
using Drom.WPF.Infrastructure;
using Drom.WPF.ViewModels;

namespace Drom.WPF.Views;

public partial class CreateAdControl : UserControl, IDialogContent<CreateAdViewModel>
{
    public CreateAdViewModel ViewModel { get; }

    public CreateAdControl(CreateAdViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewModel = viewModel;
    }
}