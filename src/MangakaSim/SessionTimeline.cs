using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace MangakaSim;
/// <summary>
/// T1.10 session timeline (Q32): one plain-text line per notable moment of a play session, kept next to the saves
/// and attached to the problem report only when the player leaves the box ticked. Never records names, typed text,
/// file paths or hardware identifiers. Presentation-side only: it never touches GameState or the save format.
/// Every write failure is swallowed so a locked or read-only folder can never interrupt play.
/// </summary>
public sealed class SessionTimeline(string folder,Func<DateTime> now,long capBytes=1_000_000)
{
    public const string FileName="timeline.log",OldFileName="timeline.old.log";
    public string Folder { get; } = folder;
    private readonly DateTime _started=now();
    private bool _ended;
    public void Start(string details)=>Record(null,"session start "+details);
    public void End(){if(_ended)return;_ended=true;Record(null,"session end");}
    public void Record(DateTime? gameDate,string text)
    {
        var at=now();var minutes=Math.Max(0,(int)(at-_started).TotalMinutes);
        var line=string.Create(CultureInfo.InvariantCulture,$"{at:yyyy-MM-dd HH:mm:ss} | +{minutes} | {(gameDate is {} d?d.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture):"-")} | {Flatten(text)}\r\n");
        try
        {
            Directory.CreateDirectory(Folder);
            var path=Path.Combine(Folder,FileName);var bytes=Encoding.UTF8.GetBytes(line);
            if(File.Exists(path)&&new FileInfo(path).Length+bytes.Length>capBytes)File.Move(path,Path.Combine(Folder,OldFileName),true);
            using var stream=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read);stream.Write(bytes);
        }
        catch(IOException){}
        catch(UnauthorizedAccessException){}
    }
    public IReadOnlyList<(string Name,string Text)> Files()
    {
        var files=new List<(string,string)>();
        foreach(var name in new[]{OldFileName,FileName})
        {
            try{var path=Path.Combine(Folder,name);if(File.Exists(path))files.Add((name,File.ReadAllText(path,Encoding.UTF8)));}
            catch(IOException){}
            catch(UnauthorizedAccessException){}
        }
        return files;
    }
    public static string ShortCareer(string id)=>string.IsNullOrEmpty(id)?"-":id[..Math.Min(6,id.Length)];
    private static string Flatten(string text)=>text.Replace('\r',' ').Replace('\n',' ').Replace('|','/');
}

/// <summary>Removes file paths and any name or title from the current career before error text reaches the timeline.</summary>
public static class TimelineRedactor
{
    private static readonly Regex Paths=new(@"(?:[A-Za-z]:\\|\\\\|user://|res://|/(?:home|Users|tmp|mnt|var)/)[^\s""'<>|]*(?:[ ][^\s""'<>|\\/:]+[\\/][^\s""'<>|]*)*",RegexOptions.Compiled);
    public static string Clean(string text,GameState? state)
    {
        var clean=Paths.Replace(text,"[path]");
        var words=new List<(string Value,string Placeholder)>();
        if(state is not null)
        {
            words.AddRange(state.Series.Select(s=>(s.Title,"[title]")).Where(w=>w.Title.Length>=2));
            words.AddRange(state.People.Select(p=>(p.Name,"[name]")).Where(w=>w.Name.Length>=2));
            words.AddRange(state.Businesses.Select(b=>(b.Name,"[name]")).Where(w=>w.Name.Length>=2));
        }
        foreach(var local in new[]{Environment.UserName,Environment.MachineName})if(local.Length>=3)words.Add((local,"[name]"));
        foreach(var (value,placeholder) in words.OrderByDescending(w=>w.Value.Length))
            clean=Regex.Replace(clean,Regex.Escape(value),placeholder,RegexOptions.IgnoreCase);
        return clean;
    }
}
