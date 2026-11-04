using System.Windows.Media;

namespace MonitorMate.Core.ViewModels;

public sealed class NavItem
{
    public NavItem(string key, string title, Geometry icon, object page)
    {
        Key = key;
        Title = title;
        Icon = icon;
        Page = page;
    }

    public string Key { get; }
    public string Title { get; }
    public Geometry Icon { get; }
    public object Page { get; }
}
