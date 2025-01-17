using System.Windows.Controls;
using Drom.WPF.Infrastructure;
using Drom.WPF.ViewModels;

namespace Drom.WPF.Views;

public partial class NewsItemAddControl : UserControl, IDialogContent<NewsItemAddViewModel>
{
    public NewsItemAddViewModel ViewModel { get; }

    public NewsItemAddControl(NewsItemAddViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }
}