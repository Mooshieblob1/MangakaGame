using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MangakaGame;

/// <summary>
/// T1.9 layout checker: walks the visible controls and reports anything off the window, overlapping a
/// sibling in the same container, or a button whose text is cut off. Intentional overlays are skipped by node.
/// </summary>
public static class DisplaySweep
{
    public static List<string> Inspect(Control root,Rect2 window,ICollection<Node> excluded,IEnumerable<Control> floatingPanels)
    {
        var problems=new List<string>();
        Walk(root,window,excluded,false,problems);
        // Floating panels over the office may not cover each other (the phone and its icon sit on top by design).
        var panels=floatingPanels.Where(p=>p.IsVisibleInTree()&&p.Size.X>0&&p.Size.Y>0).ToArray();
        for(var i=0;i<panels.Length;i++)
            for(var j=i+1;j<panels.Length;j++)
                if(Overlap(panels[i].GetGlobalRect(),panels[j].GetGlobalRect()))
                    problems.Add($"panel-overlap {Describe(panels[i])} / {Describe(panels[j])} {Format(panels[i].GetGlobalRect())} {Format(panels[j].GetGlobalRect())}");
        return problems;
    }

    private static void Walk(Node node,Rect2 bounds,ICollection<Node> excluded,bool scrolled,List<string> problems)
    {
        foreach(var child in node.GetChildren())
        {
            if(excluded.Contains(child))continue;
            if(child is Window window)
            {
                if(!window.Visible)continue;
                var rect=new Rect2(window.Position,window.Size);
                if(!Inside(rect,bounds))problems.Add($"off-window {window.GetType().Name}:{window.Title} {Format(rect)}");
                Walk(window,new Rect2(Vector2.Zero,window.Size),excluded,false,problems);
                continue;
            }
            if(child is CanvasLayer layer){if(layer.Visible)Walk(layer,bounds,excluded,scrolled,problems);continue;}
            if(child is not Control control){Walk(child,bounds,excluded,scrolled,problems);continue;}
            if(!control.IsVisibleInTree()||control is ColorRect)continue;
            var global=control.GetGlobalRect();
            if(global.Size.X<=0||global.Size.Y<=0)continue;
            if(!scrolled&&!Inside(global,bounds))problems.Add($"off-window {Describe(control)} {Format(global)}");
            if(control is Button button&&CutText(button) is {} cut)problems.Add($"text-cut {Describe(control)} needs {cut:0} px more");
            if(control is BoxContainer or GridContainer or FlowContainer)
            {
                var kids=control.GetChildren().OfType<Control>().Where(k=>!excluded.Contains(k)&&k.IsVisibleInTree()&&!k.TopLevel&&k.Size.X>0&&k.Size.Y>0).ToArray();
                for(var i=0;i<kids.Length;i++)
                    for(var j=i+1;j<kids.Length;j++)
                        if(Overlap(kids[i].GetGlobalRect(),kids[j].GetGlobalRect()))
                            problems.Add($"overlap {Describe(kids[i])} / {Describe(kids[j])}");
            }
            Walk(control,bounds,excluded,scrolled||control is ScrollContainer,problems);
        }
    }

    // Godot never shrinks a control below its minimum size, so cut-off text only happens where clipping or trimming is switched on.
    private static float? CutText(Button button)
    {
        if(string.IsNullOrEmpty(button.Text)||button.AutowrapMode!=TextServer.AutowrapMode.Off)return null;
        if(!button.ClipText&&button.TextOverrunBehavior==TextServer.OverrunBehavior.NoTrimming)return null;
        var font=button.GetThemeFont("font");var size=button.GetThemeFontSize("font_size");
        var needed=font.GetStringSize(button.Text,HorizontalAlignment.Left,-1,size).X;
        var available=button.Size.X-button.GetThemeStylebox("normal").GetMinimumSize().X;
        if(button.Icon is {} icon&&!button.ExpandIcon)available-=icon.GetWidth()+button.GetThemeConstant("h_separation");
        if(button is OptionButton)available-=button.GetThemeIcon("arrow").GetWidth()+button.GetThemeConstant("arrow_margin");
        return needed>available+2?needed-available:null;
    }

    private static bool Inside(Rect2 rect,Rect2 bounds)=>
        rect.Position.X>=bounds.Position.X-1&&rect.Position.Y>=bounds.Position.Y-1&&rect.End.X<=bounds.End.X+1&&rect.End.Y<=bounds.End.Y+1;

    private static bool Overlap(Rect2 a,Rect2 b)=>
        Math.Min(a.End.X,b.End.X)-Math.Max(a.Position.X,b.Position.X)>1&&Math.Min(a.End.Y,b.End.Y)-Math.Max(a.Position.Y,b.Position.Y)>1;

    private static string Format(Rect2 r)=>$"[{r.Position.X:0},{r.Position.Y:0} {r.Size.X:0}x{r.Size.Y:0}]";

    /// <summary>Readable name: the control's own text where it has one, then the nearest named ancestor.</summary>
    public static string Describe(Control control)
    {
        var text=control switch{Button b=>b.Text,Label l=>l.Text,LineEdit e=>e.Text,_=>""};
        text=text.ReplaceLineEndings(" ");if(text.Length>32)text=text[..32]+"…";
        var own=control.Name.ToString().StartsWith('@')?control.GetType().Name:control.Name.ToString();
        Node? parent=control.GetParent();
        while(parent is not null&&parent.Name.ToString().StartsWith('@'))parent=parent.GetParent();
        return $"{own}{(text.Length>0?$" \"{text}\"":"")} in {parent?.Name ?? "root"}";
    }
}
