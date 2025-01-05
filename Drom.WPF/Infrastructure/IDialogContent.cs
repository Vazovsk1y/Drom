using CommunityToolkit.Mvvm.ComponentModel;

namespace Drom.WPF.Infrastructure;

public interface IDialogContent<out T> where T : ObservableObject
{
    public T ViewModel { get; }
}