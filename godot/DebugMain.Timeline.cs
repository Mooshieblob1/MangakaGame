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
    private bool _timelineQuiet;
    // During the overnight transition only the speed and screen lines are held back; guidance and errors still log.
    private void LogTimeline(string text)
    {
        if(_timelineQuiet&&(text.StartsWith("speed ")||text.StartsWith("screen ")||text=="pause"))return;
        _timeline?.Record(_state?.Clock.Now,text);
    }
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
