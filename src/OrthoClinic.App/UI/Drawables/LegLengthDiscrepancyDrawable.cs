namespace OrthoClinic.UI.Drawables;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

/// <summary>
/// Векторный канвас Теста 1: Топология натяжения и асимметрия длины нижних конечностей.
/// Наследует BindableObject для реактивной привязки данных в XAML.
/// </summary>
public sealed class LegLengthDiscrepancyDrawable : BindableObject, IDrawable
{
    public static readonly BindableProperty LeftOffsetProperty = BindableProperty.Create(
        nameof(LeftOffset), typeof(double), typeof(LegLengthDiscrepancyDrawable), 0.0);

    public static readonly BindableProperty RightOffsetProperty = BindableProperty.Create(
        nameof(RightOffset), typeof(double), typeof(LegLengthDiscrepancyDrawable), 0.0);

    public double LeftOffset
    {
        get => (double)GetValue(LeftOffsetProperty);
        set => SetValue(LeftOffsetProperty, value);
    }

    public double RightOffset
    {
        get => (double)GetValue(RightOffsetProperty);
        set => SetValue(RightOffsetProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;

        float midX = dirtyRect.Center.X;
        float baseY = dirtyRect.Height * 0.55f;
        float legSpacing = 70f;

        float leftY = baseY + (float)LeftOffset * 3f;
        float rightY = baseY + (float)RightOffset * 3f;
        double delta = Math.Abs(LeftOffset - RightOffset);

        // Направляющие осей конечностей
        canvas.StrokeColor = Color.FromArgb("#334155");
        canvas.StrokeSize = 2;
        canvas.DrawLine(midX - legSpacing, 15, midX - legSpacing, leftY);
        canvas.DrawLine(midX + legSpacing, 15, midX + legSpacing, rightY);

        // Подписи сторон L и R
        canvas.FontColor = Color.FromArgb("#94A3B8");
        canvas.FontSize = 11;
        canvas.DrawString("L (Левая)", midX - legSpacing - 30, 25, 60, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
        canvas.DrawString("R (Правая)", midX + legSpacing - 30, 25, 60, 20, HorizontalAlignment.Center, VerticalAlignment.Center);

        // Индикатор перекоса лодыжек
        bool isCritical = delta > 3.0;
        canvas.StrokeSize = isCritical ? 3.5f : 1.5f;
        canvas.StrokeColor = isCritical ? Color.FromArgb("#EF4444") : Color.FromArgb("#10B981");

        if (isCritical)
        {
            canvas.StrokeDashPattern = new float[] { 6, 3 };
        }

        canvas.DrawLine(midX - legSpacing - 20, leftY, midX + legSpacing + 20, rightY);

        // Реперы лодыжек
        canvas.FillColor = isCritical ? Color.FromArgb("#EF4444") : Color.FromArgb("#38BDF8");
        canvas.FillCircle(midX - legSpacing, leftY, 6);
        canvas.FillCircle(midX + legSpacing, rightY, 6);

        // Текстовая аннотация измеренной дельты
        canvas.FontColor = isCritical ? Color.FromArgb("#FCA5A5") : Color.FromArgb("#34D399");
        canvas.FontSize = 12;
        string deltaText = $"ΔL = {delta:F1} мм {(isCritical ? "⚠ Остеопатический барьер" : "✓ Норма")}";
        canvas.DrawString(deltaText, midX - 120, dirtyRect.Height - 25, 240, 20, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.RestoreState();
    }
}
