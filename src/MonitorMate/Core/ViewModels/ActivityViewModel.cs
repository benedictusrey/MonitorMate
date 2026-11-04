using System.Windows.Input;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.Services;

namespace MonitorMate.Core.ViewModels;

public sealed class ActivityViewModel : ObservableObject
{
    private readonly AppServices _services;
    private readonly Action _requestReset;
    private IReadOnlyList<WeekPoint> _weekPoints = Array.Empty<WeekPoint>();
    private string _weekRange = string.Empty;
    private string _totalText = "0 m";
    private string _averageText = "0 m";
    private string _focusText = "0 m";
    private string _adherenceText = "No breaks yet";
    private string _bestDayText = "No data yet";
    private bool _hasData;

    public ActivityViewModel(AppServices services, Action requestReset)
    {
        _services = services;
        _requestReset = requestReset;
        RefreshCommand = new RelayCommand(Refresh);
        ResetHistoryCommand = new RelayCommand(() => _requestReset());
        Refresh();
    }

    public ICommand RefreshCommand { get; }
    public ICommand ResetHistoryCommand { get; }
    public IReadOnlyList<WeekPoint> WeekPoints { get => _weekPoints; private set => SetProperty(ref _weekPoints, value); }
    public string WeekRange { get => _weekRange; private set => SetProperty(ref _weekRange, value); }
    public string TotalText { get => _totalText; private set => SetProperty(ref _totalText, value); }
    public string AverageText { get => _averageText; private set => SetProperty(ref _averageText, value); }
    public string FocusText { get => _focusText; private set => SetProperty(ref _focusText, value); }
    public string AdherenceText { get => _adherenceText; private set => SetProperty(ref _adherenceText, value); }
    public string BestDayText { get => _bestDayText; private set => SetProperty(ref _bestDayText, value); }
    public bool HasData { get => _hasData; private set => SetProperty(ref _hasData, value); }

    public void Refresh()
    {
        var week = _services.Usage.GetLastWeek();
        WeekPoints = week;
        var first = DateTime.Today.AddDays(-6);
        WeekRange = $"{first:d MMM} – {DateTime.Today:d MMM}";
        var total = week.Sum(w => w.ScreenMinutes);
        var completed = week.Sum(w => w.BreaksCompleted);
        var skipped = week.Sum(w => w.BreaksSkipped);
        var focus = week.Sum(w => w.FocusMinutes);
        HasData = total > 0 || completed > 0 || focus > 0;
        TotalText = FormatMinutes(total);
        AverageText = FormatMinutes((int)Math.Round(total / 7.0));
        FocusText = FormatMinutes(focus);
        AdherenceText = completed + skipped == 0
            ? "No breaks yet"
            : $"{completed} of {completed + skipped} completed";
        var best = week.OrderByDescending(w => w.ScreenMinutes).FirstOrDefault();
        BestDayText = best is null || best.ScreenMinutes <= 0 ? "No data yet" : $"{best.Label}: {FormatMinutes(best.ScreenMinutes)}";
    }

    private static string FormatMinutes(int minutes)
    {
        if (minutes < 60) return $"{minutes} m";
        return $"{minutes / 60} h {minutes % 60} m";
    }
}
