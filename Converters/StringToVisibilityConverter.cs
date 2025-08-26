using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace WingetWizard.Avalonia.Converters;

/// <summary>
/// Converts a string to bool for IsVisible - true if string has content, false if empty/null
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            return true;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

