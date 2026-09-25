using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private readonly Dictionary<string,VBoxContainer> _studioSections=new();
    private readonly Dictionary<string,Button> _studioSectionButtons=new();
    private readonly List<(Button Button,Func<bool> Ready,string Reason)> _studioRequirements=new();
    private readonly List<Action> _studioLiveValues=new();
    private Label _studioHeading=null!,_studioCash=null!,_studioRent=null!,_studioDesks=null!,_studioBills=null!,_studioPropertyInfo=null!,_studioPrintQuote=null!;
    private OptionButton _studioEmployee=null!,_studioSeries=null!;
    private Control _studioContext=null!;
    private HFlowContainer _studioLocationCards=null!;
    private string _studioLocationKey="";
    private ScrollContainer _studioScroll=null!;
    private Label _studioEmployeeSummary=null!;
    private readonly Dictionary<string,ProgressBar> _studioNeeds=new();

    private VBoxContainer StudioCard(Control parent,string title,string description="")
    {
        var frame=new PanelContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill,ThemeTypeVariation="StudioCard"};frame.SetMeta("card_surface",true);
        parent.AddChild(frame);
        var box=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};frame.AddChild(box);
        if(title.Length>0)
        {var heading=Words(box,title,title==title.ToUpperInvariant()?12:18);if(title==title.ToUpperInvariant())heading.ThemeTypeVariation="SectionLabel";}
        if(description.Length>0)QuietWords(box,description,14);
        return box;
    }
    private void StudioSection(string name)
    {
        foreach(var (key,section) in _studioSections)section.Visible=key==name;
        foreach(var (key,button) in _studioSectionButtons)button.SetPressedNoSignal(key==name);
        _studioContext.Visible=name is "Team" or "Publishing" or "Locations" or "Money";
        _studioSeries.GetParent<Control>().Visible=name is "Team" or "Publishing";
        _studioScroll.ScrollVertical=0;
    }
    private void BuildStudioDashboard(VBoxContainer panel)
    {
        _studioHeading=Words(panel,"Your studio",26);
        Words(panel,"Manage your spaces, support your team, and plan the next step.",14);
        var metrics=new HFlowContainer();panel.AddChild(metrics);
        Label Metric(string title,string caption)
        {
            var card=StudioCard(metrics,"","");card.GetParent<Control>().CustomMinimumSize=new(210,0);
            Words(card,title,14);var value=Words(card,"—",24);Words(card,caption,12);return value;
        }
        _studioCash=Metric("AVAILABLE BUSINESS CASH","After reserved wages");
        _studioRent=Metric("MONTHLY RENT","All open locations");
        _studioDesks=Metric("TEAM / USABLE DESKS","Equipped workspaces");
        _studioBills=Metric("UNPAID BILLS","Outstanding business bills");
        var navigation=new HFlowContainer();panel.AddChild(navigation);
        navigation.Visible=!ManagementInterface;
        foreach(var name in new[]{"Overview","Locations","Team","Money","Publishing","Career"})
        {
            var section=name;var button=ActionButton(navigation,name,()=>{if(_managementReady)OpenWorkspace(section switch{"Locations"=>"Properties","Team"=>"Team settings","Money"=>"Business actions","Publishing"=>"Distribution settings","Career"=>"Career moves",_=>"Studio actions"});else StudioSection(section);});
            button.ToggleMode=true;button.CustomMinimumSize=new(125,44);_studioSectionButtons.Add(name,button);
        }
        _operationsFeedback=Words(panel,"",16);_operationsFeedback.Hide();
        _studioContext=new HFlowContainer();panel.AddChild(_studioContext);
        OptionButton Selector(string title)
        {
            var field=new VBoxContainer{CustomMinimumSize=new(270,0)};_studioContext.AddChild(field);Words(field,title,14);
            var choice=new OptionButton{FitToLongestItem=false,ClipText=true,SizeFlagsHorizontal=SizeFlags.ExpandFill};field.AddChild(choice);return choice;
        }
        _studioEmployee=Selector("Selected employee");_studioSeries=Selector("Selected series");
        _studioEmployee.ItemSelected+=_=>{_selectedPersonId=_studioEmployee.GetSelectedId();_dirty=true;};
        _studioSeries.ItemSelected+=_=>{if(_managementReady)SelectSeriesForWorkbench(_studioSeries.GetSelectedId());else _teamOption.Select(_teamOption.GetItemIndex(_studioSeries.GetSelectedId()));_dirty=true;};
        foreach(var name in _studioSectionButtons.Keys)
        {
            var section=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};panel.AddChild(section);_studioSections.Add(name,section);
        }
        var wellbeing=StudioCard(_studioSections["Team"],"How your employee is doing");
        _studioEmployeeSummary=Words(wellbeing,"",14);
        var needs=new HFlowContainer();wellbeing.AddChild(needs);
        foreach(var name in new[]{"Food","Drink","Comfort","Happiness","Loyalty"})
        {
            var field=new VBoxContainer{CustomMinimumSize=new(160,0)};needs.AddChild(field);Words(field,name,14);
            var bar=new ProgressBar{CustomMinimumSize=new(0,26),ShowPercentage=true};field.AddChild(bar);_studioNeeds.Add(name,bar);
        }
        var overview=_studioSections["Overview"];
        Words(overview,"Your locations",22);
        _studioLocationCards=new HFlowContainer();overview.AddChild(_studioLocationCards);
        var next=StudioCard(overview,"What would you like to do?");var shortcuts=new HFlowContainer();next.AddChild(shortcuts);
        ActionButton(shortcuts,"Find a new studio",()=>{if(_managementReady)OpenWorkspace("Properties");else StudioSection("Locations");});
        ActionButton(shortcuts,"Salaries & loans",()=>{if(_managementReady)OpenWorkspace("Business actions");else StudioSection("Money");});
        ActionButton(shortcuts,"Print, sell & attend events",()=>{if(_managementReady)OpenWorkspace("Distribution settings");else StudioSection("Publishing");});
        StudioSection("Overview");
    }
    private void StudioRequirement(Button button,Func<bool> ready,string reason)=>_studioRequirements.Add((button,ready,reason));
    private void RefreshStudioDashboard()
    {
        var locations=_state.Locations.Where(l=>l.BusinessId==_state.ControlledBusinessId&&!l.Closed).ToArray();
        _studioHeading.Text=_state.ControlledBusiness.Name+(_state.ControlledBusiness.Incorporated?" · Incorporated":_state.ControlledBusiness.Independent?" · Independent studio":" · Employer studio");
        _studioCash.Text=$"¥{_state.AvailableBusinessCash:N0}";
        _studioRent.Text=$"¥{locations.Sum(l=>l.MonthlyRent):N0}";
        _studioDesks.Text=$"{_state.ControlledStaff.Count()} / {locations.Sum(l=>_state.UsableWorkspaces(l.Id))}";
        _studioBills.Text=$"¥{_state.Bills.Where(b=>b.BusinessId==_state.ControlledBusinessId).Sum(b=>b.Remaining):N0}";
        void Select(OptionButton choice,IEnumerable<(int Id,string Name)> entries,int selected,string empty)
        {
            var rows=entries.ToArray();choice.Disabled=rows.Length==0;
            SyncOptions(choice,rows.Length==0?new[]{(0,empty)}:rows,selected);
        }
        Select(_studioEmployee,_state.ControlledStaff.Select(p=>(p.Id,p.Name)),SelectedPerson.Id,"No employees");
        Select(_studioSeries,_state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId).Select(s=>(s.Id,s.Title)),_teamOption.GetSelectedId(),"Create a series first");
        var person=SelectedPerson;
        _studioEmployeeSummary.Text=$"{person.Name} · Overtime this week: {person.WeeklyOvertime} / {person.WeeklyOvertimeLimit} hours";
        _studioNeeds["Food"].Value=person.Food;_studioNeeds["Drink"].Value=person.Drink;_studioNeeds["Comfort"].Value=person.Comfort;
        _studioNeeds["Happiness"].Value=person.Happiness;_studioNeeds["Loyalty"].Value=person.Loyalty;
        // Rebuild only when locations or their layout change; normal refreshes preserve focus.
        var key=$"{_state.ControlledBusinessId}/{_state.OfficeRevision}/"+string.Join(";",locations.Select(l=>$"{l.Id}:{l.Name}:{l.MonthlyRent}:{l.Seats}:{l.BreakSeats}:{l.Storage}"));
        if(key!=_studioLocationKey)
        {
            _studioLocationKey=key;Empty(_studioLocationCards);_studioLiveValues.Clear();
            foreach(var location in locations)
            {
                var id=location.Id;var card=StudioCard(_studioLocationCards,location.Name,location.District+(location.IsFamilyHome?" · Family home":" · Tier "+location.PropertyTier));
                card.GetParent<Control>().CustomMinimumSize=new(310,0);
                var map=new StudioPlanPreview{State=_state,Location=location,DarkMode=_darkMode,CustomMinimumSize=new(0,150)};card.AddChild(map);
                Words(card,"Floor plan · green desks / blue chairs",12);
                Words(card,$"¥{location.MonthlyRent:N0} / month",22);
                var capacity=Words(card,"",14);var bar=new ProgressBar{CustomMinimumSize=new(0,22),ShowPercentage=false};card.AddChild(bar);
                Words(card,$"{location.BreakSeats} break seats · {location.Storage:N0} copy storage",14);
                _studioLiveValues.Add(()=>
                {
                    map.State=_state;map.Location=_state.Locations.Single(l=>l.Id==id);map.DarkMode=_darkMode;map.QueueRedraw();
                    var staff=_state.ControlledStaff.Count(p=>p.Employment!.LocationId==id);var desks=_state.UsableWorkspaces(id);
                    capacity.Text=$"{staff} staff · {desks} usable desks / {location.Seats} capacity";bar.MaxValue=Math.Max(1,desks);bar.Value=staff;
                    bar.TooltipText=staff>desks?"Not enough equipped workspaces. Furnish this office to add desks and chairs.":$"{Math.Max(0,desks-staff)} equipped workspaces available.";
                });
                var actions=new HFlowContainer();card.AddChild(actions);
                ActionButton(actions,"View office",()=>{_viewLocation=id;_report.Hide();_side.Hide();_homeOffice.Show();RefreshManagement();RefreshNavigation();});
                ActionButton(actions,"Furnish this studio",()=>{_officeLocation.Select(_officeLocation.GetItemIndex(id));OpenWorkspace("Furniture");BeginOfficeEditor();}).ThemeTypeVariation="PrimaryAction";
            }
        }
        foreach(var refresh in _studioLiveValues)refresh();
        var offer=TokyoProperties.All.FirstOrDefault(p=>p.Id==_propertyChoice.GetSelectedId());
        _studioPropertyInfo.Text=offer is null?"Choose a property.":$"Tier {offer.Tier} · {offer.District}\n{offer.Seats} desks · {offer.Storage:N0} copy storage · atmosphere {offer.Atmosphere}\n¥{offer.Rent:N0} monthly rent · ¥{offer.Rent*3:N0} deposit + first rent\nMoving and furniture are quoted in the preview.";
        var book=_state.Series.SelectMany(s=>s.Volumes).FirstOrDefault(v=>v.Id==_bookChoice.GetSelectedId());
        _studioPrintQuote.Text=book is null?"No print-ready books yet. Finish a one-shot or one short numbered issue; collect five chapters into a book later.":$"Print order total: ¥{GameState.PrintingCost((PrintTier)_printerChoice.Selected,book.PrintedPages,(int)_printCopies.Value):N0} · {_printCopies.Value:N0} copies";
        foreach(var (button,ready,reason) in _studioRequirements){button.Disabled=!ready();button.TooltipText=button.Disabled?reason:"";}
        _operationsFeedback.Visible=_operationsFeedback.Text.Length>0;
    }
}
