using System.Windows;
using System.Windows.Media;

namespace MonitorMate.Controls;

/// <summary>
/// A compact 24-hour light rail. The segments are the real schedule, not decoration.
/// </summary>
public sealed class DayRibbon : FrameworkElement
{
    public static readonly DependencyProperty DayStartProperty = DependencyProperty.Register(
        nameof(DayStart), typeof(double), typeof(DayRibbon), new FrameworkPropertyMetadata(420.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty EveningStartProperty = DependencyProperty.Register(
        nameof(EveningStart), typeof(double), typeof(DayRibbon), new FrameworkPropertyMetadata(1140.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NightStartProperty = DependencyProperty.Register(
        nameof(NightStart), typeof(double), typeof(DayRibbon), new FrameworkPropertyMetadata(1350.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty NowProperty = DependencyProperty.Register(
        nameof(Now), typeof(double), typeof(DayRibbon), new FrameworkPropertyMetadata(720.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double DayStart { get => (double)GetValue(DayStartProperty); set => SetValue(DayStartProperty, value); }
    public double EveningStart { get => (double)GetValue(EveningStartProperty); set => SetValue(EveningStartProperty, value); }
    public double NightStart { get => (double)GetValue(NightStartProperty); set => SetValue(NightStartProperty, value); }
    public double Now { get => (double)GetValue(NowProperty); set => SetValue(NowProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        if (width < 1) return;

        var track = new Rect(0, 7, width, 12);
        var trackBrush = ResourceBrush("SurfaceRaisedBrush", Brushes.DimGray);
        dc.DrawRoundedRectangle(trackBrush, null, track, 6, 6);
        dc.PushClip(new RectangleGeometry(track, 6, 6));

        DrawRange(dc, width, 0, DayStart, ResourceBrush("SlateBrush", Brushes.SteelBlue));
        DrawRange(dc, width, DayStart, EveningStart, ResourceBrush("SandBrush", Brushes.Goldenrod));
        DrawRange(dc, width, EveningStart, NightStart, ResourceBrush("AmberBrush", Brushes.OrangeRed));
        DrawRange(dc, width, NightStart, 1440, ResourceBrush("SlateBrush", Brushes.SteelBlue));
        dc.Pop();

        var x = width * Math.Clamp(Now / 1440.0, 0, 1);
        var marker = ResourceBrush("TextPrimaryBrush", Brushes.White);
        dc.DrawLine(new Pen(marker, 1.2), new Point(x, 2), new Point(x, 24));
        dc.DrawEllipse(ResourceBrush("AmberBrush", Brushes.OrangeRed), null, new Point(x, 13), 3.6, 3.6);
    }

    private static void DrawRange(DrawingContext dc, double width, double from, double to, Brush brush)
    {
        var start = Math.Clamp(from, 0, 1440);
        var end = Math.Clamp(to, 0, 1440);
        if (end <= start) return;
        dc.DrawRectangle(brush, null, new Rect(width * start / 1440.0, 7, width * (end - start) / 1440.0, 12));
    }

    private static Brush ResourceBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? fallback;
}
