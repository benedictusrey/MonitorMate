using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MonitorMate.Core.Services;
using MonitorMate.Core.ViewModels;
using MonitorMate.Views;

internal static class Program
{
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)
        { var child=VisualTreeHelper.GetChild(parent,i); yield return child; foreach(var item in Descendants(child)) yield return item; }
    }
    [STAThread]
    public static int Main()
    {
        Environment.SetEnvironmentVariable("MONITOR_MATE_DATA_DIR", Path.Combine(Path.GetTempPath(), "MonitorMate-ui-" + Guid.NewGuid().ToString("N")));
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        foreach (var name in new[] { "Colors", "Typography", "Converters", "Icons", "Controls" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/MonitorMate;component/Themes/{name}.xaml") });
        using var trace = new StringWriter();
        PresentationTraceSources.DataBindingSource.Listeners.Add(new TextWriterTraceListener(trace));
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        using var services = new AppServices(); services.Settings.Replace(new());
        using var main = new MainViewModel(services, () => { }, () => { }, () => { }, (_, _, _) => false, () => { });
        var shell = new MonitorMate.MainWindow(main);
        var pages = new (string Name, UserControl View, object Model)[] {
            ("today",new TodayView(),main.TodayPage),("displays",new DisplaysView(),main.DisplaysPage),
            ("comfort",new ComfortView(),main.ComfortPage),("breaks",new BreaksView(),main.BreaksPage),
            ("focus",new FocusView(),main.FocusPage),("activity",new ActivityView(),main.ActivityPage),
            ("settings",new SettingsView(),main.SettingsPage) };
        var output = Path.Combine(Environment.CurrentDirectory,"artifacts","ui-audit"); Directory.CreateDirectory(output);
        var failures = 0;
        if(shell.Foreground is not SolidColorBrush shellText || shellText.Color != ((SolidColorBrush)app.FindResource("TextPrimaryBrush")).Color)
        { failures++; Console.WriteLine("FAIL Main window does not apply its shared text color."); }
        foreach (var (name, view, model) in pages)
        {
            view.DataContext = model;
            var surface=new Border { Background=(Brush)app.FindResource("WindowBackgroundBrush"), Child=view, Width=800 };
            TextElement.SetForeground(surface,(Brush)app.FindResource("TextPrimaryBrush"));
            surface.Measure(new Size(800,double.PositiveInfinity));
            var height = Math.Ceiling(surface.DesiredSize.Height);
            surface.Arrange(new Rect(0,0,800,height)); surface.UpdateLayout();
            app.Dispatcher.Invoke(() => { },DispatcherPriority.ContextIdle);
            var bitmap = new RenderTargetBitmap(800,(int)height,96,96,PixelFormats.Pbgra32); bitmap.Render(surface);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(output,name+".png")); encoder.Save(file);
            Console.WriteLine($"RENDER {name}: 800 x {height}");
            if(name=="displays" && !Descendants(view).OfType<TextBlock>().Any(t=>t.Text=="No displays found"))
            { failures++; Console.WriteLine("FAIL Empty display page offers a selection instead of recovery."); }
            var small = Descendants(view).OfType<TextBlock>().Count(t => t.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(t.Text) && t.FontSize < 11);
            if (small > 0) { failures++; Console.WriteLine($"FAIL {name}: {small} labels below 11 DIPs."); }

        }
        var rejected = new MonitorMate.Core.Models.DisplayState
        {
            Info = new MonitorMate.Core.Models.DisplayInfo
            {
                Id="ui-only", DeviceName="ui-only", Name="Display 1", AdapterName="Simulated",
                Bounds=new(), WorkArea=new(), GammaSupported=true
            },
            Status="Colour control unavailable",
            LastError="Windows or the display driver rejected colour control. Use Windows Night light or monitor controls; administrator access is not required."
        };
        ((List<MonitorMate.Core.Models.DisplayState>)services.Comfort.States).Add(rejected);
        main.DisplaysPage.RebuildCards();
        var rejectedView = new DisplaysView { DataContext=main.DisplaysPage };
        rejectedView.Measure(new Size(800,double.PositiveInfinity));
        rejectedView.Arrange(new Rect(0,0,800,Math.Ceiling(rejectedView.DesiredSize.Height))); rejectedView.UpdateLayout();
        app.Dispatcher.Invoke(() => { },DispatcherPriority.ContextIdle);
        var status = Descendants(rejectedView).OfType<TextBlock>().FirstOrDefault(text => text.Text == rejected.Status);
        if(status is null || status.TextWrapping != TextWrapping.Wrap)
        { failures++; Console.WriteLine("FAIL Rejected display status is missing or cannot wrap."); }
        Console.WriteLine("RENDER displays with rejected driver: " + rejectedView.ActualHeight);
        foreach (var style in new[] { "FlatButton", "PrimaryButton", "SecondaryButton", "DangerButton", "IconButton" })
        {
            var button = new Button { Content="Test", Style=(Style)app.FindResource(style), IsEnabled=false };
            button.Measure(new Size(200,50)); button.Arrange(new Rect(0,0,200,50)); button.ApplyTemplate();
            foreach(var label in Descendants(button).OfType<TextBlock>())
                if(label.Foreground is SolidColorBrush text && button.Foreground is SolidColorBrush intended && text.Color!=intended.Color)
                { failures++; Console.WriteLine($"FAIL {style}: label overrides its intended foreground."); }
            var chrome=(Border)button.Template.FindName("Chrome",button);
            if(chrome.Opacity >= 1) { failures++; Console.WriteLine($"FAIL {style}: disabled button has no visual feedback."); }
        }
        var zeroChart = new MonitorMate.Controls.WeeklyBars { Width=140, Height=100, ItemsSource=new[] { new MonitorMate.Core.Models.WeekPoint { Label="Mon", DateLabel="Monday", ScreenMinutes=0 } } };
        zeroChart.Measure(new Size(140,100)); zeroChart.Arrange(new Rect(0,0,140,100));
        var zeroBitmap=new RenderTargetBitmap(140,100,96,96,PixelFormats.Pbgra32); zeroBitmap.Render(zeroChart);
        var pixel=new byte[4]; zeroBitmap.CopyPixels(new Int32Rect(70,70,1,1),pixel,4,0);
        if(pixel[3]!=0) { failures++; Console.WriteLine("FAIL Zero activity draws a nonzero bar."); }
        Console.Write(trace.ToString());
        return trace.ToString().Length == 0 && failures == 0 ? 0 : 1;
    }
}

