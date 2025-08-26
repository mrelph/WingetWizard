using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace WingetWizard.Avalonia.Converters;

/// <summary>
/// Converts boolean values to bool for IsVisible property
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue;
        }
        return false;
    }
}


