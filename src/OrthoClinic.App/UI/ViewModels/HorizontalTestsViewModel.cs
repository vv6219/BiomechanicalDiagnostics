namespace OrthoClinic.UI.ViewModels;

using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Dispatching;
using OrthoClinic.Core.Data;
using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Engine;

/// <summary>
/// Реактивная модель представления (MVVM) клинического модуля горизонтального тестирования.
/// </summary>
public partial class HorizontalTestsViewModel : ObservableObject
{
    private readonly IHorizontalAssessmentEngine _engine;
    private readonly IExamRepository _examRepository;
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

    // Данные пациента для электронной карты
    [ObservableProperty] private string _patientId = "PAT-" + DateTime.Now.ToString("yyMMdd-HHmm");
    [ObservableProperty] private string _patientFullName = "Пациент Тестовый";

    // Анимация волны и результаты
    [ObservableProperty] private double _wavePhase;
    [ObservableProperty] private HorizontalDiagnosticReport? _evaluationResult;

    // Навигация FSM и состояние сохранения
    [ObservableProperty] private int _currentStepIndex = 1;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _statusNotification = string.Empty;

    public double CurrentDelta => Math.Abs(LeftMalleolus - RightMalleolus);
    public bool CanCalculate => !IsOsteopathicAlertActive || DiscrepancyNature != LegDiscrepancyType.Indeterminate;

    public HorizontalTestsViewModel(IHorizontalAssessmentEngine engine, IExamRepository examRepository)
    {
        _engine = engine;
        _examRepository = examRepository;
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
        TriggerLightHaptic();
    }

    [RelayCommand]
    private void ToggleFallInward()
    {
        FallsInwardTest5 = !FallsInwardTest5;
        TriggerLightHaptic();
    }

    [RelayCommand]
    private void ToggleFallOutward()
    {
        FallsOutwardTest5 = !FallsOutwardTest5;
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

        EvaluationResult = _engine.Evaluate(input);

        // Управление 60 FPS таймером анимации интерференционной волны
        if (EvaluationResult.IsComplexTorsionConflict)
        {
            StartWaveAnimation();
        }
        else
        {
            StopWaveAnimation();
        }

        StatusNotification = "Биомеханический расчет успешно завершен.";
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

            string rawDataForHash = $"{PatientId}:{PatientFullName}:{DateTime.UtcNow:yyyyMMddHH}:{LeftMalleolus}:{RightMalleolus}:{CurrentDelta}";
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
