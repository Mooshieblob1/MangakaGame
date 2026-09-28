using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private Color MutedInk=>new(_darkMode?"adbecd":"536568");
    private Color OutlineInk=>new(_darkMode?"3b5066":"c6d0ce");

    private void PolishTheme(Theme theme)
    {
        var padding=_presentation.CompactUi?8:11;
        StyleBoxFlat ControlSurface(Color color)
        {
            var style=Surface(color,padding);style.ContentMarginLeft=style.ContentMarginRight=14;
            style.BorderColor=OutlineInk;style.SetBorderWidthAll(1);return style;
        }
        foreach(var kind in new[]{"Button","OptionButton","LineEdit","TextEdit"})
        {
            theme.SetStylebox("normal",kind,ControlSurface(Paper));
            theme.SetStylebox("hover",kind,ControlSurface(Hover));
            theme.SetStylebox("pressed",kind,ControlSurface(SelectedSurface));
            theme.SetStylebox("disabled",kind,ControlSurface(Wash));
            theme.SetColor("font_disabled_color",kind,MutedInk);
            theme.SetColor("font_placeholder_color",kind,MutedInk);
        }
        var card=Surface(CardSurface,_presentation.CompactUi?10:16);card.BorderColor=OutlineInk;card.SetBorderWidthAll(1);
        theme.SetStylebox("panel","StudioCard",card);
        var primary=new Color(_darkMode?"365fa0":"315f9b");
        foreach(var (state,color) in new[]{("normal",primary),("hover",primary.Lightened(.13f)),("pressed",primary.Darkened(.12f))})
            theme.SetStylebox(state,"PrimaryAction",ControlSurface(color));
        foreach(var state in new[]{"font_color","font_hover_color","font_pressed_color","font_hover_pressed_color","font_focus_color"})theme.SetColor(state,"PrimaryAction",Colors.White);
        theme.SetTypeVariation("QuietLabel","Label");theme.SetColor("font_color","QuietLabel",MutedInk);
        theme.SetTypeVariation("SectionLabel","Label");theme.SetColor("font_color","SectionLabel",Accent);
        theme.SetTypeVariation("NavigationButton","Button");
        theme.SetStylebox("normal","NavigationButton",Surface(new Color(0,0,0,0),12));
        theme.SetStylebox("pressed","NavigationButton",ControlSurface(SelectedSurface));
        theme.SetConstant("h_separation","GridContainer",_presentation.CompactUi?8:14);
        theme.SetConstant("v_separation","GridContainer",_presentation.CompactUi?8:14);
        PolishHeaderTheme(theme);
    }

    private void PolishHeaderTheme(Theme theme)
    {
        StyleBoxFlat CompactSurface(Color color)
        {
            var style=Surface(color,_presentation.CompactUi?3:4);
            style.ContentMarginLeft=style.ContentMarginRight=10;
            style.BorderColor=OutlineInk;style.SetBorderWidthAll(1);return style;
        }
        foreach(var (kind,basis) in new[]{("HeaderButton","Button"),("HeaderOption","OptionButton")})
        {
            theme.SetTypeVariation(kind,basis);
            foreach(var (state,color) in new[]{("normal",Paper),("hover",Hover),("pressed",SelectedSurface),("disabled",Wash)})
                theme.SetStylebox(state,kind,CompactSurface(color));
        }
        theme.SetTypeVariation("HeaderActiveButton","HeaderButton");
        var active=new Color(_darkMode?"365fa0":"315f9b");
        foreach(var (state,color) in new[]{("normal",active),("hover",active.Lightened(.13f)),("pressed",active.Darkened(.12f))})
            theme.SetStylebox(state,"HeaderActiveButton",CompactSurface(color));
        foreach(var state in new[]{"font_color","font_hover_color","font_pressed_color","font_hover_pressed_color","font_focus_color"})theme.SetColor(state,"HeaderActiveButton",Colors.White);
        var funds=CompactSurface(CardSurface);funds.ContentMarginLeft=funds.ContentMarginRight=8;
        theme.SetTypeVariation("HeaderFunds","PanelContainer");theme.SetStylebox("panel","HeaderFunds",funds);
        var panel=(StyleBoxFlat)theme.GetStylebox("panel","FloatingPanel").Duplicate();
        panel.ContentMarginTop=panel.ContentMarginBottom=6;
        theme.SetTypeVariation("HeaderPanel","FloatingPanel");theme.SetStylebox("panel","HeaderPanel",panel);
    }

    private Label QuietWords(Control parent,string text,int size=14)
    {var label=Words(parent,text,size);label.ThemeTypeVariation="QuietLabel";return label;}

    private readonly Dictionary<string,bool> _disclosureStates=new();
    private VBoxContainer Disclosure(Control parent,string title,string description="",bool expanded=false,string? key=null)
    {
        var stateKey=$"{_page}:{_detailId}:{_scopeLocation}:{key??title}";
        if(_disclosureStates.TryGetValue(stateKey,out var remembered))expanded=remembered;
        var card=StudioCard(parent,"");
        var toggle=ActionButton(card,title,()=>{});toggle.ToggleMode=true;toggle.Alignment=HorizontalAlignment.Left;
        toggle.AutowrapMode=TextServer.AutowrapMode.WordSmart;toggle.CustomMinimumSize=new(0,36);
        toggle.SetPressedNoSignal(expanded);
        var content=new VBoxContainer{Visible=expanded,SizeFlagsHorizontal=SizeFlags.ExpandFill};card.AddChild(content);
        void Update(){content.Visible=toggle.ButtonPressed;toggle.Text=(content.Visible?"▾  ":"▸  ")+title;}
        toggle.Toggled+=_=>{_disclosureStates[stateKey]=toggle.ButtonPressed;Update();};Update();
        if(description.Length>0)QuietWords(content,description);
        return content;
    }

    private Button ChoiceButton(Control parent,string title,bool selected,Action action)
    {
        var button=ActionButton(parent,title,action);button.ToggleMode=true;button.SetPressedNoSignal(selected);return button;
    }

    private void CashEntryRow(Control parent,LedgerEntry entry,bool fullDate=false)
    {
        var row=new HFlowContainer();parent.AddChild(row);
        var copy=new VBoxContainer{CustomMinimumSize=new(190,0),SizeFlagsHorizontal=SizeFlags.ExpandFill};row.AddChild(copy);
        Words(copy,entry.Reason,15);
        QuietWords(copy,$"{entry.Time.ToString(fullDate?"d MMM yyyy · HH:mm":"d MMM · HH:mm")} · {Humanize(entry.Kind.ToString())}",12);
        var amount=Words(row,$"{(entry.Amount<0?"−":"+")}¥{Math.Abs((decimal)entry.Amount):N0}",18);
        amount.HorizontalAlignment=HorizontalAlignment.Right;amount.SizeFlagsVertical=SizeFlags.ShrinkCenter;
        amount.AddThemeColorOverride("font_color",new Color(entry.Amount<0?(_darkMode?"ff929b":"b52035"):(_darkMode?"78e6a2":"16703a")));
        // Theme changes recolor transaction rows without rebuilding the page or losing scroll.
        amount.SetMeta("cash_sign",entry.Amount<0?-1:1);
        parent.AddChild(new HSeparator());
    }

    private void BuildEmployeeDetails(Person employee)
    {
        var overview=StudioCard(_sideContent,employee.Name,_state.FindSeries(employee.MainSeriesId??0)?.Title??"No series assigned");
        var row=new HBoxContainer();overview.AddChild(row);
        row.AddChild(new StaffPortrait{Recipe=employee.Appearance??AppearanceRecipe.Generate(_state.RngSeed,employee.Id)});
        var info=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};row.AddChild(info);
        LiveWords(info,()=>StaffActivityText(employee),18);
        LiveWords(info,()=>$"¥{employee.Employment?.MonthlySalary??0:N0} / month",16);
        var actions=new HFlowContainer();overview.AddChild(actions);
        ActionButton(actions,"Assignments and work schedule",()=>{_selectedPersonId=employee.Id;ResetPersonInputs();OpenWorkspace("Production");}).ThemeTypeVariation="PrimaryAction";
        ActionButton(actions,"Employment and pay",()=>{_selectedPersonId=employee.Id;OpenWorkspace("Business actions");});
        ActionButton(actions,"Team & stage assignments",()=>OpenWorkspace("Recruitment"));
        var needs=StudioCard(_sideContent,"WELLBEING");
        foreach(var (name,value) in new (string,Func<double>)[]{("Happiness",()=>employee.Happiness),("Food",()=>employee.Food),("Drink",()=>employee.Drink),("Comfort",()=>employee.Comfort)})
        {
            var label=Words(needs,"",14);var bar=new ProgressBar{MaxValue=100,ShowPercentage=false,CustomMinimumSize=new(0,12)};needs.AddChild(bar);
            void RefreshNeed(){label.Text=$"{name} · {value():0} / 100";bar.Value=value();}
            RefreshNeed();_pageLiveValues.Add(RefreshNeed);
        }
        var skills=StudioCard(_sideContent,"DRAWING SKILLS");
        if(employee.HiddenTalent)QuietWords(skills,"Skills not yet known.");
        else foreach(var skill in employee.Skills)LiveWords(skills,()=>$"{(skill.Key==Stage.Name?"Storyboard":skill.Key)} · {employee.Skills[skill.Key]:0}",15);
    }
    private string StaffActivityText(Person person)
    {
        if(_state.AtOutsideJob(person,_state.Clock.Now))return "Away · part-time job";
        var activity=_state.OfficeActivities.FirstOrDefault(a=>a.PersonId==person.Id)?.Kind;
        if(activity is not null&&activity is not (OfficeActivityKind.Work or OfficeActivityKind.Idle))
            return Humanize(activity.ToString()!);
        return person.CurrentTask is {} task?$"Working · {(task.Stage==Stage.Name?"Storyboard":task.Stage)}":_state.WaitingReason(person)??"Available for work";
    }
}
