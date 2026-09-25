using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private PanelContainer _officeDashboard=null!;
    private Label _dashboardTitle=null!,_dashboardStatus=null!,_dashboardSales=null!,_dashboardWork=null!;
    private Label _dashboardFans=null!,_dashboardSold=null!,_dashboardStock=null!,_dashboardReserved=null!;
    private TextureRect _dashboardArt=null!;
    private ChapterProgressBar _dashboardProgress=null!;
    private Control _dashboardStageLegend=null!;
    private int _dashboardArtId=-1;
    private Control _dashboardTotals=null!;
    private Button _dashboardAction=null!,_dashboardConvention=null!;

    private void BuildOfficeDashboard(Control parent)
    {
        _officeDashboard=new PanelContainer{Name="OfficeDashboard",ThemeTypeVariation="FloatingPanel"};parent.AddChild(_officeDashboard);
        var box=new VBoxContainer();_officeDashboard.AddChild(box);
        var tabs=new HBoxContainer();box.AddChild(tabs);
        ActionButton(tabs,"Series",()=>OpenOfficeSidebar("Series")).SizeFlagsHorizontal=SizeFlags.ExpandFill;
        ActionButton(tabs,"Schedule",()=>OpenWorkspace("Production")).SizeFlagsHorizontal=SizeFlags.ExpandFill;
        ActionButton(tabs,"Tasks",()=>Navigate("Guidance")).SizeFlagsHorizontal=SizeFlags.ExpandFill;
        var scroll=new ScrollContainer{SizeFlagsVertical=SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};box.AddChild(scroll);
        var content=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(content);
        var title=StudioCard(content,"CURRENT SERIES");
        _dashboardArt=new TextureRect{CustomMinimumSize=new(0,112),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered};title.AddChild(_dashboardArt);
        _dashboardTitle=Words(title,"Your first page",21);
        _dashboardStatus=Words(title,"",14);
        _dashboardWork=Words(title,"",14);
        _dashboardProgress=new ChapterProgressBar{CustomMinimumSize=new(0,26)};title.AddChild(_dashboardProgress);
        _dashboardStageLegend=ChapterStageLegend(title);
        _dashboardSales=Words(title,"",14);
        _dashboardTotals=new VBoxContainer();content.AddChild(_dashboardTotals);
        QuietWords(_dashboardTotals,"ALL MANAGED TITLES",12);
        var grid=new GridContainer{Columns=2};_dashboardTotals.AddChild(grid);
        Label Tile(string caption)
        {
            var card=StudioCard(grid,"");card.GetParent<Control>().SizeFlagsHorizontal=SizeFlags.ExpandFill;QuietWords(card,caption,12);
            return Words(card,"0",22);
        }
        _dashboardFans=Tile("Title fans");_dashboardSold=Tile("Copies sold");
        _dashboardStock=Tile("In stock");_dashboardReserved=Tile("Reserved");
        var actions=new VBoxContainer();title.AddChild(actions);QuietWords(actions,"NEXT STEP",12);
        _dashboardAction=ActionButton(actions,"Create a one-shot",()=>
        {
            var selected=_state.FindSeries(_progressSeriesId);
            if(selected is null){Navigate("New doujin");return;}
            RunSeriesNextAction(selected);
        });_dashboardAction.ThemeTypeVariation="PrimaryAction";
        actions=StudioCard(content,"QUICK ACTIONS");
        ActionButton(actions,"+  Create a one-shot",()=>Navigate("New doujin"));
        ActionButton(actions,"+  Start an ongoing series",()=>Navigate("New series"));
        ActionButton(actions,"Manage series",()=>OpenOfficeSidebar("Series"));
        ActionButton(actions,"Books, printing & online sales",()=>Navigate("Books"));
        _dashboardConvention=ActionButton(actions,"Book a convention",()=>Navigate("Conventions",_progressSeriesId));
        foreach(var button in _officeDashboard.FindChildren("*","Button",true,false).OfType<Button>().Where(b=>b.GetParent() is VBoxContainer))
        {button.AutowrapMode=TextServer.AutowrapMode.WordSmart;button.CustomMinimumSize=new(0,40);}
        RefreshOfficeDashboard();
    }
    private void RefreshOfficeDashboard()
    {
        if(_officeDashboard is null)return;
        var titles=_state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId&&(_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)).ToArray();
        var series=titles.FirstOrDefault(s=>s.Id==_progressSeriesId);
        _dashboardTotals.Visible=titles.Length>0;
        _dashboardAction.Text=series is null?"Create your first one-shot":SeriesNextActionText(series);
        _dashboardConvention.Disabled=series is null;
        _dashboardConvention.TooltipText=series is null?"Create or select a series before booking its convention visit.":"Choose attendees, compare costs and reserve copies.";
        _dashboardTitle.Text=series?.Title??"Your first page";
        _dashboardStatus.Text=series is null?"A one-shot is one complete story. An ongoing series continues in numbered issues.":$"{series.Genre} · {(series.StandaloneDoujin?"ONE-SHOT":"ONGOING")}\n{PublishingStatusText(series)}";
        _dashboardWork.Text=series is null?"":_progressDescription.Text.Split('\n')[0];_dashboardWork.Visible=series is not null;
        _dashboardProgress.SetChapter(series is null?null:_currentStageBar.Chapter);
        _dashboardProgress.Visible=_dashboardStageLegend.Visible=_dashboardProgress.Chapter is not null;
        _dashboardSales.Text=series is null?"Self-published target dates carry no missed-deadline penalty.":SeriesSalesText(series);
        _dashboardArt.Visible=series is not null;
        if(_dashboardArtId!=(series?.Id??0))
        {_dashboardArtId=series?.Id??0;_dashboardArt.Texture=series is null?null:ShowcaseTexture(series.Id,series.Genre,0);}
        _dashboardFans.Text=titles.Sum(s=>s.Fanbase).ToString("N0");
        _dashboardFans.TooltipText="Fans summed across managed titles. A reader following several titles is counted for each title.";
        _dashboardSold.Text=titles.Sum(s=>_state.SeriesCopiesSold(s.Id)).ToString("N0");
        var volumes=titles.SelectMany(s=>s.Volumes).ToArray();
        _dashboardStock.Text=volumes.Sum(v=>_state.Stock(v.Id)).ToString("N0");
        _dashboardReserved.Text=volumes.Sum(v=>_state.ConventionReserved(v.Id)).ToString("N0");
    }
}
