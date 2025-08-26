using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace WingetWizard.Avalonia.Converters
{
    /// <summary>
    /// Converts available version string to status text
    /// </summary>
    public class UpdateAvailableToStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string available && !string.IsNullOrWhiteSpace(available))
            {
                return "Update Available";
            }
            return "Up to Date";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts available version string to status brush color
    /// </summary>
    public class UpdateAvailableToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string available && !string.IsNullOrWhiteSpace(available))
            {
                // Orange for updates available
                return new SolidColorBrush(Color.FromRgb(245, 158, 11)); // #F59E0B
            }
            // Green for up to date
            return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts string to boolean indicating if not empty
    /// </summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts integer to boolean indicating if greater than zero
    /// </summary>
    public class GreaterThanZeroConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}