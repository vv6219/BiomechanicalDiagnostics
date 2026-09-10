namespace OrthoClinic.UI.Converters;

using System.Globalization;
using Microsoft.Maui.Controls;
using OrthoClinic.Core.Domain;

/// <summary>
/// Конвертер проверки на NotNull (возвращает true, если значение не равно null).
/// </summary>
public sealed class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Инвертор булевого значения.
/// </summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

/// <summary>
/// Конвертер сравнения перечисления для переключения кнопок/радиогрупп.
/// </summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Конвертер цвета активной/неактивной кнопки выбора перечисления.
/// </summary>
public sealed class EnumToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Color.FromArgb("#334155");
        bool isMatch = value.ToString() == parameter.ToString();
        return isMatch ? Color.FromArgb("#0284C7") : Color.FromArgb("#334155");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
