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
    // Fresh-player finding A8: a staff member called Ren turned "current" into "cur[name]t".
    [Fact]public void Redactor_replaces_whole_names_only()
    {
        var state=GameState.NewGame(0);state.People[0].Name="Ren";
        Assert.Equal("Finish all current work.",TimelineRedactor.Clean("Finish all current work.",state));
        Assert.Equal("[name] is busy.",TimelineRedactor.Clean("Ren is busy.",state));
        Assert.Equal("Ask [name]'s editor.",TimelineRedactor.Clean("Ask Ren's editor.",state));
    }
    [Fact]public void Redactor_removes_the_windows_user_and_machine_names()
    {
        var clean=TimelineRedactor.Clean($"Access denied for {Environment.UserName} on {Environment.MachineName}",null);
        if(Environment.UserName.Length>=3)Assert.DoesNotContain(Environment.UserName,clean,StringComparison.OrdinalIgnoreCase);
        if(Environment.MachineName.Length>=3)Assert.DoesNotContain(Environment.MachineName,clean,StringComparison.OrdinalIgnoreCase);
    }
}
