# Tier 1: fresh-player test (T1.10), implementation plan

Date: 2026-09-27. Decisions: [considerations](../specs/2026-09-27-fresh-player-test-considerations.md) (Q31 to Q34). Status: plan written, awaiting review; not yet implemented.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prepare alpha.12 for unmoderated fresh-player tests: a private session timeline attached to the problem report, a practice career that reaches a setback, a rendered walk-through of the whole core journey, and a tester kit in the package.

**Architecture:** Two new engine-free classes in `src/MangakaSim` (`SessionTimeline` with `TimelineRedactor`, and `JourneyMilestones`) do all the logic and are unit tested. The Godot side only calls them from places where the events already happen (`godot/DebugMain.Timeline.cs` plus one-line hooks). The simulation, balance and save format are untouched, so play stays deterministic and existing saves load unchanged.

**Tech Stack:** C# (.NET 8 simulation, Godot 4.7.2 .NET UI), xUnit, PowerShell packaging.

**Spec:** `docs/superpowers/specs/2026-09-27-fresh-player-test-considerations.md`

## Global Constraints

- No simulation, balance or save format change. `GameState` JSON must be byte-identical before and after this work for the same commands.
- Never recorded in the timeline: names, typed text, file paths or hardware identifiers. A career appears as the first 6 characters of its identifier.
- Timeline file cap about 1 MB (`1_000_000` bytes); on overflow it becomes `timeline.old.log` and a new file starts. At most two files are kept and attached.
- Timeline line format: `yyyy-MM-dd HH:mm:ss | +<minutes> | <game date yyyy-MM-dd or -> | <event>` with CRLF line ends.
- "Attach session timeline" is ticked by default; "Attach this screen" and "Attach career" stay unticked by default.
- `ProblemReport.Build` becomes `0.8.0-private-alpha.12`.
- Practice career name "Practice: a struggling series", file name `Practice - a struggling series.mangaka`, fixed identifier `70ac71ce0000400080000000a1b2c3d4`.
- Code style: `src/MangakaSim` and `godot` use the existing compact style; `CareerPlaytest.cs` uses its existing spaced style.
- Docs use CRLF line endings, metric units, and no em dashes.
- **Every build, test run, Godot run and packaging step needs the user's explicit authorization at that time.** A previous authorization is not permanent permission. Where a step says "Run", ask first if it has not been authorized in the current session for this plan.
- Commit only when the user asks. The commit steps below are the suggested points; skip them unless the user has asked for commits.
- I never contact testers or send builds; that is the user's task, asked for when the step arrives.

## Review Focus

1. **Read-only or locked user folder, or a timeline file held open by another program.** The game must keep running and never show an error because of the timeline; logging silently stops. Test: `SessionTimeline_survives_an_unwritable_folder` (Task 1).
2. **Helper-Chan's thread trimmed or replaced (new career, load, import).** New guidance steps must still be logged once, not re-logged in bulk and not skipped. Test: `NewGuidance_handles_a_replaced_thread` (Task 4, on the pure helper `TimelineGuidance.NewSteps`).
3. **Loading an old career or the practice save.** Milestones already reached must not be logged again as "first". Test: `JourneyMilestones_premarks_reached_milestones` (Task 2).
4. **Error text that contains a path, a studio name, a person's name or a series title.** The logged line must show placeholders. Test: `Redactor_removes_paths_and_career_names` (Task 1).
5. **A very large timeline in a report** (e.g. a corrupted file). Export must refuse with the normal "Report attachment is too large." message rather than crash or build a huge ZIP. Test: `ProblemReport_refuses_an_oversized_timeline` (Task 3).

---

### Task 1: Session timeline and redactor

**Files:**
- Create: `src/MangakaSim/SessionTimeline.cs`
- Create: `tests/MangakaSim.Tests/SessionTimelineTests.cs`

**Interfaces:**
- Consumes: `GameState` (`Series`, `People`, `Businesses` via `ControlledBusiness.Name`) for the redactor.
- Produces:
  - `public sealed class SessionTimeline(string folder,Func<DateTime> now,long capBytes=1_000_000)`
  - `public string Folder { get; }`
  - `public void Start(string details)` writes `session start <details>`
  - `public void End()` writes `session end` once; later calls do nothing
  - `public void Record(DateTime? gameDate,string text)`
  - `public IReadOnlyList<(string Name,string Text)> Files()` old file first, then `timeline.log`; missing files are skipped
  - `public static string ShortCareer(string id)`
  - `public const string FileName="timeline.log", OldFileName="timeline.old.log";`
  - `public static class TimelineRedactor { public static string Clean(string text,GameState? state); }`

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Text;
using Xunit;
namespace MangakaSim.Tests;
public class SessionTimelineTests
{
    static string Folder()=>Path.Combine(Path.GetTempPath(),"mangaka-timeline-"+Guid.NewGuid().ToString("N"));
    [Fact]public void Timeline_writes_the_agreed_line_format()
    {
        var clock=new DateTime(2026,10,2,19,29,7);var dir=Folder();
        var log=new SessionTimeline(dir,()=>clock);
        log.Start("window 1920x1080 text 100 theme dark");
        clock=clock.AddMinutes(12);log.Record(new DateTime(1996,4,8,9,0,0),"milestone first-sale");
        log.Record(null,"screen Load");log.End();log.End();
        var lines=File.ReadAllText(Path.Combine(dir,SessionTimeline.FileName)).Split("\r\n",StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("2026-10-02 19:29:07 | +0 | - | session start window 1920x1080 text 100 theme dark",lines[0]);
        Assert.Equal("2026-10-02 19:41:07 | +12 | 1996-04-08 | milestone first-sale",lines[1]);
        Assert.Equal("2026-10-02 19:41:07 | +12 | - | screen Load",lines[2]);
        Assert.Equal("2026-10-02 19:41:07 | +12 | - | session end",lines[3]);
        Assert.Equal(4,lines.Length);
    }
    [Fact]public void Timeline_flattens_line_breaks_and_separators()
    {
        var dir=Folder();var log=new SessionTimeline(dir,()=>new DateTime(2026,1,1));
        log.Record(null,"error a\r\nb | c");
        Assert.EndsWith("| error a  b / c\r\n",File.ReadAllText(Path.Combine(dir,SessionTimeline.FileName)));
    }
    [Fact]public void Timeline_rolls_over_at_the_cap_and_keeps_two_files()
    {
        var dir=Folder();var log=new SessionTimeline(dir,()=>new DateTime(2026,1,1),capBytes:400);
        for(var i=0;i<40;i++)log.Record(null,"screen Production "+i);
        Assert.True(new FileInfo(Path.Combine(dir,SessionTimeline.FileName)).Length<=400);
        Assert.True(new FileInfo(Path.Combine(dir,SessionTimeline.OldFileName)).Length<=400);
        Assert.Equal(2,Directory.GetFiles(dir).Length);
        var files=log.Files();
        Assert.Equal([SessionTimeline.OldFileName,SessionTimeline.FileName],files.Select(f=>f.Name));
        Assert.Contains("screen Production 39",files[1].Text);
    }
    [Fact]public void Files_skips_missing_files()
    {
        var log=new SessionTimeline(Folder(),()=>DateTime.Now);
        Assert.Empty(log.Files());
        log.Record(null,"pause");
        Assert.Equal(SessionTimeline.FileName,Assert.Single(log.Files()).Name);
    }
    [Fact]public void SessionTimeline_survives_an_unwritable_folder()
    {
        var dir=Folder();Directory.CreateDirectory(Path.GetDirectoryName(dir)!);
        File.WriteAllText(dir,"a file where the folder should be");
        var log=new SessionTimeline(dir,()=>DateTime.Now);
        log.Start("x");log.Record(null,"pause");log.End();
        Assert.Empty(log.Files());
        File.Delete(dir);
        var locked=Folder();var open=new SessionTimeline(locked,()=>DateTime.Now);open.Record(null,"first");
        using(new FileStream(Path.Combine(locked,SessionTimeline.FileName),FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            open.Record(null,"while locked");
        open.Record(null,"after");
        var text=File.ReadAllText(Path.Combine(locked,SessionTimeline.FileName));
        Assert.Contains("first",text);Assert.Contains("after",text);Assert.DoesNotContain("while locked",text);
    }
    [Fact]public void ShortCareer_is_six_characters()
    {
        Assert.Equal("70ac71",SessionTimeline.ShortCareer("70ac71ce0000400080000000a1b2c3d4"));
        Assert.Equal("ab",SessionTimeline.ShortCareer("ab"));
        Assert.Equal("-",SessionTimeline.ShortCareer(""));
    }
    [Fact]public void Redactor_removes_paths_and_career_names()
    {
        var state=GameState.NewGame(0,protagonistName:"Mika Tanaka");
        state.Apply(new CreateDoujinCommand("First pages","adventure"));
        var studio=state.ControlledBusiness.Name;
        var text=$@"Could not read C:\Users\someone\AppData\Roaming\Godot\app_userdata\Mangaka Studio\careers\x.career for Mika Tanaka at {studio}: ""First pages"" and /home/someone/saves/y.json and user://careers/z";
        var clean=TimelineRedactor.Clean(text,state);
        Assert.DoesNotContain("someone",clean);Assert.DoesNotContain("Mika",clean);
        Assert.DoesNotContain(studio,clean);Assert.DoesNotContain("First pages",clean);
        Assert.DoesNotContain("careers",clean);
        Assert.Contains("[path]",clean);Assert.Contains("[name]",clean);Assert.Contains("[title]",clean);
        Assert.Equal("Not enough money.",TimelineRedactor.Clean("Not enough money.",state));
        Assert.Equal("[path] missing",TimelineRedactor.Clean(@"D:\x\y.png missing",null));
    }
    [Fact]public void Redactor_removes_the_windows_user_and_machine_names()
    {
        var clean=TimelineRedactor.Clean($"Access denied for {Environment.UserName} on {Environment.MachineName}",null);
        if(Environment.UserName.Length>=3)Assert.DoesNotContain(Environment.UserName,clean,StringComparison.OrdinalIgnoreCase);
        if(Environment.MachineName.Length>=3)Assert.DoesNotContain(Environment.MachineName,clean,StringComparison.OrdinalIgnoreCase);
    }
}
```

Before running, confirm the `GameState.NewGame` parameter name for the creator name (`grep -n "public static GameState NewGame" src/MangakaSim/*.cs`); the "Begin career" handler passes it as the third positional argument. Adjust `name:` to the real parameter name if it differs.

- [ ] **Step 2: Run the tests to verify they fail** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~SessionTimelineTests"`
Expected: build FAIL, `SessionTimeline` and `TimelineRedactor` do not exist.

- [ ] **Step 3: Write the implementation**

`src/MangakaSim/SessionTimeline.cs`:

```csharp
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
    private static string Flatten(string text)=>text.Replace("\r\n"," ").Replace('\r',' ').Replace('\n',' ').Replace('|','/');
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
```

Before compiling, confirm the property names used by the redactor with `grep -n "public string Title\|public string Name\|List<Business> Businesses\|List<Person> People" src/MangakaSim/*.cs` and adjust if a collection is named differently.

- [ ] **Step 4: Run the tests to verify they pass** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~SessionTimelineTests"`
Expected: PASS, 8 tests.

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add src/MangakaSim/SessionTimeline.cs tests/MangakaSim.Tests/SessionTimelineTests.cs
git commit -m "T1.10: session timeline and redactor"
```

---

### Task 2: Journey milestones

**Files:**
- Create: `src/MangakaSim/JourneyMilestones.cs`
- Create: `tests/MangakaSim.Tests/JourneyMilestonesTests.cs`

**Interfaces:**
- Consumes: `GameState.Events` (`GameEvent` with `Time`, `Type`, `SeriesId`, `PersonId`), `Series.BusinessId`, `Series.Volumes`, `Volume.CopiesSold`, `GameState.PrintRuns` (`PrintRun.VolumeId`), `GameState.ControlledBusinessId`, `GameState.ProtagonistPersonId`, `GameState.ControlledStaff`.
- Produces:
  - `public sealed class JourneyMilestones(GameState state)`, whose constructor pre-marks milestones the career has already reached
  - `public IReadOnlyList<string> Observe(GameState state,IEnumerable<GameEvent> fresh)` returns milestone ids to log, in order, for new events plus the hourly poll

Milestone ids (the strings the timeline and the journey smoke rely on):
`first-doujin-completed`, `first-sale`, `later-sale`, `later-sale reprint`, `first-pitch`, `pitch-answer rejected`, `pitch-answer offered`, `serialization-accepted`, `first-magazine-chapter`, `first-deadline-missed`, `cancellation-warning`, `cancellation`, `first-hire`, `missed-payday`.

`pitch-answer ...`, `cancellation-warning`, `cancellation`, `later-sale ...` and `missed-payday` repeat; the `first-*` ones and `serialization-accepted` log once per career. `missed-payday` logs at most once per calendar month.

- [ ] **Step 1: Write the failing tests**

```csharp
using MangakaSim.Rules;
using Xunit;
namespace MangakaSim.Tests;
public class JourneyMilestonesTests
{
    static GameEvent E(GameState s,EventType type,int? series=null,int? person=null)=>new(){Time=s.Clock.Now,Type=type,SeriesId=series,PersonId=person};
    static int Own(GameState s){s.Apply(new CreateDoujinCommand("First pages","adventure"));return s.Series[^1].Id;}
    [Fact]public void Owned_series_events_map_to_milestones()
    {
        var s=GameState.NewGame(0);var own=Own(s);var tracker=new JourneyMilestones(s);
        var ids=tracker.Observe(s,[E(s,EventType.PitchSubmitted,own),E(s,EventType.PitchSubmitted,own),E(s,EventType.PitchRejected,own),
            E(s,EventType.SerializationOffered,own),E(s,EventType.OfferAccepted,own),E(s,EventType.ChapterPublished,own),E(s,EventType.ChapterPublished,own),
            E(s,EventType.DeadlineMissed,own),E(s,EventType.CancellationWarning,own),E(s,EventType.CancellationWarning,own),E(s,EventType.SeriesCancelled,own)]);
        Assert.Equal(["first-pitch","pitch-answer rejected","pitch-answer offered","serialization-accepted","first-magazine-chapter",
            "first-deadline-missed","cancellation-warning","cancellation-warning","cancellation"],ids);
    }
    [Fact]public void Other_studios_series_are_ignored()
    {
        var s=GameState.NewGame(0);var tracker=new JourneyMilestones(s);
        var rival=s.Series.FirstOrDefault(x=>x.BusinessId!=s.ControlledBusinessId)?.Id??-1;
        Assert.Empty(tracker.Observe(s,[E(s,EventType.PitchSubmitted,rival),E(s,EventType.CancellationWarning,rival),E(s,EventType.PitchSubmitted,null)]));
    }
    [Fact]public void Hire_and_payday_milestones()
    {
        var s=GameState.NewGame(0);var tracker=new JourneyMilestones(s);
        Assert.Empty(tracker.Observe(s,[E(s,EventType.StaffHired,person:s.ProtagonistPersonId)]));
        var hire=s.People.First(p=>p.Id!=s.ProtagonistPersonId);
        var ids=tracker.Observe(s,[E(s,EventType.StaffHired,person:hire.Id),E(s,EventType.StaffHired,person:hire.Id),E(s,EventType.WageArrears),E(s,EventType.WageArrears)]);
        Assert.Equal(["first-hire","missed-payday"],ids);
        s.Advance(24*32);
        Assert.Equal(["missed-payday"],tracker.Observe(s,[E(s,EventType.WageArrears)]));
    }
    [Fact]public void Poll_finds_completion_first_sale_and_later_sales()
    {
        var s=GameState.NewGame(0);var tracker=new JourneyMilestones(s);var ids=new List<string>();
        s.Apply(new CreateDoujinCommand("First pages","adventure"));var doujin=s.Series[^1];
        for(var d=0;d<180&&doujin.Volumes.Count==0;d++){s.Advance(24);ids.AddRange(tracker.Observe(s,[]));}
        Assert.Contains("first-doujin-completed",ids);
        var v=doujin.Volumes.Single();
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:10,Value:(int)PrintTier.CopyShop));
        for(var d=0;d<60&&v.CopiesSold==0;d++){s.Advance(24);ids.AddRange(tracker.Observe(s,[]));}
        Assert.Equal(1,ids.Count(i=>i=="first-sale"));
        Assert.DoesNotContain("later-sale",ids);
        s.Apply(new StudioActionCommand(StudioAction.Print,v.Id,Amount:10,Value:(int)PrintTier.CopyShop));
        var sold=v.CopiesSold;
        for(var d=0;d<90&&!ids.Contains("later-sale reprint");d++){s.Advance(24);ids.AddRange(tracker.Observe(s,[]));}
        Assert.Contains("later-sale reprint",ids);
        Assert.True(v.CopiesSold>sold);
    }
    [Fact]public void JourneyMilestones_premarks_reached_milestones()
    {
        var s=GameState.NewGame(0);var own=Own(s);var first=new JourneyMilestones(s);
        var doujin=s.Series[^1];
        for(var d=0;d<180&&doujin.Volumes.Count==0;d++)s.Advance(24);
        s.Apply(new StudioActionCommand(StudioAction.Print,doujin.Volumes[0].Id,Amount:10,Value:(int)PrintTier.CopyShop));
        for(var d=0;d<60&&doujin.Volumes[0].CopiesSold==0;d++)s.Advance(24);
        s.Events.Add(E(s,EventType.PitchSubmitted,own));
        // A freshly loaded career: nothing already reached is logged again.
        var loaded=GameState.FromJson(s.ToJson());var tracker=new JourneyMilestones(loaded);
        Assert.Empty(tracker.Observe(loaded,[]));
        Assert.Empty(tracker.Observe(loaded,[E(loaded,EventType.PitchSubmitted,own)]));
    }
}
```

Before running, confirm `GameEvent` has a settable parameterless form (`grep -n "class GameEvent" -A10 src/MangakaSim/*.cs`). If it is a positional record, build the events with its constructor instead and keep the same fields. Confirm `s.Events` is a `List<GameEvent>`; if events are added only through an internal method, use that method in the pre-mark test.

- [ ] **Step 2: Run the tests to verify they fail** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~JourneyMilestonesTests"`
Expected: build FAIL, `JourneyMilestones` does not exist.

- [ ] **Step 3: Write the implementation**

`src/MangakaSim/JourneyMilestones.cs`:

```csharp
namespace MangakaSim;
/// <summary>
/// T1.10 journey milestones for the session timeline. Classifies the game's own events and polls volumes once per
/// scan, so the timeline shows each T1.1 step without any change to the simulation. Milestones the career already
/// reached (a loaded save, the practice career) are pre-marked so they are not logged again as "first".
/// Known limitation: first-hire is judged by current staff, so a career whose only hire has left may log it again.
/// </summary>
public sealed class JourneyMilestones
{
    private static readonly HashSet<string> Repeating=["pitch-answer rejected","pitch-answer offered","cancellation-warning","cancellation"];
    private readonly HashSet<string> _done=new();
    private readonly HashSet<int> _selling=new();
    private readonly Dictionary<int,int> _runs=new();
    private DateTime? _lastPayday;
    public JourneyMilestones(GameState state)
    {
        foreach(var e in state.Events){var id=Classify(state,e);if(id is not null&&!Repeating.Contains(id))_done.Add(id);}
        Poll(state,new List<string>());
    }
    public IReadOnlyList<string> Observe(GameState state,IEnumerable<GameEvent> fresh)
    {
        var ids=new List<string>();
        foreach(var e in fresh)
        {
            var id=Classify(state,e);if(id is null)continue;
            if(id=="missed-payday")
            {
                if(_lastPayday is {} last&&last.Year==e.Time.Year&&last.Month==e.Time.Month)continue;
                _lastPayday=e.Time;ids.Add(id);continue;
            }
            if(Repeating.Contains(id)||_done.Add(id))ids.Add(id);
        }
        Poll(state,ids);
        return ids;
    }
    private void Poll(GameState state,List<string> ids)
    {
        foreach(var series in state.Series.Where(s=>s.BusinessId==state.ControlledBusinessId))
        {
            if(series.Volumes.Any(v=>v.IsDoujin)&&_done.Add("first-doujin-completed"))ids.Add("first-doujin-completed");
            foreach(var volume in series.Volumes.Where(v=>v.IsDoujin))
            {
                var runs=state.PrintRuns.Count(r=>r.VolumeId==volume.Id);
                var known=_runs.TryGetValue(volume.Id,out var before);_runs[volume.Id]=runs;
                if(volume.CopiesSold<=0)continue;
                if(_selling.Add(volume.Id))
                {
                    if(_done.Add("first-sale"))ids.Add("first-sale");
                    else ids.Add("later-sale");
                }
                else if(known&&runs>before&&runs>1)_pendingReprint.Add(volume.Id);
                if(_pendingReprint.Contains(volume.Id)&&volume.CopiesSold>_soldAtReprint.GetValueOrDefault(volume.Id))
                {
                    _pendingReprint.Remove(volume.Id);ids.Add("later-sale reprint");
                }
                if(!_pendingReprint.Contains(volume.Id))_soldAtReprint[volume.Id]=volume.CopiesSold;
            }
        }
    }
    private readonly HashSet<int> _pendingReprint=new();
    private readonly Dictionary<int,long> _soldAtReprint=new();
    private static string? Classify(GameState state,GameEvent e)
    {
        bool Owned()=>state.Series.Any(s=>s.Id==e.SeriesId&&s.BusinessId==state.ControlledBusinessId);
        return e.Type switch
        {
            EventType.PitchSubmitted when Owned()=>"first-pitch",
            EventType.PitchRejected when Owned()=>"pitch-answer rejected",
            EventType.SerializationOffered when Owned()=>"pitch-answer offered",
            EventType.OfferAccepted when Owned()=>"serialization-accepted",
            EventType.ChapterPublished when Owned()&&state.Series.First(s=>s.Id==e.SeriesId).Contract is not null=>"first-magazine-chapter",
            EventType.DeadlineMissed or EventType.IssueMissed when Owned()=>"first-deadline-missed",
            EventType.CancellationWarning when Owned()=>"cancellation-warning",
            EventType.SeriesCancelled when Owned()=>"cancellation",
            EventType.StaffHired when e.PersonId is int id&&id!=state.ProtagonistPersonId&&state.ControlledStaff.Any(p=>p.Id==id)=>"first-hire",
            EventType.WageArrears=>"missed-payday",
            _=>null
        };
    }
}
```

The reprint rule: a new print run on a volume that has already sold arms `later-sale reprint`, which fires on the first scan where that volume's `CopiesSold` has risen since the run was ordered. A second selling volume (a new issue) logs `later-sale`. Both count as the spec's "growing readership".

Note on `ChapterPublished`: doujin chapters may also raise this event; the `Contract is not null` condition restricts it to magazine chapters. Check with `grep -n "EventType.ChapterPublished" src/MangakaSim/*.cs` that the event is raised after the contract exists; if a doujin never raises it, the condition is harmless.

- [ ] **Step 4: Run the tests to verify they pass** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~JourneyMilestonesTests"`
Expected: PASS, 5 tests.

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add src/MangakaSim/JourneyMilestones.cs tests/MangakaSim.Tests/JourneyMilestonesTests.cs
git commit -m "T1.10: journey milestones for the session timeline"
```

---

### Task 3: Timeline in the problem report, alpha.12 build name

**Files:**
- Modify: `src/MangakaSim/ProblemReport.cs` (whole `Create` method and `Build`)
- Modify: `tests/MangakaSim.Tests/AlphaTests.cs` (add three tests next to the existing problem report test, about L114-144)

**Interfaces:**
- Consumes: `SessionTimeline.Files()` shape `IReadOnlyList<(string Name,string Text)>` (Task 1).
- Produces: `ProblemReport.Create(string note,GameState state,byte[]? screenshot=null,byte[]? career=null,IReadOnlyList<(string Name,string Text)>? timeline=null)`; `report.json` gains `"Timeline": true|false`; `ProblemReport.Build=="0.8.0-private-alpha.12"`.

- [ ] **Step 1: Write the failing tests** (in `AlphaTests.cs`, compact style)

```csharp
[Fact]public void ProblemReport_attaches_the_session_timeline_as_text()
{
    var state=GameState.NewGame(0);
    var zip=new ZipArchive(new MemoryStream(ProblemReport.Create("Stuck after the pitch",state,timeline:[("timeline.old.log","old\r\n"),("timeline.log","2026-10-02 19:41:07 | +12 | 1996-04-08 | milestone first-sale\r\n")])));
    Assert.Equal(["report.json","timeline.old.log","timeline.log"],zip.Entries.Select(e=>e.FullName));
    using var report=zip.GetEntry("report.json")!.Open();
    Assert.True(JsonNode.Parse(report)!["Timeline"]!.GetValue<bool>());
    using var log=new StreamReader(zip.GetEntry("timeline.log")!.Open());
    Assert.Contains("milestone first-sale",log.ReadToEnd());
    Assert.Equal("0.8.0-private-alpha.12",ProblemReport.Build);
}
[Fact]public void ProblemReport_without_a_timeline_has_only_the_report()
{
    var state=GameState.NewGame(0);
    foreach(var timeline in new IReadOnlyList<(string,string)>?[]{null,[]})
    {
        var zip=new ZipArchive(new MemoryStream(ProblemReport.Create("Note",state,timeline:timeline)));
        Assert.Equal("report.json",Assert.Single(zip.Entries).FullName);
        using var report=zip.Entries[0].Open();
        Assert.False(JsonNode.Parse(report)!["Timeline"]!.GetValue<bool>());
    }
}
[Fact]public void ProblemReport_refuses_an_oversized_timeline()
{
    var state=GameState.NewGame(0);var huge=new string('x',8*1024*1024+1);
    var ex=Assert.Throws<InvalidDataException>(()=>ProblemReport.Create("Note",state,timeline:[("timeline.log",huge)]));
    Assert.Equal("Report attachment is too large.",ex.Message);
}
```

Also check the existing test at about L114-121 still asserts a single entry when called without a timeline; it needs no change.

- [ ] **Step 2: Run the tests to verify they fail** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~AlphaTests"`
Expected: build FAIL, no `timeline` parameter.

- [ ] **Step 3: Write the implementation**

Replace the `Build` constant and `Create` method in `src/MangakaSim/ProblemReport.cs` (keep `Write` as it is; `System.Text` is needed for `Encoding`):

```csharp
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
```

Keep the existing en dash in the note-length message exactly as it is; it is existing player-facing text checked by the L143-144 tests.

- [ ] **Step 4: Run the tests to verify they pass** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~AlphaTests"`
Expected: PASS, including the three new tests and the existing note-length tests.

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add src/MangakaSim/ProblemReport.cs tests/MangakaSim.Tests/AlphaTests.cs
git commit -m "T1.10: attach the session timeline to problem reports, alpha.12"
```

---

### Task 4: Godot wiring and the report checkbox

**Files:**
- Create: `godot/DebugMain.Timeline.cs`
- Create: `src/MangakaSim/TimelineGuidance.cs` (pure helper for Review Focus 2)
- Create: `tests/MangakaSim.Tests/TimelineGuidanceTests.cs`
- Modify: `godot/DebugMain.cs` (`ScanEvents` about L141, `SetSpeed` about L184, `TryApply` catch about L235)
- Modify: `godot/DebugMain.Management.cs` (button catch about L72, `_careers=new CareerStore(...)` about L97, `Navigate` about L177, autosave catch about L243)
- Modify: `godot/DebugMain.Gui.cs` (`ShowOffice` about L33)
- Modify: `godot/DebugMain.Alpha.cs` (`OpenPhone`, `RefreshGuidance` L137-140, `ReportProblem` L305-326)
- Modify: `godot/DebugMain.ManagementMenus.cs` (Quit about L33, Begin career L70-75, `SaveCareer` L132, import L141, `LoadCareer` L152, `ResetManagementSession`)
- Modify: `godot/DebugMain.AlphaSmoke.cs` (L78-82)

**Interfaces:**
- Consumes: `SessionTimeline`, `TimelineRedactor` (Task 1), `JourneyMilestones` (Task 2), `ProblemReport.Create(..., timeline:)` (Task 3).
- Produces (used by Task 6):
  - `private SessionTimeline? _timeline;`
  - `private JourneyMilestones? _milestones;`
  - `private void LogTimeline(string text)`
  - `private string CareerCode` (the short career code)
  - `public static IReadOnlyList<string> TimelineGuidance.NewSteps(IReadOnlyList<GuidanceMessage> thread,GuidanceMessage? lastSeen)`

- [ ] **Step 1: Write the failing test for the guidance helper**

```csharp
using Xunit;
namespace MangakaSim.Tests;
public class TimelineGuidanceTests
{
    static GuidanceMessage M(string step,int day)=>new(){Step=step,Time=new DateTime(1996,4,day)};
    [Fact]public void NewGuidance_lists_messages_after_the_last_seen_one()
    {
        var a=M("doujin",1);var b=M("print",2);var c=M("sell",3);
        Assert.Equal(["print","sell"],TimelineGuidance.NewSteps([a,b,c],a));
        Assert.Empty(TimelineGuidance.NewSteps([a,b,c],c));
        Assert.Equal(["doujin","print","sell"],TimelineGuidance.NewSteps([a,b,c],null));
    }
    [Fact]public void NewGuidance_handles_a_replaced_thread()
    {
        var old=M("doujin",1);var fresh=new[]{M("welcome",5),M("doujin",6)};
        // The last seen message is gone (a new career, load or trimmed thread): only the newest message is logged.
        Assert.Equal(["doujin"],TimelineGuidance.NewSteps(fresh,old));
        Assert.Empty(TimelineGuidance.NewSteps([],old));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~TimelineGuidanceTests"`
Expected: build FAIL, `TimelineGuidance` does not exist.

- [ ] **Step 3: Write the helper**

`src/MangakaSim/TimelineGuidance.cs`:

```csharp
namespace MangakaSim;
/// <summary>Which Helper-Chan messages are new since the timeline last looked. A replaced or trimmed thread logs only its newest message.</summary>
public static class TimelineGuidance
{
    public static IReadOnlyList<string> NewSteps(IReadOnlyList<GuidanceMessage> thread,GuidanceMessage? lastSeen)
    {
        if(lastSeen is null)return thread.Select(m=>m.Step).ToList();
        for(var i=thread.Count-1;i>=0;i--)if(ReferenceEquals(thread[i],lastSeen))return thread.Skip(i+1).Select(m=>m.Step).ToList();
        return thread.Count>0?[thread[^1].Step]:[];
    }
}
```

- [ ] **Step 4: Run the test to verify it passes** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~TimelineGuidanceTests"`
Expected: PASS, 2 tests.

- [ ] **Step 5: Create `godot/DebugMain.Timeline.cs`**

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // T1.10 session timeline (Q32): a private text log next to the saves, attached to "Report a problem" when ticked.
    private SessionTimeline? _timeline;
    private JourneyMilestones? _milestones;
    private GuidanceMessage? _timelineLastMessage;
    private string CareerCode=>SessionTimeline.ShortCareer(_careerId??"");

    private void StartTimeline()
    {
        var smoke=OS.GetCmdlineUserArgs().Any(a=>a.EndsWith("-smoke")||a=="--smoke-test");
        var folder=smoke?Path.Combine(SmokeOutput,"timeline-"+Guid.NewGuid().ToString("N")):ProjectSettings.GlobalizePath("user://");
        _timeline=new SessionTimeline(folder,()=>DateTime.Now);
        var size=GetWindow().Size;
        _timeline.Start($"window {size.X}x{size.Y} text {(int)Math.Round(_presentation.UiScale*100)} theme {(_darkMode?"dark":"light")}");
    }
    private void LogTimeline(string text)=>_timeline?.Record(_state?.Clock.Now,text);
    private void LogNewGuidance(GuidancePreferences prefs)
    {
        foreach(var step in TimelineGuidance.NewSteps(prefs.Thread,_timelineLastMessage))LogTimeline("step "+step);
        _timelineLastMessage=prefs.Thread.LastOrDefault();
    }
    public override void _Notification(int what)
    {
        if(what==NotificationWMCloseRequest)_timeline?.End();
    }
}
```

Before compiling, check `DebugMain` does not already override `_Notification` (`grep -n "_Notification" godot/*.cs`). If it does, add the `NotificationWMCloseRequest` line to that override instead of creating a second one. Also confirm `_careerId` is a `string` field and `_darkMode` is a `bool` field (both are used by existing code in `DebugMain.Alpha.cs`).

- [ ] **Step 6: Add the one-line hooks**

`godot/DebugMain.Management.cs`, right after the line that assigns `_careers=new CareerStore(...)` (about L97):

```csharp
StartTimeline();
```

`godot/DebugMain.cs`, `ScanEvents` (about L141): after the line `_scanIndex = _state.Events.Count;` add

```csharp
if(_milestones is not null)foreach(var id in _milestones.Observe(_state,fresh))LogTimeline("milestone "+id);
```

where `fresh` is the local list of new events that `ScanEvents` already builds before advancing `_scanIndex`. If the local has another name, use it; if it is built after that line, move the hook to just after it is built.

`godot/DebugMain.cs`, `SetSpeed` (about L184): after `var changed=_speed!=speed;` add

```csharp
if(changed)LogTimeline(speed>0?$"speed {speed.ToString(CultureInfo.InvariantCulture)}x":"pause");
```

(add `using System.Globalization;` if the file lacks it).

`godot/DebugMain.cs`, `TryApply` catch (about L235), and the two catches in `godot/DebugMain.Management.cs` (button catch about L72, autosave catch about L243), before the existing error display:

```csharp
LogTimeline("error "+TimelineRedactor.Clean(ex.Message,_state));
```

`godot/DebugMain.Management.cs`, `Navigate` (about L177), at the start of the method after any early return:

```csharp
LogTimeline("screen "+page);
```

`godot/DebugMain.Gui.cs`, `ShowOffice` (about L33), after its early return:

```csharp
LogTimeline("screen Office");
```

`godot/DebugMain.Alpha.cs`, `OpenPhone`: capture whether the phone was already open at the top (`var wasOpen=_phoneOpen;`) and after it opens add

```csharp
if(!wasOpen)LogTimeline("phone");
```

`godot/DebugMain.Alpha.cs`, `RefreshGuidance`: directly after `CareerGuidance.Observe(_state,prefs);` add

```csharp
LogNewGuidance(prefs);
```

`godot/DebugMain.ManagementMenus.cs`:
- Quit handler (about L33), before the tree quits: `_timeline?.End();`
- Begin career (L70-75), after `ResetManagementSession();`: `LogTimeline($"new-career {CareerCode} {_state.Progression.Difficulty} sandbox={_state.Progression.EverSandbox}");`
- `SaveCareer(string name,bool auto=false)` (L132), just before `return`: `LogTimeline($"save {CareerCode} {(auto?"auto":"manual")}");`
- Import in `LoadCareerMenu` (L141), after the imported career is loaded, in both branches: `LogTimeline($"import {CareerCode}");`
- `LoadCareer` (L152), after `_careerId` and `_state` are set: `LogTimeline($"load {CareerCode}");`
- `ResetManagementSession`, at the end:

```csharp
_milestones=new JourneyMilestones(_state);_timelineLastMessage=_presentation.Guidance.Thread.LastOrDefault();
```

- [ ] **Step 7: Add the report checkbox**

`godot/DebugMain.Alpha.cs`, `ReportProblem()` (L305-326):

After the `save` checkbox is created:

```csharp
var timeline=new CheckBox{Text="Attach session timeline",ButtonPressed=true};
```

Add it to the same container as the other two checkboxes, directly after `save`. In `Preview()`, after the career part of the contents text, append:

```csharp
+(timeline.ButtonPressed?", session timeline":"")
```

Next to the existing `Toggled` hooks: `timeline.Toggled+=_=>Preview();`

Change the `ProblemReport.Create` call to:

```csharp
ProblemReport.Create(note.Text,_state,screenshot?picture:null,save?_careers.ExportSnapshot(_careerId,_state,_presentation):null,
    timeline:timeline.ButtonPressed?_timeline?.Files():null)
```

keeping whatever the existing code names the screenshot and save checkbox values.

- [ ] **Step 8: Update the alpha smoke**

`godot/DebugMain.AlphaSmoke.cs` (L78-82). Replace the attachment check with:

```csharp
Check(choices.Length==3&&!choices[0].ButtonPressed&&!choices[1].ButtonPressed&&choices[2].ButtonPressed,"Report attachments: screen and career off, timeline on");
```

Replace the single-entry ZIP check with:

```csharp
Check(zip.Entries.Any(e=>e.FullName=="report.json")&&zip.GetEntry("timeline.log") is {} log&&new StreamReader(log.Open()).ReadToEnd().Contains("session start"),"Report holds report.json and the session timeline");
```

- [ ] **Step 9: Build and run the smoke checks** (needs authorization)

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --smoke-test
& $godot --headless --path godot -- --management-smoke
& $godot --headless --path godot -- --alpha-smoke
```
Expected: warning-free build; all three smoke checks pass. Confirm the exact alpha smoke flag name in the Validate section of `README.md`.

- [ ] **Step 10: Commit** (only if the user asked for commits)

```bash
git add godot/DebugMain.Timeline.cs godot/DebugMain.cs godot/DebugMain.Management.cs godot/DebugMain.Gui.cs godot/DebugMain.Alpha.cs godot/DebugMain.ManagementMenus.cs godot/DebugMain.AlphaSmoke.cs src/MangakaSim/TimelineGuidance.cs tests/MangakaSim.Tests/TimelineGuidanceTests.cs
git commit -m "T1.10: session timeline wiring and report checkbox"
```

---

### Task 5: Practice career

**Files:**
- Modify: `src/MangakaSim/CareerStore.cs` (add the constant; `Import` about L157)
- Modify: `tests/MangakaSim.Tests/CareerPlaytest.cs` (`GuidedPlayer`: field, constructor, `Play`, new `PlayUntil`, new `Preferences`; new test method)
- Modify: `tests/MangakaSim.Tests/MangakaSim.Tests.csproj` (L17)
- Create: `.gitattributes`
- Create: `tests/MangakaSim.Tests/PracticeCareerTests.cs`
- Create (generated by the playtest): `tests/MangakaSim.Tests/Fixtures/Practice - a struggling series.mangaka`

**Interfaces:**
- Consumes: `CareerStore.Save/Export/Import/Load`, `CareerGuidance.Observe/MarkRead`.
- Produces: `CareerStore.PracticeCareer` (string const); the fixture file used by Task 6 and Task 7.

- [ ] **Step 1: Write the failing tests** (`PracticeCareerTests.cs`, part of the normal suite)

```csharp
using Xunit;
namespace MangakaSim.Tests;
public class PracticeCareerTests
{
    static readonly string FixturePath=Path.Combine(AppContext.BaseDirectory,"Fixtures","Practice - a struggling series.mangaka");
    static void Store(Action<CareerStore> use)
    {
        var root=Path.Combine(Path.GetTempPath(),"mangaka-practice-"+Guid.NewGuid().ToString("N"));
        try{use(new CareerStore(root));}finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact]public void Practice_career_keeps_its_identifier_and_name()=>Store(store=>
    {
        var info=store.Import(File.ReadAllBytes(FixturePath));
        Assert.Equal(CareerStore.PracticeCareer,info.Career);
        Assert.Equal("Practice: a struggling series",info.Name);
        var (state,_)=store.Load(info);
        Assert.Equal(CareerDifficulty.Standard,state.Progression.Difficulty);
        Assert.False(state.Progression.EverSandbox);
        Assert.Contains(state.Series,s=>s.BusinessId==state.ControlledBusinessId&&s.Publishing==PublishingStatus.Serialized);
        Assert.DoesNotContain(state.Events,e=>e.Type==EventType.CancellationWarning&&state.Series.Any(s=>s.Id==e.SeriesId&&s.BusinessId==state.ControlledBusinessId));
    });
    [Fact]public void Importing_the_practice_career_twice_gives_two_loadable_saves()=>Store(store=>
    {
        var first=store.Import(File.ReadAllBytes(FixturePath));var second=store.Import(File.ReadAllBytes(FixturePath));
        Assert.NotEqual(first.Snapshot,second.Snapshot);
        Assert.Equal(first.Career,second.Career);
        store.Load(first);store.Load(second);
    });
    [Fact]public void Other_imports_still_get_a_new_identifier()=>Store(store=>
    {
        var state=GameState.NewGame(0);var saved=store.Save(Guid.NewGuid().ToString("N"),"Mine",state,new CareerPresentation());
        var imported=store.Import(store.Export(saved));
        Assert.NotEqual(saved.Career,imported.Career);
        Assert.NotEqual(CareerStore.PracticeCareer,imported.Career);
    });
    [Fact]public void Playing_on_brings_the_cancellation_warning()=>Store(store=>
    {
        var (state,_)=store.Load(store.Import(File.ReadAllBytes(FixturePath)));
        var start=state.Clock.Now;
        bool Warned()=>state.Events.Any(e=>e.Type==EventType.CancellationWarning&&e.Time>=start&&state.Series.Any(s=>s.Id==e.SeriesId&&s.BusinessId==state.ControlledBusinessId));
        for(var d=0;d<45&&!Warned();d++)state.Advance(24);
        // If this fails, report it; do not tune balance to make it pass (no balance change in T1.10).
        Assert.True(Warned(),$"No cancellation warning within 45 days of {start:d MMM yyyy}");
    });
}
```

Note on the last test: the fixture is saved 14 days before the warning in the scripted player's run. Without the scripted player's choices the career may drift, so 45 days gives room. If the warning does not come, stop and report the finding rather than changing balance or the fixture date on your own.

- [ ] **Step 2: Run the tests to verify they fail** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~PracticeCareerTests"`
Expected: build FAIL, `CareerStore.PracticeCareer` does not exist.

- [ ] **Step 3: Keep the practice identifier on import**

`src/MangakaSim/CareerStore.cs`, inside `CareerStore`, next to the other members near the top:

```csharp
    /// <summary>The fixed identifier of the T1.10 practice career, kept on import so its short code (70ac71) is recognisable in a timeline.</summary>
    public const string PracticeCareer="70ac71ce0000400080000000a1b2c3d4";
```

In `Import`, replace `var career=Guid.NewGuid().ToString("N");` with:

```csharp
var career=data.Career==PracticeCareer?PracticeCareer:Guid.NewGuid().ToString("N");
```

Importing the practice career twice adds a second snapshot to the same career folder, which `Save` already supports.

- [ ] **Step 4: Refactor the playtest player and add the fixture generator**

In `GuidedPlayer` (`CareerPlaytest.cs`):

Add a field next to `_start`:

```csharp
        private DateTime _nextMonth;
```

In the constructor, after `_start = State.Clock.Now;` add `_nextMonth = _start;`.

Add the property after `public GameState State { get; }`:

```csharp
        public GuidancePreferences Preferences => _guide;
```

Replace `Play` with:

```csharp
        public void Play(int years) => PlayUntil(_start.AddYears(years));

        public void PlayUntil(DateTime end)
        {
            while (State.Clock.Now < end)
            {
                if (State.Clock.Hour == 9) Decide();
                var working = State.ControlledStaff.Any(p => p.Schedule.IsRegularHour(State.Clock.Now));
                var cost = working ? WorkSecondsPerHour : NightSecondsPerHour;
                _seconds += cost; if (working) _workSeconds += cost;
                var year = (int)((State.Clock.Now - _start).TotalDays / 365.25);
                var pace = _pace.GetValueOrDefault(year);
                if (working) pace.Work++; else pace.Night++;
                _pace[year] = pace;
                State.Advance(1);
                ReadEvents();
                if (State.Clock.Now >= _nextMonth)
                {
                    _monthly.Add($"| {State.Clock.Now:yyyy-MM} | {Minutes / 60:F1} | {State.PersonalMoney:N0} | {State.Money:N0} | " +
                        $"{State.Series.Count(s => s.Publishing == PublishingStatus.Serialized)} | {State.ControlledStaff.Count()} | {Guide().Id} |");
                    _nextMonth = _nextMonth.AddMonths(1);
                }
            }
        }
```

Copy the loop body from the current `Play` exactly if it differs from the above in any line; the only intended changes are the method split and `nextMonth` becoming the `_nextMonth` field. `Three_year_guided_career` must produce identical reports before and after.

Add the fixture generator to `CareerPlaytest` (after `Three_year_guided_career`):

```csharp
    // T1.10 practice career (Part 3): seed 0 on Standard, saved two in-game weeks before the first cancellation warning.
    [Fact]
    public void Practice_career_fixture()
    {
        var scout = new GuidedPlayer(0, CareerDifficulty.Standard);
        var warning = DateTime.MinValue;
        for (var year = 1; year <= Years && warning == DateTime.MinValue; year++)
        {
            scout.Play(year);
            warning = scout.State.Events.FirstOrDefault(e => e.Type == EventType.CancellationWarning &&
                scout.State.Series.Any(s => s.Id == e.SeriesId && s.BusinessId == scout.State.ControlledBusinessId))?.Time ?? DateTime.MinValue;
        }
        Assert.True(warning != DateTime.MinValue, "Seed 0 on Standard had no cancellation warning in three years");
        Assert.True(warning.Year < 1999, $"Warning came late: {warning:d MMM yyyy}");

        var player = new GuidedPlayer(0, CareerDifficulty.Standard);
        player.PlayUntil(warning.AddDays(-14));
        var state = player.State;
        Assert.DoesNotContain(state.Events, e => e.Type == EventType.CancellationWarning &&
            state.Series.Any(s => s.Id == e.SeriesId && s.BusinessId == state.ControlledBusinessId));
        Assert.Contains(state.Series, s => s.BusinessId == state.ControlledBusinessId && s.Publishing == PublishingStatus.Serialized);

        var view = new CareerPresentation { Guidance = player.Preferences, Page = "Office" };
        CareerGuidance.Observe(state, view.Guidance);
        CareerGuidance.MarkRead(view.Guidance);
        var root = Path.Combine(Path.GetTempPath(), "mangaka-practice-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CareerStore(root);
            var info = store.Save(CareerStore.PracticeCareer, "Practice: a struggling series", state, view);
            var path = Path.Combine(RepoRoot(), "tests", "MangakaSim.Tests", "Fixtures", "Practice - a struggling series.mangaka");
            File.WriteAllBytes(path, store.Export(info));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
```

`GuidedPlayer.Play(year)` runs from the start each call only up to `_start.AddYears(year)`, continuing where the previous call stopped, so the scout loop plays each year once. Two independent players on the same seed and commands stay identical because the simulation is deterministic.

Confirm `CareerPresentation.Page` is settable (`grep -n "Page" src/MangakaSim/CareerStore.cs`), since the Godot side creates it with `new(){Page="Office"}`.

- [ ] **Step 5: Copy the fixture to the test output and mark it binary**

`tests/MangakaSim.Tests/MangakaSim.Tests.csproj`, after the L17 `<None Update="Fixtures/*.json" .../>`:

```xml
    <None Update="Fixtures/*.mangaka" CopyToOutputDirectory="PreserveNewest" />
```

New file `.gitattributes` at the repository root (LF is fine for this one-line file, but match CRLF like the docs):

```
*.mangaka binary
```

- [ ] **Step 6: Generate the fixture** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~Practice_career_fixture"`
Expected: PASS, and `tests/MangakaSim.Tests/Fixtures/Practice - a struggling series.mangaka` exists (a few hundred KB).

- [ ] **Step 7: Run the practice career tests and the full normal suite** (needs authorization)

Run:
```powershell
dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~PracticeCareerTests"
dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest"
```
Expected: 4 new tests pass; the full normal suite passes (550 before this plan plus the new tests).

- [ ] **Step 8: Check the playtest report is unchanged** (needs authorization)

Run: `dotnet test tests/MangakaSim.Tests --filter "FullyQualifiedName~Three_year_guided_career"` then `git diff --stat TestResults` is not available (git-ignored), so compare `TestResults/career-playtest/seed0-Standard.md` with a copy taken before Step 4.
Expected: identical apart from any timestamp lines.

- [ ] **Step 9: Commit** (only if the user asked for commits)

```bash
git add .gitattributes src/MangakaSim/CareerStore.cs tests/MangakaSim.Tests/CareerPlaytest.cs tests/MangakaSim.Tests/MangakaSim.Tests.csproj tests/MangakaSim.Tests/PracticeCareerTests.cs "tests/MangakaSim.Tests/Fixtures/Practice - a struggling series.mangaka"
git commit -m "T1.10: practice career fixture"
```

---

### Task 6: Journey walk-through (`--journey-smoke`)

**Files:**
- Create: `godot/DebugMain.JourneySmoke.cs`
- Modify: `godot/DebugMain.cs` (after the display sweep dispatch, about L101)

**Interfaces:**
- Consumes: `LogTimeline`, `_timeline`, `_milestones` (Task 4); `CareerStore.PracticeCareer` and the practice fixture (Task 5); existing smoke helpers `Check`, `Press`, `SettleUi`, `CaptureSmokeImage`, `SmokeOutput`, `NewCareerMenu`, `OpenWorkspace`, `Navigate`, `OpenPrinting`, `_alphaPrinter`, `_alphaCopies`, `SaveCareer`, `LoadCareer`, `ScanEvents`, `RefreshGuidance`, `TryApply`, `ResetManagementSession`, `ShowOffice`.
- Produces: the `--journey-smoke` flag; optional `--old-career=<path>`.

- [ ] **Step 1: Add the dispatch**

`godot/DebugMain.cs`, directly after the line that dispatches `--display-sweep-smoke` (about L101):

```csharp
if (OS.GetCmdlineUserArgs().Contains("--journey-smoke")) CallDeferred(nameof(RunJourneySmoke));
```

- [ ] **Step 2: Create `godot/DebugMain.JourneySmoke.cs`**

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MangakaSim;
using MangakaSim.Rules;

namespace MangakaGame;

public partial class DebugMain
{
    // T1.10 Part 4: every T1.1 step on Standard through the real screens, saving and reloading at each step, then the
    // setback through the practice career and the two oldest save shapes. Evidence for T1.6; not human play.
    // Steps whose form controls are not driven here apply the same command the screen's button sends, through TryApply.
    private async void RunJourneySmoke()
    {
        SetProcess(false);_homeOffice.SetProcess(false);_officeView.SetProcess(false);
        try
        {
            GetWindow().Size=new(1920,1080);Directory.CreateDirectory(SmokeOutput);
            _careers=new CareerStore(Path.Combine(SmokeOutput,"journey-"+Guid.NewGuid().ToString("N")));
            NewCareerMenu();Press("Begin career");await SettleUi();_helperPopup.Hide();
            Check(_state.Progression.Difficulty==CareerDifficulty.Standard&&!_state.Progression.EverSandbox,"Journey starts on Standard without Sandbox");
            await JourneyStep("new-career");

            // 1. First doujin.
            OpenWorkspace("Production");await SettleUi();
            TryApply(new CreateDoujinCommand("First pages","adventure"));
            var doujinId=_state.Series[^1].Id;
            for(var d=0;d<180&&Doujin().Volumes.Count==0;d++){_state.Advance(24);ScanEvents();}
            Check(Doujin().Volumes.Count>0,"First doujin is finished");
            await JourneyStep("first-doujin");

            // 2. First print run through the real printing screen, then the first sale.
            await Print(Doujin().Id,30);
            for(var d=0;d<60&&Doujin().Volumes[0].CopiesSold==0;d++){_state.Advance(24);ScanEvents();}
            Check(Doujin().Volumes[0].CopiesSold>0,"First doujin sells");
            await JourneyStep("first-sale");

            // 3. A later release that sells: a second print run.
            await Print(Doujin().Id,30);
            var sold=Doujin().Volumes[0].CopiesSold;
            for(var d=0;d<90&&Doujin().Volumes[0].CopiesSold==sold;d++){_state.Advance(24);ScanEvents();}
            Check(Doujin().Volumes[0].CopiesSold>sold,"Second print run sells");
            await JourneyStep("later-sale");

            // 4. Pitch and serialization.
            Navigate("New series");await SettleUi();
            var genre=_state.PublisherCatalog.Magazines.SelectMany(m=>m.GenreAffinities.Keys.Select(g=>(m,g)))
                .OrderByDescending(p=>PitchRules.Chance(p.m.Tier,60,_state.EffectiveReputation,p.m.Affinity(p.g),_state.GenrePopularity(p.g)))
                .ThenBy(p=>p.m.Id).ThenBy(p=>p.g).First().g;
            TryApply(new CreateSeriesCommand("Ink and Thunder",genre,Cadence.Monthly,16));
            var serialId=_state.Series[^1].Id;
            var pitched=false;
            for(var d=0;d<720&&Serial().Contract is null;d++)
            {
                if(Serial().Publishing==PublishingStatus.Offered){OpenWorkspace("Publishing");await SettleUi();TryApply(new AcceptOfferCommand(serialId));continue;}
                if(Serial().Publishing==PublishingStatus.Unpublished&&CareerGuidance.PitchOutlooks(_state,Serial()).FirstOrDefault(o=>o.Open) is {} best)
                {
                    OpenWorkspace("Publishing");await SettleUi();TryApply(new PitchSeriesCommand(serialId,best.Magazine.Id));
                    if(!pitched){pitched=true;await JourneyStep("first-pitch");}
                }
                _state.Advance(24);ScanEvents();
            }
            Check(Serial().Contract is not null,"Series is serialized");
            await JourneyStep("serialized");
            for(var d=0;d<240&&Serial().ChaptersPublished==0;d++){_state.Advance(24);ScanEvents();}
            Check(Serial().ChaptersPublished>0,"First magazine chapter is published");
            await JourneyStep("first-chapter");

            // 5. First hire, furnishing the room first if there is no free desk.
            OpenWorkspace("Recruitment");await SettleUi();
            bool Open(Candidate c)=>!c.Recruited&&c.ExpiresAt>_state.Clock.Now&&(c.IntroductionBusinessId is null||c.IntroductionBusinessId==_state.ControlledBusinessId);
            for(var d=0;d<60&&!_state.Candidates.Any(Open);d++){if(_state.Recruitment is null)TryApply(new RecruitStaffCommand());_state.Advance(24);ScanEvents();}
            if(_state.Candidates.FirstOrDefault(Open) is {} candidate)
            {
                var location=_state.Protagonist.Employment!.LocationId;var salary=Math.Max(StudioRules.MinimumMonthlySalary,candidate.ExpectedSalary);
                var staff=_state.ControlledStaff.Count();
                TryApply(new HireStaffCommand(candidate.Id,location,salary));
                if(_state.ControlledStaff.Count()==staff)
                {
                    var arrangement=_state.ArrangeOffice(location,true,true);
                    TryApply(new ApplyOfficeLayoutCommand(location,_state.OfficeRevision,arrangement.Placements,arrangement.Purchases,[]));
                    TryApply(new HireStaffCommand(candidate.Id,location,salary));
                }
            }
            Check(_state.ControlledStaff.Any(p=>p.Id!=_state.ProtagonistPersonId),"First hire joined");
            await JourneyStep("first-hire");

            // 6. The setback, through the practice career.
            var practice=Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","tests","MangakaSim.Tests","Fixtures","Practice - a struggling series.mangaka");
            var info=_careers.Import(File.ReadAllBytes(practice));LogTimeline($"import {SessionTimeline.ShortCareer(info.Career)}");
            Check(info.Career==CareerStore.PracticeCareer,"Practice career keeps its identifier");
            LoadCareer(info);await SettleUi();_helperPopup.Hide();
            bool Warned()=>_state.Events.Any(e=>e.Type==EventType.CancellationWarning&&_state.Series.Any(s=>s.Id==e.SeriesId&&s.BusinessId==_state.ControlledBusinessId));
            for(var d=0;d<45&&!Warned();d++){_state.Advance(24);ScanEvents();}
            Check(Warned(),"Practice career reaches the cancellation warning");
            await JourneyStep("setback");

            // 7. The oldest save fixture and an alpha.11 career: each loads, or is refused with a clear message.
            var v1=Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","tests","MangakaSim.Tests","Fixtures","v1-minimal.json");
            Check(LoadsOrRefuses(()=>GameState.ImportSupported(File.ReadAllText(v1))),"Oldest save fixture loads or is refused clearly");
            var oldCareer=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--old-career="))?["--old-career=".Length..]
                ??Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","TestResults","fresh-player","alpha11-career.mangaka");
            if(File.Exists(oldCareer))
                Check(LoadsOrRefuses(()=>{var old=_careers.Import(File.ReadAllBytes(oldCareer));LoadCareer(old);}),"alpha.11 career loads or is refused clearly");
            else GD.Print("JOURNEY NOTE: no alpha.11 career at "+oldCareer+"; make one with the alpha.11 package and rerun.");

            // 8. The timeline recorded the journey.
            var log=string.Join("",_timeline!.Files().Select(f=>f.Text));
            foreach(var line in new[]{"session start","new-career","milestone first-doujin-completed","milestone first-sale","milestone later-sale",
                "milestone first-pitch","milestone serialization-accepted","milestone first-magazine-chapter","milestone first-hire",
                "milestone cancellation-warning","screen ","step ","save ","load ","import 70ac71"})
                Check(log.Contains(line),"Timeline records "+line.Trim());
            Check(!log.Contains("First pages")&&!log.Contains("Ink and Thunder")&&!log.Contains(SmokeOutput),"Timeline holds no titles or paths");
            File.WriteAllText(Path.Combine(SmokeOutput,"journey-timeline.log"),log);

            GD.Print($"JOURNEY SMOKE PASSED: {_smokeChecks} checks.");var tree=GetTree();tree.CreateTimer(.1).Timeout+=()=>tree.Quit();QueueFree();

            Series Doujin()=>_state.Series.First(s=>s.Id==doujinId);
            Series Serial()=>_state.Series.First(s=>s.Id==serialId);
        }
        catch(Exception ex)
        {
            GD.PushError($"JOURNEY SMOKE FAILED: {ex.Message}\n{ex.StackTrace}");GetTree().Quit(1);
        }
    }

    // Captures the step, then saves, reloads and checks the career continues identically for 48 hours.
    // LoadCareer replaces _state, so callers re-fetch series and people by id after every step.
    private async Task JourneyStep(string step)
    {
        ScanEvents();RefreshGuidance();await SettleUi();await CaptureSmokeImage("journey-"+step);
        var copy=GameState.FromJson(_state.ToJson());
        var info=SaveCareer("Journey "+step);LoadCareer(info);await SettleUi();_helperPopup.Hide();
        copy.Advance(48);_state.Advance(48);ScanEvents();
        Check(copy.ToJson()==_state.ToJson(),$"Journey {step}: saved and loaded career continues identically for 48 hours");
    }

    // Orders a print run through the real printing screen: copy shop, the given number of copies, then the order button.
    private async Task Print(int seriesId,int copies)
    {
        var runs=_state.PrintRuns.Count;
        OpenPrinting(seriesId);await SettleUi();
        _alphaPrinter.Select(0);_alphaCopies.Value=copies;await SettleUi();
        Press("Order this print run");await SettleUi();
        Check(_state.PrintRuns.Count==runs+1,$"Printing screen ordered {copies} copies");
        _state.Advance(24*8);ScanEvents();
    }

    private static bool LoadsOrRefuses(Action load)
    {
        try{load();return true;}
        catch(InvalidDataException ex){GD.Print("JOURNEY NOTE: refused with: "+ex.Message);return !string.IsNullOrWhiteSpace(ex.Message);}
    }
}
```

Before compiling, confirm these existing names and shapes, adjusting the calls (not the checks) if they differ:
- `LoadCareer(CareerSaveInfo)` signature (`grep -n "void LoadCareer" godot/*.cs`).
- `OpenPrinting(int series,int book=0)`, `_alphaPrinter` (OptionButton) and `_alphaCopies` (SpinBox).
- `GameState.ImportSupported(string)` accepts the raw `v1-minimal.json` text (the store passes the snapshot's state JSON). If it expects something else, use the same call the save tests use for `v1-minimal.json` (`grep -n "v1-minimal" tests/MangakaSim.Tests/*.cs`).
- `ProjectSettings.GlobalizePath("res://")` is the `godot` folder when run with `--path godot`, so `..` is the repository root.

- [ ] **Step 3: Build and run the journey smoke** (needs authorization)

Run:
```powershell
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --path godot -- --journey-smoke
& $godot --path godot -- --journey-smoke --capture
```
Expected: `JOURNEY SMOKE PASSED`, with a note if no alpha.11 career exists yet. The captured run writes `journey-*.avif` to the usual `TestResults` smoke folder.

If there is no alpha.11 career, ask the user before making one: it means starting the alpha.11 package from `builds/`, playing a few in-game days and exporting the career to `TestResults/fresh-player/alpha11-career.mangaka`. Then rerun.

- [ ] **Step 4: Review the captures**

Look at each `journey-*.avif` myself. Record anything confusing, cut off or wrong in the completion record. Fix what is found before alpha.12 is packaged (spec Part 4), each fix in the smallest scope, and rerun Step 3.

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add godot/DebugMain.JourneySmoke.cs godot/DebugMain.cs
git commit -m "T1.10: journey walk-through smoke check"
```

---

### Task 7: Tester kit in the package

**Files:**
- Create: `docs/superpowers/fresh-player-kit/READ ME FIRST.txt`
- Create: `docs/superpowers/fresh-player-kit/Questionnaire.txt`
- Modify: `scripts/package-alpha.ps1` (after L26-27, where the build output folder `$output` is filled)

**Interfaces:**
- Consumes: the practice fixture (Task 5); `ProblemReport.Build` (Task 3) for the package name.
- Produces: an alpha.12 ZIP holding the game, the two text files and the practice career.

- [ ] **Step 1: Write `READ ME FIRST.txt`** (CRLF, plain text, one page)

```
MANGAKA STUDIO, private test (alpha.12)
=======================================

Thank you for testing. Mangaka Studio is a manga career and studio
management game that starts in Tokyo in 1996. You guide a young manga
artist from making self-published comics (doujin) at their parents'
house to magazine serialization and running a studio. Helper-Chan, the
assistant with the clipboard, texts you tips on her phone as you go.

WHAT WE ASK
- Start a new career on Standard difficulty. Do not turn on Sandbox.
- Play until you hire your first staff member. That takes about 1 to 1.5
  hours, in as many sittings as you like. The game saves as you play.
- After that, keep going for as long as you enjoy it, up to three
  in-game years.
- If nothing went wrong for your series by the end (a rejected pitch, a
  cancellation warning or a cancellation), import the practice career
  (see below) and play until Helper-Chan warns about cancellation.

THE ONE RULE
Please do not ask anyone or look anything up about the game. If you do,
that is fine, just say so in the questionnaire. Giving up is also fine
and still very useful: please send the report anyway.

STARTING THE GAME
Unzip the folder and run MangakaStudio.exe. Windows may show a blue
"Windows protected your PC" box, because this test build is not signed.
Click "More info", then "Run anyway".

THE PRACTICE CAREER
Main menu, "Load career", "Import career or previous save", then choose
"Practice - a struggling series.mangaka" from this folder.

SENDING YOUR REPORT
1. Main menu, "Report a problem".
2. Write a short note (anything, even "finished").
3. Keep "Attach session timeline" ticked and also tick "Attach career".
4. Export, and send the ZIP file together with your filled-in
   Questionnaire.txt to the person who gave you the game.

WHAT THE REPORT CONTAINS
Your note, the game version, your career save, and a session timeline:
a plain-text list of what happened when (screens opened, speed changes,
game milestones, saves and errors). It contains no names you typed, no
file paths and nothing about your computer except the window size. You
can open the ZIP and read everything before sending. Nothing is uploaded
by the game; you decide how to send it.

KNOWN GAPS
There is no music yet.
```

Check the menu labels against the game ("Load career", "Import career or previous save", "Report a problem") with `grep -n "Import career or previous save\|Report a problem\|\"Load career\"" godot/*.cs` and correct the text if a label differs. The executable name comes from `scripts/package-alpha.ps1`; correct `MangakaStudio.exe` if the script names it differently.

- [ ] **Step 2: Write `Questionnaire.txt`** (CRLF)

```
MANGAKA STUDIO, alpha.12 questionnaire
======================================
Write your answers under each question and send this file back with the
report ZIP. Short answers are fine.

1. Did anyone help you, or did you look anything up? What about?

2. Where were you stuck or confused, and how did you get past it?

3. How did the pace feel at the start, while waiting for your series to
   debut, and later on?

4. Did the 32x speed help, and did it stop at the right moments?

5. Did money changes make sense? Was anything a surprise?

6. What went wrong for your series (rejection, warning or cancellation)?
   Did it feel fair, and did you know what to do next?

7. Did saving and loading work?

8. Did the game crash or show an error?

9. Would you keep playing? (1 to 5) Why?

10. What is the one thing you would change?

11. What screen size did you play on, and was it a laptop or a desktop?

12. Anything else?
```

- [ ] **Step 3: Copy the kit into the package**

`scripts/package-alpha.ps1`, after the lines that export the game into `$output` (about L26-27), before the ZIP is made:

```powershell
$kit = Join-Path $PSScriptRoot '..\docs\superpowers\fresh-player-kit'
Copy-Item -LiteralPath (Join-Path $kit 'READ ME FIRST.txt') -Destination $output
Copy-Item -LiteralPath (Join-Path $kit 'Questionnaire.txt') -Destination $output
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\tests\MangakaSim.Tests\Fixtures\Practice - a struggling series.mangaka') -Destination $output
```

Read L1-40 of the script first and use its own variable names for the repository root and the output folder if they differ from `$PSScriptRoot` and `$output`.

- [ ] **Step 4: Convert the kit files to CRLF**

Run: `unix2dos "docs/superpowers/fresh-player-kit/READ ME FIRST.txt" docs/superpowers/fresh-player-kit/Questionnaire.txt`

- [ ] **Step 5: Commit** (only if the user asked for commits)

```bash
git add docs/superpowers/fresh-player-kit scripts/package-alpha.ps1
git commit -m "T1.10: tester kit in the alpha package"
```

---

### Task 8: Records

**Files:**
- Create: `docs/superpowers/fresh-player-test-preparation-completion.md`
- Create: `docs/superpowers/fresh-player-test-findings.md` (template)
- Modify: `docs/superpowers/specs/2026-09-22-roadmap.md` (T1.10 line and Tier 1 fix list)
- Modify: `CLAUDE.md` (section 5, T1.10 bullet)
- Modify: `README.md` (latest package name, only after alpha.12 is packaged)
- Create after an authorized build only: `docs/superpowers/alpha-12-build-verification.md`

- [ ] **Step 1: Write the completion record**

`docs/superpowers/fresh-player-test-preparation-completion.md`, in the style of `display-sweep-completion.md`: what changed (timeline, report checkbox, practice career, journey smoke, kit, alpha.12 name), what was verified and how (unit tests, smoke checks, my review of the journey captures, stated separately), anything the journey smoke found and how it was fixed, known limitations (first-hire can log again if the only hire left; no crash handler, the missing end line only hints at a crash), and the next step (the user recruits two fresh testers and sends alpha.12, asked for when that step arrives).

- [ ] **Step 2: Write the findings template**

`docs/superpowers/fresh-player-test-findings.md` with empty sections that match spec Part 5: testers A and B (round 1) and C (round 2), time to each T1.1 step from the timelines, pauses over 5 minutes, share of time at each speed, questionnaire summary, findings with proposed triage (always fixed, fixed if cheap or seen twice, deferred with a reason), and a round 2 section. No names or contact details.

- [ ] **Step 3: Update the roadmap, CLAUDE.md and README**

Roadmap T1.10 line: add "preparation done (date), see `../fresh-player-test-preparation-completion.md`; round 1 not yet run". CLAUDE.md section 5 T1.10 bullet: the same, briefly. README latest package: only once alpha.12 exists.

- [ ] **Step 4: Package alpha.12 and write the build verification** (needs authorization)

Run:
```powershell
dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest"
dotnet build MangakaGame.sln -warnaserror
& $godot --headless --editor --path godot --import --quit
& $godot --headless --path godot -- --smoke-test
& $godot --headless --path godot -- --management-smoke
& $godot --headless --path godot -- --journey-smoke
& $godot --headless --path godot -- --display-sweep-smoke
scripts/package-alpha.ps1 -Godot $godot
```
Expected: all pass; `builds/MangakaStudio-0.8.0-private-alpha.12-Windows.zip` holds the game, `READ ME FIRST.txt`, `Questionnaire.txt` and the practice career. Unzip it to a temporary folder, start it, import the practice career, export a problem report, and confirm the ZIP holds `timeline.log`. Write `alpha-12-build-verification.md` in the style of `alpha-11-build-verification.md`, separating automated checks, rendered checks and my own look at the package.

- [ ] **Step 5: Convert the records to CRLF**

Run: `unix2dos` on every Markdown file created or changed in this task.

- [ ] **Step 6: Commit** (only if the user asked for commits)

```bash
git add docs/superpowers CLAUDE.md README.md
git commit -m "T1.10: preparation records and alpha.12"
```

## Verification (only with authorization)

Nothing in this plan is verified until the user authorizes each run. When authorized:

- **Automated tests:** the new `SessionTimelineTests`, `JourneyMilestonesTests`, `TimelineGuidanceTests`, `PracticeCareerTests` and the three `AlphaTests`, plus the full normal suite (`Category!=Playtest`).
- **Playtest:** `Practice_career_fixture` generates the fixture; `Three_year_guided_career` report unchanged by the `PlayUntil` refactor.
- **Smoke checks:** `--smoke-test`, `--management-smoke`, the alpha smoke with its updated attachment checks, `--display-sweep-smoke` (no regressions from the new checkbox), and `--journey-smoke`.
- **Visual review:** my own look at the `journey-*.avif` captures, described as a rendered check plus my review, not human play.
- **Package check:** alpha.12 ZIP contents and one exported problem report from the packaged game.
- **Real playtesting:** only the testers' rounds count as human play; they start after the user sends alpha.12.

## Execution order and dependencies

Tasks 1 and 2 are independent. Task 3 needs Task 1's `Files()` shape. Task 4 needs 1 to 3. Task 5 is independent of 1 to 4 except for sharing the test project. Task 6 needs 4 and 5. Task 7 needs 5. Task 8 comes last.
