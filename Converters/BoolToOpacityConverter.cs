using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WingetWizard.Avalonia.Converters;

/// <summary>
/// Converts a boolean value to an opacity value for fade effects
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // If true, return low opacity (for loading states), if false return full opacity
            return boolValue ? 0.3 : 1.0;
        }
        return 1.0; // Default to full opacity
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}