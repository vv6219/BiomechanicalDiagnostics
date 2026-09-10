namespace OrthoClinic.Tests;

using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Domain.Protocols;
using OrthoClinic.Core.Engine;
using Xunit;

public class AdaptiveProtocolEngineTests
{
    private readonly IAdaptiveProtocolEngine _engine = new AdaptiveProtocolEngine();

    private static HorizontalInputData CreateBaseData(
        TibialTorsion tibial = TibialTorsion.Normal,
        bool antetorsion = false,
        bool piriformis = false)
    {
        return new HorizontalInputData(
            LeftMalleolusMm: 12.0,
            RightMalleolusMm: 12.0,
            DiscrepancyNature: LegDiscrepancyType.Indeterminate,
            PiriformisHypertonus: piriformis,
            IsAntetorsion: antetorsion,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: tibial,
            GlobalFlags: GlobalBiomechanicalFlags.None,
            Activity: PatientActivityLevel.Moderate
        );
    }

    [Fact]
    public void GenerateProtocol_ChildUnder15_SelectsPediatricGrowthProfile()
    {
        var input = CreateBaseData();
        var protocol = _engine.GenerateProtocol(
            clinicalData: input,
            activityTier: PatientActivityTier.Active,
            patientWeightKg: 42.0,
            patientBmi: 18.0,
            patientAge: 12,
            hasMortonOrNeuropathy: false,
            isNarrowFootwear: false
        );

        Assert.Equal(ClinicalCaseCategory.PediatricGrowth, protocol.ClinicalProfile);
        Assert.Equal("Formthotics Junior Red", protocol.RecommendedModelTitle);
        Assert.Equal(3.0, protocol.InsoleThicknessMm);
        Assert.Equal(85.0, protocol.HeatingTemperatureC);
        // t_heat = round(20 + 4.5 * 3.0) = 34
        Assert.Equal(34, protocol.HeatingSeconds);
    }

    [Fact]
    public void GenerateProtocol_MortonOrNeuropathy_SelectsSensitiveNeuropathicProfile()
    {
        var input = CreateBaseData();
        var protocol = _engine.GenerateProtocol(
            clinicalData: input,
            activityTier: PatientActivityTier.Moderate,
            patientWeightKg: 68.0,
            patientBmi: 23.0,
            patientAge: 45,
            hasMortonOrNeuropathy: true,
            isNarrowFootwear: false
        );

        Assert.Equal(ClinicalCaseCategory.SensitiveNeuropathic, protocol.ClinicalProfile);
        Assert.Equal("Formthotics ShockStop Red/Green", protocol.RecommendedModelTitle);
        Assert.Equal(5.0, protocol.InsoleThicknessMm);
        Assert.NotNull(protocol.MetatarsalPad);
        Assert.Equal("M", protocol.MetatarsalPad.PadSize);
    }

    [Fact]
    public void GenerateProtocol_HighBmiOrHeavyWeight_SelectsHighAxialCompressionProfile()
    {
        var input = CreateBaseData();
        var protocol = _engine.GenerateProtocol(
            clinicalData: input,
            activityTier: PatientActivityTier.Moderate,
            patientWeightKg: 102.0,
            patientBmi: 32.5,
            patientAge: 40,
            hasMortonOrNeuropathy: false,
            isNarrowFootwear: false
        );

        Assert.Equal(ClinicalCaseCategory.HighAxialCompression, protocol.ClinicalProfile);
        Assert.Equal("Formthotics Hard Black", protocol.RecommendedModelTitle);
        Assert.Equal(4.0, protocol.InsoleThicknessMm);
    }

    [Fact]
    public void GenerateProtocol_NarrowFootwear_SelectsExecutiveNarrowShoeProfile()
    {
        var input = CreateBaseData();
        var protocol = _engine.GenerateProtocol(
            clinicalData: input,
            activityTier: PatientActivityTier.Moderate,
            patientWeightKg: 70.0,
            patientBmi: 22.0,
            patientAge: 32,
            hasMortonOrNeuropathy: false,
            isNarrowFootwear: true
        );

        Assert.Equal(ClinicalCaseCategory.ExecutiveNarrowShoe, protocol.ClinicalProfile);
        Assert.Equal("Formthotics Low Volume / 3/4 Beige", protocol.RecommendedModelTitle);
        Assert.Equal(2.0, protocol.InsoleThicknessMm);
        // t_heat = round(20 + 4.5 * 2.0) = 29
        Assert.Equal(29, protocol.HeatingSeconds);
    }

    [Fact]
    public void GenerateProtocol_AthleticProfile_SelectsDynamicAthleticProfile()
    {
        var input = CreateBaseData();
        var protocol = _engine.GenerateProtocol(
            clinicalData: input,
            activityTier: PatientActivityTier.Active,
            patientWeightKg: 68.0,
            patientBmi: 21.5,
            patientAge: 26,
            hasMortonOrNeuropathy: false,
            isNarrowFootwear: false
        );

        Assert.Equal(ClinicalCaseCategory.DynamicAthletic, protocol.ClinicalProfile);
        Assert.Equal("Formthotics Original Single Blue", protocol.RecommendedModelTitle);
        Assert.Equal(3.5, protocol.InsoleThicknessMm);
        // at h = 3.5, t_cool = round(120 * (3.5/3.5)^0.65) = 120
        Assert.Equal(120, protocol.InShoeMoldingSeconds);
    }

    [Theory]
    [InlineData(PatientActivityTier.Sedentary, 1.0, 4.0)]
    [InlineData(PatientActivityTier.Moderate, 0.75, 3.0)]
    [InlineData(PatientActivityTier.Active, 0.50, 2.0)]
    [InlineData(PatientActivityTier.Professional, 0.25, 1.0)]
    public void GenerateProtocol_OverRotationOutward_ScalesWedgeByActivityTier(
        PatientActivityTier tier, double expectedCoeff, double expectedCalculated)
    {
        var input = CreateBaseData(tibial: TibialTorsion.OverRotationOutward);
        var protocol = _engine.GenerateProtocol(
            clinicalData: input,
            activityTier: tier,
            patientWeightKg: 75.0,
            patientBmi: 24.0,
            patientAge: 30,
            hasMortonOrNeuropathy: false,
            isNarrowFootwear: false
        );

        Assert.Single(protocol.Wedges);
        var wedge = protocol.Wedges[0];
        Assert.Equal(WedgePlacementType.AnteriorMedial, wedge.Type);
        Assert.Equal(4.0, wedge.NominalThicknessMm);
        Assert.Equal(expectedCalculated, wedge.CalculatedThicknessMm);
    }

    [Fact]
    public void GenerateProtocol_AntetorsionOrPiriformis_CalculatesAnteriorLateralWedge()
    {
        var input = CreateBaseData(antetorsion: true);
        var protocol = _engine.GenerateProtocol(
            clinicalData: input,
            activityTier: PatientActivityTier.Active, // coeff 0.50
            patientWeightKg: 70.0,
            patientBmi: 22.0,
            patientAge: 30,
            hasMortonOrNeuropathy: false,
            isNarrowFootwear: false
        );

        Assert.Single(protocol.Wedges);
        var wedge = protocol.Wedges[0];
        Assert.Equal(WedgePlacementType.AnteriorLateral, wedge.Type);
        Assert.Equal(3.5, wedge.NominalThicknessMm);
        Assert.Equal(1.8, wedge.CalculatedThicknessMm); // Math.Round(3.5 * 0.5, 1) = 1.8
    }

    [Theory]
    [InlineData(55.0, "S")]
    [InlineData(72.0, "M")]
    [InlineData(88.0, "L")]
    public void GenerateProtocol_MetatarsalPadWeightSizing_CalculatesCorrectSize(
        double weightKg, string expectedSize)
    {
        var input = CreateBaseData(tibial: TibialTorsion.OverRotationOutward);
        var protocol = _engine.GenerateProtocol(
            clinicalData: input,
            activityTier: PatientActivityTier.Moderate,
            patientWeightKg: weightKg,
            patientBmi: 24.0,
            patientAge: 35,
            hasMortonOrNeuropathy: false,
            isNarrowFootwear: false
        );

        Assert.NotNull(protocol.MetatarsalPad);
        Assert.Equal(expectedSize, protocol.MetatarsalPad.PadSize);
    }

    [Fact]
    public void GenerateProtocol_StagesTimelineAndFollowUp_AreCorrectlyStructured()
    {
        var input = CreateBaseData();
        var protocol = _engine.GenerateProtocol(
            clinicalData: input,
            activityTier: PatientActivityTier.Moderate,
            patientWeightKg: 70.0,
            patientBmi: 23.0,
            patientAge: 30,
            hasMortonOrNeuropathy: false,
            isNarrowFootwear: false
        );

        Assert.Equal(3, protocol.Timeline.Count);
        Assert.Equal(1, protocol.Timeline[0].StartDay);
        Assert.Equal(4, protocol.Timeline[0].EndDay);
        Assert.Equal(5, protocol.Timeline[1].StartDay);
        Assert.Equal(14, protocol.Timeline[1].EndDay);
        Assert.Equal(15, protocol.Timeline[2].StartDay);
        Assert.Equal(28, protocol.Timeline[2].EndDay);
        Assert.Equal(28, protocol.NextFollowUpDay);
    }
}
