using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace MangakaSim;

public static class ProblemReport
{
    public const string Build = "0.8.0-private-alpha.12";
    public static byte[] Create(string note,GameState state,byte[]? screenshot=null,byte[]? career=null,IReadOnlyList<(string Name,string Text)>? timeline=null)
    {
        if(string.IsNullOrWhiteSpace(note)||note.Length>12000)throw new InvalidDataException("Describe the problem in 1–12,000 characters.");
        if(screenshot?.Length>20*1024*1024||career?.Length>512L*1024*1024||timeline?.Sum(t=>(long)Encoding.UTF8.GetByteCount(t.Text))>8L*1024*1024)
            throw new InvalidDataException("Report attachment is too large.");
        using var output=new MemoryStream();
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true))
        {
            // Deliberately allowlisted: no log dumps, environment variables, paths or account identifiers. The session
            // timeline is opt-in (ticked by default, T1.10) and already free of names, typed text and paths.
            using(var entry=zip.CreateEntry("report.json").Open())JsonSerializer.Serialize(entry,new
            {
                Build, SimulationVersion=state.Version, CareerFormat=2, CreatedAt=DateTime.UtcNow,
                GameDate=state.Clock.Now, state.Progression.Difficulty, state.Progression.EverSandbox,
                Projects=state.Series.Count, People=state.People.Count, Events=state.Events.Count,
                Note=note, Screenshot=screenshot is not null, Career=career is not null, Timeline=timeline is {Count:>0}
            },new JsonSerializerOptions{WriteIndented=true});
            if(screenshot is not null){using var entry=zip.CreateEntry("screenshot.png").Open();entry.Write(screenshot);}
            if(career is not null){using var entry=zip.CreateEntry("career.mangaka").Open();entry.Write(career);}
            foreach(var (name,text) in timeline??[]){using var entry=zip.CreateEntry(Path.GetFileName(name)).Open();entry.Write(Encoding.UTF8.GetBytes(text));}
        }
        return output.ToArray();
    }
    public static void Write(string path,byte[] report)
    {
        var target=Path.GetFullPath(path);var temporary=target+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write)){output.Write(report);output.Flush(true);}
            File.Move(temporary,target,true);
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
