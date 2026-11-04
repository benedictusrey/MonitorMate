using System.Globalization;
using System.Windows;
using System.Windows.Media;
using MonitorMate.Core.Models;

namespace MonitorMate.Controls;

public sealed class WeeklyBars : FrameworkElement
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable<WeekPoint>), typeof(WeeklyBars),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<WeekPoint>? ItemsSource
    {
        get => (IEnumerable<WeekPoint>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var items = ItemsSource?.ToList() ?? new List<WeekPoint>();
        if (items.Count == 0)
        {
            return;
        }

        var width = ActualWidth;
        var height = ActualHeight;
        var baseline = height - 28;
        var chartHeight = Math.Max(10, baseline - 18);
        var slot = width / items.Count;
        var max = Math.Max(1, items.Max(i => i.ScreenMinutes));
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var barWidth = Math.Min(34, Math.Max(16, slot * 0.42));
            var x = i * slot + (slot - barWidth) / 2;
            var value = item.ScreenMinutes;
            var barHeight = chartHeight * Math.Max(0, value) / max;
            var y = baseline - barHeight;
            var brush = item.IsToday
                ? new SolidColorBrush(Color.FromRgb(0xE0, 0x7A, 0x5F))
                : new SolidColorBrush(Color.FromRgb(0x46, 0x55, 0x46));
            brush.Freeze();
            if (value > 0) drawingContext.DrawRoundedRectangle(brush, null, new Rect(x, y, barWidth, barHeight), 5, 5);

            if (value > 0)
            {
                var label = new FormattedText(
                    FormatMinutes(value),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI Variable Text"),
                    11,
                    new SolidColorBrush(Color.FromRgb(0xB7, 0xBF, 0xB2)),
                    pixelsPerDip);
                drawingContext.DrawText(label, new Point(x + (barWidth - label.Width) / 2, y - 17));
            }

            var day = new FormattedText(
                item.Label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Variable Text"),
                11,
                new SolidColorBrush(item.IsToday ? Color.FromRgb(0xE0, 0x7A, 0x5F) : Color.FromRgb(0x88, 0x93, 0x84)),
                pixelsPerDip);
            drawingContext.DrawText(day, new Point(i * slot + (slot - day.Width) / 2, baseline + 8));
        }

        var linePen = new Pen(new SolidColorBrush(Color.FromArgb(90, 0x2F, 0x3A, 0x2F)), 1);
        drawingContext.DrawLine(linePen, new Point(0, baseline + 1), new Point(width, baseline + 1));
    }

    private static string FormatMinutes(int minutes)
    {
        if (minutes < 60) return $"{minutes}m";
        return $"{minutes / 60}h";
    }
}
