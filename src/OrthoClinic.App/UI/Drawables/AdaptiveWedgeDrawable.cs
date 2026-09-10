namespace OrthoClinic.UI.Drawables;

using Microsoft.Maui.Graphics;

/// <summary>
/// Векторный канвас Теста 5: Профиль адаптивного клина и интерференция стоячей волны при торсионном конфликте.
/// </summary>
public sealed class AdaptiveWedgeDrawable : IDrawable
{
    public bool HasConflict { get; set; }
    public double ActivityRatio { get; set; } = 1.0;
    public double Phase { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.Antialias = true;

        float dynamicHeight = (float)(40f * ActivityRatio);
        float bottomY = dirtyRect.Height - 15f;

        // Профиль адаптивного клина
        var wedge = new PathF();
        wedge.MoveTo(20, bottomY);
        wedge.LineTo(dirtyRect.Width - 20, bottomY);
        wedge.LineTo(20, bottomY - dynamicHeight);
        wedge.Close();

        canvas.FillColor = Color.FromArgb("#1E293B");
        canvas.FillPath(wedge);
        canvas.StrokeColor = Color.FromArgb("#38BDF8");
        canvas.StrokeSize = 1.5f;
        canvas.DrawPath(wedge);

        // Информационная подпись адаптивной высоты
        canvas.FontColor = Color.FromArgb("#94A3B8");
        canvas.FontSize = 10;
        canvas.DrawString($"Адаптивная толщина: {ActivityRatio * 100:F0}%", 25, bottomY - dynamicHeight - 12, 180, 14, HorizontalAlignment.Left, VerticalAlignment.Center);

        // Отрисовка интерференционной стоячей волны стресса при конфликте
        if (HasConflict)
        {
            var wavePath = new PathF();
            float startX = 20;
            float endX = dirtyRect.Width - 20;
            float midY = bottomY - (dynamicHeight / 2f);
            float k = 0.06f;
            float amp = 10f;

            for (float x = startX; x <= endX; x += 2f)
            {
                double wFemur = amp * Math.Sin(k * x - Phase);
                double wTibia = amp * Math.Sin(k * x + Phase + Math.PI);
                float y = midY + (float)(wFemur + wTibia);

                if (x == startX) wavePath.MoveTo(x, y);
                else wavePath.LineTo(x, y);
            }

            canvas.StrokeColor = Color.FromArgb("#EF4444");
            canvas.StrokeSize = 2.5f;
            canvas.DrawPath(wavePath);

            // Очаг максимального деструктивного момента
            canvas.FillColor = Color.FromArgb("#DC2626");
            canvas.FillCircle(dirtyRect.Center.X, midY, 6);

            canvas.FontColor = Color.FromArgb("#FCA5A5");
            canvas.FontSize = 9;
            canvas.DrawString("Очаг деструктивного момента W_femur + W_tibia", dirtyRect.Center.X - 120, midY - 18, 240, 14, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        canvas.RestoreState();
    }
}
