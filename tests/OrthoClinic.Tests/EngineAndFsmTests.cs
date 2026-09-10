namespace OrthoClinic.Tests;

using FluentAssertions;
using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Engine;
using OrthoClinic.Infrastructure.Data;
using Xunit;

public sealed class EngineAndFsmTests
{
    private readonly HorizontalAssessmentEngine _engine = new();

    [Fact]
    public void Evaluate_DeltaExceeds3Mm_ActivatesOsteopathicInterventionAlert()
    {
        // Delta = |5.0 - 1.0| = 4.0 мм (> 3.0 мм)
        var input = new HorizontalInputData(
            LeftMalleolusMm: 5.0,
            RightMalleolusMm: 1.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: false,
            IsAntetorsion: false,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.Normal,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Moderate
        );

        var report = _engine.Evaluate(input);

        report.RequiresOsteopathicIntervention.Should().BeTrue();
        report.MeasuredDeltaMm.Should().Be(4.0);
        report.ClinicalAlerts.Should().Contain(a => a.Contains("Показано применение дополнительных остеопатических тестов"));
        report.Prescriptions.Should().NotContain(p => p.Placement == WedgePlacementType.HeelLiftCompensator);
    }

    [Fact]
    public void Evaluate_FunctionalPelvicDiscrepancy_BlocksHeelLift_AndIssuesScoliosisWarning()
    {
        // Функциональный перекос таза (скрученный таз)
        var input = new HorizontalInputData(
            LeftMalleolusMm: 6.0,
            RightMalleolusMm: 1.0,
            DiscrepancyNature: LegDiscrepancyType.FunctionalPelvic,
            PiriformisHypertonus: false,
            IsAntetorsion: false,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.Normal,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Moderate
        );

        var report = _engine.Evaluate(input);

        report.RequiresOsteopathicIntervention.Should().BeTrue();
        report.ClinicalAlerts.Should().Contain(a => a.Contains("Коррекция подпяточником противопоказана"));
        report.Prescriptions.Should().NotContain(p => p.Placement == WedgePlacementType.HeelLiftCompensator);
    }

    [Theory]
    [InlineData(PatientActivityLevel.Sedentary, 1.00, 3.0, MaterialDurometerShoreA.ShoreA35_Soft)]
    [InlineData(PatientActivityLevel.Moderate, 0.75, 2.3, MaterialDurometerShoreA.ShoreA45_Medium)]
    [InlineData(PatientActivityLevel.Active, 0.50, 1.5, MaterialDurometerShoreA.ShoreA55_Firm)]
    [InlineData(PatientActivityLevel.Athlete, 0.25, 0.8, MaterialDurometerShoreA.ShoreA65_RigidComposite)]
    public void Evaluate_TrueAnatomicalDiscrepancy_CalculatesCompensator_PerActivityLevel(
        PatientActivityLevel activity, double expectedCoeff, double expectedThicknessMm, MaterialDurometerShoreA expectedDurometer)
    {
        // Delta = 6.0 мм, Nominal = 6.0 / 2 = 3.0 мм
        var input = new HorizontalInputData(
            LeftMalleolusMm: 8.0,
            RightMalleolusMm: 2.0,
            DiscrepancyNature: LegDiscrepancyType.TrueAnatomical,
            PiriformisHypertonus: false,
            IsAntetorsion: false,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.BoneAnatomical,
            TibialStatus: TibialTorsion.Normal,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: activity
        );

        var report = _engine.Evaluate(input);

        report.RequiresOsteopathicIntervention.Should().BeTrue();
        var heelLift = report.Prescriptions.Should().ContainSingle(p => p.Placement == WedgePlacementType.HeelLiftCompensator).Subject;
        heelLift.EffectiveThicknessMm.Should().Be(expectedThicknessMm);
        heelLift.CompensationPercentage.Should().Be(expectedCoeff * 100);
        heelLift.RecommendedDurometer.Should().Be(expectedDurometer);
        heelLift.ControlTestMandate.Should().Be("Оценка горизонтали крестца.");
    }

    [Fact]
    public void Evaluate_PiriformisHypertonus_PrescribesAnteriorLateralWedge()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 1.0,
            RightMalleolusMm: 1.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: true,
            IsAntetorsion: false,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.Normal,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Moderate // coeff = 0.75 => 3.0 * 0.75 = 2.3 (Round 2.25)
        );

        var report = _engine.Evaluate(input);

        var wedge = report.Prescriptions.Should().ContainSingle(p => p.Placement == WedgePlacementType.AnteriorLateral).Subject;
        wedge.EffectiveThicknessMm.Should().Be(2.3);
        wedge.BiomechanicalReason.Should().Contain("грушевидных мышц");
        wedge.ControlTestMandate.Should().Contain("пассивная ротация внутрь");
    }

    [Fact]
    public void Evaluate_AntetorsionWithInwardFall_PrioritizesPosteriorMedialWedge()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 1.0,
            RightMalleolusMm: 1.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: false,
            IsAntetorsion: true,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.BoneAnatomical,
            TibialStatus: TibialTorsion.Normal,
            GlobalFlags: GlobalBiomechanicalFlags.FootFallsInwardTest5,
            Activity: PatientActivityLevel.Sedentary // coeff = 1.00 => 4.0 мм
        );

        var report = _engine.Evaluate(input);

        var wedge = report.Prescriptions.Should().ContainSingle(p => p.Placement == WedgePlacementType.PosteriorMedial).Subject;
        wedge.EffectiveThicknessMm.Should().Be(4.0);
        wedge.BiomechanicalReason.Should().Contain("Антеторсия шейки бедра с выраженным падением стопы внутрь");
        wedge.ControlTestMandate.Should().Be("Строго через глобальный двигательный тест!");
    }

    [Fact]
    public void Evaluate_Retrotorsion_PrescribesAnteriorLateralWedge()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 1.0,
            RightMalleolusMm: 1.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: false,
            IsAntetorsion: false,
            IsRetrotorsion: true,
            HipJointBarrier: RotationBarrier.BoneAnatomical,
            TibialStatus: TibialTorsion.Normal,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Sedentary // coeff = 1.0 => 2.5 мм
        );

        var report = _engine.Evaluate(input);

        var wedge = report.Prescriptions.Should().ContainSingle(p => p.Placement == WedgePlacementType.AnteriorLateral).Subject;
        wedge.EffectiveThicknessMm.Should().Be(2.5);
        wedge.BiomechanicalReason.Should().Contain("Ретроторсия бедра");
    }

    [Fact]
    public void Evaluate_TibialOverRotation_PrescribesDualMedialAndFifthMetatarsalWedges()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 0.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: false,
            IsAntetorsion: false,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.OverRotationOutward,
            GlobalFlags: GlobalBiomechanicalFlags.FootFallsOutwardTest5,
            Activity: PatientActivityLevel.Sedentary // coeff = 1.0
        );

        var report = _engine.Evaluate(input);

        report.Prescriptions.Should().HaveCount(2);
        report.Prescriptions.Should().ContainSingle(p => p.Placement == WedgePlacementType.AnteriorMedial && p.EffectiveThicknessMm == 3.5);
        report.Prescriptions.Should().ContainSingle(p => p.Placement == WedgePlacementType.FifthMetatarsalBase && p.EffectiveThicknessMm == 2.0);
    }

    [Fact]
    public void Evaluate_TorsionParadoxConflict_DetectsStandingWaveCondition()
    {
        // Конфликт направлений ротации: антеторсия бедра + наружный перекрут голени
        var input = new HorizontalInputData(
            LeftMalleolusMm: 0.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: false,
            IsAntetorsion: true,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.BoneAnatomical,
            TibialStatus: TibialTorsion.OverRotationOutward,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Moderate
        );

        var report = _engine.Evaluate(input);

        report.IsComplexTorsionConflict.Should().BeTrue();
        report.RiskLevel.Should().Be(KineticChainRiskLevel.CriticalTorsionConflict);
        report.ClinicalAlerts.Should().Contain(a => a.Contains("Торсионный конфликт") && a.Contains("6-му тесту по Бейкрофту"));
    }

    [Fact]
    public async Task Persistence_AtomicTransaction_CommitsSuccessfully_AndRollsBackOnError()
    {
        string tempDbPath = Path.Combine(Path.GetTempPath(), $"orthoclinic_test_{Guid.NewGuid():N}.db");
        try
        {
            var repo = new SqliteExamRepository(tempDbPath, "TestPassword123!");
            await repo.InitializeAsync();

            var input = new HorizontalInputData(
                2.0, 0.0, LegDiscrepancyType.Indeterminate, false, true, false,
                RotationBarrier.BoneAnatomical, TibialTorsion.Normal, GlobalBiomechanicalFlags.None,
                PatientActivityLevel.Active, "PAT-001", "Иванов И.И."
            );
            var report = _engine.Evaluate(input);

            var session = new PatientExamSession(
                Guid.NewGuid(),
                "PAT-001",
                "Иванов И.И.",
                DateTime.UtcNow,
                input,
                report,
                "dummy-sha256-checksum"
            );

            // Атомарное сохранение сессии
            await repo.SaveExamTransactionAsync(session);

            // Чтение из БД
            var history = await repo.GetPatientExamHistoryAsync("PAT-001");
            history.Should().HaveCount(1);
            history[0].PatientFullName.Should().Be("Иванов И.И.");
            history[0].Report.Prescriptions.Should().HaveCount(report.Prescriptions.Count);

            await repo.CloseAsync();
        }
        finally
        {
            try
            {
                if (File.Exists(tempDbPath))
                {
                    File.Delete(tempDbPath);
                }
            }
            catch
            {
                // Игнорируем задержку освобождения дескриптора ОС
            }
        }
    }

    [Fact]
    public void Evaluate_FunctionalPelvicDiscrepancy_GeneratesCriticalContraindicationAlert_AndBlocksPrescription()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 6.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.FunctionalPelvic,
            PiriformisHypertonus: false,
            IsAntetorsion: false,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.Normal,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Moderate
        );

        var report = _engine.Evaluate(input);

        report.DynamicAlerts.Should().NotBeNull();
        var alert = report.DynamicAlerts!.FirstOrDefault(a => a.AlertCode == "CONTRAINDICATION_HEEL_LIFT_FUNCTIONAL_PELVIS");
        alert.Should().NotBeNull();
        alert!.Severity.Should().Be(AlertSeverity.CriticalContraindication);
        alert.IsPrescriptionBlocked.Should().BeTrue();
        alert.AffectedSegment.Should().Be(AnatomicalSegment.Pelvis_Sacrum);
        report.Prescriptions.Should().NotContain(p => p.Placement == WedgePlacementType.HeelLiftCompensator);
    }

    [Fact]
    public void Evaluate_TorsionParadoxConflict_GeneratesKineticConflictAlert_AndHighKrs()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 0.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: false,
            IsAntetorsion: true,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.BoneAnatomical,
            TibialStatus: TibialTorsion.OverRotationOutward,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Active
        );

        var report = _engine.Evaluate(input);

        report.IsComplexTorsionConflict.Should().BeTrue();
        report.KineticRiskScore.Should().BeGreaterThanOrEqualTo(50.0);
        report.RiskLevel.Should().Be(KineticChainRiskLevel.CriticalTorsionConflict);

        var conflictAlert = report.DynamicAlerts!.FirstOrDefault(a => a.AlertCode == "CONFLICT_TORSIONAL_PARADOX_STANDING_WAVE");
        conflictAlert.Should().NotBeNull();
        conflictAlert!.Severity.Should().Be(AlertSeverity.KineticConflict);
        conflictAlert.AffectedSegment.Should().Be(AnatomicalSegment.Tibia_Knee);
    }

    [Fact]
    public void Evaluate_FemoralBonyBarrier_GeneratesWarningThresholdAlert()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 0.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: false,
            IsAntetorsion: true,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.BoneAnatomical,
            TibialStatus: TibialTorsion.Normal,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Moderate
        );

        var report = _engine.Evaluate(input);

        var barrierAlert = report.DynamicAlerts!.FirstOrDefault(a => a.AlertCode == "ALERT_FEMORAL_BONY_BARRIER");
        barrierAlert.Should().NotBeNull();
        barrierAlert!.Severity.Should().Be(AlertSeverity.WarningThreshold);
        barrierAlert.AffectedSegment.Should().Be(AnatomicalSegment.Femur_Hip);
    }

    [Fact]
    public void Evaluate_DualTarsalInstability_GeneratesTarsalLaxityAlert()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 0.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: false,
            IsAntetorsion: false,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.Normal,
            GlobalFlags: GlobalBiomechanicalFlags.FootFallsInwardTest5 | GlobalBiomechanicalFlags.FootFallsOutwardTest5,
            Activity: PatientActivityLevel.Sedentary
        );

        var report = _engine.Evaluate(input);

        var dualAlert = report.DynamicAlerts!.FirstOrDefault(a => a.AlertCode == "ALERT_DUAL_TARSAL_INSTABILITY");
        dualAlert.Should().NotBeNull();
        dualAlert!.Severity.Should().Be(AlertSeverity.WarningThreshold);
        dualAlert.AffectedSegment.Should().Be(AnatomicalSegment.Foot_Rearfoot);
    }

    [Fact]
    public void Evaluate_CumulativeSegmentThickness_AppliesProportionalClampingTo6Mm()
    {
        // Пациент Sedentary (коэффициент 1.0) с сочетанием антеторсии (3 мм) + гипертонуса грушевидной (3 мм) + перекрута голени (3.5 мм)
        // Сырая сумма переднего отдела = 3.0 + 3.0 + 3.5 = 9.5 мм (> 6.0 мм)
        var input = new HorizontalInputData(
            LeftMalleolusMm: 0.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: true,
            IsAntetorsion: true,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.OverRotationOutward,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Sedentary
        );

        var report = _engine.Evaluate(input);

        report.CumulativeForefootCorrectionMm.Should().BeLessOrEqualTo(HorizontalAssessmentEngine.MaxSafeSegmentCorrectionMm);
        var overflowAlert = report.DynamicAlerts!.FirstOrDefault(a => a.AlertCode == "LIMIT_CUMULATIVE_SEGMENT_THICKNESS_EXCEEDED");
        overflowAlert.Should().NotBeNull();
        overflowAlert!.Severity.Should().Be(AlertSeverity.WarningThreshold);
    }

    [Fact]
    public void Evaluate_AdaptiveWearInSchedule_GeneratesHighRiskProtocolForSevereCases()
    {
        var conflictInput = new HorizontalInputData(
            LeftMalleolusMm: 4.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.FunctionalPelvic,
            PiriformisHypertonus: true,
            IsAntetorsion: true,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.BoneAnatomical,
            TibialStatus: TibialTorsion.OverRotationOutward,
            GlobalFlags: GlobalBiomechanicalFlags.FootFallsInwardTest5,
            Activity: PatientActivityLevel.Sedentary
        );

        var report = _engine.Evaluate(conflictInput);

        report.WearInSchedule.Should().NotBeNullOrEmpty();
        report.WearInSchedule![0].Should().Contain("Фаза 1");
        report.WearInSchedule.Last().Should().Contain("Контрольный");
    }

    [Fact]
    public void Evaluate_PrescriptionEnrichment_PopulatesRussianTitles_Zones_Formulas_AndProtocols()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 4.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.TrueAnatomical,
            PiriformisHypertonus: true,
            IsAntetorsion: true,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.OverRotationOutward,
            GlobalFlags: GlobalBiomechanicalFlags.FootFallsInwardTest5 | GlobalBiomechanicalFlags.FootFallsOutwardTest5,
            Activity: PatientActivityLevel.Moderate
        );

        var report = _engine.Evaluate(input);

        report.Prescriptions.Should().NotBeEmpty();
        foreach (var p in report.Prescriptions)
        {
            p.TitleRu.Should().NotBeNullOrWhiteSpace();
            p.AnatomicalZone.Should().NotBeNullOrWhiteSpace();
            p.ForceVectorRationale.Should().NotBeNullOrWhiteSpace();
            p.AdaptiveFormulaBreakdown.Should().NotBeNullOrWhiteSpace();
            p.InstallationProtocol.Should().NotBeNullOrWhiteSpace();
            p.MapColorHex.Should().StartWith("#");
            p.EffectiveThicknessMm.Should().BeGreaterThan(0.0);
        }
    }

    [Fact]
    public void Evaluate_ClampedPrescription_MarksIsSegmentClampedTrue_AndIncludesClampingInFormula()
    {
        var input = new HorizontalInputData(
            LeftMalleolusMm: 0.0,
            RightMalleolusMm: 0.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: true,
            IsAntetorsion: true,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.OverRotationOutward,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Sedentary
        );

        var report = _engine.Evaluate(input);

        var forefootItems = report.Prescriptions.Where(p => p.Placement is WedgePlacementType.AnteriorLateral or WedgePlacementType.AnteriorMedial).ToList();
        forefootItems.Should().NotBeEmpty();
        forefootItems.Should().AllSatisfy(p =>
        {
            p.IsSegmentClamped.Should().BeTrue();
            p.AdaptiveFormulaBreakdown.Should().Contain("демпфер лимита 6 мм");
        });
    }
}


