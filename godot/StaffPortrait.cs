using Godot;
using MangakaSim;

namespace MangakaGame;

/// <summary>Lightweight mouthless chibi portrait using the employee's saved appearance.</summary>
public partial class StaffPortrait : Control
{
    public AppearanceRecipe Recipe { get; set; }=new();
    public override void _Ready(){CustomMinimumSize=new(84,110);MouseFilter=MouseFilterEnum.Ignore;Resized+=QueueRedraw;}
    public override void _Draw()
    {
        var skin=OfficeActor.SkinPalette[Recipe.Skin%4];
        var hair=OfficeActor.HairPalette[Recipe.Hair%4];
        var outfit=OfficeActor.ClothesPalette[Recipe.Outfit%6];
        var center=new Vector2(Size.X/2,39);DrawCircle(center+new Vector2(0,38),29,new Color(outfit));
        if(Recipe.Style%3!=0)DrawCircle(center+new Vector2(0,9),31,new Color(hair));
        DrawCircle(center,29,new Color(skin));DrawArc(center,27,Mathf.Pi,Mathf.Tau,24,new Color(hair),17,true);
        foreach(var x in new[]{-10,10})
        {DrawCircle(center+new Vector2(x,5),3,new Color("252b32"));if(Recipe.Glasses)DrawRect(new Rect2(center+new Vector2(x-8,-2),new Vector2(16,14)),new Color("34444c"),false,2);}
        if(Recipe.Glasses)DrawLine(center+new Vector2(-2,3),center+new Vector2(2,3),new Color("34444c"),2);
        if(Recipe.Wardrobe==0)DrawColoredPolygon(new[]{center+new Vector2(-9,31),center+new Vector2(9,31),center+new Vector2(0,53)},new Color("eee8d9"));
        if(Recipe.Wardrobe<2)foreach(var x in new[]{-1,1})DrawColoredPolygon(new[]{center+new Vector2(x*3,29),center+new Vector2(x*13,34),center+new Vector2(x*6,41)},new Color("eee8d9"));
    }
}
