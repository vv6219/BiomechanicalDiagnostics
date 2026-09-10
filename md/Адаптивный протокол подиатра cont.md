# **Техническое задание и Промпт: Подсистема генерации адаптивного подиатрического протокола и кинематической калибровки клиньев (.NET 10 MAUI / C\# 14\)**

## **1\. Системный контекст и клинические постулаты**

Разработать программный модуль **«Adaptive Podiatric Protocol & Insole Calibration Engine»** для интеграции в медицинскую рабочую станцию ортопеда-подиатра на стеке **.NET 10 (net10.0-android)**, **C\# 14** и **CommunityToolkit.Mvvm**.  
Модуль автоматизирует построение персонализированного протокола ношения, этапной динамической адаптации, теплофизического регламента формовки и расчета компенсаторных элементов (клиньев и пелотов).  
**Клинические постулаты ядра:**

* **Сохранение адаптивной функции стопы:** Подошва стопы — изменчивая адаптационная структура; избыточная толщина клиньев у высокоактивных пациентов провоцирует локальный гиперкератоз (мозоли) и приводит к срыву адаптационных механизмов.  
* **Плотность материала как лечебный фактор:** Распределение нагрузок регулируется комбинацией плотностей пены Ultralon (Formax / ShockStop), разделяя задачи динамического возврата энергии, глубокой чашечной стабилизации и демпфирования пиковых перегрузок.  
* **Рекурсивная этапность адаптации:** Постепенное ступенчатое увеличение экспозиции (часов ношения в сутки) с обязательным контрольным пересмотром на 28–30 день для точечной термокоррекции.

## **2\. Математическая модель динамической адаптации**

### **2.1. Калибровка эффективной толщины клиньев по активности ($\\kappa\_{act}$)**

Толщина клиньев ($T\_{eff}$) рассчитывается от базовой высоты ($T\_{nom}$) с обратной зависимостью от двигательной активности пациента:

$$T\_{eff} \= T\_{nom} \\cdot \\kappa\_{act}$$

* **Малоактивные (Sedentary / Bedrest):** $\\kappa\_{act} \= 1.00$ (компенсация угла недостаточности приближается к $100\\%$).  
* **Умеренная активность (Walking / Office):** $\\kappa\_{act} \= 0.75$.  
* **Высокая активность (Cardio / Heavy Work):** $\\kappa\_{act} \= 0.50$.  
* **Профессиональный спорт (Athlete):** $\\kappa\_{act} \= 0.25$ (минимальная толщина элементов во избежание конфликта с обувью и натирания).

### **2.2. Расчет времени теплофизической релаксации полимера**

Формовка заготовок Formax производится при температуре $85^\\circ\\text{C}$ непосредственно в обуви пациента под статической и динамической нагрузкой. Время предварительного прогрева ($t\_{heat}$) и время первичной полимеризации под нагрузкой ($t\_{cool}$) определяются толщиной заготовки $h$ (мм):

$$t\_{heat} \= 20 \+ 4.5 \\cdot h \\quad (\\text{секунды}), \\quad t\_{cool} \= 120 \\cdot \\left(\\frac{h}{3.5}\\right)^{0.65} \\quad (\\text{секунды})$$

## **3\. Доменные модели (Core/Domain/Protocols/)**

C\#  
namespace OrthoClinic.Core.Domain.Protocols;

using OrthoClinic.Core.Domain;

public enum PatientActivityTier  
{  
    Sedentary,     // Малоактивный образ жизни (до 5 000 шагов/день)  
    Moderate,      // Стандартный городской режим (5 000 \- 10 000 шагов/день)  
    Active,        // Регулярные тренировки / работа на ногах (10 000 \- 15 000 шагов/день)  
    Professional   // Профессиональный спорт / тяжелые осевые нагрузки (\> 15 000 шагов/день)  
}

public enum ClinicalCaseCategory  
{  
    StandardBiomechanical, // Умеренная гиперпронация, плантарный фасциит  
    DynamicAthletic,       // Беговой профиль (Running/Cardio при нормальном BMI)  
    HighAxialCompression, // Повышенный вес (BMI \> 30 или вес \> 95 кг), тяжелый труд  
    SensitiveNeuropathic,  // Диабетическая стопа, истончение жировой подушки, неврома Мортона  
    PediatricGrowth,       // Активный скелетный рост (дети и подростки до 15 лет)  
    ExecutiveNarrowShoe    // Узкая модельная обувь без запаса внутреннего объема  
}

public sealed record AdaptationStage(  
    int StartDay,  
    int EndDay,  
    string StageTitle,  
    string DailyWearHours,  
    string KinematicMode,  
    IReadOnlyList\<string\> ClinicalDirectives,  
    IReadOnlyList\<string\> WarningFlags  
);

public sealed record WedgingPlan(  
    WedgePlacementType Type,  
    double NominalThicknessMm,  
    double CalculatedThicknessMm,  
    string AnatomicalZone,  
    string BiomechanicalObjective  
);

public sealed record MetatarsalCorrectionPlan(  
    string PadSize, // S, M, L  
    string PlacementZone,  
    string DecompressionTarget  
);

public sealed record ComprehensivePodiatricProtocol(  
    ClinicalCaseCategory ClinicalProfile,  
    string RecommendedModelTitle,  
    string FoamDensityDescription,  
    double InsoleThicknessMm,  
    double HeatingTemperatureC, // 85°C  
    int HeatingSeconds,  
    int InShoeMoldingSeconds,  
    IReadOnlyList\<WedgingPlan\> Wedges,  
    MetatarsalCorrectionPlan? MetatarsalPad,  
    IReadOnlyList\<AdaptationStage\> Timeline,  
    int NextFollowUpDay, // 28-30 день  
    string CriticalClinicalAdvice  
);

## **4\. Движок генерации адаптивных протоколов (Engine/AdaptiveProtocolEngine.cs)**

C\#  
namespace OrthoClinic.Core.Engine;

using OrthoClinic.Core.Domain;  
using OrthoClinic.Core.Domain.Protocols;

public interface IAdaptiveProtocolEngine  
{  
    ComprehensivePodiatricProtocol GenerateProtocol(  
        HorizontalInputData clinicalData,   
        PatientActivityTier activityTier,  
        double patientWeightKg,  
        double patientBmi,  
        int patientAge,  
        bool hasMortonOrNeuropathy,  
        bool isNarrowFootwear  
    );  
}

public sealed class AdaptiveProtocolEngine : IAdaptiveProtocolEngine  
{  
    private const double StandardMoldingTemp \= 85.0; // Термоформовка при 85°C

    public ComprehensivePodiatricProtocol GenerateProtocol(  
        HorizontalInputData clinicalData,   
        PatientActivityTier activityTier,  
        double patientWeightKg,  
        double patientBmi,  
        int patientAge,  
        bool hasMortonOrNeuropathy,  
        bool isNarrowFootwear)  
    {  
        // 1\. Определение клинической категории  
        ClinicalCaseCategory category;  
        string modelTitle;  
        string densityDesc;  
        double thicknessMm;

        if (patientAge \< 15)  
        {  
            category \= ClinicalCaseCategory.PediatricGrowth;  
            modelTitle \= "Formthotics Junior Red";  
            densityDesc \= "Junior Formax (Single Soft/Medium)";  
            thicknessMm \= 3.0; // Толщина детской серии  
        }  
        else if (hasMortonOrNeuropathy || clinicalData.TibialStatus \== TibialTorsion.UnderRotationInward && patientBmi \< 20)  
        {  
            category \= ClinicalCaseCategory.SensitiveNeuropathic;  
            modelTitle \= "Formthotics ShockStop Red/Green";  
            densityDesc \= "Hybrid ShockStop \+ Red Soft Formax (демпфирование до 40% пиковых ударов)";  
            thicknessMm \= 5.0;  
        }  
        else if (patientBmi \>= 30.0 || patientWeightKg \>= 95.0)  
        {  
            category \= ClinicalCaseCategory.HighAxialCompression;  
            modelTitle \= "Formthotics Hard Black";  
            densityDesc \= "Firm Formax (High-Density) высокой жесткости";  
            thicknessMm \= 4.0;  
        }  
        else if (isNarrowFootwear)  
        {  
            category \= ClinicalCaseCategory.ExecutiveNarrowShoe;  
            modelTitle \= "Formthotics Low Volume / 3/4 Beige";  
            densityDesc \= "Single Soft/Medium Low-Profile Formax";  
            thicknessMm \= 2.0; // Ультратонкая пена для модельной обуви  
        }  
        else if (activityTier is PatientActivityTier.Active or PatientActivityTier.Professional && patientBmi \< 25.0)  
        {  
            category \= ClinicalCaseCategory.DynamicAthletic;  
            modelTitle \= "Formthotics Original Single Blue";  
            densityDesc \= "Single Medium Formax (динамический возврат энергии)";  
            thicknessMm \= 3.5;  
        }  
        else  
        {  
            category \= ClinicalCaseCategory.StandardBiomechanical;  
            modelTitle \= "Formthotics Dual-Density Red/Blue";  
            densityDesc \= "Dual-Density: Soft Red (верх) \+ Medium Blue (базис)";  
            thicknessMm \= 4.5;  
        }

        // 2\. Расчет коэффициента толщины по активности  
        double activityCoeff \= activityTier switch  
        {  
            PatientActivityTier.Sedentary \=\> 1.00,    // 100% компенсации угла недостаточности  
            PatientActivityTier.Moderate \=\> 0.75,  
            PatientActivityTier.Active \=\> 0.50,  
            PatientActivityTier.Professional \=\> 0.25, // Минимальная толщина во избежание мозолей  
            \_ \=\> 0.75  
        };

        // 3\. Расчет клиньев и пелотов  
        var wedges \= new List\<WedgingPlan\>();

        if (clinicalData.TibialStatus \== TibialTorsion.OverRotationOutward)  
        {  
            wedges.Add(new WedgingPlan(  
                Type: WedgePlacementType.AnteriorMedial,  
                NominalThicknessMm: 4.0,  
                CalculatedThicknessMm: Math.Round(4.0 \* activityCoeff, 1),  
                AnatomicalZone: "Передний медиальный край",  
                BiomechanicalObjective: "Устранение варуса переднего отдела стопы и разгрузка 1-го луча"  
            ));  
        }  
        else if (clinicalData.IsAntetorsion || clinicalData.PiriformisHypertonus)  
        {  
            wedges.Add(new WedgingPlan(  
                Type: WedgePlacementType.AnteriorLateral,  
                NominalThicknessMm: 3.5,  
                CalculatedThicknessMm: Math.Round(3.5 \* activityCoeff, 1),  
                AnatomicalZone: "Передний латеральный сектор",  
                BiomechanicalObjective: "Компенсация внутренней ротации и стабилизация свода"  
            ));  
        }

        MetatarsalCorrectionPlan? metaPad \= null;  
        if (hasMortonOrNeuropathy || clinicalData.TibialStatus \== TibialTorsion.OverRotationOutward)  
        {  
            string size \= patientWeightKg \> 80 ? "L" : (patientWeightKg \< 60 ? "S" : "M");  
            metaPad \= new MetatarsalCorrectionPlan(  
                PadSize: size,  
                PlacementZone: "Ретрокапитальная зона (позади головок II–IV плюсневых костей)",  
                DecompressionTarget: "Подъем поперечного свода, расширение межплюсневых промежутков и декомпрессия нервов"  
            );  
        }

        // 4\. Построение дорожной карты адаптации  
        var timeline \= new List\<AdaptationStage\>  
        {  
            new(  
                StartDay: 1,   
                EndDay: 4,   
                StageTitle: "Этап первичной нейросенсорной адаптации",   
                DailyWearHours: "2–3 часа в день в спокойном темпе ходьбы",   
                KinematicMode: "Исключить бег, прыжки и спортивные нагрузки",  
                ClinicalDirectives: new\[\] { "Формовка при 85°C в обуви", "Ношение только в базовой обуви со съемной стелькой" },  
                WarningFlags: new\[\] { "При возникновении резких болей снять стельки до следующего дня" }  
            ),  
            new(  
                StartDay: 5,   
                EndDay: 14,   
                StageTitle: "Этап динамической интеграции",   
                DailyWearHours: "Полный рабочий день (6–8+ часов)",   
                KinematicMode: "Повседневная ходьба, допуск к легким аэробным нагрузкам",  
                ClinicalDirectives: new\[\] { "Контроль состояния кожных покровов сводов", "Оценка адаптации продольного свода" },  
                WarningFlags: new\[\] { "Образование локальных покраснений указывает на необходимость снижения активности" }  
            ),  
            new(  
                StartDay: 15,   
                EndDay: 28,   
                StageTitle: "Этап стабилизации кинематического стереотипа",   
                DailyWearHours: "Постоянное ношение без ограничений",   
                KinematicMode: "Полный спортивный и рабочий режим",  
                ClinicalDirectives: new\[\] { "Закрепление правильной биомеханики переката", "Подготовка к контрольному осмотру" },  
                WarningFlags: new\[\] { "Оценка зон максимального износа пены" }  
            )  
        };

        // 5\. Теплофизические параметры  
        int heatSec \= (int)Math.Round(20.0 \+ 4.5 \* thicknessMm);  
        int coolSec \= (int)Math.Round(120.0 \* Math.Pow(thicknessMm / 3.5, 0.65));

        return new ComprehensivePodiatricProtocol(  
            ClinicalProfile: category,  
            RecommendedModelTitle: modelTitle,  
            FoamDensityDescription: densityDesc,  
            InsoleThicknessMm: thicknessMm,  
            HeatingTemperatureC: StandardMoldingTemp,  
            HeatingSeconds: heatSec,  
            InShoeMoldingSeconds: coolSec,  
            Wedges: wedges,  
            MetatarsalPad: metaPad,  
            Timeline: timeline,  
            NextFollowUpDay: 28, // Контрольный осмотр через 28-30 дней  
            CriticalClinicalAdvice: activityTier \== PatientActivityTier.Professional || activityTier \== PatientActivityTier.Active  
                ? "Внимание: Высокая физическая активность. Толщина клиньев минимальна во избежание срыва тканевой адаптации и мозолей."  
                : "Стандартный протокол: Обеспечена полная статическая поддержка сводов стопы."  
        );  
    }  
}

## **5\. UI/UX: Интерактивный дашборд протокола (UI/Views/AdaptiveProtocolView.xaml)**

Разработать интерактивную карточку отображения адаптивного протокола, включающую:

> 1. **Динамический бейдж профиля:** Цветовое кодирование категории (Зеленый \= ShockStop, Синий \= Dynamic, Красный/Синий \= Dual-Density, Черный \= High-Density).  
> 2. **Интерактивный таймлайн адаптации:** Отображение прогресса адаптации по дням с графическим индикатором часов ношения (2–3 ч $\\rightarrow$ Полный день).  
> 3. **Блок калибровки клиньев:** Визуализация соотношения номинальной и эффективной толщины клина с привязкой к ползунку активности пациента.  
> 4. **Регламент термоформовки:** Карточка с параметрами нагревателя ($85^\\circ\\text{C}$ / время в секундах / динамическое остывание).

XML  
\<?xml version="1.0" encoding="utf-8" ?\>  
\<ContentView xmlns\="http://schemas.microsoft.com/dotnet/2021/maui"  
             xmlns:x\="http://schemas.microsoft.com/winfx/2009/xaml"  
             x:Class\="OrthoClinic.UI.Views.AdaptiveProtocolView"  
             BackgroundColor\="\#0F172A"\>

    \<ScrollView\>  
        \<VerticalStackLayout Spacing\="14" Padding\="14"\>

            \<\!-- КАРТОЧКА НАЗНАЧЕННОЙ ПЕНЫ \--\>  
            \<Border BackgroundColor\="\#1E293B" Padding\="14" Stroke\="\#334155" StrokeShape\="RoundRectangle 14"\>  
                \<VerticalStackLayout Spacing\="6"\>  
                    \<HorizontalStackLayout Spacing\="8"\>  
                        \<Label Text\="НАЗНАЧЕНИЕ:" TextColor\="\#94A3B8" FontSize\="11" FontAttributes\="Bold"/\>  
                        \<Label Text\="{Binding Protocol.ClinicalProfile}" TextColor\="\#38BDF8" FontSize\="11" FontAttributes\="Bold"/\>  
                    \</HorizontalStackLayout\>  
                    \<Label Text\="{Binding Protocol.RecommendedModelTitle}" TextColor\="White" FontSize\="18" FontAttributes\="Bold"/\>  
                    \<Label Text\="{Binding Protocol.FoamDensityDescription}" TextColor\="\#CBD5E1" FontSize\="12"/\>  
                    \<Label Text\="{Binding Protocol.CriticalClinicalAdvice}" TextColor\="\#FBBF24" FontSize\="11" Margin\="0,4,0,0"/\>  
                \</VerticalStackLayout\>  
            \</Border\>

            \<\!-- ТЕПЛОФИЗИЧЕСКИЙ РЕГЛАМЕНТ 85°C \--\>  
            \<Border BackgroundColor\="\#1E293B" Padding\="12" Stroke\="\#334155" StrokeShape\="RoundRectangle 12"\>  
                \<Grid ColumnDefinitions\="\*,\*,\*" RowDefinitions\="Auto,Auto"\>  
                    \<VerticalStackLayout Grid.Column\="0"\>  
                        \<Label Text\="Температура" TextColor\="\#94A3B8" FontSize\="10"/\>  
                        \<Label Text\="{Binding Protocol.HeatingTemperatureC, StringFormat='{0:F0}°C'}" TextColor\="\#EF4444" FontAttributes\="Bold" FontSize\="16"/\>  
                    \</VerticalStackLayout\>  
                    \<VerticalStackLayout Grid.Column\="1"\>  
                        \<Label Text\="Экспозиция фена" TextColor\="\#94A3B8" FontSize\="10"/\>  
                        \<Label Text\="{Binding Protocol.HeatingSeconds, StringFormat='{0} сек'}" TextColor\="\#F59E0B" FontAttributes\="Bold" FontSize\="16"/\>  
                    \</VerticalStackLayout\>  
                    \<VerticalStackLayout Grid.Column\="2"\>  
                        \<Label Text\="Формовка в обуви" TextColor\="\#94A3B8" FontSize\="10"/\>  
                        \<Label Text\="{Binding Protocol.InShoeMoldingSeconds, StringFormat='{0} сек'}" TextColor\="\#10B981" FontAttributes\="Bold" FontSize\="16"/\>  
                    \</VerticalStackLayout\>  
                \</Grid\>  
            \</Border\>

            \<\!-- ТАЙМЛАЙН АДАПТАЦИИ (ДНИ 1-4, 5-14, 28\) \--\>  
            \<Label Text\="Этапный протокол адаптации стопы:" TextColor\="\#38BDF8" FontAttributes\="Bold" FontSize\="13"/\>  
            \<CollectionView ItemsSource\="{Binding Protocol.Timeline}"\>  
                \<CollectionView.ItemTemplate\>  
                    \<DataTemplate\>  
                        \<Border BackgroundColor\="\#1E293B" Margin\="0,4" Padding\="12" StrokeShape\="RoundRectangle 10"\>  
                            \<VerticalStackLayout Spacing\="4"\>  
                                \<Grid ColumnDefinitions\="\*,Auto"\>  
                                    \<Label Grid.Column\="0" Text\="{Binding StageTitle}" TextColor\="White" FontAttributes\="Bold" FontSize\="12"/\>  
                                    \<Label Grid.Column\="1" Text\="{Binding DailyWearHours}" TextColor\="\#10B981" FontAttributes\="Bold" FontSize\="11"/\>  
                                \</Grid\>  
                                \<Label Text\="{Binding KinematicMode, StringFormat='Режим: {0}'}" TextColor\="\#94A3B8" FontSize\="11"/\>  
                            \</VerticalStackLayout\>  
                        \</Border\>  
                    \</DataTemplate\>  
                \</CollectionView.ItemTemplate\>  
            \</CollectionView\>

            \<\!-- БЛОК КОРРЕКТОРОВ И ПЕЛОТОВ \--\>  
            \<Border IsVisible\="{Binding Protocol.MetatarsalPad, Converter={StaticResource NotNullConverter}}"   
                    BackgroundColor\="\#1E293B" Padding\="12" StrokeShape\="RoundRectangle 10"\>  
                \<VerticalStackLayout Spacing\="4"\>  
                    \<Label Text\="Метатарзальный пелот (Капля):" TextColor\="\#38BDF8" FontAttributes\="Bold" FontSize\="12"/\>  
                    \<Label Text\="{Binding Protocol.MetatarsalPad.PlacementZone}" TextColor\="\#E2E8F0" FontSize\="11"/\>  
                    \<Label Text\="{Binding Protocol.MetatarsalPad.PadSize, StringFormat='Размер: {0}'}" TextColor\="\#10B981" FontAttributes\="Bold" FontSize\="12"/\>  
                \</VerticalStackLayout\>  
            \</Border\>

            \<\!-- КОНТРОЛЬНЫЙ ОСМОТР \--\>  
            \<Border BackgroundColor\="\#0F172A" Stroke\="\#38BDF8" Padding\="12" StrokeShape\="RoundRectangle 10"\>  
                \<Grid ColumnDefinitions\="\*,Auto"\>  
                    \<VerticalStackLayout Grid.Column\="0"\>  
                        \<Label Text\="Контрольный осмотр и термокоррекция:" TextColor\="White" FontAttributes\="Bold" FontSize\="12"/\>  
                        \<Label Text\="Оценка зон износа пены, доустановка клиньев" TextColor\="\#94A3B8" FontSize\="10"/\>  
                    \</VerticalStackLayout\>  
                    \<Label Grid.Column\="1" Text\="{Binding Protocol.NextFollowUpDay, StringFormat='Через {0} дн.'}"   
                           TextColor\="\#38BDF8" FontAttributes\="Bold" VerticalOptions\="Center"/\>  
                \</Grid\>  
            \</Border\>

        \</VerticalStackLayout\>  
    \</ScrollView\>  
\</ContentView\>

## **6\. Требования к генерации кода**

> 1. Реализовать доменные классы и перечисления из раздела 3 в проекте .NET 10 MAUI.  
> 2. Реализовать класс AdaptiveProtocolEngine с полной математической разметкой поправок на активность пациента и теплофизических констант.  
> 3. Сгенерировать ViewModel с поддержкой реактивного обновления протокола при смещении ползунка активности пациента.  
> 4. Обеспечить строгую типизацию (\#nullable enable) и совместимость со сборкой в AOT-режиме без использования динамической рефлексии.