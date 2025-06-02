using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ClothesStore.WPF.ViewModels;

public record EnumViewModel<T> where T : Enum
{
    public string DisplayName { get; set; }
    
    public T Value { get; }
    
    public EnumViewModel(T value)
    {
        Value = value;
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DisplayAttribute>();
        DisplayName = attribute?.Name ?? value.ToString();
    }
}