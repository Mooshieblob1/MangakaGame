using System;
using Godot;
using MangakaSim;

namespace MangakaGame;

/// <summary>Helper-Chan's handset, drawn in code behind its children: a 1990s PHS until 2009, a smartphone from 2010.</summary>
public partial class PhoneFrame : MarginContainer
{
    public bool Modern { get; private set; }
    public bool Dark { get; private set; }
    public float TextScale { get; private set; } = 1;
    public static readonly Color LcdScreen=new("b9c9a0"),LcdText=new("1f2a14"),LcdLine=new("7d8d66");
    // The period PHS keeps its LCD; the smartphone's screen follows the logo palette (spec 2026-09-29).
    public static Color ModernScreen(bool dark)=>new(BrandPalette.For(dark).Card);
    public static Color ModernText(bool dark)=>new(BrandPalette.For(dark).Text);
    public static Color ModernBubble(bool dark)=>new(BrandPalette.For(dark).Hover);
    public static Color ModernEdge(bool dark)=>new(BrandPalette.For(dark).Outline);
    public Color ScreenColor=>Modern?ModernScreen(Dark):LcdScreen;
    public Color TextColor=>Modern?ModernText(Dark):LcdText;
    public Color BubbleColor=>Modern?ModernBubble(Dark):new Color("aebe93");
    public Color BubbleEdge=>Modern?ModernEdge(Dark):LcdLine;

    public void Configure(bool modern,bool dark,float scale)
    {
        Modern=modern;Dark=dark;TextScale=scale;
        // Margins leave room for the body: PHS antenna and earpiece above, keypad below.
        int s(float v)=>(int)Math.Round(v*scale);
        AddThemeConstantOverride("margin_left",s(modern?14:20));AddThemeConstantOverride("margin_right",s(modern?14:20));
        AddThemeConstantOverride("margin_top",s(modern?34:50));AddThemeConstantOverride("margin_bottom",s(modern?24:74));
        QueueRedraw();
    }
    public override void _Draw()
    {
        var s=TextScale;var size=Size;
        float left=GetThemeConstant("margin_left"),right=GetThemeConstant("margin_right"),top=GetThemeConstant("margin_top"),bottom=GetThemeConstant("margin_bottom");
        var screen=new Rect2(left-6*s,top-6*s,size.X-left-right+12*s,size.Y-top-bottom+12*s);
        if(Modern)
        {
            DrawStyleBox(Box(new Color("0e1013"),28*s,new Color("3a3f47"),2),new Rect2(Vector2.Zero,size));
            DrawStyleBox(Box(ScreenColor,20*s),screen);
            // Earpiece slot in the top bezel, clear of the header text.
            DrawStyleBox(Box(new Color("2a2e35"),3*s),new Rect2(size.X/2-30*s,11*s,60*s,6*s));
            DrawStyleBox(Box(new Color("5a6069"),2*s),new Rect2(size.X/2-40*s,size.Y-12*s,80*s,4*s));
            return;
        }
        // PHS: graphite body with an antenna stub, earpiece slot, green LCD and a keypad strip.
        var body=new Rect2(0,14*s,size.X,size.Y-14*s);
        DrawStyleBox(Box(new Color("30363b"),10*s),new Rect2(size.X-54*s,0,14*s,30*s));
        DrawStyleBox(Box(new Color("444b52"),26*s,new Color("5c646c"),2),body);
        DrawStyleBox(Box(new Color("1a1d20"),3*s),new Rect2(size.X/2-26*s,26*s,52*s,5*s));
        DrawStyleBox(Box(new Color("2a2f33"),10*s),screen.Grow(4*s));
        DrawStyleBox(Box(LcdScreen,6*s),screen);
        var keys=new Rect2(22*s,size.Y-bottom+16*s,size.X-44*s,bottom-28*s);
        DrawStyleBox(Box(new Color("5a626a"),keys.Size.Y/2),new Rect2(size.X/2-24*s,keys.Position.Y,48*s,keys.Size.Y*.45f));
        for(int row=0;row<2;row++)for(int col=0;col<4;col++)
        {
            var w=(keys.Size.X-3*8*s)/4;var y=keys.Position.Y+keys.Size.Y*(row==0?.52f:.8f);
            if(row==0&&(col==1||col==2))continue;
            DrawStyleBox(Box(new Color("596168"),5*s),new Rect2(keys.Position.X+col*(w+8*s),y,w,keys.Size.Y*.2f));
        }
    }
    private static StyleBoxFlat Box(Color color,float radius,Color? border=null,int width=0)
    {
        var box=new StyleBoxFlat{BgColor=color,AntiAliasing=true};box.SetCornerRadiusAll((int)radius);
        if(border is { } edge){box.BorderColor=edge;box.SetBorderWidthAll(width);}
        return box;
    }
}

/// <summary>The collapsed phone: a small handset with a red unread badge. Pressing it opens the thread.</summary>
public partial class PhoneIcon : Button
{
    private int _unread;
    public bool Modern;
    public float TextScale=1;
    public int Unread { get=>_unread; set{_unread=value;QueueRedraw();} }
    public PhoneIcon(){Flat=true;FocusMode=FocusModeEnum.All;TooltipText="Messages from Helper-Chan";}
    public override void _Draw()
    {
        var s=TextScale;var c=Size/2;
        var body=new Rect2(c.X-14*s,c.Y-22*s,28*s,46*s);
        var box=new StyleBoxFlat{BgColor=Modern?new Color("111317"):new Color("444b52"),AntiAliasing=true};box.SetCornerRadiusAll((int)(6*s));
        box.BorderColor=new Color("8a939c");box.SetBorderWidthAll(2);
        if(!Modern)DrawRect(new Rect2(body.End.X-9*s,body.Position.Y-8*s,4*s,10*s),new Color("30363b"));
        DrawStyleBox(box,body);
        var screen=new StyleBoxFlat{BgColor=Modern?new Color(BrandPalette.Mint):PhoneFrame.LcdScreen};screen.SetCornerRadiusAll((int)(3*s));
        DrawStyleBox(screen,new Rect2(body.Position.X+4*s,body.Position.Y+6*s,body.Size.X-8*s,Modern?body.Size.Y-14*s:18*s));
        if(_unread<=0)return;
        var badge=new Vector2(body.End.X,body.Position.Y+2*s);var radius=10*s;
        DrawCircle(badge,radius,new Color("d8344a"));
        var font=GetThemeDefaultFont();var size=(int)(13*s);var text=_unread>9?"9+":_unread.ToString();
        var width=font.GetStringSize(text,HorizontalAlignment.Left,-1,size).X;
        DrawString(font,new Vector2(badge.X-width/2,badge.Y+size*.36f),text,HorizontalAlignment.Left,-1,size,Colors.White);
    }
}
