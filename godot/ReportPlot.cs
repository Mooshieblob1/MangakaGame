using System;
using System.Linq;
using Godot;

namespace MangakaGame;

public partial class ReportPlot : Control
{
    private readonly (DateTime At,double Value)[] _points;
    private readonly bool _rank;
    public bool DarkMode {get;set;}
    public ReportPlot():this([],false){}
    public ReportPlot((DateTime At,double Value)[] points,bool rank=false)
    {
        // Bucket long series for drawing only; the source records remain untouched.
        _points=points.OrderBy(p=>p.At).ToArray();_rank=rank;MouseFilter=MouseFilterEnum.Stop;
        Resized+=QueueRedraw;
        MouseEntered+=QueueRedraw;
    }
    public override void _GuiInput(InputEvent ev)
    {
        if(ev is InputEventMouseMotion motion&&_points.Length>0)
        {var at=_points[0].At.AddTicks((long)(Math.Clamp((motion.Position.X-12)/Math.Max(1,Size.X-24),0,1)*(_points[^1].At-_points[0].At).Ticks));var p=_points.MinBy(p=>Math.Abs((p.At-at).Ticks));TooltipText=$"{p.At:d MMM yyyy}: {p.Value:N0}";}
    }
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero,Size),new(DarkMode?"1b252d":"ece8dc"));if(_points.Length==0)return;
        var max=Math.Max(1,_points.Max(p=>p.Value));var min=_rank?1:Math.Min(0,_points.Min(p=>p.Value));
        var font=ThemeDB.FallbackFont;DrawString(font,new(8,18),$"{(_rank?min:max):N0}",fontSize:13,modulate:new(DarkMode?"cbdadf":"52615d"));
        for(var i=1;i<4;i++)DrawLine(new(8,25+(Size.Y-42)*i/4),new(Size.X-8,25+(Size.Y-42)*i/4),new(DarkMode?"3e4e57":"d8d6c9"));
        Vector2 Point(int n){var value=(_points[n].Value-min)/Math.Max(1,max-min);var x=(_points[n].At-_points[0].At).TotalHours/Math.Max(1,(_points[^1].At-_points[0].At).TotalHours);return new(12+(Size.X-24)*(float)x,25+(Size.Y-42)*(float)(_rank?value:1-value));}
        var stride=Math.Max(1,_points.Length/400);var previous=Point(0);
        for(var i=stride;i<_points.Length;i+=stride){var next=Point(i);DrawLine(previous,next,new(DarkMode?"85d8ca":"397f82"),2,true);previous=next;}
        DrawLine(previous,Point(_points.Length-1),new(DarkMode?"85d8ca":"397f82"),2,true);
        DrawCircle(Point(_points.Length-1),3,new("b5684e"));
    }
}
