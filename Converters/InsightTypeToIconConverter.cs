using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WingetWizard.Avalonia.Converters;

/// <summary>
/// Converts AI insight type to appropriate icon
/// </summary>
public class InsightTypeToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string insightType)
        {
            return insightType.ToLower() switch
            {
                "security" => "🛡️",
                "performance" => "⚡",
                "compatibility" => "🔧",
                "warning" => "⚠️",
                "info" => "ℹ️",
                "success" => "✅",
                "update" => "🔄",
                _ => "💡"
            };
        }
        
        return "💡";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}