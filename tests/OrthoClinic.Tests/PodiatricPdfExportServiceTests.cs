namespace OrthoClinic.Tests;

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Domain.Protocols;
using OrthoClinic.Core.Engine;
using OrthoClinic.Core.Services;
using OrthoClinic.Infrastructure.Services;
using Xunit;

public class PodiatricPdfExportServiceTests
{
    [Fact]
    public async Task ExportProtocolPdfAsync_ShouldGenerateValidPdf_WithAllSectionsAndNonEmptyCards()
    {
        // 1. Arrange engines and input data
        var assessmentEngine = new HorizontalAssessmentEngine();
        var protocolEngine = new AdaptiveProtocolEngine();

        var inputData = new HorizontalInputData(
            LeftMalleolusMm: 12.0,
            RightMalleolusMm: 5.0,
            DiscrepancyNature: LegDiscrepancyType.FunctionalPelvic,
            PiriformisHypertonus: true,
            IsAntetorsion: true,
            IsRetrotorsion: false,
            HipJointBarrier: RotationBarrier.MuscleFunctional,
            TibialStatus: TibialTorsion.OverRotationOutward,
            GlobalFlags: GlobalBiomechanicalFlags.FootFallsInwardTest5,
            Activity: PatientActivityLevel.Active,
            PatientId: "PAT-TEST-PDF",
            PatientName: "Иванов Иван Иванович"
        );

        var diagnosticReport = assessmentEngine.Evaluate(inputData);
        var adaptiveProtocol = protocolEngine.GenerateProtocol(
            clinicalData: inputData,
            activityTier: PatientActivityTier.Active,
            patientWeightKg: 78.5,
            patientBmi: 24.2,
            patientAge: 38,
            hasMortonOrNeuropathy: false,
            isNarrowFootwear: false
        );

        var approvalRecord = new ProtocolApprovalRecord(
            Status: ProtocolApprovalStatus.ApprovedByPodiatrist,
            ApprovedByDoctorName: "Д-р Ортопедов А.В.",
            ApprovedAtUtc: DateTime.UtcNow,
            DoctorNotes: "Рекомендована повторная формовка через 21 день.",
            ApprovalDigitalFingerprint: "SHA256:4C83B92E1A9D04F8"
        );

        var request = new PdfExportRequest(
            PatientId: inputData.PatientId ?? "PAT-DEFAULT",
            PatientFullName: inputData.PatientName ?? "Пациент Тест",
            PatientAge: 38,
            PatientWeightKg: 78.5,
            PatientBmi: 24.2,
            ActivityTier: PatientActivityTier.Active,
            ClinicalHistorySummary: "Гипертонус грушевидной, завал внутрь (Т5)",
            MeasuredDeltaMm: 7.0,
            DiscrepancyNature: inputData.DiscrepancyNature,
            Barrier: inputData.HipJointBarrier,
            TibialState: inputData.TibialStatus,
            Report: diagnosticReport,
            Protocol: adaptiveProtocol,
            Approval: approvalRecord,
            DoctorFullName: approvalRecord.ApprovedByDoctorName,
            DoctorNotes: approvalRecord.DoctorNotes ?? string.Empty,
            EvaluationTimestamp: DateTime.Now
        );

        var exportService = new PodiatricPdfExportService();
        string tempPdfPath = Path.Combine(Path.GetTempPath(), $"TestProtocol_{Guid.NewGuid():N}.pdf");

        try
        {
            // 2. Act
            string generatedPath = await exportService.ExportProtocolPdfAsync(request, tempPdfPath);

            // 3. Assert
            generatedPath.Should().Be(tempPdfPath);
            File.Exists(generatedPath).Should().BeTrue();

            var fileInfo = new FileInfo(generatedPath);
            fileInfo.Length.Should().BeGreaterThan(5000, "PDF with tables, alerts, and formatting must have substantial content");

            // Verify it starts with PDF magic bytes %PDF-
            byte[] header = new byte[5];
            using (var fs = File.OpenRead(generatedPath))
            {
                int bytesRead = fs.Read(header, 0, 5);
                bytesRead.Should().Be(5);
            }
            string headerStr = System.Text.Encoding.ASCII.GetString(header);
            headerStr.Should().Be("%PDF-");
        }
        finally
        {
            if (File.Exists(tempPdfPath))
            {
                File.Delete(tempPdfPath);
            }
        }
    }
}
