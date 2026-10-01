using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace MangakaSim;

public sealed class CareerPresentation
{
    public GuidancePreferences Guidance { get; set; } = new();
    public double AmbienceVolume { get; set; } = .35;
    public double EffectsVolume { get; set; } = .6;
    public double MusicVolume { get; set; } = .5;
    public HashSet<int> ReadEvents { get; set; } = new();
    public HashSet<string> Tutorials { get; set; } = new();
    public bool Tips { get; set; } = true;
    public bool Stories { get; set; } = true;
    public double UiScale { get; set; } = 1;
    public bool CompactUi { get; set; }
    public bool ReducedUiMotion { get; set; }
    public bool OfficeSidebar { get; set; }
    public string InboxFilter { get; set; } = "Needs attention";
    public int SelectedSeries { get; set; }
    public int SelectedPerson { get; set; }
    public int WorkspaceScroll { get; set; }
    public string Camera { get; set; } = "";
    public string Page { get; set; } = "Inbox";
    public int Detail { get; set; }
    public int Scope { get; set; }
    public int ViewedOffice { get; set; }
    public int Scroll { get; set; }
    public int ChartDays { get; set; } = 90;
    public bool PersonalAccount { get; set; }
    public Dictionary<string,string> Artwork { get; set; } = new();
    public Dictionary<int,string> Synopses { get; set; } = new();
}
public sealed record CareerSnapshot(int Format,string Career,string Name,bool Auto,DateTime SavedAt,string State,CareerPresentation View);
public sealed record CareerSaveInfo(string Career,string Snapshot,string Name,bool Auto,DateTime SavedAt,DateTime GameDate,string Studio);
public sealed record CareerManifest(int Format, string Career, string Name, bool Auto, DateTime SavedAt,
    DateTime GameDate, string Studio, CareerPresentation View);

/// <summary>Immutable snapshots published by an atomic file rename. Artwork is immutable and shared within a career.</summary>
public sealed class CareerStore(string root)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    /// <summary>The fixed identifier of the T1.10 practice career, kept on import so its short code (70ac71) is recognisable in a timeline.</summary>
    public const string PracticeCareer="70ac71ce0000400080000000a1b2c3d4";
    public string Root { get; } = Path.GetFullPath(root);
    public Action<string>? FaultInjector { get; set; }
    private string DirectoryFor(string career)
    {
        if(!Guid.TryParseExact(career,"N",out _))throw new InvalidDataException("Invalid career identifier.");
        return Path.Combine(Root,career);
    }
    private static void HashCheck(string hash)
    {if(hash is null || hash.Length!=64 || hash.Any(c=>!Uri.IsHexDigit(c)))throw new InvalidDataException("Invalid artwork reference.");}
    public string AssetPath(string career,string hash){HashCheck(hash);return Path.Combine(DirectoryFor(career),"art",hash+".png");}
    public string ImportAsset(string career,byte[] png)
    {
        if(png.Length is <24 or >20971520 || !png.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))
            throw new InvalidDataException("Artwork must be a prepared PNG smaller than 20 MiB.");
        var width=System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16,4));
        var height=System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20,4));
        if(width is 0 or >4096 || height is 0 or >4096)throw new InvalidDataException("Artwork dimensions exceed 4096 pixels.");
        var hash=Convert.ToHexString(SHA256.HashData(png));var path=AssetPath(career,hash);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if(!File.Exists(path))AtomicWrite(path,png);return hash;
    }
    private void AtomicWrite(string target,byte[] bytes) => AtomicWrite(target, stream => stream.Write(bytes));
    private void AtomicWrite(string target,Action<Stream> write)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){write(file);file.Flush(true);}
            FaultInjector?.Invoke("before-publish");File.Move(temp,target,false);
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public CareerSaveInfo Save(string career,string name,GameState state,CareerPresentation view,bool auto=false,DateTime? savedAt=null)
    {
        if(string.IsNullOrWhiteSpace(name)||name.Length>100)throw new InvalidDataException("Use a save name of 1–100 characters.");
        var data=new CareerSnapshot(1,career,name,auto,savedAt??DateTime.UtcNow,state.ToJson(),view);
        Validate(data);
        foreach(var hash in view.Artwork.Values.Distinct())if(!File.Exists(AssetPath(career,hash)))throw new InvalidDataException("An imported artwork file is missing; restore it or reset that slot before saving.");
        var id=Guid.NewGuid().ToString("N");var target=Path.Combine(DirectoryFor(career),id+".career");
        AtomicWrite(target, stream => WritePackage(stream, data, state.Clock.Now, state.ControlledBusiness.Name, false));
        if(auto)foreach(var old in List().Where(s=>s.Career==career&&s.Auto).OrderByDescending(s=>s.SavedAt).Skip(3))
            File.Delete(SnapshotPath(old));
        return new(career,id,name,auto,data.SavedAt,state.Clock.Now,state.ControlledBusiness.Name);
    }
    /// <summary>Saves the last List() call could not read, so the Load screen can say so instead of hiding them silently.</summary>
    public int Unreadable { get; private set; }
    public IReadOnlyList<CareerSaveInfo> List()
    {
        var items=new List<CareerSaveInfo>();Unreadable=0;if(!Directory.Exists(Root))return items;
        foreach(var dir in Directory.EnumerateDirectories(Root).Where(d=>Guid.TryParseExact(Path.GetFileName(d),"N",out _)))
        foreach(var file in Directory.EnumerateFiles(dir).Where(f=>f.EndsWith(".json")||f.EndsWith(".career")))
        {
            try
            {
                if(!Guid.TryParseExact(Path.GetFileNameWithoutExtension(file),"N",out _))continue;
                if(file.EndsWith(".career"))
                {
                    using var input=File.OpenRead(file);using var zip=new ZipArchive(input,ZipArchiveMode.Read);
                    var meta=ReadManifest(zip);
                    if(meta.Career==Path.GetFileName(dir))items.Add(new(meta.Career,Path.GetFileNameWithoutExtension(file),meta.Name,meta.Auto,meta.SavedAt,meta.GameDate,meta.Studio));
                    continue;
                }
                var data=Read(file);if(data.Career!=Path.GetFileName(dir))continue;
                using var doc=JsonDocument.Parse(data.State);var state=doc.RootElement;
                var business=state.GetProperty("ControlledBusinessId").GetInt32();
                var studio=state.GetProperty("Businesses").EnumerateArray().First(b=>b.GetProperty("Id").GetInt32()==business).GetProperty("Name").GetString()!;
                items.Add(new(data.Career,Path.GetFileNameWithoutExtension(file),data.Name,data.Auto,data.SavedAt,state.GetProperty("Clock").GetProperty("Now").GetDateTime(),studio));
            }
            catch(Exception ex)when(ex is IOException or InvalidDataException or JsonException or InvalidOperationException or KeyNotFoundException){Unreadable++;}
        }
        return items.OrderByDescending(s=>s.SavedAt).ToArray();
    }
    private static CareerSnapshot Read(string file)
    {
        if(new FileInfo(file).Length>512L*1024*1024)throw new InvalidDataException("Save exceeds the supported size.");
        if(file.EndsWith(".career"))
        {
            using var input=File.OpenRead(file);using var zip=new ZipArchive(input,ZipArchiveMode.Read);
            return ReadPackage(zip);
        }
        var data=JsonSerializer.Deserialize<CareerSnapshot>(File.ReadAllText(file),Json)??throw new InvalidDataException("Empty save.");
        Validate(data);return data;
    }
    private static void Validate(CareerSnapshot data)
    {
        if(data.Format!=1||!Guid.TryParseExact(data.Career,"N",out _)||data.State is null||data.Name is null||data.View is null||
            data.View.Artwork is null||data.View.ReadEvents is null||data.View.Tutorials is null||data.View.Synopses is null||data.View.Camera is null||
            data.View.UiScale is <.75 or >1.75||!double.IsFinite(data.View.UiScale))throw new InvalidDataException("Invalid career package.");
        foreach(var pair in data.View.Artwork){HashCheck(pair.Value);if(pair.Key.Length>100)throw new InvalidDataException("Invalid artwork slot.");}
        var g=data.View.Guidance;
        if(g is null || !(CareerGuidance.Routes.Contains(g.Route) || CareerGuidance.LegacyRoutes.Contains(g.Route)) || g.Project<0 || g.Completed is null || g.Completed.Count>100 ||
            g.Completed.Any(x=>x is null||x.Length>80) || !double.IsFinite(data.View.AmbienceVolume) || data.View.AmbienceVolume is <0 or >1 ||
            !double.IsFinite(data.View.EffectsVolume) || data.View.EffectsVolume is <0 or >1 ||
            !double.IsFinite(data.View.MusicVolume) || data.View.MusicVolume is <0 or >1)throw new InvalidDataException("Invalid guidance or audio preferences.");
        if(data.View.Page is null||data.View.Scroll<0||data.View.WorkspaceScroll<0||data.View.SelectedSeries<0||data.View.SelectedPerson<0||data.View.ChartDays is not (0 or 30 or 90 or 365)||data.View.Synopses.Any(p=>p.Value is null||p.Value.Length>3000))throw new InvalidDataException("Invalid report preferences.");
    }
    public (GameState State,CareerPresentation View) Load(CareerSaveInfo info)
    {
        if(!Guid.TryParseExact(info.Snapshot,"N",out _))throw new InvalidDataException("Invalid snapshot.");
        var data=Read(SnapshotPath(info));
        if(data.Career!=info.Career)throw new InvalidDataException("Career identity mismatch.");
        return (GameState.ImportSupported(data.State),data.View);
    }
    public byte[] Export(CareerSaveInfo info)
    {
        var loaded=Load(info);var data=new CareerSnapshot(1,info.Career,info.Name,info.Auto,info.SavedAt,loaded.State.ToJson(),loaded.View);
        using var buffer=new MemoryStream();
        WritePackage(buffer,data,loaded.State.Clock.Now,loaded.State.ControlledBusiness.Name,true);
        return buffer.ToArray();
    }
    public CareerSaveInfo Import(byte[] package)
    {
        using var stream=new MemoryStream(package);using var zip=new ZipArchive(stream,ZipArchiveMode.Read);
        CheckArchive(zip);
        var data=ReadPackage(zip);
        Validate(data);var state=GameState.ImportSupported(data.State);var career=data.Career==PracticeCareer?PracticeCareer:Guid.NewGuid().ToString("N");
        foreach(var hash in data.View.Artwork.Values.Distinct())
        {
            var asset=zip.GetEntry("art/"+hash+".png")??throw new InvalidDataException("Missing artwork in package.");
            if(asset.Length>20971520)throw new InvalidDataException("Artwork is too large.");
            using var input=asset.Open();using var bytes=new MemoryStream();input.CopyTo(bytes);
            if(ImportAsset(career,bytes.ToArray())!=hash)throw new InvalidDataException("Artwork checksum mismatch.");
        }
        // An import keeps its own save time, so Continue still opens the career last played (finding B5).
        return Save(career,data.Name,state,data.View,savedAt:data.SavedAt);
    }

    private string SnapshotPath(CareerSaveInfo info)
    {
        if(!Guid.TryParseExact(info.Snapshot,"N",out _))throw new InvalidDataException("Invalid snapshot.");
        var stem=Path.Combine(DirectoryFor(info.Career),info.Snapshot);
        return File.Exists(stem+".career")?stem+".career":stem+".json";
    }
    public byte[] ExportSnapshot(string career,GameState state,CareerPresentation view)
    {
        var data=new CareerSnapshot(1,career,"Problem report snapshot",false,DateTime.UtcNow,state.ToJson(),view);
        Validate(data);using var output=new MemoryStream();
        WritePackage(output,data,state.Clock.Now,state.ControlledBusiness.Name,true);return output.ToArray();
    }
    private void WritePackage(Stream output,CareerSnapshot data,DateTime gameDate,string studio,bool artwork)
    {
        using var zip=new ZipArchive(output,ZipArchiveMode.Create,true);
        var meta=new CareerManifest(2,data.Career,data.Name,data.Auto,data.SavedAt,gameDate,studio,data.View);
        using(var entry=zip.CreateEntry("manifest.json",CompressionLevel.Fastest).Open())JsonSerializer.Serialize(entry,meta,Json);
        using(var writer=new StreamWriter(zip.CreateEntry("state.json",CompressionLevel.Fastest).Open()))writer.Write(data.State);
        if(artwork)foreach(var hash in data.View.Artwork.Values.Distinct())
        {using var source=File.OpenRead(AssetPath(data.Career,hash));using var entry=zip.CreateEntry("art/"+hash+".png").Open();source.CopyTo(entry);}
    }
    private static void CheckArchive(ZipArchive zip)
    {
        if(zip.Entries.Count>10000||zip.Entries.Sum(e=>e.Length)>1024L*1024*1024 ||
            zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.Ordinal).Count()!=zip.Entries.Count ||
            zip.Entries.Any(e=>e.FullName.Contains("..")||e.FullName.Contains('\\')||e.FullName.StartsWith('/')||e.FullName.Contains(':')))
            throw new InvalidDataException("Invalid or oversized career archive.");
    }
    private static string ReadEntry(ZipArchive zip,string name,long limit)
    {
        var entry=zip.GetEntry(name)??throw new InvalidDataException("Missing "+name+".");
        if(entry.Length>limit)throw new InvalidDataException("Career entry is too large.");
        using var input=entry.Open();using var output=new MemoryStream();var block=new byte[65536];int read;
        while((read=input.Read(block))>0){if(output.Length+read>limit)throw new InvalidDataException("Career entry is too large.");output.Write(block,0,read);}
        return System.Text.Encoding.UTF8.GetString(output.GetBuffer(),0,checked((int)output.Length));
    }
    private static CareerManifest ReadManifest(ZipArchive zip)
    {
        CheckArchive(zip);
        var meta=JsonSerializer.Deserialize<CareerManifest>(ReadEntry(zip,"manifest.json",16*1024*1024),Json)??throw new InvalidDataException("Empty manifest.");
        if(meta.Format!=2||meta.Studio is null)throw new InvalidDataException("Unsupported career format.");
        Validate(new(1,meta.Career,meta.Name,meta.Auto,meta.SavedAt,"",meta.View));return meta;
    }
    private static CareerSnapshot ReadPackage(ZipArchive zip)
    {
        CheckArchive(zip);
        if(zip.GetEntry("manifest.json") is not null)
        {
            var meta=ReadManifest(zip);
            return new(1,meta.Career,meta.Name,meta.Auto,meta.SavedAt,ReadEntry(zip,"state.json",512L*1024*1024),meta.View);
        }
        var old=JsonSerializer.Deserialize<CareerSnapshot>(ReadEntry(zip,"career.json",512L*1024*1024),Json)??throw new InvalidDataException("Invalid career.");
        Validate(old);return old;
    }
}
