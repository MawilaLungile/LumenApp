using HA = Microsoft.Maui.Graphics.HorizontalAlignment;
using VA = Microsoft.Maui.Graphics.VerticalAlignment;
using GFont = Microsoft.Maui.Graphics.Font;

namespace Lumen.Charts;

/// <summary>One segment of a donut chart.</summary>
public sealed record ChartSlice(string Label, double Value, Color Color);

/// <summary>
/// Base class for hand-drawn charts. Each chart is a <see cref="GraphicsView"/> that draws
/// itself and animates a 0→1 "reveal" value whenever its data changes. No third-party
/// charting library required.
/// </summary>
public abstract class AnimatedChartView : GraphicsView, IDrawable
{
    private const string AnimationName = "ChartReveal";

    /// <summary>Animation progress from 0 to 1.</summary>
    protected float Reveal { get; private set; } = 1f;

    protected AnimatedChartView()
    {
        Drawable = this;
        BackgroundColor = Colors.Transparent;
    }

    protected void Replay(uint length = 900)
    {
        // Animations need a handler (i.e. the view must be on screen).
        if (Handler is null)
        {
            Reveal = 1f;
            Invalidate();
            return;
        }

        try
        {
            this.AbortAnimation(AnimationName);
            var animation = new Animation(v =>
            {
                Reveal = (float)v;
                Invalidate();
            }, 0, 1, Easing.CubicOut);
            animation.Commit(this, AnimationName, 16, length);
        }
        catch
        {
            Reveal = 1f;
            Invalidate();
        }
    }

    protected static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AnimatedChartView)bindable).Invalidate();

    protected static void OnDataPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((AnimatedChartView)bindable).Replay();

    public abstract void Draw(ICanvas canvas, RectF dirtyRect);
}

// =====================================================================================
//  Bar chart
// =====================================================================================

public class BarChartView : AnimatedChartView
{
    public static readonly BindableProperty ValuesProperty = BindableProperty.Create(
        nameof(Values), typeof(IList<double>), typeof(BarChartView), null, propertyChanged: OnDataPropertyChanged);

    public static readonly BindableProperty LabelsProperty = BindableProperty.Create(
        nameof(Labels), typeof(IList<string>), typeof(BarChartView), null, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty BarColorProperty = BindableProperty.Create(
        nameof(BarColor), typeof(Color), typeof(BarChartView), Color.FromArgb("#7C5CFF"), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty HighlightColorProperty = BindableProperty.Create(
        nameof(HighlightColor), typeof(Color), typeof(BarChartView), Color.FromArgb("#00D4AA"), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(BarChartView), Color.FromArgb("#1A7C5CFF"), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(BarChartView), Colors.Gray, propertyChanged: OnVisualPropertyChanged);

    public IList<double>? Values { get => (IList<double>?)GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }
    public IList<string>? Labels { get => (IList<string>?)GetValue(LabelsProperty); set => SetValue(LabelsProperty, value); }
    public Color BarColor { get => (Color)GetValue(BarColorProperty); set => SetValue(BarColorProperty, value); }
    public Color HighlightColor { get => (Color)GetValue(HighlightColorProperty); set => SetValue(HighlightColorProperty, value); }
    public Color TrackColor { get => (Color)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }

    public override void Draw(ICanvas canvas, RectF rect)
    {
        var values = Values;
        if (values is null || values.Count == 0) return;

        const float top = 22f;
        const float labelHeight = 22f;
        var chartHeight = rect.Height - top - labelHeight;
        if (chartHeight <= 0) return;

        var max = values.Max();
        var maxIndex = values.IndexOf(max);
        if (max <= 0) max = 1;

        var count = values.Count;
        var slot = rect.Width / count;
        var barWidth = Math.Min(slot * 0.5f, 26f);
        var labels = Labels;

        for (var i = 0; i < count; i++)
        {
            var x = rect.Left + slot * i + (slot - barWidth) / 2f;

            // Track
            canvas.FillColor = TrackColor;
            canvas.FillRoundedRectangle(x, rect.Top + top, barWidth, chartHeight, barWidth / 2f);

            // Bar
            var h = (float)(values[i] / max) * chartHeight * Reveal;
            if (h > 0.5f)
            {
                var y = rect.Top + top + chartHeight - h;
                var color = i == maxIndex ? HighlightColor : BarColor;

                var paint = new LinearGradientPaint
                {
                    StartColor = color,
                    EndColor = color.WithAlpha(0.55f),
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };
                canvas.SetFillPaint(paint, new RectF(x, y, barWidth, h));
                canvas.FillRoundedRectangle(x, y, barWidth, h, Math.Min(barWidth / 2f, h / 2f));

                // Value bubble over the highest bar
                if (i == maxIndex && values[i] > 0 && Reveal > 0.95f)
                {
                    canvas.Font = GFont.DefaultBold;
                    canvas.FontSize = 11;
                    canvas.FontColor = TextColor;
                    canvas.DrawString(Money.Compact(values[i]), rect.Left + slot * i - 20, y - 20, slot + 40, 18, HA.Center, VA.Center);
                }
            }

            // X-axis label
            if (labels is not null && i < labels.Count)
            {
                canvas.Font = i == count - 1 ? GFont.DefaultBold : GFont.Default;
                canvas.FontSize = 11;
                canvas.FontColor = TextColor;
                canvas.DrawString(labels[i], rect.Left + slot * i, rect.Bottom - labelHeight, slot, labelHeight, HA.Center, VA.Center);
            }
        }
    }
}

// =====================================================================================
//  Donut chart
// =====================================================================================

public class DonutChartView : AnimatedChartView
{
    public static readonly BindableProperty SlicesProperty = BindableProperty.Create(
        nameof(Slices), typeof(IList<ChartSlice>), typeof(DonutChartView), null, propertyChanged: OnDataPropertyChanged);

    public static readonly BindableProperty RingThicknessProperty = BindableProperty.Create(
        nameof(RingThickness), typeof(float), typeof(DonutChartView), 18f, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(DonutChartView), Color.FromArgb("#1A7C5CFF"), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty CenterTextProperty = BindableProperty.Create(
        nameof(CenterText), typeof(string), typeof(DonutChartView), string.Empty, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty CenterSubTextProperty = BindableProperty.Create(
        nameof(CenterSubText), typeof(string), typeof(DonutChartView), string.Empty, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(DonutChartView), Colors.Black, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty SubTextColorProperty = BindableProperty.Create(
        nameof(SubTextColor), typeof(Color), typeof(DonutChartView), Colors.Gray, propertyChanged: OnVisualPropertyChanged);

    public IList<ChartSlice>? Slices { get => (IList<ChartSlice>?)GetValue(SlicesProperty); set => SetValue(SlicesProperty, value); }
    public float RingThickness { get => (float)GetValue(RingThicknessProperty); set => SetValue(RingThicknessProperty, value); }
    public Color TrackColor { get => (Color)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }
    public string CenterText { get => (string)GetValue(CenterTextProperty); set => SetValue(CenterTextProperty, value); }
    public string CenterSubText { get => (string)GetValue(CenterSubTextProperty); set => SetValue(CenterSubTextProperty, value); }
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public Color SubTextColor { get => (Color)GetValue(SubTextColorProperty); set => SetValue(SubTextColorProperty, value); }

    public override void Draw(ICanvas canvas, RectF rect)
    {
        var size = Math.Min(rect.Width, rect.Height);
        var t = RingThickness;
        var x = rect.Center.X - size / 2f + t / 2f;
        var y = rect.Center.Y - size / 2f + t / 2f;
        var d = size - t;

        canvas.StrokeSize = t;
        canvas.StrokeColor = TrackColor;
        canvas.DrawEllipse(x, y, d, d);

        var slices = Slices;
        var total = slices?.Sum(s => s.Value) ?? 0;

        if (slices is not null && total > 0)
        {
            canvas.StrokeLineCap = LineCap.Butt;
            var visibleSweep = 360f * Reveal;
            var start = 90f;      // 12 o'clock
            var consumed = 0f;

            foreach (var slice in slices)
            {
                var sweep = (float)(slice.Value / total) * 360f;
                var visible = Math.Clamp(visibleSweep - consumed, 0f, sweep);
                if (visible <= 0) break;

                var gap = slices.Count > 1 && visible > 4f ? 2f : 0f;
                canvas.StrokeColor = slice.Color;

                if (visible >= 359.5f)
                    canvas.DrawEllipse(x, y, d, d);
                else
                    canvas.DrawArc(x, y, d, d, start, start - (visible - gap), true, false);

                start -= sweep;
                consumed += sweep;
            }
        }

        // Centre labels
        if (!string.IsNullOrEmpty(CenterText))
        {
            canvas.Font = GFont.DefaultBold;
            canvas.FontSize = size > 140 ? 17 : 14;
            canvas.FontColor = TextColor;
            canvas.DrawString(CenterText, rect.Left, rect.Center.Y - 22, rect.Width, 24, HA.Center, VA.Center);
        }

        if (!string.IsNullOrEmpty(CenterSubText))
        {
            canvas.Font = GFont.Default;
            canvas.FontSize = 11;
            canvas.FontColor = SubTextColor;
            canvas.DrawString(CenterSubText, rect.Left, rect.Center.Y + 2, rect.Width, 18, HA.Center, VA.Center);
        }
    }
}

// =====================================================================================
//  Progress ring
// =====================================================================================

public class ProgressRingView : AnimatedChartView
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(ProgressRingView), 0d, propertyChanged: OnDataPropertyChanged);

    public static readonly BindableProperty RingColorProperty = BindableProperty.Create(
        nameof(RingColor), typeof(Color), typeof(ProgressRingView), Color.FromArgb("#7C5CFF"), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(ProgressRingView), Color.FromArgb("#1A7C5CFF"), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty RingThicknessProperty = BindableProperty.Create(
        nameof(RingThickness), typeof(float), typeof(ProgressRingView), 10f, propertyChanged: OnVisualPropertyChanged);

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public Color RingColor { get => (Color)GetValue(RingColorProperty); set => SetValue(RingColorProperty, value); }
    public Color TrackColor { get => (Color)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }
    public float RingThickness { get => (float)GetValue(RingThicknessProperty); set => SetValue(RingThicknessProperty, value); }

    public override void Draw(ICanvas canvas, RectF rect)
    {
        var size = Math.Min(rect.Width, rect.Height);
        var t = RingThickness;
        var x = rect.Center.X - size / 2f + t / 2f;
        var y = rect.Center.Y - size / 2f + t / 2f;
        var d = size - t;

        canvas.StrokeSize = t;
        canvas.StrokeColor = TrackColor;
        canvas.DrawEllipse(x, y, d, d);

        var progress = (float)Math.Clamp(Value, 0, 1) * Reveal;
        if (progress <= 0.001f) return;

        canvas.StrokeColor = RingColor;
        canvas.StrokeLineCap = LineCap.Round;

        if (progress >= 0.999f)
            canvas.DrawEllipse(x, y, d, d);
        else
            canvas.DrawArc(x, y, d, d, 90, 90 - 360 * progress, true, false);
    }
}

// =====================================================================================
//  Linear progress bar
// =====================================================================================

public class LinearProgressView : AnimatedChartView
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(double), typeof(LinearProgressView), 0d, propertyChanged: OnDataPropertyChanged);

    public static readonly BindableProperty BarColorProperty = BindableProperty.Create(
        nameof(BarColor), typeof(Color), typeof(LinearProgressView), Color.FromArgb("#7C5CFF"), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(LinearProgressView), Color.FromArgb("#1A7C5CFF"), propertyChanged: OnVisualPropertyChanged);

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public Color BarColor { get => (Color)GetValue(BarColorProperty); set => SetValue(BarColorProperty, value); }
    public Color TrackColor { get => (Color)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }

    public override void Draw(ICanvas canvas, RectF rect)
    {
        var radius = rect.Height / 2f;

        canvas.FillColor = TrackColor;
        canvas.FillRoundedRectangle(rect.X, rect.Y, rect.Width, rect.Height, radius);

        var width = rect.Width * (float)Math.Clamp(Value, 0, 1) * Reveal;
        if (width < 1) return;

        width = Math.Max(width, rect.Height); // keep the pill shape at tiny values
        var paint = new LinearGradientPaint
        {
            StartColor = BarColor.WithAlpha(0.7f),
            EndColor = BarColor,
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0)
        };
        canvas.SetFillPaint(paint, new RectF(rect.X, rect.Y, width, rect.Height));
        canvas.FillRoundedRectangle(rect.X, rect.Y, width, rect.Height, radius);
    }
}
