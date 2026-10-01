using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private HFlowContainer PageActions(params (string Label,Action Run)[] actions)
    {
        var row=new HFlowContainer();_sideContent.AddChild(row);
        foreach(var (label,run) in actions)ActionButton(row,label,run);
        return row;
    }
    private Label LiveWords(Control parent,Func<string> text,int size=16)
    {var label=Words(parent,text(),size);_pageLiveValues.Add(()=>label.Text=text());return label;}
    private void Metric(Control parent,string title,Func<string> value,string caption="")
    {
        var box=StudioCard(parent,"");box.GetParent<Control>().CustomMinimumSize=new(175,0);
        QuietWords(box,title,12);
        Figure(LiveWords(box,value,25));
        if(caption.Length>0)QuietWords(box,caption,12);
    }
    private void SeriesLifecycle(Control parent,Series series)
    {
        var lanes=new HFlowContainer();parent.AddChild(lanes);
        var production=StudioCard(lanes,"PRODUCTION");production.GetParent<Control>().CustomMinimumSize=new(220,0);
        LiveWords(production,()=>{var c=SidebarChapter(series);return c is null?"Ready to begin":$"Chapter {c.Number} · {Humanize(c.Status.ToString())}";},14);
        var chapter=SidebarChapter(series);if(chapter is not null)ChapterProgress(production,chapter);
        var release=StudioCard(lanes,"RELEASE");release.GetParent<Control>().CustomMinimumSize=new(220,0);
        LiveWords(release,()=>PublishingStatusText(series),14);
        var readers=StudioCard(lanes,"READERS");readers.GetParent<Control>().CustomMinimumSize=new(190,0);
        LiveWords(readers,()=>$"{series.Fanbase:N0} fans\n{_state.SeriesCopiesSold(series.Id):N0} copies sold",18);
    }
    private void SeriesNextAction(Control parent,Series series)
    {
        var button=ActionButton(parent,"",()=>RunSeriesNextAction(series));
        button.ThemeTypeVariation="PrimaryAction";
        void RefreshAction()=>button.Text=SeriesNextActionText(series);
        RefreshAction();_pageLiveValues.Add(RefreshAction);
    }
    private string SeriesNextActionText(Series series)=>series.PendingOffer is not null?"Review publishing offer":
        series.Volumes.Any(v=>v.IsDoujin&&v.BusinessId==_state.ControlledBusinessId)?"Print & sell a ready edition":"Manage current production";
    private void RunSeriesNextAction(Series series)
    {
        SelectSeriesForWorkbench(series.Id);
        if(series.PendingOffer is not null)OpenWorkspace("Publishing");
        else if(series.Volumes.Any(v=>v.IsDoujin&&v.BusinessId==_state.ControlledBusinessId))OpenPrinting(series.Id);
        else OpenWorkspace("Production");
    }
    private void BuildSeriesOverview()
    {
        PageActions(("+ One-shot doujin",()=>Navigate("New doujin")),("+ Ongoing series",()=>Navigate("New series")));
        Words(_sideContent,"One-shot: one complete story. Ongoing: numbered issues first, collected books after five chapters. Self-published dates are personal targets.",14);
        ScopeFilter();
        foreach(var s in ManagedSeries)
        {
            var card=Card(s.Title,$"{s.Genre} · {_state.FindPerson(s.LeadPersonId)?.Name} · {(s.StandaloneDoujin?"ONE-SHOT":"ONGOING")}");
            if(!_officeSidebar)
            {
                var art=new TextureRect{Texture=ShowcaseTexture(s.Id,s.Genre,0),CustomMinimumSize=new(0,140),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered};card.AddChild(art);
                SeriesLifecycle(card,s);
            }
            else
            {
                LiveWords(card,()=>PublishingStatusText(s),14);
                var current=SidebarChapter(s);if(current is not null)ChapterProgress(card,current);
            }
            LiveWords(card,()=>SeriesSalesText(s),14);
            var actions=new HFlowContainer();card.AddChild(actions);SeriesNextAction(actions,s);
            ActionButton(actions,"Open series",()=>Navigate("Series details",s.Id));
            ActionButton(actions,"Send to convention",()=>Navigate("Conventions",s.Id));
            if(s.StandaloneDoujin)ActionButton(actions,"Continue as ongoing series",()=>ContinueOneShotFromSidebar(s.Id));
        }
        if(!ManagedSeries.Any())Words(Card("Your first page","Create a one-shot or an ongoing series above."),"Helper-Chan will guide you from production to your first sale.");
    }
    private void BuildStaffOverview()
    {
        PageActions(("Recruit & compare candidates",()=>OpenWorkspace("Recruitment")),("Team & overtime",()=>OpenWorkspace("Team settings")),("Pay & employment",()=>OpenWorkspace("Business actions")));
        ScopeFilter();Words(_sideContent,"Helper-Chan is your companion and guide. She uses no staff slot or payroll.",14);
        var flow=new HFlowContainer();_sideContent.AddChild(flow);
        foreach(var p in ManagedPeople)
        {
            var card=StudioCard(flow,p.Name,_state.FindSeries(p.MainSeriesId??0)?.Title??"Unassigned");card.GetParent<Control>().CustomMinimumSize=new(300,0);
            var row=new HBoxContainer();card.AddChild(row);
            row.AddChild(new StaffPortrait{Recipe=p.Appearance??AppearanceRecipe.Generate(_state.RngSeed,p.Id)});
            var copy=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};row.AddChild(copy);
            LiveWords(copy,()=>StaffActivityText(p),15);
            Words(copy,$"¥{p.Employment!.MonthlySalary:N0} / month",16);
            LiveWords(copy,()=>$"Happiness {p.Happiness:0} · Comfort {p.Comfort:0}",14);
            Words(card,p.HiddenTalent?"Skills not yet known":string.Join(" · ",p.Skills.Select(s=>$"{(s.Key==Stage.Name?"Storyboard":s.Key.ToString())} {s.Value}")),14);
            ActionButton(card,"Manage employee",()=>{_selectedPersonId=p.Id;ResetPersonInputs();Navigate("Person",p.Id);});
        }
        var table=Table("Employee","Storyboard / Pencil / Ink / Background / Tone","Monthly salary","Series");
        table.CustomMinimumSize=new(0,240);Disclosure(_sideContent,"Compare staff skills & pay").AddChild(table);var root=table.CreateItem();
        foreach(var p in ManagedPeople){var row=Row(table,root,p.Name,p.HiddenTalent?"Skills not yet known":string.Join(" / ",StageOrder.All.Select(s=>p.Skills[s])),$"¥{p.Employment!.MonthlySalary:N0}",_state.FindSeries(p.MainSeriesId??0)?.Title??"Unassigned");row.SetMetadata(0,p.Id);}
        table.ItemActivated+=()=>{if(table.GetSelected() is {} item)Navigate("Person",(int)item.GetMetadata(0));};
    }
    private void BuildStudioOverview()
    {
        PageActions(("Compare properties & move",()=>OpenWorkspace("Properties")),("Tokyo map",()=>OpenWorkspace("Tokyo map")),("Career moves",()=>OpenWorkspace("Career moves")));
        Words(_sideContent,"Choose a location to view or furnish. Moving uses a furniture preview with a full quote before you commit.",14);
        var flow=new HFlowContainer();_sideContent.AddChild(flow);
        foreach(var l in _state.Locations.Where(l=>l.BusinessId==_state.ControlledBusinessId))
        {
            var card=StudioCard(flow,l.Name,l.District+(l.Closed?" · Closed":l.IsFamilyHome?" · Family home":" · Tier "+l.PropertyTier));card.GetParent<Control>().CustomMinimumSize=new(330,0);
            card.AddChild(new StudioPlanPreview{State=_state,Location=l,DarkMode=_darkMode,CustomMinimumSize=new(0,170)});
            Figure(Words(card,$"¥{l.MonthlyRent:N0} / month",24));
            LiveWords(card,()=>$"{_state.ControlledStaff.Count(p=>p.Employment!.LocationId==l.Id)} staff · {_state.UsableWorkspaces(l.Id)} usable desks\n{l.BreakSeats} break seats · {l.Storage:N0} copy storage",15);
            if(l.Closed)continue;
            var row=new HFlowContainer();card.AddChild(row);
            ActionButton(row,"View office",()=>{_viewLocation=l.Id;ShowOffice();});
            ActionButton(row,"Furnish",()=>{_officeLocation.Select(_officeLocation.GetItemIndex(l.Id));OpenWorkspace("Furniture");BeginOfficeEditor();}).ThemeTypeVariation="PrimaryAction";
        }
    }
    private void BuildIndustryOverview()
    {
        Words(_sideContent,"THE MANGA INDUSTRY",30);
        Words(_sideContent,_state.World.SimulatedFuture?"Simulated future · historical coverage ends in 2025":"News and opportunities from your current era",14);
        PageActions(("Awards & contests",()=>Navigate("Awards")),("Anime & merchandise",()=>Navigate("Licenses")),("Career milestones",()=>Navigate("Legacy")),("Scouting & negotiations",()=>OpenWorkspace("Industry contacts")),("Publishers & rankings",()=>OpenWorkspace("Publishing")));
        var feature=ManagedSeries.OrderByDescending(s=>s.Fanbase).FirstOrDefault();
        if(feature is not null)
        {
            var card=Card("FROM YOUR STUDIO",feature.Title+" · "+feature.Genre);
            card.AddChild(new TextureRect{Texture=ShowcaseTexture(feature.Id,feature.Genre,0),CustomMinimumSize=new(0,200),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered});
            ActionButton(card,"View this series",()=>Navigate("Series details",feature.Id));
        }
        foreach(var news in _state.World.News.TakeLast(5).Reverse())Words(Card(news.Simulated?"Industry news · simulated":"Industry news",$"{news.Time:d MMM yyyy}"),news.Message);
        var grid=new HFlowContainer();_sideContent.AddChild(grid);
        foreach(var market in _state.Markets)
        {
            var magazine=_state.PublisherCatalog.Get(market.MagazineId);
            var card=StudioCard(grid,magazine.Name,$"Next issue {market.NextIssueClose:d MMM}");card.GetParent<Control>().CustomMinimumSize=new(310,0);
            Words(card,market.LastRanking.Count==0?"Rankings arrive after the first issue.":string.Join("\n",market.LastRanking.Take(5).Select(r=>$"#{r.Rank}  {r.Title}")),15);
            ActionButton(card,"Explore publisher",()=>{for(var i=0;i<_state.PublisherCatalog.Magazines.Count;i++)if(_state.PublisherCatalog.Magazines[i].Id==magazine.Id)_magazineOption.Select(i);OpenWorkspace("Publishing");});
        }
    }
}
