using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

// The career goals board (spec 2026-10-01): the current chapter's goals, their progress and Helper-Chan's How? tips.
public partial class DebugMain
{
    private Label _dashboardGoalsTitle=null!;
    private VBoxContainer _dashboardGoalsList=null!;
    private string _dashboardGoalsKey="";

    private (int Done,int Total) GoalTally()
    {
        if(_state.Goals is not {} goals||goals.Chapter>=GoalCatalog.Chapters.Length)return (0,0);
        var current=GoalCatalog.In(goals.Chapter).ToArray();
        return (current.Count(g=>_state.GoalDone(g.Id)),current.Length);
    }
    private string GoalsRailText(){var (done,total)=GoalTally();return total==0?"★  Goals":$"★  Goals  {done}/{total}";}

    private void BuildGoals()
    {
        if(_state.Goals is not {} goals||goals.Chapter>=GoalCatalog.Chapters.Length)
        {
            Words(_sideContent,"Every goal complete",28);
            QuietWords(_sideContent,"You have finished every chapter and mastery goal. The career carries on as long as you like.");
            return;
        }
        var chapter=GoalCatalog.Chapters[goals.Chapter];
        Words(_sideContent,chapter.Name,28);
        QuietWords(_sideContent,chapter.Blurb);
        LiveWords(_sideContent,()=>{var (done,total)=GoalTally();return $"{done} of {total} goals done";},15);
        foreach(var goal in GoalCatalog.In(goals.Chapter))GoalCard(_sideContent,goal);
        if(chapter.Reward.Describe() is {Length:>0} reward){var card=StudioCard(_sideContent,"CHAPTER REWARD");Words(card,reward,15);}
    }

    private void GoalCard(Control parent,GoalDefinition goal)
    {
        var card=StudioCard(parent,goal.Title,goal.Why);
        var bar=new ProgressBar{MaxValue=1,Step=.001,ShowPercentage=false,CustomMinimumSize=new(0,14)};card.AddChild(bar);
        var line=LiveWords(card,()=>GoalLine(goal),14);
        QuietWords(card,"Reward: "+goal.Reward.Describe(),13);
        var how=ActionButton(card,"How?",()=>ShowGoalTip(goal));how.Name="GoalHow-"+goal.Id;
        void Update()
        {
            var done=_state.GoalDone(goal.Id);
            bar.Value=done?1:goal.Measure(_state).Fraction;how.Visible=!done;
            line.ThemeTypeVariation=done?"SectionLabel":""; // a mint tick for finished goals
        }
        Update();_pageLiveValues.Add(Update);
    }
    private string GoalLine(GoalDefinition goal)=>_state.Goals?.Completed.FirstOrDefault(r=>r.Id==goal.Id) is {} done
        ?$"✓ Done {done.At:d MMM yyyy}":goal.Measure(_state).Text;

    private void ShowGoalTip(GoalDefinition goal)
    {
        if(_inMenu||_helperPopup.Visible||OfficeEditing){Notify("Finish or close the current dialog or furniture draft first.");return;}
        var project=_state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId).OrderBy(s=>s.Id).LastOrDefault()?.Id??0;
        if(project>0)SelectSeriesForWorkbench(project);
        ClosePhone();
        var (focus,_)=RouteGuidance(goal.Target,project);
        HighlightGuidance(focus);
        CareerGuidance.Say(_presentation.Guidance,_state.Clock.Now,goal.Tip);CareerGuidance.MarkRead(_presentation.Guidance);
        RefreshGuidance();Notify("Helper-Chan: "+goal.Tip);
    }

    private void BuildDashboardGoals(Control content)
    {
        var card=StudioCard(content,"THIS CHAPTER");
        _dashboardGoalsTitle=Words(card,"",18);
        _dashboardGoalsList=new VBoxContainer();card.AddChild(_dashboardGoalsList);
        ActionButton(card,"All goals",()=>Navigate("Goals"));
    }
    private void RefreshDashboardGoals()
    {
        if(_state.Goals is not {} goals||goals.Chapter>=GoalCatalog.Chapters.Length)
        {_dashboardGoalsTitle.Text="Every goal complete";Empty(_dashboardGoalsList);_dashboardGoalsKey="";return;}
        _dashboardGoalsTitle.Text=GoalCatalog.Chapters[goals.Chapter].Name;
        var next=GoalCatalog.In(goals.Chapter).Where(g=>!_state.GoalDone(g.Id)).Select(g=>(Goal:g,Progress:g.Measure(_state)))
            .OrderByDescending(x=>x.Progress.Fraction).ThenBy(x=>x.Goal.Id,System.StringComparer.Ordinal).Take(2).ToArray();
        var key=string.Join("|",next.Select(x=>x.Goal.Id+":"+x.Progress.Text));
        if(key==_dashboardGoalsKey)return;
        _dashboardGoalsKey=key;Empty(_dashboardGoalsList);
        foreach(var (goal,progress) in next)
        {
            Words(_dashboardGoalsList,goal.Title,14);
            _dashboardGoalsList.AddChild(new ProgressBar{MaxValue=1,Step=.001,Value=progress.Fraction,ShowPercentage=false,CustomMinimumSize=new(0,10)});
            QuietWords(_dashboardGoalsList,progress.Text,12);
        }
    }
}
