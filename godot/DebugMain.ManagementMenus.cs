using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private void ShowMenu()
    {
        if(OfficeEditing){Notify("Apply or discard furniture changes before opening the menu.");return;}
        if(!_inMenu)CaptureReportScreen();
        Pause();_inMenu=true;_menu.Show();Empty(_menuContent);
        var hero=StudioCard(_menuContent,"");var heading=new HBoxContainer();hero.AddChild(heading);
        heading.AddChild(HelperPortrait(112,126));var title=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};heading.AddChild(title);
        Words(title,"MANGAKA STUDIO",36);Words(title,"A career told one page at a time.",20);
        Words(title,"TOKYO · 1996",14);
        var cards=new HFlowContainer();_menuContent.AddChild(cards);
        var play=StudioCard(cards,"YOUR NEXT PAGE");play.GetParent<Control>().CustomMinimumSize=new(370,0);
        ActionButton(play,"Return to this studio",()=>{_inMenu=false;_menu.Hide();}).ThemeTypeVariation="PrimaryAction";
        ActionButton(play,"Continue latest career",()=>
        {
            string? failure=null;
            foreach(var save in _careers.List())try{LoadCareer(save);if(failure is not null)Notify("Loaded an earlier valid snapshot. The latest could not be read: "+failure);return;}
                catch(Exception ex)when(ex is System.IO.IOException or System.IO.InvalidDataException or JsonException){failure=ex.Message;}
            Notify(failure??"No saved careers yet. Choose New Career to begin.");
        });
        ActionButton(play,"New Career",NewCareerMenu);ActionButton(play,"Load Career",LoadCareerMenu);
        var more=StudioCard(cards,"MAKE YOURSELF AT HOME","Begin at your parents' house, with a drawing desk and a dream.");more.GetParent<Control>().CustomMinimumSize=new(300,0);
        ActionButton(more,"Settings",SettingsMenu);ActionButton(more,"Report a problem",ReportProblem);ActionButton(more,"Quit",()=>GetTree().Quit());
        Words(_menuContent,"Private alpha · "+ProblemReport.Build,13);
    }
    private void NewCareerMenu()
    {
        Empty(_menuContent);Words(_menuContent,"A new career",30);
        Words(_menuContent,"1 April 1996 · A rent-free room at your parents' home",16);
        var sections=MenuSections("Your mangaka","Career rules");
        var character=new HFlowContainer();sections[0].AddChild(character);
        var portrait=StudioCard(character,"YOUR STARTING PRODIGY");portrait.GetParent<Control>().CustomMinimumSize=new(320,0);
        var preview=new CreatorPreview{Name="CreatorPreview"};portrait.AddChild(preview);
        var previewName=Words(portrait,"Aki",20);previewName.HorizontalAlignment=HorizontalAlignment.Center;
        var turn=new HFlowContainer();portrait.AddChild(turn);
        ActionButton(turn,"Turn left",()=>preview.Turn(-45));ActionButton(turn,"Front",preview.FaceFront);ActionButton(turn,"Turn right",()=>preview.Turn(45));
        var choices=StudioCard(character,"MAKE THEM YOUR OWN");choices.GetParent<Control>().CustomMinimumSize=new(320,0);
        Words(choices,"Name",14);var name=new LineEdit{Name="CreatorName",Text="Aki",PlaceholderText="Aki",MaxLength=40};choices.AddChild(name);
        name.TextChanged+=text=>previewName.Text=string.IsNullOrWhiteSpace(text)?"Aki":text.Trim();
        var options=new OptionButton[6];
        var appearanceFields=new GridContainer{Columns=2,SizeFlagsHorizontal=SizeFlags.ExpandFill};choices.AddChild(appearanceFields);
        var labels=new[]{"Skin tone","Hair colour","Outfit colour","Hair style","Clothing","Build"};
        var values=new[]{new[]{"Light","Medium","Tan","Deep"},new[]{"Black","Brown","Chestnut","Silver"},new[]{"Teal","Burgundy","Olive","Ochre","Violet","Grey"},new[]{"Short","Bob","Tied back"},new[]{"Cardigan","Collared shirt","Sweater"},new[]{"Slim","Medium","Broad"}};
        for(var index=0;index<options.Length;index++)
        {
            var field=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};appearanceFields.AddChild(field);
            Words(field,labels[index],14);var option=new OptionButton{Name=$"CreatorAppearance{index}"};
            foreach(var value in values[index])option.AddItem(value);field.AddChild(option);options[index]=option;
        }
        var glasses=new CheckBox{Name="CreatorGlasses",Text="Glasses"};choices.AddChild(glasses);
        AppearanceRecipe SelectedLook()=>new(options[0].Selected,options[1].Selected,options[2].Selected,options[3].Selected,glasses.ButtonPressed,options[5].Selected,options[4].Selected);
        foreach(var option in options)option.ItemSelected+=_=>preview.Recipe=SelectedLook();
        glasses.Toggled+=_=>preview.Recipe=SelectedLook();preview.Recipe=SelectedLook();
        var world=StudioCard(sections[1],"YOUR WORLD");
        Words(world,"World seed",14);var seed=new SpinBox{MinValue=0,MaxValue=int.MaxValue,Value=0};world.AddChild(seed);
        Words(world,"When other lead creators leave",14);var rights=new OptionButton();rights.AddItem("Studio keeps future rights unless released");rights.AddItem("Creator keeps future rights");world.AddChild(rights);
        var difficulty = DifficultyControls(sections[1], true);
        var actions=new HFlowContainer();_menuContent.AddChild(actions);
        var begin=ActionButton(actions,"Begin career",()=>
        {
            _state=GameState.NewGame((int)seed.Value,rights.Selected==0?OwnershipMode.StudioRetention:OwnershipMode.CreatorRetention,name.Text);
            _state.Apply(difficulty());
            _state.Apply(new SetAppearanceCommand(SelectedLook()));
            _presentation=new(){Page="Office"};_careerId=Guid.NewGuid().ToString("N");ResetManagementSession();SaveCareer("The first page");ShowOffice();
        });begin.ThemeTypeVariation="PrimaryAction";
        name.TextChanged+=text=>{begin.Disabled=text.Trim().Any(char.IsControl);name.TooltipText=begin.Disabled?"Use a name without control characters.":"Up to 40 characters. Leave blank to use Aki.";};
        ActionButton(actions,"Back",ShowMenu);
    }
    private void SettingsMenu()
    {
        Empty(_menuContent);Words(_menuContent,"Settings",30);
        var sections=MenuSections("Display & sound","Helper & pauses","Difficulty & Sandbox");
        var display=StudioCard(sections[0],"COMFORT & CONTROLS");AlphaSettings(display);
        var help=StudioCard(sections[1],"HELPER-CHAN","Important gameplay notices stay enabled independently of tutorials and stories.");
        var tips=new CheckBox{Text="Helper-Chan's first-use tips",ButtonPressed=_presentation.Tips};help.AddChild(tips);tips.Toggled+=v=>_presentation.Tips=v;
        var stories=new CheckBox{Text="Optional conversation prompts",ButtonPressed=_presentation.Stories};help.AddChild(stories);stories.Toggled+=v=>_presentation.Stories=v;
        var difficulty = DifficultyControls(sections[2], false);
        ActionButton(sections[2],"Apply difficulty and Sandbox options",()=>{_state.Apply(difficulty());_dirty=true;Notify("Career settings saved. " + (AchievementDelivery.Eligible(_state)?"Achievements remain eligible.":"Steam achievements are disabled for this save."));});
        Words(display,"Text scale",14);var scale=new HSlider{MinValue=.8,MaxValue=1.5,Step=.1,Value=_presentation.UiScale};display.AddChild(scale);
        scale.ValueChanged+=value=>{_presentation.UiScale=value;ApplyTextScale();};
        var compact=new CheckBox{Text="Compact interface spacing",ButtonPressed=_presentation.CompactUi};display.AddChild(compact);
        compact.Toggled+=value=>{_presentation.CompactUi=value;ApplyUiTheme();};
        var reduced=new CheckBox{Text="Reduced interface motion",ButtonPressed=_presentation.ReducedUiMotion};display.AddChild(reduced);
        reduced.Toggled+=value=>{_presentation.ReducedUiMotion=value;_pageTween?.Kill();_side.Modulate=_report.Modulate=Colors.White;};
        Words(display,"Motion trails",14);var motion=new OptionButton();foreach(var text in new[]{"Full","Reduced","Off"})motion.AddItem(text);motion.Select(2-_officeView.Effect);display.AddChild(motion);
        motion.ItemSelected+=i=>{_officeView.Effect=2-(int)i;_homeOffice.Effect=2-(int)i;_homeOffice.ClearMotion();};
        var pauses=StudioCard(sections[1],"AUTOMATIC PAUSE","Overnight results wait until morning.");
        foreach(var type in new[]{EventType.IndustryDecision,EventType.StaffNotice,EventType.WageArrears,EventType.SerializationOffered,EventType.ChapterCompleted,EventType.DeadlineMissed})
        {var key=type;var check=new CheckBox{Text=Humanize(type.ToString()),ButtonPressed=_state.Settings.AutoPause.GetValueOrDefault(type)};pauses.AddChild(check);check.Toggled+=v=>_state.Settings.AutoPause[key]=v;}
        ActionButton(_menuContent,"Back",ShowMenu);
    }
    private VBoxContainer[] MenuSections(params string[] names)
    {
        var tabs=new HFlowContainer();_menuContent.AddChild(tabs);
        var pages=names.Select(_=>new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill}).ToArray();
        var buttons=new List<Button>();
        for(var i=0;i<names.Length;i++)
        {
            var index=i;var button=ActionButton(tabs,names[i],()=>
            {
                for(var j=0;j<pages.Length;j++){pages[j].Visible=j==index;buttons[j].SetPressedNoSignal(j==index);buttons[j].ThemeTypeVariation=j==index?"PrimaryAction":"Button";}
                _menuContent.GetParent<ScrollContainer>().SetDeferred("scroll_vertical",0);
            });
            button.ToggleMode=true;button.SetPressedNoSignal(i==0);button.ThemeTypeVariation=i==0?"PrimaryAction":"Button";buttons.Add(button);
        }
        for(var i=0;i<pages.Length;i++){_menuContent.AddChild(pages[i]);pages[i].Visible=i==0;}
        return pages;
    }
    private static string Humanize(string value)=>System.Text.RegularExpressions.Regex.Replace(value,"(?<=[a-z])([A-Z])"," $1");
    private void OpenSaveMenu()
    {
        if(OfficeEditing){Notify("Finish your furniture changes before saving.");return;}
        Pause();_inMenu=true;_menu.Show();Empty(_menuContent);Words(_menuContent,"Keep this chapter of your career",30);
        var name=new LineEdit{Text=$"{_state.ControlledBusiness.Name} · {_state.Clock.Now:d MMM yyyy}",MaxLength=100};_menuContent.AddChild(name);
        ActionButton(_menuContent,"Save snapshot",()=>{SaveCareer(name.Text);_menu.Hide();_inMenu=false;});
        ActionButton(_menuContent,"Export portable career",()=>
        {
            var saved=SaveCareer(name.Text);ChooseFile("Export career",FileDialog.FileModeEnum.SaveFile,["*.mangaka ; Portable career"],path=>System.IO.File.WriteAllBytes(path,_careers.Export(saved)));
        });
        ActionButton(_menuContent,"Back",ShowMenu);
    }
    private CareerSaveInfo SaveCareer(string name,bool auto=false)
    {
        _presentation.Camera=JsonSerializer.Serialize(_homeOffice.CapturePreferences());
        _presentation.Page=_page;_presentation.Detail=_detailId;_presentation.Scope=_scopeLocation;_presentation.ViewedOffice=_viewLocation;
        _presentation.Scroll=_sideScroll.ScrollVertical;_presentation.ChartDays=_chartDays;_presentation.PersonalAccount=_personalAccount;
        _presentation.OfficeSidebar=_officeSidebar;_presentation.InboxFilter=_inboxFilter;
        _presentation.SelectedSeries=_progressSeriesId;_presentation.SelectedPerson=SelectedPerson.Id;_presentation.WorkspaceScroll=WorkspaceScroll();
        var result=_careers.Save(_careerId,name,_state,_presentation,auto);Notify(auto?"Daily autosave kept.":"Career saved: "+name);return result;
    }
    private void LoadCareerMenu()
    {
        Empty(_menuContent);Words(_menuContent,"Your careers",30);
        ActionButton(_menuContent,"Import career or previous save",()=>ChooseFile("Import career",FileDialog.FileModeEnum.OpenFile,["*.mangaka ; Portable career","*.json ; Previous save"],path=>
        {
            if(path.EndsWith(".mangaka",StringComparison.OrdinalIgnoreCase))LoadCareer(_careers.Import(System.IO.File.ReadAllBytes(path)));
            else{_state=GameState.ImportSupported(System.IO.File.ReadAllText(path));_careerId=Guid.NewGuid().ToString("N");_presentation=new();ResetManagementSession();SaveCareer("Imported career");}
        }));
        foreach(var save in _careers.List().Take(80)){var entry=save;ActionButton(_menuContent,$"{save.Name}   ·   {save.GameDate:d MMM yyyy}   ·   {save.Studio}"+(save.Auto?"   [auto]":""),()=>LoadCareer(entry));}
        ActionButton(_menuContent,"Back",ShowMenu);
    }
    private void LoadCareer(CareerSaveInfo save)
    {
        var loaded=_careers.Load(save);_state=loaded.State;_presentation=loaded.View;_careerId=save.Career;ResetManagementSession();
        if(_presentation.Camera.Length>0)try{_homeOffice.RestorePreferences(JsonSerializer.Deserialize<OfficeViewPreferences>(_presentation.Camera)!);}catch(JsonException){Notify("The office view was reset.");}
        _officeView.Effect=_homeOffice.Effect;_officeEffect.Select(2-_homeOffice.Effect);
        if(_presentation.Artwork.Values.Any(hash=>!System.IO.File.Exists(_careers.AssetPath(_careerId,hash))))Notify("Some custom artwork is missing. Bundled artwork is shown until it is restored or reset.");
    }
    private void ResetManagementSession()
    {
        CancelOvernight();
        _disclosureStates.Clear();
        _guiAuthority="";
        _studioLocationKey="";StudioSection("Overview");_operationsFeedback.Text="";_operationsFeedback.Hide();
        _refreshPrintPanel=null;_refreshOnlinePanel=null;_refreshConvention=null;_printingBookId=0;
        _progressSeriesId=_presentation.Page is "Series details" or "Sell online" or "Print doujin" or "Showcase"?_presentation.Detail:_presentation.SelectedSeries;
        Pause();_scanIndex=_state.Events.Count;_popupEvents.Clear();_recapDialog.Hide();_helperPopup.Hide();_storyOpen=false;
        _viewLocation=_presentation.ViewedOffice;_selectedPersonId=_state.ControlledStaff.Any(p=>p.Id==_presentation.SelectedPerson)?_presentation.SelectedPerson:_state.ProtagonistPersonId;_scopeLocation=_presentation.Scope;_detailId=_presentation.Detail;_back.Clear();
        _page=new[]{"Office","Inbox","Books","Series","Series details","Staff","Person","Finances","Studios","Industry","Showcase","Help","Awards","Licenses","Legacy","Guidance","New doujin","New series","Conventions","Print doujin","Sell online"}.Contains(_presentation.Page)||WorkspaceTabs.ContainsKey(_presentation.Page)?_presentation.Page:"Inbox";
        _officeSidebar=_presentation.OfficeSidebar&&_page is "Inbox" or "Series";
        _inboxFilter=new[]{"Needs attention","Results & milestones","Routine","All"}.Contains(_presentation.InboxFilter)?_presentation.InboxFilter:"Needs attention";_inboxPage=0;
        if(!_state.Locations.Any(l=>l.Id==_scopeLocation&&l.BusinessId==_state.ControlledBusinessId))_scopeLocation=0;
        _chartDays=_presentation.ChartDays;_personalAccount=_presentation.PersonalAccount;
        _lastAutosave=_state.Clock.Now.Date;_lastUiEvents=-1;_inMenu=false;_menu.Hide();_report.Hide();ResetPersonInputs();_dirty=true;
        _pageLiveValues.Clear();ApplyUiTheme();Refresh();RefreshManagement();_side.Show();ApplyPageLayout();BuildManagementPage();
        if(_progressSeriesId>0)SelectSeriesForWorkbench(_progressSeriesId);
        if(_page=="Office")ShowOffice();else OpenManagementWorkbench();
        RestoreWorkspaceScroll(_report.Visible?_presentation.WorkspaceScroll:_presentation.Scroll);
    }
    private void ChooseFile(string title,FileDialog.FileModeEnum mode,string[] filters,Action<string> selected)
    {
        var dialog=new FileDialog{Title=title,FileMode=mode,Access=FileDialog.AccessEnum.Filesystem,Filters=filters,UseNativeDialog=!OS.GetCmdlineUserArgs().Contains("--alpha-smoke")};AddChild(dialog);
        dialog.FileSelected+=path=>{try{selected(path);}catch(Exception ex)when(ex is System.IO.IOException or System.IO.InvalidDataException or JsonException or InvalidOperationException or UnauthorizedAccessException){Notify(ex.Message);}finally{dialog.Hide();dialog.QueueFree();}};
        dialog.Canceled+=()=>{dialog.Hide();dialog.QueueFree();};dialog.PopupCentered(new(900,600));
    }
}
