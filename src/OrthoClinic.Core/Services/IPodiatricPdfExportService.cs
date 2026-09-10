namespace OrthoClinic.Core.Services;

using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Domain.Protocols;

/// <summary>
/// Полный контекст параметров для генерации официального медицинского PDF-протокола.
/// </summary>
public sealed record PdfExportRequest(
    string PatientId,
    string PatientFullName,
    int PatientAge,
    double PatientWeightKg,
    double PatientBmi,
    PatientActivityTier ActivityTier,
    string ClinicalHistorySummary,
    double MeasuredDeltaMm,
    LegDiscrepancyType DiscrepancyNature,
    RotationBarrier Barrier,
    TibialTorsion TibialState,
    HorizontalDiagnosticReport Report,
    ComprehensivePodiatricProtocol Protocol,
    ProtocolApprovalRecord? Approval,
    string DoctorFullName,
    string DoctorNotes,
    DateTime EvaluationTimestamp
);

/// <summary>
/// Сервис генерации официального печатного PDF-бланка медицинского протокола подиатра Formthotics.
/// </summary>
public interface IPodiatricPdfExportService
{
    Task<string> ExportProtocolPdfAsync(PdfExportRequest request, string? outputFilePath = null);
}
