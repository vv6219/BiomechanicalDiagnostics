namespace OrthoClinic.Core.Domain;

/// <summary>
/// Характер барьера вращения в тазобедренном суставе.
/// </summary>
public enum RotationBarrier
{
    MuscleFunctional, // Мышечный (функциональный) барьер
    BoneAnatomical    // Костный (анатомический) замок
}

/// <summary>
/// Торсия большеберцовой кости относительно мыщелков бедра.
/// </summary>
public enum TibialTorsion
{
    Normal,               // Физиологическая норма (параллельно/слегка кнаружи)
    UnderRotationInward,  // Недокрут внутрь (ось стопы кнутри)
    OverRotationOutward   // Перекрут кнаружи (ось стопы кнаружи, варус переднего отдела)
}

/// <summary>
/// Природа асимметрии длины нижних конечностей.
/// </summary>
public enum LegDiscrepancyType
{
    Indeterminate,    // Не дифференцировано
    TrueAnatomical,   // Истинное анатомическое укорочение
    FunctionalPelvic  // Функциональный перекос таза (скрученный/косой таз)
}

/// <summary>
/// Уровень двигательной и метаболической активности пациента.
/// </summary>
public enum PatientActivityLevel
{
    Sedentary = 0, // Малоактивный (коэффициент 1.00)
    Moderate = 1,  // Умеренный (коэффициент 0.75)
    Active = 2,    // Высокая активность (коэффициент 0.50)
    Athlete = 3    // Профессиональный спорт (коэффициент 0.25)
}

/// <summary>
/// Топографическое положение ортопедического клина на подошве / стельке.
/// </summary>
public enum WedgePlacementType
{
    AnteriorLateral,      // Передний наружный клин
    AnteriorMedial,       // Передний медиальный клин
    PosteriorMedial,      // Задний медиальный клин
    FifthMetatarsalBase,  // Клин под основание V плюсневой кости
    HeelLiftCompensator   // Разгрузочный подпяточник
}

/// <summary>
/// Глобальные динамические биомеханические маркеры (Тест 5 Formthotics / Beycroft).
/// </summary>
[Flags]
public enum GlobalBiomechanicalFlags : uint
{
    None = 0,
    FootFallsInwardTest5 = 1 << 0,  // Стопа сильно заваливается внутрь в 5-м тесте Formthotics
    FootFallsOutwardTest5 = 1 << 1  // Пациент заваливается кнаружи при динамической пробе
}

/// <summary>
/// Рекомендуемая твердость материала клина по шкале Шора А (EVA / термоформуемый полимер).
/// </summary>
public enum MaterialDurometerShoreA
{
    ShoreA35_Soft = 35,            // Мягкий (для малоактивных пациентов с тонкой жировой подушкой)
    ShoreA45_Medium = 45,          // Средний (базовый клинический стандарт)
    ShoreA55_Firm = 55,            // Плотный (высокая активность, повышенная упругость)
    ShoreA65_RigidComposite = 65  // Высокоплотный/композит (спорт, защита от динамического сминания)
}

/// <summary>
/// Уровень риска деструктивного сдвигового напряжения в суставной кинетической цепи.
/// </summary>
public enum KineticChainRiskLevel
{
    Low,
    Moderate,
    CriticalTorsionConflict // Торсионный конфликт антеторсии бедра и наружного перекрута голени
}

/// <summary>
/// Состояния клинического конечного автомата (FSM).
/// </summary>
public enum AssessmentFsmState
{
    Intake,
    LegLengthAssessment,
    OsteopathicThresholdGating,
    RotatorsAssessment,
    FemoralTorsionAssessment,
    TibialTorsionAssessment,
    DynamicGaitSynthesis,
    EvaluationCompleted
}
