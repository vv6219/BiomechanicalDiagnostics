namespace OrthoClinic.Core.Data;

using OrthoClinic.Core.Domain;

/// <summary>
/// Репозиторий хранения медицинских записей обследования с гарантией атомарных транзакций.
/// </summary>
public interface IExamRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task SaveExamTransactionAsync(PatientExamSession session, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientExamSession>> GetPatientExamHistoryAsync(string patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientExamSession>> GetAllExamsAsync(CancellationToken cancellationToken = default);

    Task CloseAsync();
}
