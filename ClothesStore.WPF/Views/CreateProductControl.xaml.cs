using System.Windows.Controls;
using ClothesStore.WPF.Infrastructure;
using ClothesStore.WPF.ViewModels;

namespace ClothesStore.WPF.Views;

public partial class CreateProductControl : UserControl, IDialogContent<CreateProductViewModel>
{
    public CreateProductViewModel ViewModel { get; }

    public CreateProductControl(CreateProductViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewModel = viewModel;
    }
}