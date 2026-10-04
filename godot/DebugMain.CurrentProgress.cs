using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private HFlowContainer _currentProgress=null!;
    private OptionButton _progressSeries=null!;
    private Label _progressDescription=null!;
    private Label _currentFans=null!,_currentCopies=null!;
    private ChapterProgressBar _currentStageBar=null!;
    private int _progressSeriesId;

    private void BuildCurrentProgress()
    {
        _currentProgress=new HFlowContainer();_currentProgress.AddThemeConstantOverride("v_separation",4);_currentProgress.AddThemeConstantOverride("h_separation",8);_shell.AddChild(_currentProgress);
        _progressSeries=new OptionButton{ThemeTypeVariation="HeaderOption",FitToLongestItem=false,ClipText=true,TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis,CustomMinimumSize=new(230,32),SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsVertical=SizeFlags.ShrinkCenter};
        _currentProgress.AddChild(_progressSeries);
        _currentFans=new Label{CustomMinimumSize=new(85,0),VerticalAlignment=VerticalAlignment.Center};_currentProgress.AddChild(_currentFans);
        _currentCopies=new Label{CustomMinimumSize=new(200,0),VerticalAlignment=VerticalAlignment.Center,FocusMode=FocusModeEnum.All};_currentProgress.AddChild(_currentCopies);
        _progressDescription=new Label{CustomMinimumSize=new(240,0),SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsStretchRatio=2,TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis};
        _currentProgress.AddChild(_progressDescription);
        foreach(var label in new[]{_currentFans,_currentCopies,_progressDescription})
        {label.SetMeta("base_font_size",14);label.AddThemeFontSizeOverride("font_size",(int)(14*_presentation.UiScale));label.SizeFlagsVertical=SizeFlags.ShrinkCenter;}
        _currentStageBar=new ChapterProgressBar{CustomMinimumSize=new(180,26),SizeFlagsVertical=SizeFlags.ShrinkCenter};_currentProgress.AddChild(_currentStageBar);
        _progressSeries.ItemSelected+=_=>
        {
            var id=_progressSeries.GetSelectedId();SelectSeriesForWorkbench(id);
            if(_side.Visible&&!_report.Visible&&_page is "Series details" or "Sell online" or "Print doujin" or "Showcase")Navigate(_page,id);
        };
        foreach(var choice in new[]{_seriesOption,_publishingSeries,_channelSeries,_teamOption})
        {var selector=choice;selector.ItemSelected+=_=>{if(selector.GetSelectedId()>0)SelectSeriesForWorkbench(selector.GetSelectedId());};}
    }
    private void RefreshCurrentProgress()
    {
        if(_currentProgress is null)return;
        var series=_state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId&&
            (_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)).ToArray();
        _currentProgress.Show();_progressSeries.Disabled=series.Length==0;
        _currentFans.Visible=_currentCopies.Visible=_currentStageBar.Visible=series.Length>0;
        if(series.Length==0){_progressSeriesId=0;_progressSeries.Clear();_progressSeries.AddItem("No current series");_currentFans.Text="Title fans\n0";_currentCopies.Text="Sold 0 · Stock 0\nReserved 0";_progressDescription.Text="Series → Create a one-shot or ongoing series";_currentStageBar.SetChapter(null);return;}
        var current=series.FirstOrDefault(s=>s.Id==_progressSeriesId)??series.FirstOrDefault(s=>s.Id==_presentation.Guidance.Project)??
            series.FirstOrDefault(s=>s.Status==SeriesStatus.Active)??series[0];_progressSeriesId=current.Id;
        // Keep the dropdown stable while the player is choosing a series.
        if(_progressSeries.ItemCount!=series.Length||series.Where((s,i)=>_progressSeries.GetItemId(i)!=s.Id||_progressSeries.GetItemText(i)!=s.Title).Any())
        {_progressSeries.Clear();foreach(var s in series)_progressSeries.AddItem(s.Title,s.Id);}
        _progressSeries.Select(_progressSeries.GetItemIndex(current.Id));_progressSeries.TooltipText="Current series: "+current.Title;
        _currentFans.Text=$"Title fans\n{current.Fanbase:N0}";_currentFans.TooltipText=$"Fans of {current.Title}";
        _currentCopies.Text=$"Sold {_state.SeriesCopiesSold(current.Id):N0} · Stock {current.Volumes.Sum(v=>_state.Stock(v.Id)):N0}\nReserved {current.Volumes.Sum(v=>_state.ConventionReserved(v.Id)):N0}";
        _currentCopies.TooltipText=SeriesSalesText(current);
        var tasks=_state.People.Where(p=>p.CurrentTask is {} task&&current.Chapters.Any(c=>c.Id==task.ChapterId)).OrderByDescending(p=>p.Id==_state.ProtagonistPersonId).Select(p=>p.CurrentTask!.Value).ToArray();
        var chapter=tasks.Select(t=>current.Chapters.First(c=>c.Id==t.ChapterId)).FirstOrDefault(c=>!c.IsFinished)??
            current.Chapters.FirstOrDefault(c=>c.Status!=ChapterStatus.Complete)??current.Chapters.LastOrDefault();
        if(chapter is null){_progressDescription.Text="No chapter scheduled";_currentStageBar.Hide();_currentStageBar.SetChapter(null);return;}
        var work=tasks.Where(t=>t.ChapterId==chapter.Id).Select(t=>chapter.StageWork(t.Stage)).FirstOrDefault(w=>!w.IsDone)??chapter.Stages.FirstOrDefault(w=>!w.IsDone);
        var total=ChapterPercent(chapter);
        var away=_state.AtOutsideJob(_state.Protagonist,_state.Clock.Now)&&current.LeadPersonId==_state.ProtagonistPersonId;
        var prefix=away?"Away at part-time job · ":current.Status==SeriesStatus.Paused?"Series paused · ":current.Status==SeriesStatus.Ended?"Series ended · ":"";
        var stage=work is null?(chapter.Status!=ChapterStatus.Complete?"Awaiting chapter approval":current.StandaloneDoujin?"Book ready · production complete":"Chapter complete"):
            (work.Stage==Stage.Name?"Storyboard":work.Stage.ToString());
        _progressDescription.Text=$"{prefix}Ch. {chapter.Number} · {stage}\n{PublishingStatusText(current).Split('\n')[0]} · Chapter {Math.Floor(total):0}%";
        _progressDescription.TooltipText=_progressDescription.Text;
        _currentStageBar.SetChapter(chapter);
    }
}
