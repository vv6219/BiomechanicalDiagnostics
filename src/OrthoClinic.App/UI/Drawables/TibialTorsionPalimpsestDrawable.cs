namespace OrthoClinic.UI.Drawables;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using OrthoClinic.Core.Domain;

/// <summary>
/// Векторный канвас Теста 4: Палимпсест уровней торсии большеберцовой кости относительно оси мыщелков.
/// </summary>
public sealed class TibialTorsionPalimpsestDrawable : BindableObject, IDrawable
{
    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State), typeof(TibialTorsion), typeof(TibialTorsionPalimpsestDrawable), TibialTorsion.Normal);

    public TibialTorsion State
    {
        get => (TibialTorsion)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;

        float cx = dirtyRect.Center.X;
        float cy = dirtyRect.Center.Y;

        // 1. Нижний уровень (Bootloader): Мыщелки бедра (горизонтальная база отсчета)
        canvas.StrokeColor = Color.FromArgb("#475569");
        canvas.StrokeSize = 3;
        canvas.DrawLine(cx - 90, cy, cx + 90, cy);
        canvas.FillColor = Color.FromArgb("#334155");
        canvas.FillCircle(cx - 90, cy, 8);
        canvas.FillCircle(cx + 90, cy, 8);

        canvas.FontColor = Color.FromArgb("#64748B");
        canvas.FontSize = 10;
        canvas.DrawString("Ось мыщелков бедра", cx - 75, cy + 12, 150, 16, HorizontalAlignment.Center, VerticalAlignment.Center);

        // 2. Верхний уровень: Продольная ось стопы с трансформацией угла
        float rotationDeg = State switch
        {
            TibialTorsion.UnderRotationInward => -18f,
            TibialTorsion.OverRotationOutward => 22f,
            _ => 3f
        };

        canvas.Rotate(rotationDeg, cx, cy);

        // Отрисовка контура стопы
        var footColor = State switch
        {
            TibialTorsion.Normal => Color.FromArgb("#38BDF8"),
            TibialTorsion.UnderRotationInward => Color.FromArgb("#F59E0B"),
            TibialTorsion.OverRotationOutward => Color.FromArgb("#EF4444"),
            _ => Color.FromArgb("#38BDF8")
        };

        canvas.StrokeColor = footColor;
        canvas.StrokeSize = 2.5f;

        var footPath = new PathF();
        footPath.MoveTo(cx - 20, cy - 65);
        footPath.LineTo(cx + 20, cy - 65);
        footPath.LineTo(cx + 25, cy + 45);
        footPath.LineTo(cx - 25, cy + 45);
        footPath.Close();
        canvas.DrawPath(footPath);

        // Вектор скручивания
        canvas.StrokeColor = State == TibialTorsion.OverRotationOutward ? Color.FromArgb("#EF4444") : Color.FromArgb("#10B981");
        canvas.StrokeSize = 2.0f;
        canvas.DrawLine(cx, cy + 45, cx, cy - 80);

        // Стрелка вектора
        canvas.FillColor = State == TibialTorsion.OverRotationOutward ? Color.FromArgb("#EF4444") : Color.FromArgb("#10B981");
        canvas.FillCircle(cx, cy - 80, 4);

        canvas.RestoreState();
    }
}
