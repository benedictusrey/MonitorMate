using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MonitorMate.Reminders;
namespace MonitorMate.Desktop;

public partial class MainWindow : Window
{
    private readonly ReminderEngine _engine = new();
    private readonly PreviewStore _store;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private long _previousTick;
    private int _savedBreaks;
    private int _initialTotal;
    private PreviewSettings _savedSettings = new();
    private Window? _breakWindow;
    private TextBlock? _breakCountdown;
    private bool _closingBreak;
    public ReminderEngine Engine => _engine;

    public MainWindow() : this(new PreviewStore()) { }
    public MainWindow(PreviewStore store)
    {
        InitializeComponent();
        _store = store;
        var state = _savedSettings = store.Load();
        WorkMinutesInput.Value = state.WorkMinutes; BreakSecondsInput.Value = state.BreakSeconds;
        _initialTotal = state.CompletedBreaks;
        if (store.Warning is { } warning) StatusText.Text = warning;
        _timer.Tick += (_, _) => Tick();
        Closed += (_, _) => { _timer.Stop(); CloseBreak(); };
        Refresh();
    }
    private (int Work, int BreakSeconds) ReadRhythm()
    {
        var work = WorkMinutesInput.Value ?? 0; var rest = BreakSecondsInput.Value ?? 0;
        if (work != decimal.Truncate(work) || rest != decimal.Truncate(rest))
            throw new ArgumentException("Use whole minutes and seconds.");
        ReminderEngine.Validate((int)work,(int)rest);
        return ((int)work,(int)rest);
    }
    private void Start_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var rhythm = ReadRhythm(); _engine.Start(rhythm.Work,rhythm.BreakSeconds); CloseBreak();
            _previousTick = Stopwatch.GetTimestamp(); _timer.Start();
            StatusText.Text = "Reminders are running. You can pause or stop at any time.";
            SaveRhythm(); Refresh();
        }
        catch (ArgumentException) { StatusText.Text = "Enter whole work minutes from 1 to 240 and break seconds from 5 to 600."; }
    }
    private void Pause_Click(object? sender, RoutedEventArgs e)
    {
        if (_engine.IsPaused) { _engine.Resume(); _previousTick=Stopwatch.GetTimestamp(); }
        else _engine.Pause();
        Refresh();
    }
    private void Stop_Click(object? sender, RoutedEventArgs e)
    {
        _engine.Stop(); _timer.Stop(); CloseBreak(); StatusText.Text="Reminders stopped. Start again when you are ready."; Refresh();
    }
    private void Break_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var rhythm=ReadRhythm();
            if (_engine.Phase==ReminderPhase.Stopped) _engine.Start(rhythm.Work,rhythm.BreakSeconds);
            _engine.TakeBreak(rhythm.BreakSeconds);
            _previousTick=Stopwatch.GetTimestamp(); _timer.Start(); ShowBreak(); Refresh();
        }
        catch (ArgumentException) { StatusText.Text="Choose a valid break length in Rhythm first."; }
    }
    private void Save_Click(object? sender, RoutedEventArgs e) => SaveRhythm();
    private void SaveRhythm()
    {
        try
        {
            var rhythm=ReadRhythm();
            var settings = new PreviewSettings(rhythm.Work,rhythm.BreakSeconds,_initialTotal+_engine.CompletedBreaks);
            _store.Save(settings); _savedSettings = settings;
            StatusText.Text = _engine.Phase==ReminderPhase.Stopped ? "Rhythm saved." : "Rhythm saved. Restart reminders to apply any changes.";
        }
        catch (ArgumentException) { StatusText.Text="Enter whole work minutes from 1 to 240 and break seconds from 5 to 600."; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { StatusText.Text="Could not save settings. Reminders still work for this session."; }
    }
    private void ShortRhythm_Click(object? sender, RoutedEventArgs e) { WorkMinutesInput.Value=20; BreakSecondsInput.Value=20; }
    private void Pomodoro_Click(object? sender, RoutedEventArgs e) { WorkMinutesInput.Value=25; BreakSecondsInput.Value=300; }
    private void DeepWork_Click(object? sender, RoutedEventArgs e) { WorkMinutesInput.Value=50; BreakSecondsInput.Value=300; }
    private void Tick()
    {
        var now=Stopwatch.GetTimestamp(); var elapsed=Stopwatch.GetElapsedTime(_previousTick,now); _previousTick=now;
        if (elapsed>TimeSpan.FromSeconds(5))
        { _engine.Pause(); StatusText.Text="Reminders paused after a delayed tick or sleep. Choose Resume when you return."; }
        else _engine.Advance(elapsed);
        if (_engine.Phase==ReminderPhase.Break) ShowBreak(); else CloseBreak();
        if (_savedBreaks!=_engine.CompletedBreaks)
        {
            _savedBreaks=_engine.CompletedBreaks;
            try { _store.Save(_savedSettings with { CompletedBreaks=_initialTotal+_engine.CompletedBreaks }); }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException)
            { StatusText.Text="Could not save the completed-break total. This session still shows it."; }
        }
        Refresh();
    }
    private void Refresh()
    {
        var remaining = Math.Max(0,(int)Math.Ceiling(_engine.Remaining.TotalSeconds));
        var text=$"{remaining/60:00}:{remaining%60:00}";
        CountdownText.Text=_engine.Phase==ReminderPhase.Stopped ? "Ready" : text;
        if (_breakCountdown is not null) _breakCountdown.Text=text;
        PhaseText.Text=_engine.IsPaused ? "Paused" : _engine.Phase switch { ReminderPhase.Working=>"Time for your work", ReminderPhase.Break=>"Time for a break", _=>"Ready to begin" };
        PauseButton.Content=_engine.IsPaused ? "Resume" : "Pause";
        PauseButton.IsEnabled=StopButton.IsEnabled=_engine.Phase!=ReminderPhase.Stopped;
        var completed=_initialTotal+_engine.CompletedBreaks;
        CompletedText.Text=$"{completed} {(completed==1 ? "break" : "breaks")} completed locally";
        TimeProgress.Value=_engine.Phase==ReminderPhase.Stopped ? 0 : 1-Math.Clamp(_engine.Remaining.TotalSeconds/_engine.Duration.TotalSeconds,0,1);
        Title="MonitorMate · "+(_engine.Phase==ReminderPhase.Stopped ? "Reminder preview" : PhaseText.Text+" · "+text);
    }
    private void ShowBreak()
    {
        if (_breakWindow is not null) return;
        _breakCountdown=new TextBlock { FontSize=40, Text="Break", HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Center };
        var skip=new Button { Content="Skip this break", HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Center };
        skip.Click+=(_,_) => { _engine.SkipBreak(); CloseBreak(); Refresh(); };
        _breakWindow=new Window { Icon=Icon, Title="MonitorMate · Take a break", Width=420, Height=290, CanResize=false, Topmost=true,
            Content=new StackPanel { Margin=new Thickness(28), Spacing=18, Children={new TextBlock { Text="Look away. Rest your eyes.", FontSize=22, TextWrapping=Avalonia.Media.TextWrapping.Wrap },_breakCountdown,new TextBlock { Text="You can skip any break. Esc also closes it.", TextWrapping=Avalonia.Media.TextWrapping.Wrap },skip} } };
        _breakWindow.KeyDown+=(_,e)=> { if(e.Key==Avalonia.Input.Key.Escape) _breakWindow?.Close(); };
        _breakWindow.Closed+=(_,_)=> { if(!_closingBreak) { _engine.SkipBreak(); _breakWindow=null; _breakCountdown=null; Refresh(); } };
        _breakWindow.Show();
    }
    private void CloseBreak()
    {
        if (_breakWindow is null) return;
        _closingBreak=true; _breakWindow.Close(); _breakWindow=null; _breakCountdown=null; _closingBreak=false;
    }
}
