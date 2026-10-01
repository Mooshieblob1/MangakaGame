using System;
using Godot;
using MangakaSim;

namespace MangakaGame;

/// <summary>A small, read-only floor plan for the studio overview.</summary>
public partial class StudioPlanPreview : Control
{
    public GameState State { get; set; }=null!;
    public StudioLocation Location { get; set; }=null!;
    public bool DarkMode { get; set; }
    // Floor plans follow the logo palette (final review, spec 2026-09-29).
    public static Color Floor(bool dark)=>new(BrandPalette.For(dark).Wash);
    public override void _Ready(){MouseFilter=MouseFilterEnum.Ignore;Resized+=QueueRedraw;}
    public override void _Draw()
    {
        if(State is null||Location is null)return;
        var plan=OfficeCatalog.Plan(Location);
        var scale=Math.Min((Size.X-24)/plan.Width,(Size.Y-20)/plan.Depth);
        var origin=(Size-new Vector2(plan.Width,plan.Depth)*scale)/2;
        var floor=new Rect2(origin,new Vector2(plan.Width,plan.Depth)*scale);
        DrawRect(floor,Floor(DarkMode));
        DrawRect(floor,new Color(BrandPalette.For(DarkMode).Muted),false,2);
        foreach(var placement in State.OfficeAt(Location.Id).Placements)
        {
            var item=State.Furniture.Find(i=>i.Id==placement.ItemId);if(item is null)continue;
            var definition=OfficeCatalog.Get(item.Kind);
            var rotated=placement.Rotation%2!=0;
            var size=new Vector2(rotated?definition.Depth:definition.Width,rotated?definition.Width:definition.Depth)*scale;
            var color=new Color(definition.Desk?BrandPalette.Mint:definition.Chair?"899fae":definition.Break?"e2bc76":"708783");
            DrawRect(new Rect2(origin+new Vector2(placement.X,placement.Z)*scale+Vector2.One,size-Vector2.One*2),color);
        }
        var entrance=origin+new Vector2(plan.Entrance.X,plan.Depth)*scale;
        DrawLine(entrance-new Vector2(scale,0),entrance+new Vector2(scale*2,0),new Color(BrandPalette.Mint),4);
    }
}
