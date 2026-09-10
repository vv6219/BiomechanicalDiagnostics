namespace OrthoClinic.Infrastructure.Data.Entities;

using SQLite;

[Table("ExamRecords")]
public sealed class ExamRecordEntity
{
    [PrimaryKey]
    public string SessionId { get; set; } = string.Empty;

    [Indexed]
    public string PatientId { get; set; } = string.Empty;

    public string PatientFullName { get; set; } = string.Empty;

    [Indexed]
    public DateTime TimestampUtc { get; set; }

    public double LeftMalleolusMm { get; set; }
    public double RightMalleolusMm { get; set; }
    public double MeasuredDeltaMm { get; set; }
    public string DiscrepancyNature { get; set; } = string.Empty;
    public bool RequiresOsteopathy { get; set; }
    public bool IsComplexTorsionConflict { get; set; }

    public string SerializedInputJson { get; set; } = string.Empty;
    public string SerializedReportJson { get; set; } = string.Empty;

    /// <summary>
    /// Зашифрованный AES-256 полезный блок медицинских данных.
    /// </summary>
    public string EncryptedMedicalPayloadBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Контрольная сумма целостности SHA-256.
    /// </summary>
    public string SecurityChecksumSha256 { get; set; } = string.Empty;
}

[Table("PrescriptionItems")]
public sealed class PrescriptionEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string SessionId { get; set; } = string.Empty;

    public string Placement { get; set; } = string.Empty;
    public double EffectiveThicknessMm { get; set; }
    public double NominalThicknessMm { get; set; }
    public double CompensationPercentage { get; set; }
    public string BiomechanicalReason { get; set; } = string.Empty;
    public string ControlTestMandate { get; set; } = string.Empty;
    public string RecommendedDurometer { get; set; } = string.Empty;
    public int InsoleQuadrant { get; set; }
}
