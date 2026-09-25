using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

/// <summary>Schematic geographic view; no implied street routes or ward boundaries.</summary>
public partial class TokyoMap : Control
{
    public string Origin { get; set; } = "Nerima";
    public string SelectedDistrict { get; set; } = "Koto";
    public Action<string>? DistrictSelected { get; set; }
    private Vector2 Point(string name)
    {
        var p=TokyoProperties.Centres[name];
        return new Vector2(45+(float)((p.Lon-139.5)/.65)*(Size.X-90),35+(float)((35.9-p.Lat)/.5)*(Size.Y-70));
    }
    public override void _Ready()
    { CustomMinimumSize=new Vector2(650,390); MouseFilter=MouseFilterEnum.Stop; Resized+=QueueRedraw; }
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero,Size),new Color("15232c"));
        var from=Point(Origin); var to=Point(SelectedDistrict);
        DrawLine(from,to,new Color("f4c86c"),2,true);
        foreach(var name in TokyoProperties.Centres.Keys)
        {
            var p=Point(name); var active=name==SelectedDistrict||name==Origin;
            DrawCircle(p,active?7:4,active?new Color("f4c86c"):new Color("81b4c7"),true,-1,true);
            if(active || name is "Chiba" or "Urawa" or "Yokohama" or "Mitaka")
                DrawString(ThemeDB.FallbackFont,p+new Vector2(10,-7),name,HorizontalAlignment.Left,-1,16,new Color("edf0e8"));
        }
        DrawString(ThemeDB.FallbackFont,new Vector2(16,24),"N ↑  Greater Tokyo • select a dot to inspect travel",HorizontalAlignment.Left,-1,18,Colors.White);
    }
    public override void _GuiInput(InputEvent input)
    {
        if(input is not InputEventMouseButton {Pressed:true,ButtonIndex:MouseButton.Left} click) return;
        var nearest=TokyoProperties.Centres.Keys.OrderBy(n=>Point(n).DistanceSquaredTo(click.Position)).First();
        if(Point(nearest).DistanceTo(click.Position)>20) return;
        SelectedDistrict=nearest;DistrictSelected?.Invoke(nearest);QueueRedraw();
    }
}
