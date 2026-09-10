namespace OrthoClinic.Infrastructure.Services;

using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OrthoClinic.Core.Domain;
using OrthoClinic.Core.Domain.Protocols;
using OrthoClinic.Core.Services;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;

/// <summary>
/// Резолвер системных TTF шрифтов с полной поддержкой кириллицы для PdfSharpCore.
/// </summary>
public sealed class CyrillicFontResolver : IFontResolver
{
    public string DefaultFontName => "Arial";

    private static readonly byte[]? ArialRegularBytes;
    private static readonly byte[]? ArialBoldBytes;
    private static readonly byte[]? ArialItalicBytes;

    static CyrillicFontResolver()
    {
        string winFonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        
        string regularPath = Path.Combine(winFonts, "arial.ttf");
        string boldPath = Path.Combine(winFonts, "arialbd.ttf");
        string italicPath = Path.Combine(winFonts, "ariali.ttf");

        if (File.Exists(regularPath)) ArialRegularBytes = File.ReadAllBytes(regularPath);
        if (File.Exists(boldPath)) ArialBoldBytes = File.ReadAllBytes(boldPath);
        if (File.Exists(italicPath)) ArialItalicBytes = File.ReadAllBytes(italicPath);
    }

    public byte[]? GetFont(string faceName)
    {
        return faceName switch
        {
            "Arial#b" => ArialBoldBytes ?? ArialRegularBytes,
            "Arial#i" => ArialItalicBytes ?? ArialRegularBytes,
            "Arial#bi" => ArialBoldBytes ?? ArialRegularBytes,
            _ => ArialRegularBytes
        };
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        string suffix = "";
        if (isBold && isItalic) suffix = "#bi";
        else if (isBold) suffix = "#b";
        else if (isItalic) suffix = "#i";

        return new FontResolverInfo("Arial" + suffix);
    }
}

/// <summary>
/// Реализация промышленного экспорта официального медицинского протокола в формат PDF.
/// </summary>
public sealed class PodiatricPdfExportService : IPodiatricPdfExportService
{
    private static bool _fontResolverRegistered = false;
    private static readonly object _lock = new();

    public PodiatricPdfExportService()
    {
        lock (_lock)
        {
            if (!_fontResolverRegistered)
            {
                try
                {
                    GlobalFontSettings.FontResolver = new CyrillicFontResolver();
                    _fontResolverRegistered = true;
                }
                catch
                {
                    // Игнорируем, если резолвер уже был назначен
                }
            }
        }
    }

    public Task<string> ExportProtocolPdfAsync(PdfExportRequest request, string? outputFilePath = null)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                string docsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OrthoClinic_Protocols");
                Directory.CreateDirectory(docsDir);
                string cleanId = string.Concat(request.PatientId.Split(Path.GetInvalidFileNameChars()));
                string fileName = $"Protocol_{cleanId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                outputFilePath = Path.Combine(docsDir, fileName);
            }

            using var document = new PdfDocument();
            document.Info.Title = $"Протокол адаптации Formthotics - {request.PatientFullName}";
            document.Info.Author = request.DoctorFullName;
            document.Info.Subject = "Официальный протокол биомеханического моделирования и ортезирования стопы";

            var page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);

            double margin = 36;
            double pageWidth = page.Width.Point;
            double pageHeight = page.Height.Point;
            double contentWidth = pageWidth - (margin * 2);
            double currentY = margin;

            // Цветовая палитра
            var primaryBlue = XColor.FromArgb(3, 105, 161);    // #0369A1
            var darkSlate = XColor.FromArgb(15, 23, 42);       // #0F172A
            var mutedGray = XColor.FromArgb(100, 116, 139);    // #64748B
            var cardBg = XColor.FromArgb(248, 250, 252);       // #F8FAFC
            var borderGray = XColor.FromArgb(226, 232, 240);   // #E2E8F0
            var accentGreen = XColor.FromArgb(5, 150, 105);    // #059669
            var warningOrange = XColor.FromArgb(217, 119, 6);  // #D97706
            var criticalRed = XColor.FromArgb(220, 38, 38);    // #DC2626

            // Шрифты
            var fontTitle = new XFont("Arial", 14, XFontStyle.Bold);
            var fontSubtitle = new XFont("Arial", 9, XFontStyle.Bold);
            var fontSectionHeader = new XFont("Arial", 10, XFontStyle.Bold);
            var fontBodyBold = new XFont("Arial", 9, XFontStyle.Bold);
            var fontBody = new XFont("Arial", 8.5, XFontStyle.Regular);
            var fontSmall = new XFont("Arial", 7.5, XFontStyle.Regular);
            var fontSmallBold = new XFont("Arial", 7.5, XFontStyle.Bold);

            void CheckPageOverflow(double neededHeight)
            {
                if (currentY + neededHeight > pageHeight - margin)
                {
                    page = document.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    gfx = XGraphics.FromPdfPage(page);
                    currentY = margin;

                    // Повтор заголовка на следующей странице
                    gfx.DrawString($"Протокол ортезирования Formthotics — Пациент: {request.PatientFullName} (Стр. {document.PageCount})", fontSmall, new XSolidBrush(mutedGray), margin, currentY);
                    currentY += 14;
                    gfx.DrawLine(new XPen(borderGray, 0.5), margin, currentY, margin + contentWidth, currentY);
                    currentY += 10;
                }
            }

            // =========================================================================
            // 1. ШАПКА ДОКУМЕНТА И МЕДИЦИНСКИЙ БРЕНДИНГ
            // =========================================================================
            gfx.DrawString("МЕДИЦИНСКИЙ ЦЕНТР ОРТОПЕДИИ И ПОДИАТРИИ", fontSubtitle, new XSolidBrush(darkSlate), margin, currentY);
            string dateStr = $"Дата: {request.EvaluationTimestamp:dd.MM.yyyy HH:mm}";
            gfx.DrawString(dateStr, fontSmall, new XSolidBrush(mutedGray), margin + contentWidth - gfx.MeasureString(dateStr, fontSmall).Width, currentY);
            currentY += 14;

            gfx.DrawString("ПРОТОКОЛ БИОМЕХАНИЧЕСКОГО МОДЕЛИРОВАНИЯ И АДАПТАЦИИ СТОПЫ", fontTitle, new XSolidBrush(primaryBlue), margin, currentY + 4);
            currentY += 18;

            gfx.DrawString("Комплексная система динамического ортезирования Formthotics™ Medical", fontSmall, new XSolidBrush(mutedGray), margin, currentY);
            string idStr = $"ID: {request.PatientId}";
            gfx.DrawString(idStr, fontBodyBold, new XSolidBrush(darkSlate), margin + contentWidth - gfx.MeasureString(idStr, fontBodyBold).Width, currentY);
            currentY += 8;

            gfx.DrawLine(new XPen(primaryBlue, 1.5), margin, currentY, margin + contentWidth, currentY);
            currentY += 10;

            // =========================================================================
            // 2. БЛОК 1: ДАННЫЕ ПАЦИЕНТА
            // =========================================================================
            CheckPageOverflow(54);
            gfx.DrawRoundedRectangle(new XPen(borderGray, 0.5), new XSolidBrush(cardBg), margin, currentY, contentWidth, 50, 4, 4);

            double colW = contentWidth / 4;
            gfx.DrawString("ФИО Пациента:", fontSmall, new XSolidBrush(mutedGray), margin + 8, currentY + 12);
            gfx.DrawString(request.PatientFullName, fontBodyBold, new XSolidBrush(darkSlate), margin + 8, currentY + 24);

            gfx.DrawString("Возраст:", fontSmall, new XSolidBrush(mutedGray), margin + colW * 2, currentY + 12);
            gfx.DrawString($"{request.PatientAge} лет", fontBodyBold, new XSolidBrush(darkSlate), margin + colW * 2, currentY + 24);

            gfx.DrawString("Масса / BMI:", fontSmall, new XSolidBrush(mutedGray), margin + colW * 3, currentY + 12);
            gfx.DrawString($"{request.PatientWeightKg:F1} кг (BMI: {request.PatientBmi:F1})", fontBodyBold, new XSolidBrush(darkSlate), margin + colW * 3, currentY + 24);

            gfx.DrawString("Двигательная активность:", fontSmall, new XSolidBrush(mutedGray), margin + 8, currentY + 36);
            gfx.DrawString(request.ActivityTier.ToString(), fontBodyBold, new XSolidBrush(primaryBlue), margin + 120, currentY + 36);

            gfx.DrawString("Анамнез:", fontSmall, new XSolidBrush(mutedGray), margin + colW * 2, currentY + 36);
            gfx.DrawString(request.ClinicalHistorySummary, fontBody, new XSolidBrush(darkSlate), margin + colW * 2 + 45, currentY + 36);

            currentY += 58;

            // =========================================================================
            // 3. БЛОК 2: ГОРИЗОНТАЛЬНЫЕ ТЕСТЫ НА КУШЕТКЕ
            // =========================================================================
            CheckPageOverflow(44);
            gfx.DrawRoundedRectangle(new XPen(borderGray, 0.5), new XSolidBrush(cardBg), margin, currentY, contentWidth, 40, 4, 4);

            gfx.DrawString("БИОМЕХАНИЧЕСКИЙ СТАТУС НА КУШЕТКЕ:", fontSmallBold, new XSolidBrush(primaryBlue), margin + 8, currentY + 12);

            gfx.DrawString($"Разница ног (ΔL): {request.MeasuredDeltaMm:F1} мм", fontBodyBold, new XSolidBrush(darkSlate), margin + 8, currentY + 26);
            gfx.DrawString($"Тип: {request.DiscrepancyNature}", fontBody, new XSolidBrush(mutedGray), margin + 140, currentY + 26);
            gfx.DrawString($"Барьер ТБС: {request.Barrier}", fontBody, new XSolidBrush(mutedGray), margin + 280, currentY + 26);
            gfx.DrawString($"Торсия голени: {request.TibialState}", fontBody, new XSolidBrush(mutedGray), margin + 400, currentY + 26);

            currentY += 48;

            // =========================================================================
            // 4. БЛОК 3: НАЗНАЧЕННАЯ МОДЕЛЬ FORMTHOTICS И ТЕПЛОФИЗИЧЕСКИЙ РЕГЛАМЕНТ (85°C)
            // =========================================================================
            CheckPageOverflow(66);
            var modelBg = XColor.FromArgb(239, 246, 255); // #EFF6FF
            var modelBorder = XColor.FromArgb(191, 219, 254);
            gfx.DrawRoundedRectangle(new XPen(modelBorder, 1.0), new XSolidBrush(modelBg), margin, currentY, contentWidth, 60, 4, 4);

            gfx.DrawString("НАЗНАЧЕННАЯ ОРТОПЕДИЧЕСКАЯ БАЗА И ТЕПЛОФИЗИЧЕСКИЙ РАСЧЕТ (FORMAX 85°C):", fontSmallBold, new XSolidBrush(primaryBlue), margin + 8, currentY + 12);
            gfx.DrawString(request.Protocol.RecommendedModelTitle, fontTitle, new XSolidBrush(darkSlate), margin + 8, currentY + 28);
            gfx.DrawString(request.Protocol.FoamDensityDescription, fontBody, new XSolidBrush(darkSlate), margin + 8, currentY + 40);

            // Параметры термоформовки в рамке
            double badgeX = margin + contentWidth - 180;
            gfx.DrawString($"Толщина h: {request.Protocol.InsoleThicknessMm:F1} мм", fontBodyBold, new XSolidBrush(primaryBlue), badgeX, currentY + 24);
            gfx.DrawString($"Нагрев феном: t_heat = {request.Protocol.HeatingSeconds} сек", fontBodyBold, new XSolidBrush(warningOrange), badgeX, currentY + 36);
            gfx.DrawString($"Формовка в обуви: t_cool = {request.Protocol.InShoeMoldingSeconds} сек", fontBodyBold, new XSolidBrush(accentGreen), badgeX, currentY + 48);

            currentY += 68;

            // =========================================================================
            // 5. БЛОК 4: КАЛИБРОВАННЫЕ КЛИНИЧЕСКИЕ КЛИНЬЯ (СПЕЦИФИКАЦИЯ ДЛЯ ТЕХНИКА)
            // =========================================================================
            CheckPageOverflow(26);
            gfx.DrawString("СПЕЦИФИКАЦИЯ КОМПЕНСАТОРНЫХ КЛИНИЧЕСКИХ КЛИНИЕВ:", fontSectionHeader, new XSolidBrush(primaryBlue), margin, currentY);
            currentY += 8;

            // Шапка таблицы
            double thH = 16;
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(226, 232, 240)), margin, currentY, contentWidth, thH);
            gfx.DrawString("Анатомическая зона", fontSmallBold, new XSolidBrush(darkSlate), margin + 6, currentY + 11);
            gfx.DrawString("Тип клина", fontSmallBold, new XSolidBrush(darkSlate), margin + 140, currentY + 11);
            gfx.DrawString("T_nom", fontSmallBold, new XSolidBrush(darkSlate), margin + 240, currentY + 11);
            gfx.DrawString("T_eff (ИТОГ)", fontSmallBold, new XSolidBrush(primaryBlue), margin + 290, currentY + 11);
            gfx.DrawString("Биомеханическое назначение", fontSmallBold, new XSolidBrush(darkSlate), margin + 360, currentY + 11);
            currentY += thH;

            foreach (var w in request.Protocol.Wedges)
            {
                CheckPageOverflow(18);
                gfx.DrawRectangle(new XPen(borderGray, 0.5), new XSolidBrush(XColor.FromArgb(255, 255, 255)), margin, currentY, contentWidth, 18);
                gfx.DrawString(w.AnatomicalZone, fontBodyBold, new XSolidBrush(darkSlate), margin + 6, currentY + 12);
                gfx.DrawString(w.Type.ToString(), fontBody, new XSolidBrush(mutedGray), margin + 140, currentY + 12);
                gfx.DrawString($"{w.NominalThicknessMm:F1} мм", fontBody, new XSolidBrush(mutedGray), margin + 240, currentY + 12);
                gfx.DrawString($"{w.CalculatedThicknessMm:F1} мм", fontBodyBold, new XSolidBrush(accentGreen), margin + 290, currentY + 12);
                
                string objStr = w.BiomechanicalObjective;
                if (objStr.Length > 40) objStr = objStr.Substring(0, 37) + "...";
                gfx.DrawString(objStr, fontSmall, new XSolidBrush(darkSlate), margin + 360, currentY + 12);
                currentY += 18;
            }

            currentY += 10;

            // =========================================================================
            // 6. БЛОК 5: МЕТАТАРЗАЛЬНЫЙ ПЕЛОТ
            // =========================================================================
            if (request.Protocol.MetatarsalPad != null)
            {
                CheckPageOverflow(26);
                gfx.DrawRoundedRectangle(new XPen(borderGray, 0.5), new XSolidBrush(cardBg), margin, currentY, contentWidth, 24, 4, 4);
                gfx.DrawString("Метатарзальный пелот (Капля):", fontSmallBold, new XSolidBrush(primaryBlue), margin + 8, currentY + 15);
                gfx.DrawString($"{request.Protocol.MetatarsalPad.PlacementZone} — Размер: {request.Protocol.MetatarsalPad.PadSize}", fontBody, new XSolidBrush(darkSlate), margin + 170, currentY + 15);
                currentY += 30;
            }

            // =========================================================================
            // 7. БЛОК 6: КЛИНИЧЕСКИЕ АЛЕРТЫ И АДАПТИВНЫЙ ПРОТОКОЛ ПОДИАТРА (ТОЛЬКО НЕПУСТЫЕ КАРТОЧКИ)
            // =========================================================================
            var validAlerts = request.Report.DynamicAlerts?
                .Where(a => !string.IsNullOrWhiteSpace(a.Title))
                .ToList();

            if (validAlerts != null && validAlerts.Count > 0)
            {
                CheckPageOverflow(24);
                gfx.DrawString("КЛИНИЧЕСКИЕ АЛЕРТЫ И РЕГЛАМЕНТЫ АДАПТАЦИИ ПОДИАТРА:", fontSectionHeader, new XSolidBrush(criticalRed), margin, currentY);
                currentY += 10;

                foreach (var alert in validAlerts)
                {
                    // Расчет высоты карточки
                    double alertCardHeight = 65;
                    if (!string.IsNullOrWhiteSpace(alert.AdaptiveActionPlan))
                    {
                        alertCardHeight = 110;
                    }

                    CheckPageOverflow(alertCardHeight + 8);

                    var alertBg = alert.Severity switch
                    {
                        AlertSeverity.CriticalContraindication => XColor.FromArgb(254, 242, 242),
                        AlertSeverity.KineticConflict => XColor.FromArgb(255, 247, 237),
                        AlertSeverity.WarningThreshold => XColor.FromArgb(254, 252, 232),
                        _ => XColor.FromArgb(240, 249, 255)
                    };

                    var alertBorder = alert.Severity switch
                    {
                        AlertSeverity.CriticalContraindication => criticalRed,
                        AlertSeverity.KineticConflict => warningOrange,
                        AlertSeverity.WarningThreshold => warningOrange,
                        _ => primaryBlue
                    };

                    gfx.DrawRoundedRectangle(new XPen(alertBorder, 1.0), new XSolidBrush(alertBg), margin, currentY, contentWidth, alertCardHeight, 4, 4);

                    string badgeText = alert.Severity switch
                    {
                        AlertSeverity.CriticalContraindication => "🛑 ПРОТИВОПОКАЗАНИЕ",
                        AlertSeverity.KineticConflict => "⚡ КИНЕМАТИЧЕСКИЙ КОНФЛИКТ",
                        AlertSeverity.WarningThreshold => "⚠ ПОРОГОВЫЙ БАРЬЕР",
                        _ => "💡 АДАПТИВНЫЙ ПРОТОКОЛ"
                    };

                    gfx.DrawString(badgeText, fontSmallBold, new XSolidBrush(alertBorder), margin + 8, currentY + 12);
                    gfx.DrawString($"Сегмент: {alert.AffectedSegment}", fontSmall, new XSolidBrush(mutedGray), margin + 180, currentY + 12);

                    gfx.DrawString(alert.Title, fontBodyBold, new XSolidBrush(darkSlate), margin + 8, currentY + 25);

                    // Обоснование
                    string bioStr = alert.BiomechanicalRationale;
                    if (bioStr.Length > 95) bioStr = bioStr.Substring(0, 92) + "...";
                    gfx.DrawString(bioStr, fontBody, new XSolidBrush(darkSlate), margin + 8, currentY + 38);

                    // Адаптивный план подиатра
                    if (!string.IsNullOrWhiteSpace(alert.AdaptiveActionPlan))
                    {
                        double planY = currentY + 46;
                        gfx.DrawRoundedRectangle(new XPen(alertBorder, 0.5), new XSolidBrush(XColor.FromArgb(255, 255, 255)), margin + 6, planY, contentWidth - 12, alertCardHeight - 52, 3, 3);
                        gfx.DrawString("📋 Адаптивный регламент подиатра:", fontSmallBold, new XSolidBrush(primaryBlue), margin + 12, planY + 11);

                        // Разбиваем план на строки
                        string[] lines = alert.AdaptiveActionPlan.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        double lineY = planY + 22;
                        int maxLines = 4;
                        for (int i = 0; i < Math.Min(lines.Length, maxLines); i++)
                        {
                            string l = lines[i];
                            if (l.Length > 105) l = l.Substring(0, 102) + "...";
                            gfx.DrawString(l, fontSmall, new XSolidBrush(darkSlate), margin + 14, lineY);
                            lineY += 10;
                        }
                    }

                    currentY += alertCardHeight + 8;
                }
            }

            // =========================================================================
            // 8. БЛОК 7: ДОРОЖНАЯ КАРТА АДАПТАЦИИ СТОПЫ (ДНИ 1–28)
            // =========================================================================
            CheckPageOverflow(60);
            gfx.DrawString("ИНДИВИДУАЛЬНЫЙ РЕГЛАМЕНТ АДАПТАЦИИ СТОПЫ К НАГРУЗКЕ (ДНИ 1–28):", fontSectionHeader, new XSolidBrush(primaryBlue), margin, currentY);
            currentY += 8;

            foreach (var stage in request.Protocol.Timeline)
            {
                CheckPageOverflow(18);
                gfx.DrawRoundedRectangle(new XPen(borderGray, 0.5), new XSolidBrush(cardBg), margin, currentY, contentWidth, 16, 3, 3);
                gfx.DrawString(stage.StageTitle, fontBodyBold, new XSolidBrush(darkSlate), margin + 8, currentY + 11);
                gfx.DrawString(stage.DailyWearHours, fontBodyBold, new XSolidBrush(accentGreen), margin + 220, currentY + 11);
                gfx.DrawString(stage.KinematicMode, fontSmall, new XSolidBrush(mutedGray), margin + 360, currentY + 11);
                currentY += 18;
            }

            gfx.DrawString($"📅 Контрольный осмотр и термокоррекция: через {request.Protocol.NextFollowUpDay} дней", fontBodyBold, new XSolidBrush(primaryBlue), margin, currentY + 8);
            currentY += 22;

            // =========================================================================
            // 9. БЛОК 8: УТВЕРЖДЕНИЕ ВРАЧОМ-ПОДИАТРОМ И ЦИФРОВАЯ ПОДПИСЬ
            // =========================================================================
            CheckPageOverflow(65);
            gfx.DrawRoundedRectangle(new XPen(primaryBlue, 1.2), new XSolidBrush(cardBg), margin, currentY, contentWidth, 60, 4, 4);

            gfx.DrawString("СОГЛАСОВАНИЕ И УТВЕРЖДЕНИЕ ВРАЧОМ-ПОДИАТРОМ:", fontSmallBold, new XSolidBrush(darkSlate), margin + 8, currentY + 13);
            
            string statusStr = request.Approval?.Status == ProtocolApprovalStatus.ApprovedByPodiatrist
                ? "✓ УТВЕРЖДЕНО ВРАЧОМ-ПОДИАТРОМ"
                : "⏳ ЧЕРНОВИК (ОЖИДАЕТ УТВЕРЖДЕНИЯ)";
            var statusColor = request.Approval?.Status == ProtocolApprovalStatus.ApprovedByPodiatrist ? accentGreen : warningOrange;
            gfx.DrawString(statusStr, fontBodyBold, new XSolidBrush(statusColor), margin + contentWidth - 190, currentY + 13);

            gfx.DrawString($"Лечащий врач: {request.DoctorFullName}", fontBodyBold, new XSolidBrush(darkSlate), margin + 8, currentY + 28);
            if (!string.IsNullOrWhiteSpace(request.DoctorNotes))
            {
                gfx.DrawString($"Примечания: {request.DoctorNotes}", fontSmall, new XSolidBrush(mutedGray), margin + 8, currentY + 40);
            }

            string hashStr = request.Approval?.ApprovalDigitalFingerprint ?? "DRAFT";
            string timeStr = request.Approval?.ApprovedAtUtc?.ToString("dd.MM.yyyy HH:mm:ss UTC") ?? DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm:ss UTC");
            gfx.DrawString($"Электронный хэш: {hashStr}  |  Время фиксации: {timeStr}", fontSmall, new XSolidBrush(mutedGray), margin + 8, currentY + 52);

            // Место для личной подписи
            double signX = margin + contentWidth - 140;
            gfx.DrawLine(new XPen(mutedGray, 0.5), signX, currentY + 45, signX + 130, currentY + 45);
            gfx.DrawString("Подпись врача-подиатра", fontSmall, new XSolidBrush(mutedGray), signX + 15, currentY + 54);

            currentY += 66;

            // Сохранение документа на диск
            document.Save(outputFilePath);

            return outputFilePath;
        });
    }
}
