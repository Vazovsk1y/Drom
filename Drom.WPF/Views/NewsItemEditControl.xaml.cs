using System.Windows.Controls;
using Drom.WPF.Infrastructure;
using Drom.WPF.ViewModels;

namespace Drom.WPF.Views;

public partial class NewsItemEditControl : UserControl, IDialogContent<NewsItemEditViewModel>
{
    public NewsItemEditViewModel ViewModel { get; }

    public NewsItemEditControl(NewsItemEditViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }
}