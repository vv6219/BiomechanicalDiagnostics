namespace OrthoClinic.Core.Engine;

using OrthoClinic.Core.Domain;

/// <summary>
/// Клинический детерминированный алгоритмический движок сопоставления
/// результатов горизонтального тестирования и подбора ортопедических клиньев.
/// </summary>
public sealed class HorizontalAssessmentEngine : IHorizontalAssessmentEngine
{
    private const double MaxSafeSegmentCorrectionMm = 6.0;

    public HorizontalDiagnosticReport Evaluate(HorizontalInputData input)
    {
        var alerts = new List<string>();
        var rawPrescriptions = new List<WedgeItemPrescription>();
        double deltaMm = Math.Abs(input.LeftMalleolusMm - input.RightMalleolusMm);
        bool needsOsteopathy = false;

        // Коэффициент толщины по уровню метаболической и кинематической активности пациента
        double activityCoeff = input.Activity switch
        {
            PatientActivityLevel.Sedentary => 1.00,
            PatientActivityLevel.Moderate => 0.75,
            PatientActivityLevel.Active => 0.50,
            PatientActivityLevel.Athlete => 0.25,
            _ => 0.75
        };

        // Рекомендуемая твердость материала по Шору А
        var durometer = input.Activity switch
        {
            PatientActivityLevel.Sedentary => MaterialDurometerShoreA.ShoreA35_Soft,
            PatientActivityLevel.Moderate => MaterialDurometerShoreA.ShoreA45_Medium,
            PatientActivityLevel.Active => MaterialDurometerShoreA.ShoreA55_Firm,
            PatientActivityLevel.Athlete => MaterialDurometerShoreA.ShoreA65_RigidComposite,
            _ => MaterialDurometerShoreA.ShoreA45_Medium
        };

        // ТЕСТ 1: Оценка длины ног и остеопатический порог
        if (deltaMm > 3.0)
        {
            needsOsteopathy = true;
            alerts.Add("Показано применение дополнительных остеопатических тестов.");

            if (input.DiscrepancyNature == LegDiscrepancyType.FunctionalPelvic)
            {
                alerts.Add("Выявлен функциональный перекос таза. Коррекция подпяточником противопоказана во избежание фиксации дисфункции.");
            }
            else if (input.DiscrepancyNature == LegDiscrepancyType.TrueAnatomical)
            {
                double heelThickness = Math.Round((deltaMm / 2.0) * activityCoeff, 1, MidpointRounding.AwayFromZero);
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.HeelLiftCompensator,
                    EffectiveThicknessMm: heelThickness,
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: $"Истинное анатомическое укорочение ({deltaMm:F1} мм). Частичная разгрузочная компенсация.",
                    ControlTestMandate: "Оценка горизонтали крестца.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 5,
                    NominalThicknessMm: Math.Round(deltaMm / 2.0, 1, MidpointRounding.AwayFromZero)
                ));
            }
        }

        // ТЕСТ 2: Тест ротаторов (грушевидные мышцы)
        if (input.PiriformisHypertonus)
        {
            rawPrescriptions.Add(new WedgeItemPrescription(
                Placement: WedgePlacementType.AnteriorLateral,
                EffectiveThicknessMm: Math.Round(3.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                CompensationPercentage: activityCoeff * 100,
                BiomechanicalReason: "Гипертонус грушевидных мышц, натяжение тканей и наружный разворот стоп.",
                ControlTestMandate: "Повторная пассивная ротация внутрь на кушетке.",
                RecommendedDurometer: durometer,
                InsoleQuadrant: 2,
                NominalThicknessMm: 3.0
            ));
        }

        // ТЕСТ 3: Изолированная ротация бедра
        if (input.IsAntetorsion)
        {
            if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsInwardTest5))
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.PosteriorMedial,
                    EffectiveThicknessMm: Math.Round(4.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Антеторсия шейки бедра с выраженным падением стопы внутрь в глобальном тесте.",
                    ControlTestMandate: "Строго через глобальный двигательный тест!",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 3,
                    NominalThicknessMm: 4.0
                ));
            }
            else
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.AnteriorLateral,
                    EffectiveThicknessMm: Math.Round(3.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Антеторсия шейки бедра (костный барьер наружной ротации, компенсаторный вальгус шага).",
                    ControlTestMandate: "Дифференциация барьера вращения шейки бедра.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 2,
                    NominalThicknessMm: 3.0
                ));
            }
        }
        else if (input.IsRetrotorsion)
        {
            rawPrescriptions.Add(new WedgeItemPrescription(
                Placement: WedgePlacementType.AnteriorLateral,
                EffectiveThicknessMm: Math.Round(2.5 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                CompensationPercentage: activityCoeff * 100,
                BiomechanicalReason: "Ретроторсия бедра с патологической наружной установкой нижней конечности.",
                ControlTestMandate: "Тест ротаторов бедра.",
                RecommendedDurometer: durometer,
                InsoleQuadrant: 2,
                NominalThicknessMm: 2.5
            ));
        }

        // ТЕСТ 4: Торсия большеберцовой кости
        if (input.TibialStatus == TibialTorsion.UnderRotationInward)
        {
            if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsInwardTest5))
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.PosteriorMedial,
                    EffectiveThicknessMm: Math.Round(4.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Внутренний недокрут большеберцовой кости с завалом стопы внутрь.",
                    ControlTestMandate: "Контроль по мануальному мышечному тесту подколенной мышцы.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 3,
                    NominalThicknessMm: 4.0
                ));
            }
            else
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.AnteriorLateral,
                    EffectiveThicknessMm: Math.Round(3.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Недокрут большеберцовой кости внутрь (провокация вальгусной деформации).",
                    ControlTestMandate: "Визуальный контроль оси голень-стопа.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 2,
                    NominalThicknessMm: 3.0
                ));
            }
        }
        else if (input.TibialStatus == TibialTorsion.OverRotationOutward)
        {
            rawPrescriptions.Add(new WedgeItemPrescription(
                Placement: WedgePlacementType.AnteriorMedial,
                EffectiveThicknessMm: Math.Round(3.5 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                CompensationPercentage: activityCoeff * 100,
                BiomechanicalReason: "Перекрут большеберцовой кости кнаружи, варус переднего отдела стопы, риск Hallux Valgus.",
                ControlTestMandate: "Оценка параллельности оси сгибания коленного сустава.",
                RecommendedDurometer: durometer,
                InsoleQuadrant: 1,
                NominalThicknessMm: 3.5
            ));

            if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsOutwardTest5))
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.FifthMetatarsalBase,
                    EffectiveThicknessMm: Math.Round(2.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Динамическое падение стопы кнаружи при торсионном перекруте голени.",
                    ControlTestMandate: "Тест стабильности латерального свода.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 4,
                    NominalThicknessMm: 2.0
                ));
            }
        }

        // ТЕСТ 5: Сочетанные патологии и торсионный конфликт
        bool isConflict = input.IsAntetorsion && input.TibialStatus == TibialTorsion.OverRotationOutward;
        if (isConflict)
        {
            alerts.Add("Внимание: Торсионный конфликт (Антеторсия бедра + Наружный перекрут голени). Вычисление средней результирующей оси. Приоритет отдается 6-му тесту по Бейкрофту для сохранения адаптации подошвы.");
        }

        // Защита от срыва адаптации: вычисление кумулятивной толщины по отделам стопы
        double forefootSum = rawPrescriptions
            .Where(p => p.Placement is WedgePlacementType.AnteriorLateral or WedgePlacementType.AnteriorMedial or WedgePlacementType.FifthMetatarsalBase)
            .Sum(p => p.EffectiveThicknessMm);

        double rearfootSum = rawPrescriptions
            .Where(p => p.Placement is WedgePlacementType.PosteriorMedial or WedgePlacementType.HeelLiftCompensator)
            .Sum(p => p.EffectiveThicknessMm);

        if (forefootSum > MaxSafeSegmentCorrectionMm || rearfootSum > MaxSafeSegmentCorrectionMm)
        {
            alerts.Add($"Внимание: Суммарная толщина клиньев в сегменте ({Math.Max(forefootSum, rearfootSum):F1} мм) близка к критической. Контролируйте отсутствие гиперкератоза и мышечного сопротивления.");
        }

        var riskLevel = isConflict
            ? KineticChainRiskLevel.CriticalTorsionConflict
            : (needsOsteopathy ? KineticChainRiskLevel.Moderate : KineticChainRiskLevel.Low);

        var wearInSchedule = new List<string>
        {
            "Дни 1–3: Ношение индивидуальных стелек 2 часа в день при умеренной нагрузке.",
            "Дни 4–7: Увеличение времени адаптации до 4–5 часов в день при обычной ходьбе.",
            "Неделя 2+: Полноценная эксплуатация в течение всего активного дня.",
            "Контрольный осмотр подиатра и динамическая переоценка через 21 день."
        };

        return new HorizontalDiagnosticReport(
            RequiresOsteopathicIntervention: needsOsteopathy,
            MeasuredDeltaMm: deltaMm,
            IsComplexTorsionConflict: isConflict,
            Prescriptions: rawPrescriptions,
            ClinicalAlerts: alerts,
            RiskLevel: riskLevel,
            CumulativeForefootCorrectionMm: Math.Round(forefootSum, 1),
            CumulativeRearfootCorrectionMm: Math.Round(rearfootSum, 1),
            WearInSchedule: wearInSchedule
        );
    }
}
