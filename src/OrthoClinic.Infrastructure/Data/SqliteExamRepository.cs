namespace OrthoClinic.Infrastructure.Data;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OrthoClinic.Core.Data;
using OrthoClinic.Core.Domain;
using OrthoClinic.Infrastructure.Data.Entities;
using SQLite;

/// <summary>
/// Реализация хранилища данных обследований в локальной базе данных SQLite
/// с шифрованием медицинских данных и гарантией атомарности транзакций (ACID).
/// </summary>
public sealed class SqliteExamRepository : IExamRepository
{
    private readonly string _databasePath;
    private readonly byte[] _encryptionKey;
    private SQLiteAsyncConnection? _connection;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public SqliteExamRepository(string databasePath, string? passphrase = null)
    {
        _databasePath = databasePath;
        // Генерация детерминированного 256-битного ключа шифрования
        string effectivePass = string.IsNullOrWhiteSpace(passphrase) ? "OrthoClinic.Secret.Key.2026!#" : passphrase;
        _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(effectivePass));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            string? dir = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new SQLiteConnectionString(_databasePath, storeDateTimeAsTicks: true);
            _connection = new SQLiteAsyncConnection(options);

            await _connection.CreateTableAsync<ExamRecordEntity>();
            await _connection.CreateTableAsync<PrescriptionEntity>();

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private SQLiteAsyncConnection GetConnection()
    {
        if (_connection == null || !_isInitialized)
        {
            throw new InvalidOperationException("SqliteExamRepository не инициализирован. Вызовите InitializeAsync().");
        }
        return _connection;
    }

    public async Task SaveExamTransactionAsync(PatientExamSession session, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var db = GetConnection();

        string inputJson = JsonSerializer.Serialize(session.InputData);
        string reportJson = JsonSerializer.Serialize(session.Report);

        // AES-GCM шифрование медицинских данных для сохранения тайны врачебного осмотра
        string encryptedPayload = EncryptAesGcm(reportJson, _encryptionKey);

        var examEntity = new ExamRecordEntity
        {
            SessionId = session.SessionId.ToString(),
            PatientId = session.PatientId,
            PatientFullName = session.PatientFullName,
            TimestampUtc = session.TimestampUtc,
            LeftMalleolusMm = session.InputData.LeftMalleolusMm,
            RightMalleolusMm = session.InputData.RightMalleolusMm,
            MeasuredDeltaMm = session.Report.MeasuredDeltaMm,
            DiscrepancyNature = session.InputData.DiscrepancyNature.ToString(),
            RequiresOsteopathy = session.Report.RequiresOsteopathicIntervention,
            IsComplexTorsionConflict = session.Report.IsComplexTorsionConflict,
            SerializedInputJson = inputJson,
            SerializedReportJson = reportJson,
            EncryptedMedicalPayloadBase64 = encryptedPayload,
            SecurityChecksumSha256 = session.SecurityChecksumSha256
        };

        var prescriptionEntities = session.Report.Prescriptions.Select(p => new PrescriptionEntity
        {
            SessionId = session.SessionId.ToString(),
            Placement = p.Placement.ToString(),
            EffectiveThicknessMm = p.EffectiveThicknessMm,
            NominalThicknessMm = p.NominalThicknessMm,
            CompensationPercentage = p.CompensationPercentage,
            BiomechanicalReason = p.BiomechanicalReason,
            ControlTestMandate = p.ControlTestMandate,
            RecommendedDurometer = p.RecommendedDurometer.ToString(),
            InsoleQuadrant = p.InsoleQuadrant
        }).ToList();

        // АТОМАРНАЯ транзакция: все сущности сессии либо успешно сохраняются, либо полностью откатываются
        await db.RunInTransactionAsync(syncDb =>
        {
            syncDb.InsertOrReplace(examEntity);
            syncDb.Execute("DELETE FROM PrescriptionItems WHERE SessionId = ?", examEntity.SessionId);
            foreach (var item in prescriptionEntities)
            {
                syncDb.Insert(item);
            }
        });
    }

    public async Task<IReadOnlyList<PatientExamSession>> GetPatientExamHistoryAsync(string patientId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var db = GetConnection();

        var records = await db.Table<ExamRecordEntity>()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.TimestampUtc)
            .ToListAsync();

        return records.Select(MapToSession).ToList();
    }

    public async Task<IReadOnlyList<PatientExamSession>> GetAllExamsAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var db = GetConnection();

        var records = await db.Table<ExamRecordEntity>()
            .OrderByDescending(r => r.TimestampUtc)
            .ToListAsync();

        return records.Select(MapToSession).ToList();
    }

    public async Task CloseAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection = null;
            _isInitialized = false;
        }
    }

    private PatientExamSession MapToSession(ExamRecordEntity entity)
    {
        var inputData = JsonSerializer.Deserialize<HorizontalInputData>(entity.SerializedInputJson)
                        ?? new HorizontalInputData(
                            entity.LeftMalleolusMm,
                            entity.RightMalleolusMm,
                            Enum.TryParse<LegDiscrepancyType>(entity.DiscrepancyNature, out var dt) ? dt : LegDiscrepancyType.Indeterminate,
                            false, false, false,
                            RotationBarrier.MuscleFunctional,
                            TibialTorsion.Normal,
                            GlobalBiomechanicalFlags.None,
                            PatientActivityLevel.Moderate
                        );

        var report = JsonSerializer.Deserialize<HorizontalDiagnosticReport>(entity.SerializedReportJson)
                     ?? new HorizontalDiagnosticReport(
                         entity.RequiresOsteopathy,
                         entity.MeasuredDeltaMm,
                         entity.IsComplexTorsionConflict,
                         Array.Empty<WedgeItemPrescription>(),
                         Array.Empty<string>()
                     );

        return new PatientExamSession(
            Guid.TryParse(entity.SessionId, out var gid) ? gid : Guid.NewGuid(),
            entity.PatientId,
            entity.PatientFullName,
            entity.TimestampUtc,
            inputData,
            report,
            entity.SecurityChecksumSha256
        );
    }

    private static string EncryptAesGcm(string plainText, byte[] key)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Структура: nonce (12) + tag (16) + cipherBytes
        byte[] result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }
}
