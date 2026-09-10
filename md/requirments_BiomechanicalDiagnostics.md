# **Техническое задание и Промпт: Клиническая рабочая станция горизонтальной ортопедической диагностики и подбора клиньев (.NET 10 MAUI / C\# 14\)**

## **1\. Системная архитектура и клинические постулаты**

Необходимо разработать коммерческий оффлайн-модуль медицинской рабочей станции для ортопедов-подиатров на стеке **.NET 10 (net10.0-android)**, **C\# 14** и **MAUI**. Модуль переводит горизонтальный протокол мануального тестирования пациента на кушетке в детерминированный алгоритмический контур с реактивным визуальным интерфейсом.  
**Клинические правила биомеханики ядра:**

* Бедро стремится к центрации в тазобедренном суставе, плоскость колена и ось стопы — к сагиттальной плоскости.  
* Подошва стопы — адаптивная динамическая структура; её компенсаторную функцию запрещено срывать избыточной коррекцией.  
* Любая структурная асимметрия тазового пояса и длины конечностей блокирует изолированную коррекцию стопы: система реализует строгий конечный автомат (FSM), предотвращающий редукционизм и направляющий клинический маршрут на остеопатический уровень при превышении порогов.  
* Каждое сохранение результатов обследования представляет собой **АТОМАРНУЮ** транзакцию в локальном зашифрованном хранилище SQLite.

## **2\. Математический аппарат и интерференция торсионов**

### **2.1. Критерий структурной асимметрии длины ног**

Относительное смещение лодыжек во фронтальной плоскости:

$$\\Delta L \= \\vert{}L\_{left} \- L\_{right}\\vert{}$$  
При $\\Delta L \> 3.0\\text{ мм}$ система генерирует системный отказ в односторонней коррекции и требует дифференциального маневра Вебера-Барстоу.

### **2.2. Расчет итоговой толщины клиньев (Сохранение адаптации)**

Толщина клина обратно пропорциональна метаболической и кинематической активности пациента во избежание срыва адаптации и гиперкератоза:

$$T\_{final} \= T\_{nominal} \\cdot \\kappa\_{activity}$$  
Где $\\kappa\_{activity}$:

* Sedentary (Малоактивный): $\\kappa\_{activity} \= 1.00$ ($100\\%$ компенсации угла недостаточности).  
* Moderate (Умеренный): $\\kappa\_{activity} \= 0.75$  
* Active (Высокая активность): $\\kappa\_{activity} \= 0.50$  
* Athlete (Профессиональный спорт): $\\kappa\_{activity} \= 0.25$ (минимальная толщина).

### **2.3. Математическая модель торсионного парадокса (Тест 5\)**

При конфликте направлений ротации (антеторсия бедра $+$ перекрут голени кнаружи) суперпозиция биомеханических векторов формирует стоячую волну деструктивного напряжения:

$$W\_{femur}(x, t) \= A\_1 \\sin(k x \- \\omega t)$$

$$W\_{tibia}(x, t) \= A\_2 \\sin(k x \+ \\omega t \+ \\pi)$$

$$Y\_{conflict}(x, t) \= W\_{femur}(x, t) \+ W\_{tibia}(x, t)$$  
Где $k$ — волновое число суставной цепи, $\\omega$ — циклическая частота шагового цикла. Результирующая интерференция визуализируется на канвасе как зоны пикового стресса.

## **3\. Спецификация доменного слоя (Core/Domain/)**

C\#  
namespace OrthoClinic.Core.Domain;

public enum RotationBarrier   
{   
    MuscleFunctional, // Мышечный (функциональный) барьер  
    BoneAnatomical    // Костный (анатомический) замок  
}

public enum TibialTorsion   
{   
    Normal,               // Физиологическая норма (параллельно/слегка кнаружи)  
    UnderRotationInward,  // Недокрут внутрь (ось стопы кнутри)  
    OverRotationOutward   // Перекрут кнаружи (ось стопы кнаружи, варус переднего отдела)  
}

public enum LegDiscrepancyType   
{   
    Indeterminate,   
    TrueAnatomical,   
    FunctionalPelvic   
}

public enum PatientActivityLevel   
{   
    Sedentary \= 0,   
    Moderate \= 1,   
    Active \= 2,   
    Athlete \= 3   
}

public enum WedgePlacementType   
{   
    AnteriorLateral,      // Передний наружный клин  
    AnteriorMedial,       // Передний медиальный клин  
    PosteriorMedial,      // Задний медиальный клин  
    FifthMetatarsalBase,  // Клин под основание V плюсневой кости  
    HeelLiftCompensator   // Разгрузочный подпяточник  
}

\[Flags\]  
public enum GlobalBiomechanicalFlags : uint  
{  
    None \= 0,  
    FootFallsInwardTest5 \= 1 \<\< 0,  // Стопа сильно заваливается внутрь в 5-м тесте Formthotics  
    FootFallsOutwardTest5 \= 1 \<\< 1  // Пациент заваливается кнаружи при динамической пробе  
}

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
    PatientActivityLevel Activity  
);

public sealed record WedgeItemPrescription(  
    WedgePlacementType Placement,  
    double EffectiveThicknessMm,  
    double CompensationPercentage,  
    string BiomechanicalReason,  
    string ControlTestMandate  
);

public sealed record HorizontalDiagnosticReport(  
    bool RequiresOsteopathicIntervention,  
    double MeasuredDeltaMm,  
    bool IsComplexTorsionConflict,  
    IReadOnlyList\<WedgeItemPrescription\> Prescriptions,  
    IReadOnlyList\<string\> ClinicalAlerts  
);

## **4\. Клинический алгоритм и движок сопоставления (Engine/HorizontalAssessmentEngine.cs)**

C\#  
namespace OrthoClinic.Core.Engine;

using OrthoClinic.Core.Domain;

public interface IHorizontalAssessmentEngine  
{  
    HorizontalDiagnosticReport Evaluate(HorizontalInputData input);  
}

public sealed class HorizontalAssessmentEngine : IHorizontalAssessmentEngine  
{  
    public HorizontalDiagnosticReport Evaluate(HorizontalInputData input)  
    {  
        var alerts \= new List\<string\>();  
        var prescriptions \= new List\<WedgeItemPrescription\>();  
        double deltaMm \= Math.Abs(input.LeftMalleolusMm \- input.RightMalleolusMm);  
        bool needsOsteopathy \= false;

        // Коэффициент толщины по активности  
        double activityCoeff \= input.Activity switch  
        {  
            PatientActivityLevel.Sedentary \=\> 1.00,  
            PatientActivityLevel.Moderate \=\> 0.75,  
            PatientActivityLevel.Active \=\> 0.50,  
            PatientActivityLevel.Athlete \=\> 0.25,  
            \_ \=\> 0.75  
        };

        // ТЕСТ 1: Оценка длины ног и остеопатический порог  
        if (deltaMm \> 3.0)  
        {  
            needsOsteopathy \= true;  
            alerts.Add("Показано применение дополнительных остеопатических тестов.");

            if (input.DiscrepancyNature \== LegDiscrepancyType.FunctionalPelvic)  
            {  
                alerts.Add("Выявлен функциональный перекос таза. Коррекция подпяточником противопоказана во избежание фиксации дисфункции.");  
            }  
            else if (input.DiscrepancyNature \== LegDiscrepancyType.TrueAnatomical)  
            {  
                prescriptions.Add(new WedgeItemPrescription(  
                    Placement: WedgePlacementType.HeelLiftCompensator,  
                    EffectiveThicknessMm: Math.Round((deltaMm / 2.0) \* activityCoeff, 1),  
                    CompensationPercentage: activityCoeff \* 100,  
                    BiomechanicalReason: $"Истинное анатомическое укорочение ({deltaMm:F1} мм). Частичная разгрузочная компенсация.",  
                    ControlTestMandate: "Оценка горизонтали крестца."  
                ));  
            }  
        }

        // ТЕСТ 2: Тест ротаторов (грушевидные мышцы)  
        if (input.PiriformisHypertonus)  
        {  
            prescriptions.Add(new WedgeItemPrescription(  
                Placement: WedgePlacementType.AnteriorLateral,  
                EffectiveThicknessMm: 3.0 \* activityCoeff,  
                CompensationPercentage: activityCoeff \* 100,  
                BiomechanicalReason: "Гипертонус грушевидных мышц, натяжение тканей и наружный разворот стоп.",  
                ControlTestMandate: "Повторная пассивная ротация внутрь на кушетке."  
            ));  
        }

        // ТЕСТ 3: Изолированная ротация бедра  
        if (input.IsAntetorsion)  
        {  
            if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsInwardTest5))  
            {  
                prescriptions.Add(new WedgeItemPrescription(  
                    Placement: WedgePlacementType.PosteriorMedial,  
                    EffectiveThicknessMm: 4.0 \* activityCoeff,  
                    CompensationPercentage: activityCoeff \* 100,  
                    BiomechanicalReason: "Антеторсия шейки бедра с выраженным падением стопы внутрь в глобальном тесте.",  
                    ControlTestMandate: "Строго через глобальный двигательный тест\!"  
                ));  
            }  
            else  
            {  
                prescriptions.Add(new WedgeItemPrescription(  
                    Placement: WedgePlacementType.AnteriorLateral,  
                    EffectiveThicknessMm: 3.0 \* activityCoeff,  
                    CompensationPercentage: activityCoeff \* 100,  
                    BiomechanicalReason: "Антеторсия шейки бедра (костный барьер наружной ротации, компенсаторный вальгус шага).",  
                    ControlTestMandate: "Дифференциация барьера вращения шейки бедра."  
                ));  
            }  
        }  
        else if (input.IsRetrotorsion)  
        {  
            prescriptions.Add(new WedgeItemPrescription(  
                Placement: WedgePlacementType.AnteriorLateral,  
                EffectiveThicknessMm: 2.5 \* activityCoeff,  
                CompensationPercentage: activityCoeff \* 100,  
                BiomechanicalReason: "Ретроторсия бедра с патологической наружной установкой нижней конечности.",  
                ControlTestMandate: "Тест ротаторов бедра."  
            ));  
        }

        // ТЕСТ 4: Торсия большеберцовой кости  
        if (input.TibialStatus \== TibialTorsion.UnderRotationInward)  
        {  
            if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsInwardTest5))  
            {  
                prescriptions.Add(new WedgeItemPrescription(  
                    Placement: WedgePlacementType.PosteriorMedial,  
                    EffectiveThicknessMm: 4.0 \* activityCoeff,  
                    CompensationPercentage: activityCoeff \* 100,  
                    BiomechanicalReason: "Внутренний недокрут большеберцовой кости с завалом стопы внутрь.",  
                    ControlTestMandate: "Контроль по мануальному мышечному тесту подколенной мышцы."  
                ));  
            }  
            else  
            {  
                prescriptions.Add(new WedgeItemPrescription(  
                    Placement: WedgePlacementType.AnteriorLateral,  
                    EffectiveThicknessMm: 3.0 \* activityCoeff,  
                    CompensationPercentage: activityCoeff \* 100,  
                    BiomechanicalReason: "Недокрут большеберцовой кости внутрь (провокация вальгусной деформации).",  
                    ControlTestMandate: "Визуальный контроль оси голень-стопа."  
                ));  
            }  
        }  
        else if (input.TibialStatus \== TibialTorsion.OverRotationOutward)  
        {  
            prescriptions.Add(new WedgeItemPrescription(  
                Placement: WedgePlacementType.AnteriorMedial,  
                EffectiveThicknessMm: 3.5 \* activityCoeff,  
                CompensationPercentage: activityCoeff \* 100,  
                BiomechanicalReason: "Перекрут большеберцовой кости кнаружи, варус переднего отдела стопы, риск Hallux Valgus.",  
                ControlTestMandate: "Оценка параллельности оси сгибания коленного сустава."  
            ));

            if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsOutwardTest5))  
            {  
                prescriptions.Add(new WedgeItemPrescription(  
                    Placement: WedgePlacementType.FifthMetatarsalBase,  
                    EffectiveThicknessMm: 2.0 \* activityCoeff,  
                    CompensationPercentage: activityCoeff \* 100,  
                    BiomechanicalReason: "Динамическое падение стопы кнаружи при торсионном перекруте голени.",  
                    ControlTestMandate: "Тест стабильности латерального свода."  
                ));  
            }  
        }

        // ТЕСТ 5: Сочетанные патологии и торсионный конфликт  
        bool isConflict \= input.IsAntetorsion && input.TibialStatus \== TibialTorsion.OverRotationOutward;  
        if (isConflict)  
        {  
            alerts.Add("Внимание: Торсионный конфликт (Антеторсия бедра \+ Наружный перекрут голени). Вычисление средней результирующей оси. Приоритет отдается 6-му тесту по Бейкрофту для сохранения адаптации подошвы.");  
        }

        return new HorizontalDiagnosticReport(  
            RequiresOsteopathicIntervention: needsOsteopathy,  
            MeasuredDeltaMm: deltaMm,  
            IsComplexTorsionConflict: isConflict,  
            Prescriptions: prescriptions,  
            ClinicalAlerts: alerts  
        );  
    }  
}

## **5\. Интерактивные векторные канвасы (UI/Drawables/)**

### **5.1. LegLengthDiscrepancyDrawable.cs (Тест 1: Топология натяжения)**

C\#  
namespace OrthoClinic.UI.Drawables;

using Microsoft.Maui.Graphics;

public sealed class LegLengthDiscrepancyDrawable : IDrawable  
{  
    public double LeftOffset { get; set; }  
    public double RightOffset { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)  
    {  
        canvas.SaveState();  
        canvas.Antialiasing \= true;

        float midX \= dirtyRect.Center.X;  
        float baseY \= dirtyRect.Height \* 0.6f;  
        float legSpacing \= 70f;

        float leftY \= baseY \+ (float)LeftOffset \* 3f;  
        float rightY \= baseY \+ (float)RightOffset \* 3f;  
        double delta \= Math.Abs(LeftOffset \- RightOffset);

        // Направляющие осей конечностей  
        canvas.StrokeColor \= Color.FromArgb("\#334155");  
        canvas.StrokeSize \= 2;  
        canvas.DrawLine(midX \- legSpacing, 20, midX \- legSpacing, leftY);  
        canvas.DrawLine(midX \+ legSpacing, 20, midX \+ legSpacing, rightY);

        // Индикатор перекоса лодыжек  
        canvas.StrokeSize \= delta \> 3.0 ? 3.5f : 1.5f;  
        canvas.StrokeColor \= delta \> 3.0 ? Color.FromArgb("\#EF4444") : Color.FromArgb("\#10B981");

        if (delta \> 3.0)  
            canvas.StrokeDashPattern \= new float\[\] { 6, 3 };

        canvas.DrawLine(midX \- legSpacing \- 20, leftY, midX \+ legSpacing \+ 20, rightY);

        // Реперы лодыжек  
        canvas.FillColor \= delta \> 3.0 ? Color.FromArgb("\#EF4444") : Color.FromArgb("\#38BDF8");  
        canvas.FillCircle(midX \- legSpacing, leftY, 6);  
        canvas.FillCircle(midX \+ legSpacing, rightY, 6);

        canvas.RestoreState();  
    }  
}

### **5.2. TibialTorsionPalimpsestDrawable.cs (Тест 4: Палимпсест уровней)**

C\#  
namespace OrthoClinic.UI.Drawables;

using Microsoft.Maui.Graphics;  
using OrthoClinic.Core.Domain;

public sealed class TibialTorsionPalimpsestDrawable : IDrawable  
{  
    public TibialTorsion State { get; set; } \= TibialTorsion.Normal;

    public void Draw(ICanvas canvas, RectF dirtyRect)  
    {  
        canvas.SaveState();  
        canvas.Antialiasing \= true;

        float cx \= dirtyRect.Center.X;  
        float cy \= dirtyRect.Center.Y;

        // 1\. Нижний уровень (Bootloader): Мыщелки бедра (горизонтальная ось)  
        canvas.StrokeColor \= Color.FromArgb("\#475569");  
        canvas.StrokeSize \= 3;  
        canvas.DrawLine(cx \- 90, cy, cx \+ 90, cy);  
        canvas.FillColor \= Color.FromArgb("\#334155");  
        canvas.FillCircle(cx \- 90, cy, 8);  
        canvas.FillCircle(cx \+ 90, cy, 8);

        // 2\. Верхний уровень: Продольная ось стопы с трансформацией угла  
        float rotationDeg \= State switch  
        {  
            TibialTorsion.UnderRotationInward \=\> \-18f,  
            TibialTorsion.OverRotationOutward \=\> 22f,  
            \_ \=\> 3f  
        };

        canvas.Rotate(rotationDeg, cx, cy);

        // Отрисовка контура стопы  
        var footColor \= State \== TibialTorsion.Normal ? Color.FromArgb("\#38BDF8") : Color.FromArgb("\#F59E0B");  
        canvas.StrokeColor \= footColor;  
        canvas.StrokeSize \= 2.5f;

        var footPath \= new PathF();  
        footPath.MoveTo(cx \- 20, cy \- 70);  
        footPath.LineTo(cx \+ 20, cy \- 70);  
        footPath.LineTo(cx \+ 25, cy \+ 50);  
        footPath.LineTo(cx \- 25, cy \+ 50);  
        footPath.Close();  
        canvas.DrawPath(footPath);

        // Вектор скручивания  
        canvas.StrokeColor \= State \== TibialTorsion.OverRotationOutward ? Color.FromArgb("\#EF4444") : Color.FromArgb("\#10B981");  
        canvas.DrawLine(cx, cy \+ 50, cx, cy \- 85);

        canvas.RestoreState();  
    }  
}

### **5.3. AdaptiveWedgeDrawable.cs (Тест 5: Волновой конфликт и адаптивный клин)**

C\#  
namespace OrthoClinic.UI.Drawables;

using Microsoft.Maui.Graphics;

public sealed class AdaptiveWedgeDrawable : IDrawable  
{  
    public bool HasConflict { get; set; }  
    public double ActivityRatio { get; set; } \= 1.0;  
    public double Phase { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)  
    {  
        canvas.SaveState();  
        canvas.Antialiasing \= true;

        float dynamicHeight \= (float)(45f \* ActivityRatio);  
        float bottomY \= dirtyRect.Height \- 15f;

        // Профиль адаптивного клина  
        var wedge \= new PathF();  
        wedge.MoveTo(20, bottomY);  
        wedge.LineTo(dirtyRect.Width \- 20, bottomY);  
        wedge.LineTo(20, bottomY \- dynamicHeight);  
        wedge.Close();

        canvas.FillColor \= Color.FromArgb("\#1E293B");  
        canvas.FillPath(wedge);  
        canvas.StrokeColor \= Color.FromArgb("\#38BDF8");  
        canvas.StrokeSize \= 1.5f;  
        canvas.DrawPath(wedge);

        // Отрисовка интерференционной стоячей волны стресса при конфликте  
        if (HasConflict)  
        {  
            var wavePath \= new PathF();  
            float startX \= 20;  
            float endX \= dirtyRect.Width \- 20;  
            float midY \= bottomY \- (dynamicHeight / 2f);  
            float k \= 0.06f;  
            float amp \= 10f;

            for (float x \= startX; x \<= endX; x \+= 2f)  
            {  
                double wFemur \= amp \* Math.Sin(k \* x \- Phase);  
                double wTibia \= amp \* Math.Sin(k \* x \+ Phase \+ Math.PI);  
                float y \= midY \+ (float)(wFemur \+ wTibia);

                if (x \== startX) wavePath.MoveTo(x, y);  
                else wavePath.LineTo(x, y);  
            }

            canvas.StrokeColor \= Color.FromArgb("\#EF4444");  
            canvas.StrokeSize \= 2.5f;  
            canvas.DrawPath(wavePath);

            // Очаг максимального деструктивного момента  
            canvas.FillColor \= Color.FromArgb("\#DC2626");  
            canvas.FillCircle(dirtyRect.Center.X, midY, 5);  
        }

        canvas.RestoreState();  
    }  
}

## **6\. Реактивная ViewModel (HorizontalTestsViewModel.cs)**

C\#  
namespace OrthoClinic.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;  
using CommunityToolkit.Mvvm.Input;  
using Microsoft.Maui.Devices;  
using OrthoClinic.Core.Domain;  
using OrthoClinic.Core.Engine;

public partial class HorizontalTestsViewModel : ObservableObject  
{  
    private readonly IHorizontalAssessmentEngine \_engine;

    // Шаг 1: Асимметрия длины  
    \[ObservableProperty\] private double \_leftMalleolus;  
    \[ObservableProperty\] private double \_rightMalleolus;  
    \[ObservableProperty\] private LegDiscrepancyType \_discrepancyNature \= LegDiscrepancyType.Indeterminate;  
    \[ObservableProperty\] private bool \_isOsteopathicAlertActive;

    // Шаг 2: Ротаторы  
    \[ObservableProperty\] private bool \_piriformisTension;

    // Шаг 3: Бедро  
    \[ObservableProperty\] private bool \_antetorsion;  
    \[ObservableProperty\] private bool \_retrotorsion;  
    \[ObservableProperty\] private RotationBarrier \_barrier \= RotationBarrier.MuscleFunctional;

    // Шаг 4: Голень  
    \[ObservableProperty\] private TibialTorsion \_tibialState \= TibialTorsion.Normal;

    // Шаг 5: Глобальные маркеры и Активность  
    \[ObservableProperty\] private bool \_fallsInwardTest5;  
    \[ObservableProperty\] private bool \_fallsOutwardTest5;  
    \[ObservableProperty\] private PatientActivityLevel \_activity \= PatientActivityLevel.Moderate;

    // Анимация волны и результаты  
    \[ObservableProperty\] private double \_wavePhase;  
    \[ObservableProperty\] private HorizontalDiagnosticReport? \_evaluationResult;

    public double CurrentDelta \=\> Math.Abs(LeftMalleolus \- RightMalleolus);  
    public bool CanCalculate \=\> \!IsOsteopathicAlertActive || DiscrepancyNature \!= LegDiscrepancyType.Indeterminate;

    public HorizontalTestsViewModel(IHorizontalAssessmentEngine engine)  
    {  
        \_engine \= engine;  
    }

    partial void OnLeftMalleolusChanged(double value) \=\> EvaluateDeltaDiscrepancy();  
    partial void OnRightMalleolusChanged(double value) \=\> EvaluateDeltaDiscrepancy();

    private void EvaluateDeltaDiscrepancy()  
    {  
        OnPropertyChanged(nameof(CurrentDelta));  
        bool previousState \= IsOsteopathicAlertActive;  
        IsOsteopathicAlertActive \= CurrentDelta \> 3.0;

        if (IsOsteopathicAlertActive && \!previousState)  
        {  
            try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }  
        }

        OnPropertyChanged(nameof(CanCalculate));  
    }

    \[RelayCommand\]  
    private void SetDiscrepancyType(LegDiscrepancyType type)  
    {  
        DiscrepancyNature \= type;  
        OnPropertyChanged(nameof(CanCalculate));  
    }

    \[RelayCommand\]  
    public void RunEvaluation()  
    {  
        var flags \= GlobalBiomechanicalFlags.None;  
        if (FallsInwardTest5) flags |= GlobalBiomechanicalFlags.FootFallsInwardTest5;  
        if (FallsOutwardTest5) flags |= GlobalBiomechanicalFlags.FootFallsOutwardTest5;

        var input \= new HorizontalInputData(  
            LeftMalleolusMm: LeftMalleolus,  
            RightMalleolusMm: RightMalleolus,  
            DiscrepancyNature: DiscrepancyNature,  
            PiriformisHypertonus: PiriformisTension,  
            IsAntetorsion: Antetorsion,  
            IsRetrotorsion: Retrotorsion,  
            HipJointBarrier: Barrier,  
            TibialStatus: TibialState,  
            GlobalFlags: flags,  
            Activity: Activity  
        );

        EvaluationResult \= \_engine.Evaluate(input);  
    }  
}

## **7\. XAML Представление модуля (HorizontalAssessmentView.xaml)**

XML  
\<?xml version="1.0" encoding="utf-8" ?\>  
\<ContentPage xmlns\="http://schemas.microsoft.com/dotnet/2021/maui"  
             xmlns:x\="http://schemas.microsoft.com/winfx/2009/xaml"  
             xmlns:drawables\="clr-namespace:OrthoClinic.UI.Drawables"  
             xmlns:vm\="clr-namespace:OrthoClinic.UI.ViewModels"  
             x:Class\="OrthoClinic.UI.Views.HorizontalAssessmentView"  
             BackgroundColor\="\#0F172A"\>

    \<ScrollView\>  
        \<VerticalStackLayout Padding\="16" Spacing\="18"\>

            \<\!-- ШАГ 1: ОЦЕНКА ДЛИНЫ НОГ \--\>  
            \<Border BackgroundColor\="\#1E293B" Padding\="14" Stroke\="\#334155" StrokeShape\="RoundRectangle 14"\>  
                \<VerticalStackLayout Spacing\="10"\>  
                    \<Label Text\="Тест 1: Взаимное положение лодыжек (Кушетка)" TextColor\="\#38BDF8" FontAttributes\="Bold" FontSize\="14"/\>  
                      
                    \<GraphicsView HeightRequest\="130" HorizontalOptions\="FillAndExpand"\>  
                        \<GraphicsView.Drawable\>  
                            \<drawables:LegLengthDiscrepancyDrawable   
                                LeftOffset\="{Binding LeftMalleolus}"   
                                RightOffset\="{Binding RightMalleolus}"/\>  
                        \</GraphicsView.Drawable\>  
                    \</GraphicsView\>

                    \<Grid ColumnDefinitions\="\*,\*" ColumnSpacing\="12"\>  
                        \<VerticalStackLayout Grid.Column\="0"\>  
                            \<Label Text\="Левая лодыжка (мм)" TextColor\="\#94A3B8" FontSize\="11"/\>  
                            \<Slider Minimum\="-15" Maximum\="15" Value\="{Binding LeftMalleolus}"/\>  
                        \</VerticalStackLayout\>  
                        \<VerticalStackLayout Grid.Column\="1"\>  
                            \<Label Text\="Правая лодыжка (мм)" TextColor\="\#94A3B8" FontSize\="11"/\>  
                            \<Slider Minimum\="-15" Maximum\="15" Value\="{Binding RightMalleolus}"/\>  
                        \</VerticalStackLayout\>  
                    \</Grid\>

                    \<\!-- ОСТЕОПАТИЧЕСКИЙ БАРЬЕР \--\>  
                    \<Border IsVisible\="{Binding IsOsteopathicAlertActive}" BackgroundColor\="\#450A0A" Stroke\="\#DC2626" Padding\="12" StrokeShape\="RoundRectangle 10"\>  
                        \<VerticalStackLayout Spacing\="8"\>  
                            \<Label Text\="⚠ Показано применение дополнительных остеопатических тестов" TextColor\="\#FCA5A5" FontAttributes\="Bold" FontSize\="13"/\>  
                            \<Label Text\="Выполните маневр Вебера-Барстоу для дифференциации перекоса таза:" TextColor\="\#E2E8F0" FontSize\="11"/\>  
                            \<HorizontalStackLayout Spacing\="10"\>  
                                \<Button Text\="Анатомическое" Command\="{Binding SetDiscrepancyTypeCommand}" CommandParameter\="TrueAnatomical" BackgroundColor\="\#DC2626" TextColor\="White" HeightRequest\="38" CornerRadius\="8"/\>  
                                \<Button Text\="Функциональное (Таз)" Command\="{Binding SetDiscrepancyTypeCommand}" CommandParameter\="FunctionalPelvic" BackgroundColor\="\#B91C1C" TextColor\="White" HeightRequest\="38" CornerRadius\="8"/\>  
                            \</HorizontalStackLayout\>  
                        \</VerticalStackLayout\>  
                    \</Border\>  
                \</VerticalStackLayout\>  
            \</Border\>

            \<\!-- ШАГ 4: ТОРСИЯ ГОЛЕНИ (ПАЛИМПСЕСТ) \--\>  
            \<Border BackgroundColor\="\#1E293B" Padding\="14" Stroke\="\#334155" StrokeShape\="RoundRectangle 14"\>  
                \<VerticalStackLayout Spacing\="10"\>  
                    \<Label Text\="Тест 4: Торсия большеберцовой кости относительно мыщелков" TextColor\="\#38BDF8" FontAttributes\="Bold" FontSize\="14"/\>  
                    \<GraphicsView HeightRequest\="150" HorizontalOptions\="FillAndExpand"\>  
                        \<GraphicsView.Drawable\>  
                            \<drawables:TibialTorsionPalimpsestDrawable State\="{Binding TibialState}"/\>  
                        \</GraphicsView.Drawable\>  
                    \</GraphicsView\>  
                    \<HorizontalStackLayout Spacing\="8" HorizontalOptions\="Center"\>  
                        \<Button Text\="Недокрут внутрь" Command\="{Binding SetTibialStateCommand}" CommandParameter\="UnderRotationInward" BackgroundColor\="\#334155" TextColor\="White" CornerRadius\="8"/\>  
                        \<Button Text\="Норма" Command\="{Binding SetTibialStateCommand}" CommandParameter\="Normal" BackgroundColor\="\#0284C7" TextColor\="White" CornerRadius\="8"/\>  
                        \<Button Text\="Перекрут кнаружи" Command\="{Binding SetTibialStateCommand}" CommandParameter\="OverRotationOutward" BackgroundColor\="\#334155" TextColor\="White" CornerRadius\="8"/\>  
                    \</HorizontalStackLayout\>  
                \</VerticalStackLayout\>  
            \</Border\>

            \<\!-- РАСЧЕТ И КОМПЕНСАТОРНЫЙ КЛИН \--\>  
            \<Button Text\="Выполнить биомеханический расчет клиньев"   
                    Command\="{Binding RunEvaluationCommand}"  
                    IsEnabled\="{Binding CanCalculate}"  
                    BackgroundColor\="\#0284C7" TextColor\="White" FontAttributes\="Bold" HeightRequest\="50" CornerRadius\="12"/\>

            \<\!-- ОТРИСОВКА КЛИНИЧЕСКОГО ЗАКЛЮЧЕНИЯ И ВОЛНОВОГО КОНФЛИКТА \--\>  
            \<Border IsVisible\="{Binding EvaluationResult, Converter={StaticResource NotNullConverter}}" BackgroundColor\="\#1E293B" Padding\="14" StrokeShape\="RoundRectangle 14"\>  
                \<VerticalStackLayout Spacing\="10"\>  
                    \<Label Text\="Спецификация индивидуальной коррекции:" TextColor\="\#38BDF8" FontAttributes\="Bold"/\>  
                    \<GraphicsView HeightRequest\="90"\>  
                        \<GraphicsView.Drawable\>  
                            \<drawables:AdaptiveWedgeDrawable   
                                HasConflict\="{Binding EvaluationResult.IsComplexTorsionConflict}"   
                                Phase\="{Binding WavePhase}"/\>  
                        \</GraphicsView.Drawable\>  
                    \</GraphicsView\>  
                      
                    \<CollectionView ItemsSource\="{Binding EvaluationResult.Prescriptions}"\>  
                        \<CollectionView.ItemTemplate\>  
                            \<DataTemplate\>  
                                \<Border BackgroundColor\="\#0F172A" Margin\="0,4" Padding\="10" StrokeShape\="RoundRectangle 8"\>  
                                    \<Grid ColumnDefinitions\="\*,Auto"\>  
                                        \<VerticalStackLayout Grid.Column\="0"\>  
                                            \<Label Text\="{Binding Placement}" TextColor\="White" FontAttributes\="Bold" FontSize\="13"/\>  
                                            \<Label Text\="{Binding BiomechanicalReason}" TextColor\="\#94A3B8" FontSize\="11"/\>  
                                            \<Label Text\="{Binding ControlTestMandate, StringFormat='Контроль: {0}'}" TextColor\="\#38BDF8" FontSize\="10"/\>  
                                        \</VerticalStackLayout\>  
                                        \<Label Grid.Column\="1" Text\="{Binding EffectiveThicknessMm, StringFormat='{0:F1} мм'}" TextColor\="\#10B981" FontAttributes\="Bold" VerticalOptions\="Center"/\>  
                                    \</Grid\>  
                                \</Border\>  
                            \</DataTemplate\>  
                        \</CollectionView.ItemTemplate\>  
                    \</CollectionView\>  
                \</VerticalStackLayout\>  
            \</Border\>

        \</VerticalStackLayout\>  
    \</ScrollView\>  
\</ContentPage\>

## **8\. Задание для компиляции и кодогенерации**

> 1. Реализовать доменную модель, движок HorizontalAssessmentEngine со всеми ветками исключений глобальных тестов и остеопатического триггера.  
> 2. Создать 3 графических класса IDrawable с аппаратной отрисовкой осей, трансформацией углов через canvas.Rotate и вычислением интерференции $W\_{femur} \+ W\_{tibia}$.  
> 3. Обеспечить корректную работу гаптики через HapticFeedback.Default.Perform при переходе границы $\\Delta L \= 3.0\\text{ мм}$.  
> 4. Код должен собираться под net10.0-android в режиме Strict Mode (\#nullable enable) без сторонних UI-библиотек.