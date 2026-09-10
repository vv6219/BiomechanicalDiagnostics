namespace OrthoClinic.Core.Domain.Protocols;

using OrthoClinic.Core.Domain;

/// <summary>
/// Уровень двигательной активности пациента для расчета толщины и плотности ортезов Formthotics.
/// </summary>
public enum PatientActivityTier
{
    /// <summary>
    /// Малоактивный образ жизни (до 5 000 шагов/день, постельный/офисный режим). Коэффициент 1.00.
    /// </summary>
    Sedentary,

    /// <summary>
    /// Стандартный городской режим (5 000 - 10 000 шагов/день). Коэффициент 0.75.
    /// </summary>
    Moderate,

    /// <summary>
    /// Регулярные тренировки / работа на ногах (10 000 - 15 000 шагов/день). Коэффициент 0.50.
    /// </summary>
    Active,

    /// <summary>
    /// Профессиональный спорт / тяжелые осевые нагрузки (> 15 000 шагов/день). Коэффициент 0.25.
    /// </summary>
    Professional
}

/// <summary>
/// Клинический профиль патологии и кинематического паттерна пациента.
/// </summary>
public enum ClinicalCaseCategory
{
    /// <summary>
    /// Стандартная биомеханическая коррекция (гиперпронация, плантарный фасциит).
    /// </summary>
    StandardBiomechanical,

    /// <summary>
    /// Динамический спортивный профиль (бег, кардио при нормальном BMI).
    /// </summary>
    DynamicAthletic,

    /// <summary>
    /// Высокая осевая компрессия (BMI >= 30 или вес >= 95 кг, тяжелый физический труд).
    /// </summary>
    HighAxialCompression,

    /// <summary>
    /// Чувствительная нейропатическая стопа (диабет, неврома Мортона, истончение жировой подушки).
    /// </summary>
    SensitiveNeuropathic,

    /// <summary>
    /// Активный скелетный рост (дети и подростки до 15 лет).
    /// </summary>
    PediatricGrowth,

    /// <summary>
    /// Узкая модельная обувь без запаса внутреннего объема.
    /// </summary>
    ExecutiveNarrowShoe
}

/// <summary>
/// Статус клинического согласования и утверждения протокола врачом.
/// </summary>
public enum ProtocolApprovalStatus
{
    DraftPendingApproval,
    ApprovedByPodiatrist
}

/// <summary>
/// Этап постепенной нейромышечной и кинематической адаптации к ортопедическим стелькам.
/// </summary>
public sealed record AdaptationStage(
    int StartDay,
    int EndDay,
    string StageTitle,
    string DailyWearHours,
    string KinematicMode,
    IReadOnlyList<string> ClinicalDirectives,
    IReadOnlyList<string> WarningFlags
);

/// <summary>
/// План наложения корректирующего клина с калибровкой по активности пациента.
/// </summary>
public sealed record WedgingPlan(
    WedgePlacementType Type,
    double NominalThicknessMm,
    double CalculatedThicknessMm,
    string AnatomicalZone,
    string BiomechanicalObjective
);

/// <summary>
/// План установки метатарзального пелота (капли) для декомпрессии нервов и поперечного свода.
/// </summary>
public sealed record MetatarsalCorrectionPlan(
    string PadSize, // S, M, L
    string PlacementZone,
    string DecompressionTarget
);

/// <summary>
/// Полный адаптивный подиатрический протокол подбора, термоформовки и динамической адаптации.
/// </summary>
public sealed record ComprehensivePodiatricProtocol(
    ClinicalCaseCategory ClinicalProfile,
    string RecommendedModelTitle,
    string FoamDensityDescription,
    double InsoleThicknessMm,
    double HeatingTemperatureC, // 85°C
    int HeatingSeconds,
    int InShoeMoldingSeconds,
    IReadOnlyList<WedgingPlan> Wedges,
    MetatarsalCorrectionPlan? MetatarsalPad,
    IReadOnlyList<AdaptationStage> Timeline,
    int NextFollowUpDay, // 28-30 день
    string CriticalClinicalAdvice
);

/// <summary>
/// Запись утверждения протокола врачом-подиатором с цифровой подписью.
/// </summary>
public sealed record ProtocolApprovalRecord(
    ProtocolApprovalStatus Status,
    string ApprovedByDoctorName,
    DateTime? ApprovedAtUtc,
    string? DoctorNotes,
    string ApprovalDigitalFingerprint
);
