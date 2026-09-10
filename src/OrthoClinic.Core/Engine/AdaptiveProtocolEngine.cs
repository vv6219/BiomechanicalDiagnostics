namespace OrthoClinic.Core.Engine;

using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Domain.Protocols;

/// <summary>
/// Реализация движка клинической генерации адаптивного подиатрического протокола Formthotics.
/// </summary>
public sealed class AdaptiveProtocolEngine : IAdaptiveProtocolEngine
{
    private const double StandardMoldingTemp = 85.0; // Термоформовка при 85°C

    public ComprehensivePodiatricProtocol GenerateProtocol(
        HorizontalInputData clinicalData,
        PatientActivityTier activityTier,
        double patientWeightKg,
        double patientBmi,
        int patientAge,
        bool hasMortonOrNeuropathy,
        bool isNarrowFootwear,
        IReadOnlyList<WedgeItemPrescription>? calculatedPrescriptions = null)
    {
        // 1. Определение клинической категории и модели стельки
        ClinicalCaseCategory category;
        string modelTitle;
        string densityDesc;
        double thicknessMm;

        if (patientAge < 15)
        {
            category = ClinicalCaseCategory.PediatricGrowth;
            modelTitle = "Formthotics Junior Red";
            densityDesc = "Junior Formax (Single Soft/Medium)";
            thicknessMm = 3.0; // Толщина детской серии
        }
        else if (hasMortonOrNeuropathy || (clinicalData.TibialStatus == TibialTorsion.UnderRotationInward && patientBmi < 20.0))
        {
            category = ClinicalCaseCategory.SensitiveNeuropathic;
            modelTitle = "Formthotics ShockStop Red/Green";
            densityDesc = "Hybrid ShockStop + Red Soft Formax (демпфирование до 40% пиковых ударов)";
            thicknessMm = 5.0;
        }
        else if (patientBmi >= 30.0 || patientWeightKg >= 95.0)
        {
            category = ClinicalCaseCategory.HighAxialCompression;
            modelTitle = "Formthotics Hard Black";
            densityDesc = "Firm Formax (High-Density) высокой жесткости";
            thicknessMm = 4.0;
        }
        else if (isNarrowFootwear)
        {
            category = ClinicalCaseCategory.ExecutiveNarrowShoe;
            modelTitle = "Formthotics Low Volume / 3/4 Beige";
            densityDesc = "Single Soft/Medium Low-Profile Formax";
            thicknessMm = 2.0; // Ультратонкая пена для модельной обуви
        }
        else if (activityTier is PatientActivityTier.Active or PatientActivityTier.Professional && patientBmi < 25.0)
        {
            category = ClinicalCaseCategory.DynamicAthletic;
            modelTitle = "Formthotics Original Single Blue";
            densityDesc = "Single Medium Formax (динамический возврат энергии)";
            thicknessMm = 3.5;
        }
        else
        {
            category = ClinicalCaseCategory.StandardBiomechanical;
            modelTitle = "Formthotics Dual-Density Red/Blue";
            densityDesc = "Dual-Density: Soft Red (верх) + Medium Blue (базис)";
            thicknessMm = 4.5;
        }

        // 2. Расчет коэффициента толщины клиньев по двигательной активности
        double activityCoeff = activityTier switch
        {
            PatientActivityTier.Sedentary => 1.00,    // 100% компенсации угла недостаточности
            PatientActivityTier.Moderate => 0.75,
            PatientActivityTier.Active => 0.50,
            PatientActivityTier.Professional => 0.25, // Минимальная толщина во избежание гиперкератоза
            _ => 0.75
        };

        // 3. Расчет клиньев и компенсаторов (ВСЕГДА ЗАПОЛНЕН)
        var wedges = new List<WedgingPlan>();

        // Если переданы уже рассчитанные предписания из HorizontalAssessmentEngine - синхронизируем их
        if (calculatedPrescriptions != null && calculatedPrescriptions.Count > 0)
        {
            foreach (var p in calculatedPrescriptions)
            {
                wedges.Add(new WedgingPlan(
                    Type: p.Placement,
                    NominalThicknessMm: p.NominalThicknessMm > 0 ? p.NominalThicknessMm : p.EffectiveThicknessMm,
                    CalculatedThicknessMm: p.EffectiveThicknessMm,
                    AnatomicalZone: string.IsNullOrWhiteSpace(p.AnatomicalZone) ? p.Placement.ToString() : p.AnatomicalZone,
                    BiomechanicalObjective: string.IsNullOrWhiteSpace(p.ForceVectorRationale) ? p.BiomechanicalReason : p.ForceVectorRationale
                ));
            }
        }
        else
        {
            // Автономный расчет по тестам
            if (clinicalData.TibialStatus == TibialTorsion.OverRotationOutward)
            {
                wedges.Add(new WedgingPlan(
                    Type: WedgePlacementType.AnteriorMedial,
                    NominalThicknessMm: 4.0,
                    CalculatedThicknessMm: Math.Round(4.0 * activityCoeff, 1),
                    AnatomicalZone: "Передний медиальный край",
                    BiomechanicalObjective: "Устранение варуса переднего отдела стопы и разгрузка 1-го луча"
                ));
            }
            else if (clinicalData.IsAntetorsion || clinicalData.PiriformisHypertonus)
            {
                wedges.Add(new WedgingPlan(
                    Type: WedgePlacementType.AnteriorLateral,
                    NominalThicknessMm: 3.5,
                    CalculatedThicknessMm: Math.Round(3.5 * activityCoeff, 1),
                    AnatomicalZone: "Передний латеральный сектор",
                    BiomechanicalObjective: "Компенсация внутренней ротации и стабилизация свода"
                ));
            }
            else if (clinicalData.TibialStatus == TibialTorsion.UnderRotationInward)
            {
                wedges.Add(new WedgingPlan(
                    Type: WedgePlacementType.AnteriorLateral,
                    NominalThicknessMm: 3.0,
                    CalculatedThicknessMm: Math.Round(3.0 * activityCoeff, 1),
                    AnatomicalZone: "Передний латеральный сектор",
                    BiomechanicalObjective: "Коррекция недокрута голени, устранение тенденции к вальгусу"
                ));
            }
            else if (clinicalData.IsRetrotorsion)
            {
                wedges.Add(new WedgingPlan(
                    Type: WedgePlacementType.AnteriorLateral,
                    NominalThicknessMm: 2.5,
                    CalculatedThicknessMm: Math.Round(2.5 * activityCoeff, 1),
                    AnatomicalZone: "Головки III–V плюсневых костей",
                    BiomechanicalObjective: "Супинационный момент переднего отдела стопы для деротации бедра"
                ));
            }

            double deltaMm = Math.Abs(clinicalData.LeftMalleolusMm - clinicalData.RightMalleolusMm);
            if (clinicalData.DiscrepancyNature == LegDiscrepancyType.TrueAnatomical && deltaMm > 0)
            {
                double nomHeel = Math.Round(deltaMm / 2.0, 1);
                wedges.Add(new WedgingPlan(
                    Type: WedgePlacementType.HeelLiftCompensator,
                    NominalThicknessMm: nomHeel,
                    CalculatedThicknessMm: Math.Round(nomHeel * activityCoeff, 1),
                    AnatomicalZone: "Пяточная чаша",
                    BiomechanicalObjective: $"Анатомическая разгрузочная компенсация длины конечности (ΔL = {deltaMm:F1} мм)"
                ));
            }
        }

        // Если клиньев нет (физиологическая норма) - ВСЕГДА фиксируем базовый физиологический протокол
        if (wedges.Count == 0)
        {
            wedges.Add(new WedgingPlan(
                Type: WedgePlacementType.AnteriorMedial,
                NominalThicknessMm: 0.0,
                CalculatedThicknessMm: 0.0,
                AnatomicalZone: "Физиологический свод стопы (базовый рельеф Formthotics)",
                BiomechanicalObjective: "Симметричная динамическая поддержка стопы. Анатомическая термоформовка 85°C обеспечивает центрацию подтаранного сустава без дополнительных клиньев."
            ));
        }

        // Расчет метатарзального пелота (ВСЕГДА ИНФОРМАТИВЕН)
        MetatarsalCorrectionPlan metaPad;
        string size = patientWeightKg > 80 ? "L" : (patientWeightKg < 60 ? "S" : "M");

        if (hasMortonOrNeuropathy || clinicalData.TibialStatus == TibialTorsion.OverRotationOutward)
        {
            metaPad = new MetatarsalCorrectionPlan(
                PadSize: size,
                PlacementZone: "Ретрокапитальная зона (позади головок II–IV плюсневых костей)",
                DecompressionTarget: "Подъем поперечного свода, расширение межплюсневых промежутков и декомпрессия подошвенных нервов"
            );
        }
        else
        {
            metaPad = new MetatarsalCorrectionPlan(
                PadSize: "Не требуется (норма)",
                PlacementZone: "Поперечный свод в пределах физиологической нормы",
                DecompressionTarget: "Признаков компрессии подошвенных нервов не выявлено. Поддержка естественной арки за счет формы заготовки."
            );
        }

        // 4. Построение дорожной карты адаптации (Дни 1-4, 5-14, 15-28)
        var timeline = new List<AdaptationStage>
        {
            new(
                StartDay: 1,
                EndDay: 4,
                StageTitle: "Этап первичной нейросенсорной адаптации",
                DailyWearHours: "2–3 часа в день в спокойном темпе ходьбы",
                KinematicMode: "Исключить бег, прыжки и спортивные нагрузки",
                ClinicalDirectives: new[] { "Формовка при 85°C в обуви", "Ношение только в базовой обуви со съемной стелькой" },
                WarningFlags: new[] { "При возникновении резких болей снять стельки до следующего дня" }
            ),
            new(
                StartDay: 5,
                EndDay: 14,
                StageTitle: "Этап динамической интеграции",
                DailyWearHours: "Полный рабочий день (6–8+ часов)",
                KinematicMode: "Повседневная ходьба, допуск к легким аэробным нагрузкам",
                ClinicalDirectives: new[] { "Контроль состояния кожных покровов сводов", "Оценка адаптации продольного свода" },
                WarningFlags: new[] { "Образование локальных покраснений указывает на необходимость снижения активности" }
            ),
            new(
                StartDay: 15,
                EndDay: 28,
                StageTitle: "Этап стабилизации кинематического стереотипа",
                DailyWearHours: "Постоянное ношение без ограничений",
                KinematicMode: "Полный спортивный и рабочий режим",
                ClinicalDirectives: new[] { "Закрепление правильной биомеханики переката", "Подготовка к контрольному осмотру" },
                WarningFlags: new[] { "Оценка зон максимального износа пены" }
            )
        };

        // 5. Теплофизические параметры
        int heatSec = (int)Math.Round(20.0 + 4.5 * thicknessMm);
        int coolSec = (int)Math.Round(120.0 * Math.Pow(thicknessMm / 3.5, 0.65));

        return new ComprehensivePodiatricProtocol(
            ClinicalProfile: category,
            RecommendedModelTitle: modelTitle,
            FoamDensityDescription: densityDesc,
            InsoleThicknessMm: thicknessMm,
            HeatingTemperatureC: StandardMoldingTemp,
            HeatingSeconds: heatSec,
            InShoeMoldingSeconds: coolSec,
            Wedges: wedges,
            MetatarsalPad: metaPad,
            Timeline: timeline,
            NextFollowUpDay: 28, // Контрольный осмотр через 28-30 дней
            CriticalClinicalAdvice: activityTier is PatientActivityTier.Professional or PatientActivityTier.Active
                ? "Внимание: Высокая физическая активность. Толщина клиньев минимальна во избежание срыва тканевой адаптации и мозолей."
                : "Стандартный протокол: Обеспечена полная статическая поддержка сводов стопы."
        );
    }
}
