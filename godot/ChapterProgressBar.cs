using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

/// <summary>One chapter, with stage widths weighted by their required work.</summary>
public partial class ChapterProgressBar : ProgressBar
{
    public static readonly (Stage Stage,string Name,Color Color)[] Stages=
    [
        (Stage.Name,"Storyboard",new("65b6e8")),
        (Stage.Pencils,"Pencils",new("c19bea")),
        (Stage.Inks,"Inks",new("ecaa65")),
        (Stage.Backgrounds,"Backgrounds",new("72c7ab")),
        (Stage.Tones,"Tones",new("e68ca9"))
    ];
    private (double Weight,double Done,Color Color)[] _sections=[];
    private Control? _segments;
    private Label? _percentage;
    public Chapter? Chapter { get; private set; }

    public ChapterProgressBar()
    {MinValue=0;MaxValue=100;Step=0;ShowPercentage=false;MouseFilter=MouseFilterEnum.Pass;}

    public override void _Ready()
    {
        AddThemeStyleboxOverride("fill",new StyleBoxEmpty());
        // Draw above the native bar background, then put the number above the segments.
        var segments=new Control{MouseFilter=MouseFilterEnum.Ignore};_segments=segments;AddChild(segments);
        segments.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        segments.Draw+=()=>DrawSections(segments);segments.Resized+=segments.QueueRedraw;
        _percentage=new Label{HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,
            MouseFilter=MouseFilterEnum.Ignore};AddChild(_percentage);_percentage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _percentage.AddThemeColorOverride("font_color",Colors.White);
        _percentage.AddThemeColorOverride("font_outline_color",new("17222d"));_percentage.AddThemeConstantOverride("outline_size",3);
        SetChapter(Chapter);
    }
    public static double PercentComplete(Chapter chapter)
    {
        var required=chapter.Stages.Sum(s=>Math.Max(0,s.HoursRequired));
        return required<=0?chapter.IsFinished?100:0:
            100*chapter.Stages.Sum(s=>s.IsDone?Math.Max(0,s.HoursRequired):Math.Clamp(s.HoursDone,0,Math.Max(0,s.HoursRequired)))/required;
    }
    public void SetChapter(Chapter? chapter)
    {
        Chapter=chapter;Value=chapter is null?0:PercentComplete(chapter);
        _sections=chapter is null?[]:Stages.Select(stage=>
        {
            var work=chapter.Stages.FirstOrDefault(s=>s.Stage==stage.Stage);
            var weight=Math.Max(0,work?.HoursRequired??0);
            return (weight,work?.IsDone==true?weight:Math.Clamp(work?.HoursDone??0,0,weight),stage.Color);
        }).ToArray();
        if(_percentage is not null)_percentage.Text=$"{Math.Floor(Value):0}%";
        TooltipText=chapter is null?"No chapter scheduled":$"Chapter {chapter.Number} · {Value:0.#}% overall\n"+
            string.Join("\n",Stages.Select(stage=>
            {
                var work=chapter.Stages.FirstOrDefault(s=>s.Stage==stage.Stage);
                return $"{stage.Name}: {(work?.IsDone==true?Math.Max(0,work.HoursRequired):Math.Clamp(work?.HoursDone??0,0,Math.Max(0,work?.HoursRequired??0))):0.#} / {Math.Max(0,work?.HoursRequired??0):0.#} work hours";
            }));
        _segments?.QueueRedraw();
    }
    private void DrawSections(Control canvas)
    {
        var total=_sections.Sum(s=>s.Weight);if(total<=0)return;
        var width=Math.Max(0,canvas.Size.X-4);var height=Math.Max(0,canvas.Size.Y-4);var x=2f;
        foreach(var section in _sections)
        {
            if(section.Weight<=0)continue;
            var extent=(float)(width*section.Weight/total);
            canvas.DrawRect(new Rect2(x,2,extent,height),new Color(section.Color,.18f));
            if(section.Done>0)canvas.DrawRect(new Rect2(x,2,(float)(extent*section.Done/section.Weight),height),section.Color);
            if(x>2)canvas.DrawLine(new(x,2),new(x,canvas.Size.Y-2),new("17222d"),1);
            x+=extent;
        }
    }
}
