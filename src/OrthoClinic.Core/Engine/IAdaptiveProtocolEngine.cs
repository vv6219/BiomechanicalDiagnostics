namespace OrthoClinic.Core.Engine;

using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Domain.Protocols;

/// <summary>
/// Движок синтеза адаптивного подиатрического протокола, теплофизического регламента и калибровки ортезов.
/// </summary>
public interface IAdaptiveProtocolEngine
{
    /// <summary>
    /// Генерирует комплексный персонализированный подиатрический протокол на основе биомеханических тестов и биометрии.
    /// </summary>
    ComprehensivePodiatricProtocol GenerateProtocol(
        HorizontalInputData clinicalData,
        PatientActivityTier activityTier,
        double patientWeightKg,
        double patientBmi,
        int patientAge,
        bool hasMortonOrNeuropathy,
        bool isNarrowFootwear,
        IReadOnlyList<WedgeItemPrescription>? calculatedPrescriptions = null
    );
}
