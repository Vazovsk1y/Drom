using System.Windows;
using Drom.WPF.ViewModels;

namespace Drom.WPF.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PageChangedCommand.Execute(viewModel.NewsPageViewModel);
        }
    }
}