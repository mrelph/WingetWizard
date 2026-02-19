using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WingetWizard.Avalonia.Converters;

/// <summary>
/// Converts a boolean value to one of two string values based on parameter
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramString)
        {
            var parts = paramString.Split('|');
            if (parts.Length == 2)
            {
                return boolValue ? parts[1] : parts[0];
            }
        }
        
        return "⌄";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}