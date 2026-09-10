namespace OrthoClinic.UI.Converters;

using System.Globalization;
using Microsoft.Maui.Controls;
using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Domain.Protocols;

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

/// <summary>
/// Конвертер цвета текста и рамки карточки в зависимости от AlertSeverity.
/// </summary>
public sealed class AlertSeverityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.CriticalContraindication => Color.FromArgb("#EF4444"),
                AlertSeverity.KineticConflict => Color.FromArgb("#F97316"),
                AlertSeverity.WarningThreshold => Color.FromArgb("#F59E0B"),
                AlertSeverity.ClinicalAdvisory => Color.FromArgb("#38BDF8"),
                _ => Color.FromArgb("#94A3B8")
            };
        }
        return Color.FromArgb("#94A3B8");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер фонового цвета карточки в зависимости от AlertSeverity.
/// </summary>
public sealed class AlertSeverityToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.CriticalContraindication => Color.FromArgb("#2D0A0A"),
                AlertSeverity.KineticConflict => Color.FromArgb("#2A1205"),
                AlertSeverity.WarningThreshold => Color.FromArgb("#2A1B05"),
                AlertSeverity.ClinicalAdvisory => Color.FromArgb("#0A2540"),
                _ => Color.FromArgb("#1E293B")
            };
        }
        return Color.FromArgb("#1E293B");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер заголовка бейджа в зависимости от AlertSeverity.
/// </summary>
public sealed class AlertSeverityToBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AlertSeverity severity)
        {
            return severity switch
            {
                AlertSeverity.CriticalContraindication => "🛑 ПРОТИВОПОКАЗАНИЕ",
                AlertSeverity.KineticConflict => "⚡ КИНЕМАТИЧЕСКИЙ КОНФЛИКТ",
                AlertSeverity.WarningThreshold => "⚠ ПОРОГОВЫЙ БАРЬЕР",
                AlertSeverity.ClinicalAdvisory => "💡 АДАПТИВНЫЙ ПРОТОКОЛ",
                _ => "ИНФОРМАЦИЯ"
            };
        }
        return "ИНФОРМАЦИЯ";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер цвета для индикатора индекса кинематического риска (KRS).
/// </summary>
public sealed class KrsToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double score)
        {
            if (score >= 70.0) return Color.FromArgb("#EF4444"); // Red
            if (score >= 45.0) return Color.FromArgb("#F97316"); // Orange
            if (score >= 25.0) return Color.FromArgb("#F59E0B"); // Amber
            return Color.FromArgb("#10B981");                    // Green
        }
        return Color.FromArgb("#10B981");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер текстового статуса для индикатора индекса кинематического риска (KRS).
/// </summary>
public sealed class KrsToBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double score)
        {
            if (score >= 70.0) return "КРИТИЧЕСКИЙ РИСК ДЕКОМПЕНСАЦИИ";
            if (score >= 45.0) return "ВЫСОКИЙ КИНЕМАТИЧЕСКИЙ РИСК";
            if (score >= 25.0) return "УМЕРЕННЫЙ РИСК";
            return "НИЗКИЙ РИСК (НОРМА)";
        }
        return "НОРМА";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер HEX-строки цвета в Color.
/// </summary>
public sealed class HexColorToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return Color.FromArgb(hex);
            }
            catch
            {
                return Color.FromArgb("#38BDF8");
            }
        }
        return Color.FromArgb("#38BDF8");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер перечисления твердости клина по Шору А в понятное русское описание материала.
/// </summary>
public sealed class DurometerToBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MaterialDurometerShoreA durometer)
        {
            return durometer switch
            {
                MaterialDurometerShoreA.ShoreA35_Soft => "Shore A 35 (Мягкий EVA / комфорт)",
                MaterialDurometerShoreA.ShoreA45_Medium => "Shore A 45 (Средняя плотность / стандарт)",
                MaterialDurometerShoreA.ShoreA55_Firm => "Shore A 55 (Упругий полимер / активность)",
                MaterialDurometerShoreA.ShoreA65_RigidComposite => "Shore A 65 (Жесткий композит / спорт)",
                _ => "Shore A 45"
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер номера квадранта стельки в анатомическую зону.
/// </summary>
public sealed class QuadrantToBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int quadrant)
        {
            return quadrant switch
            {
                1 => "Квадрант I: Передний медиальный",
                2 => "Квадрант II: Передний латеральный",
                3 => "Квадрант III: Задний медиальный",
                4 => "Квадрант IV: Латеральный средний (V луч)",
                5 => "Квадрант V: Пяточная чаша (компенсатор)",
                _ => $"Квадрант {quadrant}"
            };
        }
        return "Зона не указана";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер цвета для клинического профиля Formthotics.
/// </summary>
public sealed class ClinicalProfileToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ClinicalCaseCategory cat)
        {
            return cat switch
            {
                ClinicalCaseCategory.SensitiveNeuropathic => Color.FromArgb("#10B981"),
                ClinicalCaseCategory.DynamicAthletic => Color.FromArgb("#0284C7"),
                ClinicalCaseCategory.HighAxialCompression => Color.FromArgb("#94A3B8"),
                ClinicalCaseCategory.PediatricGrowth => Color.FromArgb("#F59E0B"),
                ClinicalCaseCategory.ExecutiveNarrowShoe => Color.FromArgb("#C084FC"),
                ClinicalCaseCategory.StandardBiomechanical => Color.FromArgb("#38BDF8"),
                _ => Color.FromArgb("#38BDF8")
            };
        }
        return Color.FromArgb("#38BDF8");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер русскоязычного названия для клинического профиля Formthotics.
/// </summary>
public sealed class ClinicalProfileToBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ClinicalCaseCategory cat)
        {
            return cat switch
            {
                ClinicalCaseCategory.SensitiveNeuropathic => "НЕЙРОПАТИЯ / МОРТОН / SHOCKSTOP",
                ClinicalCaseCategory.DynamicAthletic => "СПОРТ / ДИНАМИЧЕСКИЙ ВОЗВРАТ",
                ClinicalCaseCategory.HighAxialCompression => "ВЫСОКАЯ ОСЕВАЯ КОМПРЕССИЯ / ЖЕСТКИЙ",
                ClinicalCaseCategory.PediatricGrowth => "ЮВЕНИЛЬНЫЙ РОСТ / JUNIOR",
                ClinicalCaseCategory.ExecutiveNarrowShoe => "МОДЕЛЬНАЯ УЗКАЯ ОБУВЬ / LOW-PROFILE",
                ClinicalCaseCategory.StandardBiomechanical => "СТАНДАРТНАЯ БИОМЕХАНИКА / DUAL-DENSITY",
                _ => cat.ToString()
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер цвета статуса согласования протокола.
/// </summary>
public sealed class ApprovalStatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProtocolApprovalStatus status)
        {
            return status switch
            {
                ProtocolApprovalStatus.ApprovedByPodiatrist => Color.FromArgb("#10B981"),
                ProtocolApprovalStatus.DraftPendingApproval => Color.FromArgb("#F59E0B"),
                _ => Color.FromArgb("#94A3B8")
            };
        }
        return Color.FromArgb("#94A3B8");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер текстового статуса согласования протокола.
/// </summary>
public sealed class ApprovalStatusToBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProtocolApprovalStatus status)
        {
            return status switch
            {
                ProtocolApprovalStatus.ApprovedByPodiatrist => "✓ УТВЕРЖДЕНО ВРАЧОМ-ПОДИАТРОМ",
                ProtocolApprovalStatus.DraftPendingApproval => "⏳ ЧЕРНОВИК (ОЖИДАЕТ УТВЕРЖДЕНИЯ)",
                _ => "НЕ ОПРЕДЕЛЕНО"
            };
        }
        return "⏳ ЧЕРНОВИК (ОЖИДАЕТ УТВЕРЖДЕНИЯ)";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер проверки на непустую строку / коллекцию (скрывает пустые карточки и блоки).
/// </summary>
public sealed class NotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return false;
        if (value is string s) return !string.IsNullOrWhiteSpace(s);
        if (value is System.Collections.ICollection col) return col.Count > 0;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Конвертер анатомического сегмента в понятное русское название (вместо enum 0, 1, 2).
/// </summary>
public sealed class AnatomicalSegmentToNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AnatomicalSegment segment)
        {
            return segment switch
            {
                AnatomicalSegment.Pelvis_Sacrum => "Таз / Крестец",
                AnatomicalSegment.Femur_Hip => "Бедро / ТБС",
                AnatomicalSegment.Tibia_Knee => "Голень / Колено",
                AnatomicalSegment.Foot_Forefoot => "Передний отдел стопы",
                AnatomicalSegment.Foot_Rearfoot => "Задний отдел стопы",
                AnatomicalSegment.GlobalKineticChain => "Кинетическая цепь",
                _ => segment.ToString()
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

