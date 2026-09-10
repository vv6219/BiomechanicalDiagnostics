namespace OrthoClinic.Core.Engine;

using OrthoClinic.Core.Domain;

/// <summary>
/// Клинический детерминированный алгоритмический движок сопоставления
/// результатов горизонтального тестирования и адаптивного подбора ортопедических клиньев
/// с динамической системой выявления алертов, противопоказаний и вычисления Kinetic Risk Score.
/// </summary>
public sealed class HorizontalAssessmentEngine : IHorizontalAssessmentEngine
{
    public const double MaxSafeSegmentCorrectionMm = 6.0;

    public HorizontalDiagnosticReport Evaluate(HorizontalInputData input)
    {
        var alerts = new List<string>();
        var dynamicAlerts = new List<ClinicalAlertItem>();
        var rawPrescriptions = new List<WedgeItemPrescription>();

        double deltaMm = Math.Abs(input.LeftMalleolusMm - input.RightMalleolusMm);
        bool needsOsteopathy = false;

        // Коэффициент толщины по уровню метаболической и кинематической активности пациента
        double activityCoeff = input.Activity switch
        {
            PatientActivityLevel.Sedentary => 1.00,
            PatientActivityLevel.Moderate => 0.75,
            PatientActivityLevel.Active => 0.50,
            PatientActivityLevel.Athlete => 0.25,
            _ => 0.75
        };

        // Рекомендуемая твердость материала по Шору А
        var durometer = input.Activity switch
        {
            PatientActivityLevel.Sedentary => MaterialDurometerShoreA.ShoreA35_Soft,
            PatientActivityLevel.Moderate => MaterialDurometerShoreA.ShoreA45_Medium,
            PatientActivityLevel.Active => MaterialDurometerShoreA.ShoreA55_Firm,
            PatientActivityLevel.Athlete => MaterialDurometerShoreA.ShoreA65_RigidComposite,
            _ => MaterialDurometerShoreA.ShoreA45_Medium
        };

        // =========================================================================
        // ТЕСТ 1: ОЦЕНКА ДЛИНЫ НОГ И ОСТЕОПАТИЧЕСКИЙ БАРЬЕР
        // =========================================================================
        if (deltaMm > 3.0)
        {
            needsOsteopathy = true;
            alerts.Add("Показано применение дополнительных остеопатических тестов.");

            dynamicAlerts.Add(new ClinicalAlertItem(
                AlertCode: "ALERT_OSTEOPATHIC_BARRIER",
                Severity: AlertSeverity.WarningThreshold,
                AffectedSegment: AnatomicalSegment.Pelvis_Sacrum,
                Title: "Превышение остеопатического порога асимметрии",
                BiomechanicalRationale: $"Измеренная асимметрия длины конечностей ΔL = {deltaMm:F1} мм превышает допустимый физиологический порог 3.0 мм. Риск восходящей постуральной декомпенсации.",
                AdaptiveActionPlan: "1. Мануальный протокол: Выполнить мобилизационный маневр Вебера-Барстоу (сгибание коленей с разгрузочным мостиком таза). Пальпаторно верифицировать симметрию передних (ASIS) и задних (PSIS) верхних подвздошных остей, гребней подвздошных костей и медиальных лодыжек.\n" +
                                    "2. Ортопедический регламент: До верификации анатомического или функционального генеза асимметрии отложить назначение постоянного подпяточника. Применить нейтральную тестовую стельку без асимметричных компенсаторов.\n" +
                                    "3. Двигательный протокол: Мягкая мобилизация пояснично-крестцового сочленения на валике, устранение функционального блока крестцово-подвздошного сочленения (КПС).\n" +
                                    "4. Контроль: Повторное измерение длины нижних конечностей в положении стоя под лазерным горизонтом через 7–10 дней."
            ));

            if (input.DiscrepancyNature == LegDiscrepancyType.FunctionalPelvic)
            {
                alerts.Add("Выявлен функциональный перекос таза. Коррекция подпяточником противопоказана во избежание фиксации дисфункции.");

                dynamicAlerts.Add(new ClinicalAlertItem(
                    AlertCode: "CONTRAINDICATION_HEEL_LIFT_FUNCTIONAL_PELVIS",
                    Severity: AlertSeverity.CriticalContraindication,
                    AffectedSegment: AnatomicalSegment.Pelvis_Sacrum,
                    Title: "Абсолютное противопоказание: Подпяточник при функциональном перекосе",
                    BiomechanicalRationale: "Выявлен функциональный (мышечно-связочный) перекос таза. Установка жесткого подпяточника заблокирует крестцово-подвздошные суставы (КПС) в патологической торсии и вызовет нисходящий дуговой сколиоз позвоночника.",
                    AdaptiveActionPlan: "1. Экспертный запрет: НАЗНАЧЕНИЕ ПОДПЯТОЧНИКА КАТЕГОРИЧЕСКИ ЗАПРЕЩЕНО! Установка клина зафиксирует патологическую ротацию подвздошной кости и вызовет вторичную люмбалгию.\n" +
                                        "2. Ортопедический регламент: Применить симметричную термоформуемую стельку Formthotics с нулевым компенсатором под пятку для гармонизации проприоцепции стопы без вмешательства в длину конечностей.\n" +
                                        "3. Реабилитационный протокол: Направить пациента к остеопату или мануальному терапевту для деротации таза и миофасциального релиза квадратной мышцы поясницы и подвздошно-поясничной мышцы (тест Томаса). Назначить комплекс постизометрической релаксации (ПИР).\n" +
                                        "4. Динамический контроль: Обязательный повторный прием через 14 дней. Повторное тестирование горизонтали таза после остеопатической коррекции.",
                    IsPrescriptionBlocked: true
                ));
            }
            else if (input.DiscrepancyNature == LegDiscrepancyType.TrueAnatomical)
            {
                double nominalHeel = Math.Round(deltaMm / 2.0, 1, MidpointRounding.AwayFromZero);
                double heelThickness = Math.Round(nominalHeel * activityCoeff, 1, MidpointRounding.AwayFromZero);

                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.HeelLiftCompensator,
                    EffectiveThicknessMm: heelThickness,
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: $"Истинное анатомическое укорочение ({deltaMm:F1} мм). Частичная разгрузочная компенсация.",
                    ControlTestMandate: "Оценка горизонтали крестца.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 5,
                    NominalThicknessMm: nominalHeel,
                    TitleRu: "Подпяточник-компенсатор длины (разгрузочный лифт)",
                    AnatomicalZone: "Пяточная чаша / задний отдел стопы",
                    ForceVectorRationale: "Вертикальный постуральный подъем пяточной кости, нивелирование косого наклона крестца, разгрузка подвздошно-поясничной мышцы",
                    InstallationProtocol: "Устанавливать строго под пяточную чашу стельки Formthotics с плавным схождением в 0 мм к границе кубовидной кости",
                    MapColorHex: "#EF4444"
                ));

                if (deltaMm > 8.0)
                {
                    dynamicAlerts.Add(new ClinicalAlertItem(
                        AlertCode: "ALERT_SEVERE_ANATOMICAL_SHORTENING",
                        Severity: AlertSeverity.WarningThreshold,
                        AffectedSegment: AnatomicalSegment.Pelvis_Sacrum,
                        Title: "Значительное анатомическое укорочение (> 8 мм)",
                        BiomechanicalRationale: $"Выраженная костная асимметрия ΔL = {deltaMm:F1} мм. Единовременная полная коррекция вызовет миофасциальный спазм квадратной мышцы поясницы и выраженный болевой синдром.",
                        AdaptiveActionPlan: $"1. Клинический регламент: Категорически исключить одномоментную полную компенсацию дефицита {deltaMm:F1} мм во избежание острого корешкового синдрома и срыва постуральной компенсации.\n" +
                                            $"2. Ортопедический регламент: Ступенчатый протокол адаптации (Step-Up): стартовая высота подпяточника установлена на {heelThickness:F1} мм (с учетом спортивно-двигательной редукции). Материал: плотный износостойкий EVA высокой упругости.\n" +
                                            "3. Домашний протокол: Адаптационный режим ношения «2+1» часа в день, регулярная растяжка икроножной мышцы и ахиллова сухожилия на стороне укорочения.\n" +
                                            "4. План визитов: Контрольный осмотр через 3–4 недели: при отсутствии мышечных болей в пояснице наращивание высоты подпяточника порциями по +1.5...2.0 мм до целевого физиологического баланса таза."
                    ));
                }
                else
                {
                    dynamicAlerts.Add(new ClinicalAlertItem(
                        AlertCode: "ADVISORY_ANATOMICAL_HEEL_LIFT",
                        Severity: AlertSeverity.ClinicalAdvisory,
                        AffectedSegment: AnatomicalSegment.Pelvis_Sacrum,
                        Title: "Анатомическая компенсация подпяточником",
                        BiomechanicalRationale: $"Расчет компенсатора выполнен по правилу (ΔL / 2) · κ_activity = {heelThickness:F1} мм для разгрузки пояснично-крестцового сочленения.",
                        AdaptiveActionPlan: $"1. Клинический регламент: Размещение компенсирующего подпяточника {heelThickness:F1} мм под анатомически укороченную конечность. Пальпаторный и оптический контроль выравнивания гребней подвздошных костей в положении стоя под осевой нагрузкой.\n" +
                                            "2. Технический регламент: Подпяточник фиксируется под пяточную чашу стельки с плавным схождением к 0 мм."
                    ));
                }
            }
        }

        // =========================================================================
        // ТЕСТ 2: ТЕСТ РОТАТОРОВ (ГРУШЕВИДНЫЕ МЫШЦЫ)
        // =========================================================================
        if (input.PiriformisHypertonus)
        {
            rawPrescriptions.Add(new WedgeItemPrescription(
                Placement: WedgePlacementType.AnteriorLateral,
                EffectiveThicknessMm: Math.Round(3.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                CompensationPercentage: activityCoeff * 100,
                BiomechanicalReason: "Гипертонус грушевидных мышц, натяжение тканей и наружный разворот стоп.",
                ControlTestMandate: "Повторная пассивная ротация внутрь на кушетке.",
                RecommendedDurometer: durometer,
                InsoleQuadrant: 2,
                NominalThicknessMm: 3.0,
                TitleRu: "Передний латеральный клин (деротатор грушевидной)",
                AnatomicalZone: "Головки III–V плюсневых костей",
                ForceVectorRationale: "Супинационный момент переднего отдела стопы, рефлекторная внутренняя деротация бедра при толчке, миофасциальная разгрузка грушевидной мышцы",
                InstallationProtocol: "Установка под наружный край носочной части стельки со скосом под 45° кнаружи",
                MapColorHex: "#38BDF8"
            ));

            dynamicAlerts.Add(new ClinicalAlertItem(
                AlertCode: "ALERT_PIRIFORMIS_HYPERTONUS",
                Severity: AlertSeverity.ClinicalAdvisory,
                AffectedSegment: AnatomicalSegment.Femur_Hip,
                Title: "Миофасциальный гипертонус грушевидной мышцы",
                BiomechanicalRationale: "Спазм m. piriformis формирует стойкую наружную ротацию бедра, компремирует седалищный нерв и нарушает толчковую фазу шага.",
                AdaptiveActionPlan: $"1. Клинический регламент: Провести пальпацию выхода седалищного нерва под грушевидной мышцей и тест Бонне-Бобровниковой (приведение бедра с внутренней ротацией на кушетке) для исключения компрессионной нейропатии.\n" +
                                    $"2. Ортопедический регламент: Установка переднего латерального клина ({Math.Round(3.0 * activityCoeff, 1):F1} мм) в проекции головок III–V плюсневых костей. Клин создает супинационный рычаг передней части стопы, стимулируя рефлекторную внутреннюю деротацию бедра при толчке и расслабляя m. piriformis.\n" +
                                    "3. Двигательный протокол: Назначить курс постизометрической релаксации (ПИР) грушевидной мышцы, миофасциальный релиз ягодичной зоны массажным мячом и аппликацию кинезиотейпа по ходу волокон m. piriformis с натяжением 15–20%.\n" +
                                    "4. Контроль: Оценка амплитуды внутренней ротации бедра и динамики походки через 14 дней."
            ));
        }

        // =========================================================================
        // ТЕСТ 3: ИЗОЛИРОВАННАЯ РОТАЦИЯ БЕДРА И АНАТОМИЧЕСКИЕ БАРЬЕРЫ
        // =========================================================================
        if (input.IsAntetorsion)
        {
            if (input.HipJointBarrier == RotationBarrier.BoneAnatomical)
            {
                dynamicAlerts.Add(new ClinicalAlertItem(
                    AlertCode: "ALERT_FEMORAL_BONY_BARRIER",
                    Severity: AlertSeverity.WarningThreshold,
                    AffectedSegment: AnatomicalSegment.Femur_Hip,
                    Title: "Анатомический костный барьер ротации шейки бедра",
                    BiomechanicalRationale: "Выявлено истинное костное анатомическое ограничение наружной ротации. Попытка силовой деротации стелькой вызовет деструктивный крутящий момент в коленном суставе.",
                    AdaptiveActionPlan: "1. Клинический регламент: ПАЛЛИАТИВНЫЙ ОРТОПЕДИЧЕСКИЙ РЕЖИМ. Силовая механическая деротация бедра стелькой абсолютно противопоказана: жесткое сопротивление костного барьера сместит крутящий момент в коленный сустав с риском повреждения менисков и связочного аппарата.\n" +
                                        "2. Ортопедический регламент: Использовать эластичные ортезы Formthotics одинарной или комфортной двойной плотности. Предельная высота клиньев ограничена 2.0–2.5 мм; строгий запрет на жесткие негнущиеся карбоновые основания.\n" +
                                        "3. Двигательный протокол: Исключить агрессивные упражнения на принудительное раскрытие тазобедренного сустава (растяжка «бабочка» с отягощением). Акцент на укрепление стабилизаторов колена и мягкую мобилизацию ТБС в физиологической амплитуде.\n" +
                                        "4. Контроль: Мониторинг комфорта и отсутствия боли в коленном суставе через 14 и 30 дней."
                ));
            }

            if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsInwardTest5))
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.PosteriorMedial,
                    EffectiveThicknessMm: Math.Round(4.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Антеторсия шейки бедра с выраженным падением стопы внутрь в глобальном тесте.",
                    ControlTestMandate: "Строго через глобальный двигательный тест!",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 3,
                    NominalThicknessMm: 4.0,
                    TitleRu: "Задний медиальный клин (поддержка sustentaculum tali)",
                    AnatomicalZone: "Медиальный отдел пяточной чаши / подтаранный сустав",
                    ForceVectorRationale: "Опора шейки таранной кости, блокировка пронационного завала заднего отдела стопы, предотвращение вторичного вальгуса колена",
                    InstallationProtocol: "Монтаж в медиальную часть пяточной чаши стельки Formthotics с плавным заходом на внутренний продольный свод",
                    MapColorHex: "#10B981"
                ));

                dynamicAlerts.Add(new ClinicalAlertItem(
                    AlertCode: "ALERT_MEDIAL_COLLAPSE_HALLUX_VALGUS",
                    Severity: AlertSeverity.WarningThreshold,
                    AffectedSegment: AnatomicalSegment.GlobalKineticChain,
                    Title: "Медиальный коллапс кинематической цепи",
                    BiomechanicalRationale: "Синхронный завал бедра и стопы внутрь формирует каскадный вальгус нижней конечности, перегрузку медиального мениска и провоцирует Hallux Valgus.",
                    AdaptiveActionPlan: $"1. Клинический регламент: Выполнить функциональный тест «Single Leg Squat» (приседание на одной ноге) для документирования динамического вальгуса колена и пронационного свала таранной кости.\n" +
                                        $"2. Ортопедический регламент: Назначить форсированный задний медиальный клин ({Math.Round(4.0 * activityCoeff, 1):F1} мм) строго под sustentaculum tali (опору таранной кости) с плавным переходом на супинатор ладьевидной кости. Глубокая формовка пяточного гнезда Formthotics с высоким медиальным бортом для фиксации пяты от эверсии.\n" +
                                        "3. Двигательный протокол: Укрепление m. tibialis posterior («короткая стопа», сминание полотенца стопой), активация средней ягодичной мышцы (упражнение «ракушка» с ленточным амортизатором), супинирующее кинезиотейпирование продольного свода.\n" +
                                        "4. Контроль: Оценка одноопорной устойчивости и редукции вальгуса колена через 14 дней."
                ));
            }
            else
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.AnteriorLateral,
                    EffectiveThicknessMm: Math.Round(3.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Антеторсия шейки бедра (костный барьер наружной ротации, компенсаторный вальгус шага).",
                    ControlTestMandate: "Дифференциация барьера вращения шейки бедра.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 2,
                    NominalThicknessMm: 3.0,
                    TitleRu: "Передний латеральный клин (наружный супинатор)",
                    AnatomicalZone: "Головки III–V плюсневых костей",
                    ForceVectorRationale: "Супинационный момент переднего отдела стопы, стимуляция внутренней деротации конечности, снятие натяжения с глубоких ротаторов бедра",
                    InstallationProtocol: "Фиксация под подошвенной стороной стельки в проекции головок 3–5 плюсневых костей со скосом под 45° кнаружи",
                    MapColorHex: "#38BDF8"
                ));
            }
        }
        else if (input.IsRetrotorsion)
        {
            rawPrescriptions.Add(new WedgeItemPrescription(
                Placement: WedgePlacementType.AnteriorLateral,
                EffectiveThicknessMm: Math.Round(2.5 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                CompensationPercentage: activityCoeff * 100,
                BiomechanicalReason: "Ретроторсия бедра с патологической наружной установкой нижней конечности.",
                ControlTestMandate: "Тест ротаторов бедра.",
                RecommendedDurometer: durometer,
                InsoleQuadrant: 2,
                NominalThicknessMm: 2.5,
                TitleRu: "Передний латеральный клин (деротатор ретроторсии)",
                AnatomicalZone: "Головки III–V плюсневых костей",
                ForceVectorRationale: "Создание супинационного рычага переднего отдела стопы для компенсации патологической наружной установки нижней конечности",
                InstallationProtocol: "Фиксация под подошвенной стороной стельки в проекции головок 3–5 плюсневых костей со скосом под 45° кнаружи",
                MapColorHex: "#38BDF8"
            ));

            dynamicAlerts.Add(new ClinicalAlertItem(
                AlertCode: "ALERT_FEMORAL_RETROTORSION",
                Severity: AlertSeverity.ClinicalAdvisory,
                AffectedSegment: AnatomicalSegment.Femur_Hip,
                Title: "Ретроторсия шейки бедра (наружная установка)",
                BiomechanicalRationale: "Патологическая наружная торсия бедра снижает рессорные свойства продольного свода и перегружает латеральный столб стопы.",
                AdaptiveActionPlan: $"1. Клинический регламент: Оценить угол атаки и траекторию переката стопы при динамической походке. Пропальпировать наружный край стопы и кубовидную кость для исключения латерального импиджмент-синдрома.\n" +
                                    $"2. Ортопедический регламент: Установить передний латеральный деротационный клин ({Math.Round(2.5 * activityCoeff, 1):F1} мм) под головки III–V плюсневых костей со скосом кнаружи для создания супинационного рычага переднего отдела и нормализации фазы толчка через I луч.\n" +
                                    "3. Обувной протокол: Рекомендовать обувь с жестким формованным задником и широкой подошвой с торсионным мостом, препятствующей супинационному подворачиванию каблука.\n" +
                                    "4. Контроль: Анализ картины износа протектора обуви и повторная оценка паттерна шага через 30 дней."
            ));
        }

        // =========================================================================
        // ТЕСТ 4: ТОРСИЯ БОЛЬШЕБЕРЦОВОЙ КОСТИ
        // =========================================================================
        if (input.TibialStatus == TibialTorsion.UnderRotationInward)
        {
            if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsInwardTest5))
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.PosteriorMedial,
                    EffectiveThicknessMm: Math.Round(4.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Внутренний недокрут большеберцовой кости с завалом стопы внутрь.",
                    ControlTestMandate: "Контроль по мануальному мышечному тесту подколенной мышцы.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 3,
                    NominalThicknessMm: 4.0,
                    TitleRu: "Задний медиальный клин (поддержка sustentaculum tali)",
                    AnatomicalZone: "Медиальный отдел пяточной чаши / подтаранный сустав",
                    ForceVectorRationale: "Опора шейки таранной кости, блокировка пронационного завала заднего отдела стопы, предотвращение вторичного вальгуса колена",
                    InstallationProtocol: "Монтаж в медиальную часть пяточной чаши стельки Formthotics с плавным заходом на внутренний продольный свод",
                    MapColorHex: "#10B981"
                ));
            }
            else
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.AnteriorLateral,
                    EffectiveThicknessMm: Math.Round(3.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Недокрут большеберцовой кости внутрь (провокация вальгусной деформации).",
                    ControlTestMandate: "Визуальный контроль оси голень-стопа.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 2,
                    NominalThicknessMm: 3.0,
                    TitleRu: "Передний латеральный клин (наружный супинатор)",
                    AnatomicalZone: "Головки III–V плюсневых костей",
                    ForceVectorRationale: "Супинационный момент переднего отдела стопы, стимуляция внутренней деротации конечности",
                    InstallationProtocol: "Фиксация под подошвенной стороной стельки в проекции головок 3–5 плюсневых костей со скосом под 45° кнаружи",
                    MapColorHex: "#38BDF8"
                ));
            }
        }
        else if (input.TibialStatus == TibialTorsion.OverRotationOutward)
        {
            rawPrescriptions.Add(new WedgeItemPrescription(
                Placement: WedgePlacementType.AnteriorMedial,
                EffectiveThicknessMm: Math.Round(3.5 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                CompensationPercentage: activityCoeff * 100,
                BiomechanicalReason: "Перекрут большеберцовой кости кнаружи, варус переднего отдела стопы, риск Hallux Valgus.",
                ControlTestMandate: "Оценка параллельности оси сгибания коленного сустава.",
                RecommendedDurometer: durometer,
                InsoleQuadrant: 1,
                NominalThicknessMm: 3.5,
                TitleRu: "Передний медиальный клин (деротатор голени)",
                AnatomicalZone: "Головка I плюсневой кости и проксимальная фаланга",
                ForceVectorRationale: "Пронационный направляющий момент, эвакуация пикового варусного давления с наружного края стопы, разгрузка I ПФС (профилактика Hallux Valgus)",
                InstallationProtocol: "Наклеивать под подошвенную поверхность стельки в зоне головки I плюсневой кости со скосом медиально",
                MapColorHex: "#F59E0B"
            ));

            if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsOutwardTest5))
            {
                rawPrescriptions.Add(new WedgeItemPrescription(
                    Placement: WedgePlacementType.FifthMetatarsalBase,
                    EffectiveThicknessMm: Math.Round(2.0 * activityCoeff, 1, MidpointRounding.AwayFromZero),
                    CompensationPercentage: activityCoeff * 100,
                    BiomechanicalReason: "Динамическое падение стопы кнаружи при торсионном перекруте голени.",
                    ControlTestMandate: "Тест стабильности латерального свода.",
                    RecommendedDurometer: durometer,
                    InsoleQuadrant: 4,
                    NominalThicknessMm: 2.0,
                    TitleRu: "Клин основания V плюсневой кости (латеральный демпфер)",
                    AnatomicalZone: "Бугристость V плюсневой кости (латеральная колонна)",
                    ForceVectorRationale: "Демпфирование ударной волны при наружном завале стопы, разгрузка бугристости 5-й плюсневой кости от тракционного стресса m. peroneus brevis",
                    InstallationProtocol: "Устанавливать строго позади бугристости V плюсневой кости по наружному краю стельки без захода на сустав Лисфранка",
                    MapColorHex: "#EC4899"
                ));

                dynamicAlerts.Add(new ClinicalAlertItem(
                    AlertCode: "ALERT_FIFTH_METATARSAL_OVERLOAD",
                    Severity: AlertSeverity.WarningThreshold,
                    AffectedSegment: AnatomicalSegment.Foot_Forefoot,
                    Title: "Латеральная нестабильность и перегрузка V плюсневой кости",
                    BiomechanicalRationale: "Наружный перекрут голени провоцирует супинационный завал стопы при динамической нагрузке, создавая стрессовую перегрузку бугристости 5-й плюсневой кости.",
                    AdaptiveActionPlan: "1. Клинический регламент: Пальпация основания и бугристости V плюсневой кости, сухожилия m. peroneus brevis для исключения тракционного авульсионного повреждения.\n" +
                                        $"2. Ортопедический регламент: Установка латерального клина основания 5-й плюсневой кости ({Math.Round(2.0 * activityCoeff, 1):F1} мм) с дополнительным демпфирующим слоем под наружный край для устранения супинационного срыва стопы.\n" +
                                        "3. Двигательный протокол: Укрепление длинной и короткой малоберцовых мышц (эверсия стопы с сопротивлением эластичной ленты), тренировка проприоцепции на балансировочной подушке.\n" +
                                        "4. Контроль: Оценка стабильности латерального свода и отсутствия болей при осевой ходьбе через 14 дней."
                ));
            }
        }

        // Двойной завал стопы (тарзальная гипермобильность)
        if (input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsInwardTest5) && 
            input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsOutwardTest5))
        {
            dynamicAlerts.Add(new ClinicalAlertItem(
                AlertCode: "ALERT_DUAL_TARSAL_INSTABILITY",
                Severity: AlertSeverity.WarningThreshold,
                AffectedSegment: AnatomicalSegment.Foot_Rearfoot,
                Title: "Тарзальная нестабильность и гипермобильность стопы",
                BiomechanicalRationale: "Парадоксальный разнонаправленный завал стопы указывает на выраженную несостоятельность связочного аппарата голеностопа (дисплазия соединительной ткани).",
                AdaptiveActionPlan: "1. Клинический регламент: Оценка шкалы Бейтона для верификации генерализованной гипермобильности суставов. Исключить жесткую клиновидную фиксацию.\n" +
                                    "2. Ортопедический регламент: Назначение двухслойных стелек Formthotics Dual-Density с глубокой формовкой пяточной чаши (Deep Heel Cup) и высокими поддерживающими бортами без использования агрессивных жестких клиньев.\n" +
                                    "3. Двигательный протокол: Упражнения на стабилизацию голеностопного сустава в закрытой кинематической цепи, укрепление передней и задней большеберцовых мышц.\n" +
                                    "4. Контроль: Оценка стабильности походки и утомляемости стоп через 21 день."
            ));
        }

        // =========================================================================
        // ТЕСТ 5: СОЧЕТАННЫЕ ПАТОЛОГИИ И ТОРСИОННЫЙ КОНФЛИКТ (СТОЯЧАЯ ВОЛНА)
        // =========================================================================
        bool isConflict = input.IsAntetorsion && input.TibialStatus == TibialTorsion.OverRotationOutward;
        if (isConflict)
        {
            alerts.Add("Внимание: Торсионный конфликт (Антеторсия бедра + Наружный перекрут голени). Вычисление средней результирующей оси. Приоритет отдается 6-му тесту по Бейкрофту для сохранения адаптации подошвы.");

            dynamicAlerts.Add(new ClinicalAlertItem(
                AlertCode: "CONFLICT_TORSIONAL_PARADOX_STANDING_WAVE",
                Severity: AlertSeverity.KineticConflict,
                AffectedSegment: AnatomicalSegment.Tibia_Knee,
                Title: "Торсионный резонансный конфликт (Стоячая волна)",
                BiomechanicalRationale: "Антагонистическое скручивание: антеторсия бедра закручивает ось внутрь, а наружный перекрут голени — кнаружи. В плоскости коленного сустава формируется стоячая волна деструктивного момента W_femur + W_tibia, угрожающая передней крестообразной связке (ПКС) и внутреннему мениску.",
                AdaptiveActionPlan: "1. Клинический регламент: Протокол Бейкрофта (Baycroft 6-test): категорический запрет на жесткие разнонаправленные клинья, провоцирующие разрыв кинематической цепи колена.\n" +
                                    "2. Ортопедический регламент: Назначение медиального деротатора умеренной плотности Formax Single Blue с постепенной адаптивной формовкой. Расчет результирующего деротирующего вектора по тесту Бейкрофта.\n" +
                                    "3. Двигательный протокол: Режим высокой осторожности (ношение 1-2 ч/день в первые 5 дней). Мобилизация ТБС без осевого давления, стабилизация капсулы коленного сустава.\n" +
                                    "4. Контроль: Внеочередной осмотр врача-подиатра через 14 дней с оценкой состояния медиального мениска и оси колена."
            ));
        }

        // =========================================================================
        // АДАПТИВНОЕ ДЕМПФИРОВАНИЕ КУМУЛЯТИВНОЙ ТОЛЩИНЫ (SEGMENT CLAMPING)
        // =========================================================================
        double forefootRaw = rawPrescriptions
            .Where(p => p.Placement is WedgePlacementType.AnteriorLateral or WedgePlacementType.AnteriorMedial or WedgePlacementType.FifthMetatarsalBase)
            .Sum(p => p.EffectiveThicknessMm);

        double rearfootRaw = rawPrescriptions
            .Where(p => p.Placement is WedgePlacementType.PosteriorMedial or WedgePlacementType.HeelLiftCompensator)
            .Sum(p => p.EffectiveThicknessMm);

        bool hasThicknessOverflow = forefootRaw > MaxSafeSegmentCorrectionMm || rearfootRaw > MaxSafeSegmentCorrectionMm;

        if (hasThicknessOverflow)
        {
            alerts.Add($"Внимание: Суммарная толщина клиньев в сегменте ({Math.Max(forefootRaw, rearfootRaw):F1} мм) близка к критической. Контролируйте отсутствие гиперкератоза и мышечного сопротивления.");

            dynamicAlerts.Add(new ClinicalAlertItem(
                AlertCode: "LIMIT_CUMULATIVE_SEGMENT_THICKNESS_EXCEEDED",
                Severity: AlertSeverity.WarningThreshold,
                AffectedSegment: forefootRaw > rearfootRaw ? AnatomicalSegment.Foot_Forefoot : AnatomicalSegment.Foot_Rearfoot,
                Title: "Превышение физиологического потолка толщины (> 6.0 мм)",
                BiomechanicalRationale: $"Суммарная сырая толщина клиньев в сегменте ({Math.Max(forefootRaw, rearfootRaw):F1} мм) превышает физиологический лимит 6.0 мм. Возникает риск локального гиперкератоза, сдавления подошвенных нервов и срыва сенсорной проприоцепции.",
                AdaptiveActionPlan: "1. Клинический регламент: Применен алгоритм адаптивного демпфирования (Segment Clamping): суммарная толщина клиньев пропорционально редуцирована до физиологического предела 6.0 мм.\n" +
                                    "2. Ортопедический регламент: Двухэтапная установка клиньев. Первый этап: адаптация к базовой редуцированной толщине в течение 14 дней. Второй этап: добавление микроклиньев 1.0 мм при сохранении комфорта.\n" +
                                    "3. Двигательный протокол: Осмотр кожи подошвы на предмет эритемы и натоптышей. При первых признаках локального давления уменьшить время ношения.\n" +
                                    "4. Контроль: Контрольная плантоскопия и коррекция толщин через 14 дней."
            ));
        }

        // Пропорциональное адаптивное масштабирование клиньев при переполнении сегмента
        var adaptedPrescriptions = new List<WedgeItemPrescription>();
        double forefootScale = forefootRaw > MaxSafeSegmentCorrectionMm ? (MaxSafeSegmentCorrectionMm / forefootRaw) : 1.0;
        double rearfootScale = rearfootRaw > MaxSafeSegmentCorrectionMm ? (MaxSafeSegmentCorrectionMm / rearfootRaw) : 1.0;

        foreach (var p in rawPrescriptions)
        {
            bool isForefoot = p.Placement is WedgePlacementType.AnteriorLateral or WedgePlacementType.AnteriorMedial or WedgePlacementType.FifthMetatarsalBase;
            double scale = isForefoot ? forefootScale : rearfootScale;
            bool isClamped = scale < 0.999;

            double adaptedThickness = isClamped 
                ? Math.Round(p.EffectiveThicknessMm * scale, 1, MidpointRounding.AwayFromZero)
                : p.EffectiveThicknessMm;

            double compensationPct = isClamped
                ? Math.Round(p.CompensationPercentage * scale, 1)
                : p.CompensationPercentage;

            string formulaText = isClamped
                ? $"{p.NominalThicknessMm:F1} мм × {activityCoeff:F2} (активность) × {scale:F2} (демпфер лимита 6 мм) = {adaptedThickness:F1} мм"
                : $"{p.NominalThicknessMm:F1} мм × {activityCoeff:F2} (активность) = {adaptedThickness:F1} мм";

            adaptedPrescriptions.Add(p with 
            { 
                EffectiveThicknessMm = adaptedThickness,
                CompensationPercentage = compensationPct,
                AdaptiveFormulaBreakdown = formulaText,
                IsSegmentClamped = isClamped
            });
        }

        // Если клиньев нет (физиологическая норма / компенсация) - ВСЕГДА формируем базовую спецификацию Formthotics!
        if (adaptedPrescriptions.Count == 0)
        {
            adaptedPrescriptions.Add(new WedgeItemPrescription(
                Placement: WedgePlacementType.AnteriorMedial,
                EffectiveThicknessMm: 0.0,
                CompensationPercentage: 100,
                BiomechanicalReason: "Физиологическая симметрия стопы и сегментов голени. Патологических торсионных скручиваний не выявлено.",
                ControlTestMandate: "Плановый динамический контроль походки через 28 дней.",
                RecommendedDurometer: durometer,
                InsoleQuadrant: 1,
                NominalThicknessMm: 0.0,
                TitleRu: "Физиологическая балансировка Formthotics (клинья не требуются)",
                AnatomicalZone: "Физиологический свод стопы (билатерально)",
                ForceVectorRationale: "Сохранение естественной сенсомоторной проприоцепции, равномерное распределение плантарного давления без искусственной элевации",
                InstallationProtocol: "Индивидуальная термоформовка базовой заготовки Formthotics при 85°C без наклейки асимметричных корректирующих клиньев",
                MapColorHex: "#38BDF8"
            ));
        }

        // =========================================================================
        // СПЕЦИАЛЬНЫЙ АЛЕРТ: ВЫСОКАЯ СПОРТИВНАЯ АКТИВНОСТЬ (ATHLETE G-FORCE)
        // =========================================================================
        if (input.Activity == PatientActivityLevel.Athlete)
        {
            dynamicAlerts.Add(new ClinicalAlertItem(
                AlertCode: "ADVISORY_ATHLETIC_G_FORCE_PROTECTION",
                Severity: AlertSeverity.ClinicalAdvisory,
                AffectedSegment: AnatomicalSegment.GlobalKineticChain,
                Title: "Спортивные пиковые перегрузки (G-Force Protection)",
                BiomechanicalRationale: "При беговых и ударных спортивных нагрузках силы реакции опоры достигают 3–4G. Мягкие материалы проминаются до упора, а избыточная толщина клиньев провоцирует периостит надкостницы.",
                AdaptiveActionPlan: "1. Клинический регламент: Эксплуатация в режиме циклических беговых и прыжковых перегрузок. Высокий риск стрессовых переломов и усталостного периостита.\n" +
                                    "2. Ортопедический регламент: Применен спортивный коэффициент редукции толщины (κ = 0.25) с назначением жесткой двухслойной базы Formthotics Dual-Density Hard Black (Shore A 65) с высокой упругостью.\n" +
                                    "3. Двигательный протокол: Адаптация на тренировках: в 1-ю неделю использование только на разминках и легких кроссах (до 30-40 мин). Полноценные скоростно-силовые тренировки со 2-й недели.\n" +
                                    "4. Контроль: Осмотр амортизационного износа заготовки и баланса стопы через 21 день."
            ));
        }

        // =========================================================================
        // РАСЧЕТ ИНТЕГРАЛЬНОГО ИНДЕКСА КИНЕМАТИЧЕСКОГО РИСКА (KINETIC RISK SCORE, 0-100%)
        // =========================================================================
        double deltaScore = deltaMm <= 3.0 ? 0.0 : (input.DiscrepancyNature switch
        {
            LegDiscrepancyType.FunctionalPelvic => 25.0,
            LegDiscrepancyType.TrueAnatomical => 15.0,
            _ => 30.0
        });

        double torsionScore = isConflict ? 35.0 : 0.0;
        double boneBarrierScore = (input.IsAntetorsion && input.HipJointBarrier == RotationBarrier.BoneAnatomical) ? 15.0 : 0.0;
        
        bool dualFall = input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsInwardTest5) && 
                        input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsOutwardTest5);
        bool singleFall = input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsInwardTest5) || 
                          input.GlobalFlags.HasFlag(GlobalBiomechanicalFlags.FootFallsOutwardTest5);
        double instabilityScore = dualFall ? 25.0 : (singleFall ? 15.0 : 0.0);

        double thicknessScore = hasThicknessOverflow ? 20.0 : 0.0;

        double krs = Math.Min(100.0, deltaScore + torsionScore + boneBarrierScore + instabilityScore + thicknessScore);

        var riskLevel = isConflict
            ? KineticChainRiskLevel.CriticalTorsionConflict
            : (krs >= 50.0 ? KineticChainRiskLevel.High : (needsOsteopathy ? KineticChainRiskLevel.Moderate : KineticChainRiskLevel.Low));

        // =========================================================================
        // ДИНАМИЧЕСКИЙ АДАПТИВНЫЙ ГРАФИК НОШЕНИЯ (STEP-DOWN WEAR-IN SCHEDULE)
        // =========================================================================
        var wearInSchedule = new List<string>();
        if (isConflict || krs >= 70.0)
        {
            wearInSchedule.Add("Фаза 1 (Дни 1–5): Ношение строго 1 час в день в спокойном темпе по ровной поверхности.");
            wearInSchedule.Add("Фаза 2 (Дни 6–12): Увеличение до 2–3 часов в день. Избегать бега, прыжков и длительного стояния.");
            wearInSchedule.Add("Фаза 3 (Дни 13–20): Адаптация до 4–5 часов в день при условии отсутствия дискомфорта в коленях.");
            wearInSchedule.Add("Контрольный визит к подиастру на 21-й день: оценка стабильности коленного сустава и деротации.");
        }
        else if (krs >= 40.0)
        {
            wearInSchedule.Add("Дни 1–3: Ношение индивидуальных стелек 1.5–2 часа в день при обычной ходьбе.");
            wearInSchedule.Add("Дни 4–8: Постепенное увеличение времени до 3–4 часов в день.");
            wearInSchedule.Add("Неделя 2+: Полноценная эксплуатация в течение всего активного рабочего дня.");
            wearInSchedule.Add("Плановый контрольный осмотр подиатра через 21 день.");
        }
        else
        {
            wearInSchedule.Add("Дни 1–3: Ношение индивидуальных стелек 2 часа в день при умеренной нагрузке.");
            wearInSchedule.Add("Дни 4–7: Увеличение времени адаптации до 4–5 часов в день при обычной ходьбе.");
            wearInSchedule.Add("Неделя 2+: Полноценная эксплуатация в течение всего активного дня.");
            wearInSchedule.Add("Контрольный осмотр подиатра и динамическая переоценка через 21 день.");
        }

        double adaptedForefootSum = adaptedPrescriptions
            .Where(p => p.Placement is WedgePlacementType.AnteriorLateral or WedgePlacementType.AnteriorMedial or WedgePlacementType.FifthMetatarsalBase)
            .Sum(p => p.EffectiveThicknessMm);

        double adaptedRearfootSum = adaptedPrescriptions
            .Where(p => p.Placement is WedgePlacementType.PosteriorMedial or WedgePlacementType.HeelLiftCompensator)
            .Sum(p => p.EffectiveThicknessMm);

        // Если патологических алертов нет (норма / компенсация) - ВСЕГДА формируем физиологический адаптивный протокол
        if (dynamicAlerts.Count == 0)
        {
            dynamicAlerts.Add(new ClinicalAlertItem(
                AlertCode: "ADVISORY_PHYSIOLOGICAL_ADAPTATION",
                Severity: AlertSeverity.ClinicalAdvisory,
                AffectedSegment: AnatomicalSegment.GlobalKineticChain,
                Title: "Стандартный физиологический протокол адаптации Formthotics",
                BiomechanicalRationale: "Критических торсионных асимметрий, костных барьеров и мышечных спазмов не выявлено. Суставная кинетическая цепь находится в состоянии физиологической компенсации.",
                AdaptiveActionPlan: "1. Протокол адаптации: Ступенчатое увеличение времени ношения (дни 1–4: 2–3 ч/день, дни 5–14: 6–8+ ч/день, со 2-й недели: полный день без ограничений).\n" +
                                    "2. Теплофизический регламент: Термоформовка заготовки при 85°C в обуви пациента с остыванием под статической нагрузкой для стабилизации подтаранного сустава.\n" +
                                    "3. Двигательный стереотип: Закрепление физиологического переката стопы через первый луч. Контрольный осмотр и термокоррекция через 28 дней."
            ));
        }

        return new HorizontalDiagnosticReport(
            RequiresOsteopathicIntervention: needsOsteopathy,
            MeasuredDeltaMm: deltaMm,
            IsComplexTorsionConflict: isConflict,
            Prescriptions: adaptedPrescriptions,
            ClinicalAlerts: alerts,
            RiskLevel: riskLevel,
            CumulativeForefootCorrectionMm: Math.Round(adaptedForefootSum, 1),
            CumulativeRearfootCorrectionMm: Math.Round(adaptedRearfootSum, 1),
            WearInSchedule: wearInSchedule,
            DynamicAlerts: dynamicAlerts,
            KineticRiskScore: Math.Round(krs, 1)
        );
    }
}
