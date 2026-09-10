namespace OrthoClinic.Core.Domain;

/// <summary>
/// Уровень клинической опасности предупреждения / алерта.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Категорическое противопоказание: блокирует назначение клиньев или требует немедленного устранения.
    /// </summary>
    CriticalContraindication,

    /// <summary>
    /// Кинематический резонансный конфликт (скручивание, стоячая волна деструкции сустава).
    /// </summary>
    KineticConflict,

    /// <summary>
    /// Превышение анатомического или адаптивного физиологического барьера.
    /// </summary>
    WarningThreshold,

    /// <summary>
    /// Клиническая оптимизация и протокол адаптивного ведения пациента.
    /// </summary>
    ClinicalAdvisory
}

/// <summary>
/// Анатомический уровень кинематической цепи, вовлеченный в дисфункцию.
/// </summary>
public enum AnatomicalSegment
{
    Pelvis_Sacrum,
    Femur_Hip,
    Tibia_Knee,
    Foot_Forefoot,
    Foot_Rearfoot,
    GlobalKineticChain
}

/// <summary>
/// Структурированное клиническое предупреждение экспертной системы OrthoClinic
/// с патогенетическим обоснованием и адаптивным планом действий подиатра.
/// </summary>
public sealed record ClinicalAlertItem(
    string AlertCode,
    AlertSeverity Severity,
    AnatomicalSegment AffectedSegment,
    string Title,
    string BiomechanicalRationale,
    string AdaptiveActionPlan,
    bool IsPrescriptionBlocked = false
);
