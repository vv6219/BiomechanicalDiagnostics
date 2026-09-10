namespace OrthoClinic.UI.Drawables;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using OrthoClinic.Core.Domain;

/// <summary>
/// Интерактивная карта плантарной поверхности стопы и ортопедической стельки
/// с визуализацией точных зон наложения выписанных клиньев и их толщин.
/// </summary>
public sealed class PlantarInsoleMapDrawable : BindableObject, IDrawable
{
    public static readonly BindableProperty PrescriptionsProperty = BindableProperty.Create(
        nameof(Prescriptions), typeof(IReadOnlyList<WedgeItemPrescription>), typeof(PlantarInsoleMapDrawable), null);

    public IReadOnlyList<WedgeItemPrescription>? Prescriptions
    {
        get => (IReadOnlyList<WedgeItemPrescription>?)GetValue(PrescriptionsProperty);
        set => SetValue(PrescriptionsProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;

        float cx = dirtyRect.Center.X;
        float cy = dirtyRect.Center.Y;

        // Контур ортопедической стельки (правая стопа, вид сверху/плантарно)
        var insolePath = new PathF();
        insolePath.MoveTo(cx - 35, cy - 85);
        insolePath.CurveTo(cx - 10, cy - 95, cx + 25, cy - 90, cx + 38, cy - 75);
        insolePath.CurveTo(cx + 45, cy - 50, cx + 42, cy - 20, cx + 38, cy + 10);
        insolePath.CurveTo(cx + 35, cy + 40, cx + 32, cy + 65, cx + 24, cy + 85);
        insolePath.CurveTo(cx + 10, cy + 98, cx - 15, cy + 98, cx - 24, cy + 85);
        insolePath.CurveTo(cx - 28, cy + 55, cx - 18, cy + 20, cx - 20, cy - 20);
        insolePath.CurveTo(cx - 30, cy - 45, cx - 42, cy - 65, cx - 35, cy - 85);
        insolePath.Close();

        // Заливка стельки
        canvas.FillColor = Color.FromArgb("#1E293B");
        canvas.FillPath(insolePath);
        canvas.StrokeColor = Color.FromArgb("#475569");
        canvas.StrokeSize = 2.0f;
        canvas.DrawPath(insolePath);

        // Осевая линия баланса стопы
        canvas.StrokeColor = Color.FromArgb("#334155");
        canvas.StrokeDashPattern = new float[] { 4, 3 };
        canvas.StrokeSize = 1.0f;
        canvas.DrawLine(cx, cy - 85, cx, cy + 85);
        canvas.StrokeDashPattern = null;

        if (Prescriptions == null || Prescriptions.Count == 0)
        {
            canvas.FontColor = Color.FromArgb("#64748B");
            canvas.FontSize = 11;
            canvas.DrawString("Клинья не назначены (норма)", cx - 90, cy - 10, 180, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.RestoreState();
            return;
        }

        // Отрисовка активных клиньев
        foreach (var p in Prescriptions)
        {
            switch (p.Placement)
            {
                case WedgePlacementType.AnteriorLateral:
                    DrawWedgeSpot(canvas, cx + 26, cy - 60, 16, 12, "#38BDF8", $"{p.EffectiveThicknessMm:F1}");
                    break;

                case WedgePlacementType.AnteriorMedial:
                    DrawWedgeSpot(canvas, cx - 22, cy - 65, 18, 14, "#F59E0B", $"{p.EffectiveThicknessMm:F1}");
                    break;

                case WedgePlacementType.PosteriorMedial:
                    DrawWedgeSpot(canvas, cx - 18, cy + 15, 16, 22, "#10B981", $"{p.EffectiveThicknessMm:F1}");
                    break;

                case WedgePlacementType.FifthMetatarsalBase:
                    DrawWedgeSpot(canvas, cx + 32, cy - 10, 14, 18, "#EC4899", $"{p.EffectiveThicknessMm:F1}");
                    break;

                case WedgePlacementType.HeelLiftCompensator:
                    DrawWedgeSpot(canvas, cx, cy + 70, 28, 18, "#EF4444", $"{p.EffectiveThicknessMm:F1}");
                    break;
            }
        }

        canvas.RestoreState();
    }

    private static void DrawWedgeSpot(ICanvas canvas, float x, float y, float w, float h, string hexColor, string thicknessText)
    {
        var rect = new RectF(x - w / 2, y - h / 2, w, h);
        canvas.FillColor = Color.FromArgb(hexColor).WithAlpha(0.6f);
        canvas.FillRoundedRectangle(rect, 4);
        canvas.StrokeColor = Color.FromArgb(hexColor);
        canvas.StrokeSize = 1.5f;
        canvas.DrawRoundedRectangle(rect, 4);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 9;
        canvas.DrawString(thicknessText, rect.X - 5, rect.Y, rect.Width + 10, rect.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
