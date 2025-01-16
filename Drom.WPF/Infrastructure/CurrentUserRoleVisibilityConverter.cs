using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Drom.WPF.DAL.Models;

namespace Drom.WPF.Infrastructure;

public class CurrentUserRoleVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CurrentUser currentUser || parameter is not Role roleParameter)
        {
            return Visibility.Collapsed;
        }
        
        return currentUser.Role.Equals(roleParameter) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}