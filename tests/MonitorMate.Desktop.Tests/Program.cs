using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using MonitorMate.Desktop;
using MonitorMate.Reminders;
using Avalonia.VisualTree;

internal static class Program
{
    private static int _checks, _failed;
    private static void Check(string name, Action test) { _checks++; try { test(); Console.WriteLine("PASS "+name); } catch(Exception e) { _failed++; Console.WriteLine("FAIL "+name+": "+e); } }
    private static void Require(bool value, string message) { if(!value) throw new Exception(message); }
    [STAThread]
    public static int Main()
    {
        AppBuilder.Configure<App>().UseSkia().WithInterFont().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing=false }).SetupWithoutStarting();
        var directory=Path.Combine(Path.GetTempPath(),"MonitorMate-desktop-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        Check("Settings round-trip and preserve totals",()=> { var store=new PreviewStore(Path.Combine(directory,"roundtrip.json")); store.Save(new(25,300,7)); Require(store.Load()==new PreviewSettings(25,300,7),"Settings did not round-trip."); });
        Check("Invalid settings preserve the original",()=> { var path=Path.Combine(directory,"invalid.json"); File.WriteAllText(path,"{bad json"); var store=new PreviewStore(path); Require(store.Load()==new PreviewSettings() && store.Warning is not null,"Invalid settings did not recover."); Require(Directory.GetFiles(directory,"invalid.json.corrupt-*").Length==1,"No corrupt-settings backup."); });
        using var window = new WindowHolder(new MainWindow(new PreviewStore(Path.Combine(directory,"ui.json"))));
        var main=window.Window; main.Show(); Dispatcher.UIThread.RunJobs();
        Button Button(string name)=>main.FindControl<Button>(name)!;
        void Click(string name)
        {
            var button=Button(name); var point=button.TranslatePoint(new Point(button.Bounds.Width/2,button.Bounds.Height/2),main)??throw new Exception("No hit-test position.");
            main.MouseDown(point,MouseButton.Left); main.MouseUp(point,MouseButton.Left); Dispatcher.UIThread.RunJobs();
        }
        Check("Pointer starts, pauses, resumes and stops reminders",()=> {
            Require(main.Engine.Phase==ReminderPhase.Stopped,"Reminders started without consent.");
            Click("StartButton"); Require(main.Engine.Phase==ReminderPhase.Working,"Start click failed.");
            Require(main.FindControl<TextBlock>("StatusText")!.Text!.Contains("running"),"Start incorrectly asks the user to restart.");
            Click("PauseButton"); Require(main.Engine.IsPaused,"Pause click failed.");
            Click("PauseButton"); Require(!main.Engine.IsPaused,"Resume click failed.");
            Click("StopButton"); Require(main.Engine.Phase==ReminderPhase.Stopped,"Stop click failed.");
        });
        Check("Keyboard activates Start",()=> { Button("StartButton").Focus(); main.KeyPressQwerty(PhysicalKey.Space,RawInputModifiers.None); main.KeyReleaseQwerty(PhysicalKey.Space,RawInputModifiers.None); Require(main.Engine.Phase==ReminderPhase.Working,"Space did not activate Start."); Click("StopButton"); });
        Check("Manual break stays skippable",()=> { Click("BreakButton"); Require(main.Engine.Phase==ReminderPhase.Break,"Break did not open."); Click("StopButton"); Require(main.Engine.CompletedBreaks==0,"Stopped break was counted."); });
        Check("Pending rhythm edits do not alter running progress",()=> {
            Click("StartButton"); main.FindControl<NumericUpDown>("WorkMinutesInput")!.Value=60;
            typeof(MainWindow).GetMethod("Tick",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(main,null);
            Require(main.FindControl<ProgressBar>("TimeProgress")!.Value<0.01,"Pending settings changed active timer progress.");
            Click("StopButton"); main.FindControl<NumericUpDown>("WorkMinutesInput")!.Value=20;
        });
        Check("Manual break uses the chosen work rhythm before Start",()=> {
            Click("StopButton"); main.FindControl<NumericUpDown>("WorkMinutesInput")!.Value=25; Click("BreakButton");
            main.Engine.SkipBreak(); Require(main.Engine.Remaining==TimeSpan.FromMinutes(25),"Manual break returned to the wrong work interval.");
            Click("StopButton"); main.FindControl<NumericUpDown>("WorkMinutesInput")!.Value=20;
        });
        Check("Completed totals save even while a rhythm field is empty",()=> {
            main.Engine.Start(20,20); main.Engine.TakeBreak(20);
            main.FindControl<NumericUpDown>("WorkMinutesInput")!.Value=null;
            main.Engine.Advance(TimeSpan.FromSeconds(20));
            typeof(MainWindow).GetMethod("Tick",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(main,null);
            Require(new PreviewStore(Path.Combine(directory,"ui.json")).Load().CompletedBreaks==1,"Unsaved field blocked completed-break persistence.");
            main.FindControl<NumericUpDown>("WorkMinutesInput")!.Value=20; Click("StopButton");
        });
        var output=Path.Combine(Environment.CurrentDirectory,"artifacts","portable-ui");Directory.CreateDirectory(output);
        Check("Every page renders at minimum and default size",()=> {
            foreach(var size in new[]{new Size(680,590),new Size(820,700),new Size(1100,800)})
            {
                main.Width=size.Width;main.Height=size.Height;
                for(var page=0;page<3;page++)
                {
                    main.FindControl<TabControl>("Pages")!.SelectedIndex=page;Dispatcher.UIThread.RunJobs();
                    using var frame=main.CaptureRenderedFrame(); Require(frame is not null,"No rendered frame.");
                    frame!.Save(Path.Combine(output,$"page-{page}-{size.Width}.png"),Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                    Require(main.GetVisualDescendants().OfType<TextBlock>().All(t=>t.FontSize>=11),"Text below minimum size.");
                }
            }
        });
        Console.WriteLine($"{_checks} portable UI/persistence checks; {_failed} failures. Headless input is not native OS acceptance.");
        return _failed==0?0:1;
    }
    private sealed class WindowHolder(MainWindow window) : IDisposable { public MainWindow Window => window; public void Dispose()=>window.Close(); }
}
