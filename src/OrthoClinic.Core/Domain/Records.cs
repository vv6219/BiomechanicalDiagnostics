namespace OrthoClinic.Core.Domain;

/// <summary>
/// Входные параметры горизонтального протокола мануального тестирования пациента на кушетке.
/// </summary>
public sealed record HorizontalInputData(
    double LeftMalleolusMm,
    double RightMalleolusMm,
    LegDiscrepancyType DiscrepancyNature,
    bool PiriformisHypertonus,
    bool IsAntetorsion,
    bool IsRetrotorsion,
    RotationBarrier HipJointBarrier,
    TibialTorsion TibialStatus,
    GlobalBiomechanicalFlags GlobalFlags,
    PatientActivityLevel Activity,
    string? PatientId = null,
    string? PatientName = null
);

/// <summary>
/// Спецификация выписанного ортопедического клина / компенсатора.
/// </summary>
public sealed record WedgeItemPrescription(
    WedgePlacementType Placement,
    double EffectiveThicknessMm,
    double CompensationPercentage,
    string BiomechanicalReason,
    string ControlTestMandate,
    MaterialDurometerShoreA RecommendedDurometer = MaterialDurometerShoreA.ShoreA45_Medium,
    int InsoleQuadrant = 1,
    double NominalThicknessMm = 0.0,
    string TitleRu = "",
    string AnatomicalZone = "",
    string ForceVectorRationale = "",
    string AdaptiveFormulaBreakdown = "",
    string InstallationProtocol = "",
    string MapColorHex = "#38BDF8",
    bool IsSegmentClamped = false
);

/// <summary>
/// Итоговое клиническое заключение горизонтальной биомеханической диагностики.
/// </summary>
public sealed record HorizontalDiagnosticReport(
    bool RequiresOsteopathicIntervention,
    double MeasuredDeltaMm,
    bool IsComplexTorsionConflict,
    IReadOnlyList<WedgeItemPrescription> Prescriptions,
    IReadOnlyList<string> ClinicalAlerts,
    KineticChainRiskLevel RiskLevel = KineticChainRiskLevel.Low,
    double CumulativeForefootCorrectionMm = 0.0,
    double CumulativeRearfootCorrectionMm = 0.0,
    IReadOnlyList<string>? WearInSchedule = null,
    IReadOnlyList<ClinicalAlertItem>? DynamicAlerts = null,
    double KineticRiskScore = 0.0,
    OrthoClinic.Core.Domain.Protocols.ComprehensivePodiatricProtocol? AdaptiveProtocol = null,
    OrthoClinic.Core.Domain.Protocols.ProtocolApprovalRecord? Approval = null
);

/// <summary>
/// Сессия осмотра пациента для долговременного зашифрованного сохранения в медицинской карте.
/// </summary>
public sealed record PatientExamSession(
    Guid SessionId,
    string PatientId,
    string PatientFullName,
    DateTime TimestampUtc,
    HorizontalInputData InputData,
    HorizontalDiagnosticReport Report,
    string SecurityChecksumSha256
);
