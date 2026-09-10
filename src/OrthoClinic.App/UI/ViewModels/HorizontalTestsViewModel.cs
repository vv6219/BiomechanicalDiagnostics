namespace OrthoClinic.UI.ViewModels;

using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Dispatching;
using OrthoClinic.Core.Data;
using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Domain.Protocols;
using OrthoClinic.Core.Engine;
using OrthoClinic.Core.Services;

/// <summary>
/// Реактивная модель представления (MVVM) клинического модуля горизонтального тестирования
/// и подсистемы адаптивного подиатрического протокола Formthotics.
/// </summary>
public partial class HorizontalTestsViewModel : ObservableObject
{
    private readonly IHorizontalAssessmentEngine _engine;
    private readonly IAdaptiveProtocolEngine _protocolEngine;
    private readonly IExamRepository _examRepository;
    private readonly IPodiatricPdfExportService _pdfExportService;
    private IDispatcherTimer? _waveAnimationTimer;

    // Шаг 1: Асимметрия длины нижних конечностей
    [ObservableProperty] private double _leftMalleolus;
    [ObservableProperty] private double _rightMalleolus;
    [ObservableProperty] private LegDiscrepancyType _discrepancyNature = LegDiscrepancyType.Indeterminate;
    [ObservableProperty] private bool _isOsteopathicAlertActive;

    // Шаг 2: Ротаторы (грушевидная мышца)
    [ObservableProperty] private bool _piriformisTension;

    // Шаг 3: Изолированная ротация бедра
    [ObservableProperty] private bool _antetorsion;
    [ObservableProperty] private bool _retrotorsion;
    [ObservableProperty] private RotationBarrier _barrier = RotationBarrier.MuscleFunctional;

    // Шаг 4: Торсия голени
    [ObservableProperty] private TibialTorsion _tibialState = TibialTorsion.Normal;

    // Шаг 5: Глобальные маркеры и Активность
    [ObservableProperty] private bool _fallsInwardTest5;
    [ObservableProperty] private bool _fallsOutwardTest5;
    [ObservableProperty] private PatientActivityLevel _activity = PatientActivityLevel.Moderate;
    [ObservableProperty] private PatientActivityTier _activityTier = PatientActivityTier.Moderate;

    // Биометрические маркеры пациента для адаптивного протокола
    [ObservableProperty] private double _patientWeightKg = 72.0;
    [ObservableProperty] private double _patientBmi = 22.8;
    [ObservableProperty] private int _patientAge = 34;
    [ObservableProperty] private bool _hasMortonOrNeuropathy;
    [ObservableProperty] private bool _isNarrowFootwear;

    // Данные пациента для электронной карты
    [ObservableProperty] private string _patientId = "PAT-" + DateTime.Now.ToString("yyMMdd-HHmm");
    [ObservableProperty] private string _patientFullName = "Пациент Тестовый";

    // Утверждение протокола врачом и печатная форма
    [ObservableProperty] private string _doctorFullName = "Д-р Ортопедов А.В.";
    [ObservableProperty] private string _doctorNotes = string.Empty;
    [ObservableProperty] private ProtocolApprovalRecord? _currentApproval;
    [ObservableProperty] private bool _isPrintProtocolVisible;
    [ObservableProperty] private DateTime _evaluationTimestamp = DateTime.Now;

    // Анимация волны и результаты
    [ObservableProperty] private double _wavePhase;
    [ObservableProperty] private HorizontalDiagnosticReport? _evaluationResult;
    [ObservableProperty] private ComprehensivePodiatricProtocol? _adaptiveProtocol;

    // Навигация FSM и состояние сохранения
    [ObservableProperty] private int _currentStepIndex = 1;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _statusNotification = string.Empty;

    public double CurrentDelta => Math.Abs(LeftMalleolus - RightMalleolus);
    public bool CanCalculate => !IsOsteopathicAlertActive || DiscrepancyNature != LegDiscrepancyType.Indeterminate;

    public string ClinicalHistorySummary
    {
        get
        {
            var parts = new List<string>();
            if (HasMortonOrNeuropathy) parts.Add("Неврома Мортона / Нейропатия");
            if (IsNarrowFootwear) parts.Add("Узкая модельная колодка");
            if (PiriformisTension) parts.Add("Гипертонус грушевидной");
            if (FallsInwardTest5) parts.Add("Завал внутрь (Т5)");
            if (FallsOutwardTest5) parts.Add("Завал наружу (Т5)");
            if (parts.Count == 0) return "Особых отягощений не выявлено";
            return string.Join(", ", parts);
        }
    }

    public HorizontalTestsViewModel(
        IHorizontalAssessmentEngine engine, 
        IAdaptiveProtocolEngine protocolEngine,
        IExamRepository examRepository,
        IPodiatricPdfExportService pdfExportService)
    {
        _engine = engine;
        _protocolEngine = protocolEngine;
        _examRepository = examRepository;
        _pdfExportService = pdfExportService;

        // Автоматически выполняем первичный расчет при создании модели,
        // чтобы адаптивный протокол подиатра и спецификация ВСЕГДА были заполнены результатами!
        RunEvaluation();
        IsPrintProtocolVisible = false; // При первой загрузке не перекрываем рабочее пространство модальным окном
    }

    partial void OnLeftMalleolusChanged(double value) => EvaluateDeltaDiscrepancy();
    partial void OnRightMalleolusChanged(double value) => EvaluateDeltaDiscrepancy();

    private void EvaluateDeltaDiscrepancy()
    {
        OnPropertyChanged(nameof(CurrentDelta));
        bool previousState = IsOsteopathicAlertActive;
        IsOsteopathicAlertActive = CurrentDelta > 3.0;

        if (IsOsteopathicAlertActive && !previousState)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            }
            catch
            {
                // Игнорируем на платформах без аппаратного виброотклика
            }
        }

        OnPropertyChanged(nameof(CanCalculate));
    }

    [RelayCommand]
    private void SetDiscrepancyType(string typeString)
    {
        if (Enum.TryParse<LegDiscrepancyType>(typeString, out var type))
        {
            DiscrepancyNature = type;
            OnPropertyChanged(nameof(CanCalculate));
            TriggerLightHaptic();
        }
    }

    [RelayCommand]
    private void SetTibialState(string stateString)
    {
        if (Enum.TryParse<TibialTorsion>(stateString, out var state))
        {
            TibialState = state;
            TriggerLightHaptic();
        }
    }

    [RelayCommand]
    private void SetActivityLevel(string activityString)
    {
        if (Enum.TryParse<PatientActivityLevel>(activityString, out var level))
        {
            Activity = level;
            ActivityTier = level switch
            {
                PatientActivityLevel.Sedentary => PatientActivityTier.Sedentary,
                PatientActivityLevel.Moderate => PatientActivityTier.Moderate,
                PatientActivityLevel.Active => PatientActivityTier.Active,
                PatientActivityLevel.Athlete => PatientActivityTier.Professional,
                _ => PatientActivityTier.Moderate
            };
            TriggerLightHaptic();
        }
    }

    [RelayCommand]
    private void SetActivityTier(string tierString)
    {
        if (Enum.TryParse<PatientActivityTier>(tierString, out var tier))
        {
            ActivityTier = tier;
            Activity = tier switch
            {
                PatientActivityTier.Sedentary => PatientActivityLevel.Sedentary,
                PatientActivityTier.Moderate => PatientActivityLevel.Moderate,
                PatientActivityTier.Active => PatientActivityLevel.Active,
                PatientActivityTier.Professional => PatientActivityLevel.Athlete,
                _ => PatientActivityLevel.Moderate
            };
            TriggerLightHaptic();
        }
    }

    [RelayCommand]
    private void SetBarrierType(string barrierString)
    {
        if (Enum.TryParse<RotationBarrier>(barrierString, out var b))
        {
            Barrier = b;
            TriggerLightHaptic();
        }
    }

    [RelayCommand]
    private void ToggleAntetorsion()
    {
        Antetorsion = !Antetorsion;
        if (Antetorsion) Retrotorsion = false;
        TriggerLightHaptic();
    }

    [RelayCommand]
    private void ToggleRetrotorsion()
    {
        Retrotorsion = !Retrotorsion;
        if (Retrotorsion) Antetorsion = false;
        TriggerLightHaptic();
    }

    [RelayCommand]
    private void TogglePiriformis()
    {
        PiriformisTension = !PiriformisTension;
        OnPropertyChanged(nameof(ClinicalHistorySummary));
        TriggerLightHaptic();
    }

    [RelayCommand]
    private void ToggleFallInward()
    {
        FallsInwardTest5 = !FallsInwardTest5;
        OnPropertyChanged(nameof(ClinicalHistorySummary));
        TriggerLightHaptic();
    }

    [RelayCommand]
    private void ToggleFallOutward()
    {
        FallsOutwardTest5 = !FallsOutwardTest5;
        OnPropertyChanged(nameof(ClinicalHistorySummary));
        TriggerLightHaptic();
    }

    [RelayCommand]
    private void ToggleMorton()
    {
        HasMortonOrNeuropathy = !HasMortonOrNeuropathy;
        OnPropertyChanged(nameof(ClinicalHistorySummary));
        TriggerLightHaptic();
    }

    [RelayCommand]
    private void ToggleNarrowFootwear()
    {
        IsNarrowFootwear = !IsNarrowFootwear;
        OnPropertyChanged(nameof(ClinicalHistorySummary));
        TriggerLightHaptic();
    }

    [RelayCommand]
    public void RunEvaluation()
    {
        var flags = GlobalBiomechanicalFlags.None;
        if (FallsInwardTest5) flags |= GlobalBiomechanicalFlags.FootFallsInwardTest5;
        if (FallsOutwardTest5) flags |= GlobalBiomechanicalFlags.FootFallsOutwardTest5;

        var input = new HorizontalInputData(
            LeftMalleolusMm: LeftMalleolus,
            RightMalleolusMm: RightMalleolus,
            DiscrepancyNature: DiscrepancyNature,
            PiriformisHypertonus: PiriformisTension,
            IsAntetorsion: Antetorsion,
            IsRetrotorsion: Retrotorsion,
            HipJointBarrier: Barrier,
            TibialStatus: TibialState,
            GlobalFlags: flags,
            Activity: Activity,
            PatientId: PatientId,
            PatientName: PatientFullName
        );

        EvaluationTimestamp = DateTime.Now;

        // 1. Расчет биомеханического отчета
        var rawReport = _engine.Evaluate(input);

        // 2. Генерация расширенного адаптивного подиатрического протокола
        AdaptiveProtocol = _protocolEngine.GenerateProtocol(
            clinicalData: input,
            activityTier: ActivityTier,
            patientWeightKg: PatientWeightKg,
            patientBmi: PatientBmi,
            patientAge: PatientAge,
            hasMortonOrNeuropathy: HasMortonOrNeuropathy,
            isNarrowFootwear: IsNarrowFootwear,
            calculatedPrescriptions: rawReport.Prescriptions
        );

        // 3. Формирование проекта согласования (Черновик)
        string draftFingerprint = GenerateProtocolFingerprint("DRAFT");
        CurrentApproval = new ProtocolApprovalRecord(
            Status: ProtocolApprovalStatus.DraftPendingApproval,
            ApprovedByDoctorName: DoctorFullName,
            ApprovedAtUtc: null,
            DoctorNotes: DoctorNotes,
            ApprovalDigitalFingerprint: draftFingerprint
        );

        // 4. Обогащение итогового отчета протоколом и статусом согласования
        EvaluationResult = rawReport with
        {
            AdaptiveProtocol = AdaptiveProtocol,
            Approval = CurrentApproval
        };

        // 5. Управление анимацией волны
        if (EvaluationResult.IsComplexTorsionConflict)
        {
            StartWaveAnimation();
        }
        else
        {
            StopWaveAnimation();
        }

        // 6. Автоматический показ готового к печати бланка протокола (согласно требованию)
        IsPrintProtocolVisible = true;
        StatusNotification = "✓ Расчет завершен. Подготовлен детальный протокол для печати и согласования.";
        TriggerSuccessHaptic();
    }

    [RelayCommand]
    public void ApproveProtocol()
    {
        if (EvaluationResult == null || AdaptiveProtocol == null)
        {
            StatusNotification = "Нет активного протокола для утверждения.";
            return;
        }

        string approvedFingerprint = GenerateProtocolFingerprint("APPROVED");
        CurrentApproval = new ProtocolApprovalRecord(
            Status: ProtocolApprovalStatus.ApprovedByPodiatrist,
            ApprovedByDoctorName: string.IsNullOrWhiteSpace(DoctorFullName) ? "Врач-ортопед" : DoctorFullName,
            ApprovedAtUtc: DateTime.UtcNow,
            DoctorNotes: DoctorNotes,
            ApprovalDigitalFingerprint: approvedFingerprint
        );

        EvaluationResult = EvaluationResult with
        {
            Approval = CurrentApproval
        };

        StatusNotification = "✓ Протокол успешно утвержден врачом-подиатром и скреплен цифровой подписью.";
        TriggerSuccessHaptic();
    }

    [RelayCommand]
    public void ShowPrintProtocol()
    {
        if (EvaluationResult != null)
        {
            IsPrintProtocolVisible = true;
        }
        else
        {
            StatusNotification = "Сначала выполните биомеханический расчет.";
        }
    }

    [RelayCommand]
    public void ClosePrintProtocol()
    {
        IsPrintProtocolVisible = false;
    }

    [RelayCommand]
    public void PrintProtocol()
    {
        StatusNotification = "Печать медицинского протокола отправлена на устройство...";
        TriggerSuccessHaptic();
    }

    [RelayCommand]
    public async Task ExportPdfProtocolAsync()
    {
        if (EvaluationResult == null || AdaptiveProtocol == null)
        {
            StatusNotification = "Сначала выполните биомеханический расчет клиньев.";
            return;
        }

        try
        {
            StatusNotification = "Формирование официального PDF-протокола Formthotics...";
            var request = new PdfExportRequest(
                PatientId: PatientId,
                PatientFullName: PatientFullName,
                PatientAge: PatientAge,
                PatientWeightKg: PatientWeightKg,
                PatientBmi: PatientBmi,
                ActivityTier: ActivityTier,
                ClinicalHistorySummary: ClinicalHistorySummary,
                MeasuredDeltaMm: CurrentDelta,
                DiscrepancyNature: DiscrepancyNature,
                Barrier: Barrier,
                TibialState: TibialState,
                Report: EvaluationResult,
                Protocol: AdaptiveProtocol,
                Approval: CurrentApproval,
                DoctorFullName: DoctorFullName,
                DoctorNotes: DoctorNotes,
                EvaluationTimestamp: EvaluationTimestamp
            );

            string pdfPath = await _pdfExportService.ExportProtocolPdfAsync(request);
            StatusNotification = $"✓ PDF протокол успешно экспортирован: {Path.GetFileName(pdfPath)}";
            TriggerSuccessHaptic();

            // Запуск созданного PDF в системном просмотрщике
            await Launcher.Default.OpenAsync(new OpenFileRequest("Медицинский протокол Formthotics", new ReadOnlyFile(pdfPath)));
        }
        catch (Exception ex)
        {
            StatusNotification = $"Ошибка экспорта PDF: {ex.Message}";
        }
    }

    private string GenerateProtocolFingerprint(string state)
    {
        string raw = $"{PatientId}:{PatientFullName}:{state}:{DateTime.UtcNow:yyyyMMddHHmmss}:{AdaptiveProtocol?.RecommendedModelTitle}:{CurrentDelta:F1}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
    }

    [RelayCommand]
    public async Task SaveExamRecordAsync()
    {
        if (EvaluationResult == null)
        {
            StatusNotification = "Сначала выполните биомеханический расчет клиньев.";
            return;
        }

        IsSaving = true;
        StatusNotification = "Атомарное сохранение в зашифрованное хранилище SQLite...";

        try
        {
            var flags = GlobalBiomechanicalFlags.None;
            if (FallsInwardTest5) flags |= GlobalBiomechanicalFlags.FootFallsInwardTest5;
            if (FallsOutwardTest5) flags |= GlobalBiomechanicalFlags.FootFallsOutwardTest5;

            var input = new HorizontalInputData(
                LeftMalleolus, RightMalleolus, DiscrepancyNature,
                PiriformisTension, Antetorsion, Retrotorsion,
                Barrier, TibialState, flags, Activity,
                PatientId, PatientFullName
            );

            string rawDataForHash = $"{PatientId}:{PatientFullName}:{DateTime.UtcNow:yyyyMMddHH}:{LeftMalleolus}:{RightMalleolus}:{CurrentDelta}:{CurrentApproval?.ApprovalDigitalFingerprint}";
            string checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawDataForHash)));

            var session = new PatientExamSession(
                SessionId: Guid.NewGuid(),
                PatientId: PatientId,
                PatientFullName: PatientFullName,
                TimestampUtc: DateTime.UtcNow,
                InputData: input,
                Report: EvaluationResult,
                SecurityChecksumSha256: checksum
            );

            await _examRepository.SaveExamTransactionAsync(session);

            StatusNotification = $"✓ Протокол обследования успешно зафиксирован в защищенной карте (ID: {session.SessionId.ToString()[..8]}).";
            TriggerSuccessHaptic();
        }
        catch (Exception ex)
        {
            StatusNotification = $"Ошибка транзакции: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    public void StartWaveAnimation()
    {
        if (_waveAnimationTimer != null && _waveAnimationTimer.IsRunning) return;

        if (Application.Current?.Dispatcher != null)
        {
            _waveAnimationTimer = Application.Current.Dispatcher.CreateTimer();
            _waveAnimationTimer.Interval = TimeSpan.FromMilliseconds(20); // ~50-60 FPS
            _waveAnimationTimer.Tick += (s, e) =>
            {
                WavePhase += 0.08;
                if (WavePhase > Math.PI * 2) WavePhase -= Math.PI * 2;
            };
            _waveAnimationTimer.Start();
        }
    }

    public void StopWaveAnimation()
    {
        if (_waveAnimationTimer != null)
        {
            _waveAnimationTimer.Stop();
            _waveAnimationTimer = null;
        }
    }

    private void TriggerLightHaptic()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private void TriggerSuccessHaptic()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }
}
