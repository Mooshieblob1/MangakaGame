using System;
using Godot;

namespace MangakaGame;

/// <summary>A world anchor with readable UI text, independent of 3D zoom and texture filtering.</summary>
public partial class OfficeWorldLabel : Node3D
{
    private Label? _caption;
    private OfficeView? _office;
    private int _appliedFontSize;
    public string Text { get; set; }="";
    public int FontSize { get; set; }=18;
    public Color TextColor { get; set; }=new("f5f0e5");
    public Vector2 ScreenOffset { get; set; }

    public override void _Ready()
    {
        // Actors and the camera finish moving before their labels are projected.
        ProcessPriority=20;
        for(Node? ancestor=GetParent();ancestor is not null;ancestor=ancestor.GetParent())
            if(ancestor is OfficeView office){_office=office;break;}
        _caption=new Label{MouseFilter=Control.MouseFilterEnum.Ignore,FocusMode=Control.FocusModeEnum.None,
            HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,
            TopLevel=true,ZAsRelative=false,ZIndex=50,Visible=false};
        AddChild(_caption);
        _caption.AddThemeColorOverride("font_color",TextColor);
        _caption.AddThemeColorOverride("font_outline_color",new("17222d"));
        _caption.AddThemeConstantOverride("outline_size",4);
        VisibilityChanged+=RefreshCaption;RefreshCaption();
    }
    public override void _Process(double delta)=>RefreshCaption();
    public override void _ExitTree()=>_caption?.Hide();

    private void RefreshCaption()
    {
        if(_caption is null||!IsInsideTree())return;
        var viewport=GetViewport();var camera=viewport.GetCamera3D();
        if(!IsVisibleInTree()||Text.Length==0||camera is null||camera.IsPositionBehind(GlobalPosition))
        {_caption.Hide();return;}
        var point=camera.UnprojectPosition(GlobalPosition);
        if(!viewport.GetVisibleRect().HasPoint(point)){_caption.Hide();return;}
        var size=Math.Max(16,(int)Math.Round(FontSize*(_office?.LabelTextScale??1)));
        if(size!=_appliedFontSize)
        {_caption.AddThemeFontSizeOverride("font_size",size);_appliedFontSize=size;}
        if(_caption.Text!=Text)_caption.Text=Text;
        _caption.Size=_caption.GetCombinedMinimumSize().Ceil();
        // Rasterize at the displayed size and avoid subpixel placement shimmer.
        _caption.Position=(point+ScreenOffset-_caption.Size/2).Round();
        _caption.Show();
    }
}
