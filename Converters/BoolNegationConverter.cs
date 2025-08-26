using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace WingetWizard.Avalonia.Converters;

/// <summary>
/// Converts boolean values to their negated equivalent
/// </summary>
public class BoolNegationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true; // Default to enabled if not a boolean
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}


