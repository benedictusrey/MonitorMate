using System.Text.Json;
using MonitorMate.Reminders;
namespace MonitorMate.Desktop;
public sealed record PreviewSettings(int WorkMinutes=20,int BreakSeconds=20,int CompletedBreaks=0);
public sealed class PreviewStore
{
    private readonly string _path;
    private bool _readOnly;
    public string? Warning { get; private set; }
    public PreviewStore(string? path=null) => _path=path??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MonitorMate","ReminderPreview","settings.json");
    public PreviewSettings Load()
    {
        if (!File.Exists(_path)) return new();
        try
        {
            var settings=JsonSerializer.Deserialize<PreviewSettings>(File.ReadAllText(_path))??throw new JsonException("Empty settings.");
            ReminderEngine.Validate(settings.WorkMinutes,settings.BreakSeconds);
            if (settings.CompletedBreaks<0) throw new JsonException("Invalid total.");
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            try { File.Copy(_path,_path+".corrupt-"+Guid.NewGuid().ToString("N")); Warning="Settings were unreadable. A backup was kept; reminders use safe defaults."; }
            catch(Exception backup) when (backup is IOException or UnauthorizedAccessException) { _readOnly=true; Warning="Settings could not be read or backed up. This session will not overwrite them."; }
            return new();
        }
    }
    public void Save(PreviewSettings settings)
    {
        if (_readOnly) throw new IOException("Settings backup was not possible.");
        ReminderEngine.Validate(settings.WorkMinutes,settings.BreakSeconds);
        if (settings.CompletedBreaks<0) throw new ArgumentOutOfRangeException(nameof(settings));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        var temporary=_path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try { File.WriteAllText(temporary,JsonSerializer.Serialize(settings)); File.Move(temporary,_path,true); }
        finally { if(File.Exists(temporary)) File.Delete(temporary); }
    }
}
