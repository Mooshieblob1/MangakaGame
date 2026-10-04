using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    [Export] public bool ManagementInterface { get; set; }
    private CareerStore _careers=null!;
    private string _careerId=Guid.NewGuid().ToString("N");
    private CareerPresentation _presentation=new();
    private VBoxContainer _shell=null!,_sideContent=null!,_menuContent=null!;
    private PanelContainer _side=null!,_menu=null!,_report=null!,_helperPopup=null!;
    private OfficeView _homeOffice=null!;
    private Label _status=null!,_notice=null!,_sideTitle=null!,_workbenchNotice=null!;
    private HFlowContainer _managementHeader=null!;
    private Button _inboxShortcut=null!;
    private OptionButton _viewOffice=null!;
    private Label _viewOfficeDistrict=null!;
    private ScrollContainer _sideScroll=null!;
    private int _viewLocation,_scopeLocation,_detailId;
    private string _page="Inbox";
    private Label _viewOfficeHeading=null!;
    private readonly Stack<(string Page,int Detail,int Scope,int Scroll,bool Sidebar,int Series,int Person)> _back=new();
    private bool _managementReady,_inMenu=true,_expanded,_storyOpen;
    private bool _menuCompact;
    private Label? _menuNotice;
    private DateTime _lastAutosave=GameClock.Start.Date;
    private int _lastUiEvents=-1;
    private double _storyResume;
    private readonly Queue<int> _popupEvents=new();

    private static StyleBoxFlat Surface(Color color,int padding=12)
    {
        var box=new StyleBoxFlat{BgColor=color,CornerRadiusTopLeft=8,CornerRadiusTopRight=8,CornerRadiusBottomLeft=8,CornerRadiusBottomRight=8};
        box.ContentMarginLeft=box.ContentMarginRight=box.ContentMarginTop=box.ContentMarginBottom=padding;return box;
    }
    // Lilita One for buttons and headings (spec 2026-09-29, Q46); body text and numbers keep the reading font.
    private string _fontPath="res://Assets/Fonts/LilitaOne-Regular.ttf";
    private static Font? HeadingFont;
    private Font? LoadHeadingFont()
    {
        if(ResourceLoader.Exists(_fontPath)&&ResourceLoader.Load<Font>(_fontPath) is {} font)
        {
            // Lilita One lacks symbols such as the pause bars and arrows; the reading font supplies them (final review).
            var fallbacks=font.Fallbacks;if(!fallbacks.Contains(ThemeDB.FallbackFont)){fallbacks.Add(ThemeDB.FallbackFont);font.Fallbacks=fallbacks;}
            return font;
        }
        LogTimeline("error heading font missing: "+_fontPath);return null;
    }
    private Theme EditorialTheme()
    {
        var theme=new Theme{DefaultFontSize=16};
        HeadingFont=LoadHeadingFont();
        if(HeadingFont is not null)
        {
            theme.SetFont("font","Button",HeadingFont);theme.SetFont("font","TabContainer",HeadingFont);
            // Drop-downs show values and check boxes read as options, so they keep the reading font.
            foreach(var kind in new[]{"OptionButton","CheckBox","CheckButton"})theme.SetFont("font",kind,ThemeDB.FallbackFont);
        }
        foreach(var kind in new[]{"Label","Button","OptionButton","LineEdit","TextEdit","RichTextLabel","CheckBox","Tree","ItemList","PopupMenu","TooltipLabel"})
        {theme.SetColor("font_color",kind,Ink);theme.SetColor("font_hover_color",kind,Accent);theme.SetColor("font_pressed_color",kind,Ink);theme.SetColor("font_hover_pressed_color",kind,Accent);theme.SetColor("font_focus_color",kind,Ink);theme.SetColor("font_disabled_color",kind,Ink.Darkened(.35f));theme.SetColor("font_placeholder_color",kind,Ink.Darkened(.3f));theme.SetColor("caret_color",kind,Ink);theme.SetColor("selection_color",kind,SelectedSurface);}
        foreach(var kind in new[]{"Button","OptionButton","LineEdit","TextEdit","Tree","ItemList"})
        {
            theme.SetStylebox("normal",kind,Surface(Paper));theme.SetStylebox("hover",kind,Surface(Hover));
            theme.SetStylebox("pressed",kind,Surface(SelectedSurface));theme.SetStylebox("disabled",kind,Surface(Wash));
            var focus=Surface(new Color(0,0,0,0),3);focus.BorderColor=Accent;focus.SetBorderWidthAll(2);theme.SetStylebox("focus",kind,focus);
        }
        theme.SetStylebox("panel","PanelContainer",Surface(Paper,18));theme.SetStylebox("panel","TabContainer",Surface(Paper));
        theme.SetTypeVariation("StudioCard","PanelContainer");theme.SetStylebox("panel","StudioCard",Surface(CardSurface,14));
        var floating=Surface(Paper,10);floating.BorderColor=Ink.Darkened(.65f);floating.SetBorderWidthAll(1);
        floating.ShadowColor=new Color(0,0,0,.25f);floating.ShadowSize=5;
        theme.SetTypeVariation("FloatingPanel","PanelContainer");theme.SetStylebox("panel","FloatingPanel",floating);
        theme.SetColor("default_color","RichTextLabel",Ink);
        foreach(var kind in new[]{"PopupMenu","TooltipPanel","Window","AcceptDialog"})theme.SetStylebox("panel",kind,Surface(Paper));
        theme.SetStylebox("hover","PopupMenu",Surface(Hover));
        theme.SetStylebox("tab_selected","TabContainer",SlabStyle(BrandPalette.Mint,BrandPalette.MintBase,3,12,4)); // where you are
        theme.SetStylebox("tab_unselected","TabContainer",Surface(Wash,10));theme.SetStylebox("tab_hovered","TabContainer",Surface(Hover,10));
        theme.SetColor("font_selected_color","TabContainer",new Color(BrandPalette.Ink));theme.SetColor("font_unselected_color","TabContainer",Ink);
        theme.SetTypeVariation("PrimaryAction","Button");theme.SetStylebox("normal","PrimaryAction",Surface(SelectedSurface));
        var track=Surface(CardSurface,0);track.BorderColor=Ink.Darkened(.5f);track.SetBorderWidthAll(1);theme.SetStylebox("background","ProgressBar",track);theme.SetStylebox("fill","ProgressBar",Surface(new Color(Brand.Progress),0));
        theme.SetColor("font_color","ProgressBar",Ink);theme.SetColor("font_outline_color","ProgressBar",Wash);theme.SetConstant("outline_size","ProgressBar",3);
        var spacing=_presentation.CompactUi?6:12;
        theme.SetConstant("separation","VBoxContainer",spacing);theme.SetConstant("separation","HBoxContainer",spacing);
        theme.SetConstant("h_separation","HFlowContainer",spacing);theme.SetConstant("v_separation","HFlowContainer",spacing);
        PolishTheme(theme);
        return theme;
    }
    private Button ActionButton(Control parent,string title,Action action)
    {
        var button=new Button{Text=title,MouseDefaultCursorShape=CursorShape.PointingHand};parent.AddChild(button);
        button.Pressed+=()=>{try{_audio?.Cue();action();}catch(Exception ex)when(ex is System.IO.IOException or System.IO.InvalidDataException or InvalidCommandException or JsonException or InvalidOperationException or UnauthorizedAccessException){LogTimeline("error "+TimelineRedactor.Clean(ex.Message,_state));Notify(ex.Message);}};
        return button;
    }
    private Button HeaderButton(Control parent,string title,Action action)
    {
        var button=ActionButton(parent,title,action);button.ThemeTypeVariation="HeaderButton";
        button.SizeFlagsVertical=SizeFlags.ShrinkCenter;button.CustomMinimumSize=new(0,32);return button;
    }
    private static Label Words(Control parent,string text,int size=16)
    {
        var label=new Label{Text=text,AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=SizeFlags.ExpandFill};label.SetMeta("base_font_size",size);parent.AddChild(label);
        var root=parent.IsInsideTree()?parent.GetTree().Root.GetChildren().OfType<DebugMain>().FirstOrDefault():null;label.AddThemeFontSizeOverride("font_size",(int)(size*(root?._presentation.UiScale??1)));
        if(size>=20&&HeadingFont is not null)label.AddThemeFontOverride("font",HeadingFont); // headings (20 px and above)
        return label;
    }
    // Numbers keep the reading font even at heading sizes (spec 2026-09-29).
    private static Label Figure(Label label){label.RemoveThemeFontOverride("font");return label;}
    private void ApplyTextScale()
    {
        // The per-career text scale is retired: the per-computer interface size scales everything (spec 2026-10-04).
        _presentation.UiScale=1;
        Theme.DefaultFontSize=(int)(16*_presentation.UiScale);
        // Name tags render inside the full-resolution office image, so they follow the interface size.
        if(_homeOffice is not null)_homeOffice.LabelTextScale=InterfaceScale;
        if(_officeView is not null)_officeView.LabelTextScale=InterfaceScale;
        foreach(var label in FindChildren("*","Label",true,false).OfType<Label>())if(label.HasMeta("base_font_size"))label.AddThemeFontSizeOverride("font_size",(int)((int)label.GetMeta("base_font_size")*_presentation.UiScale));
        ResizeGui();
    }
    private static void Empty(Control node){foreach(var child in node.GetChildren()){node.RemoveChild(child);child.QueueFree();}}
    private void BuildManagementShell(Control debugRoot)
    {
        debugRoot.Hide();LoadUiPreferences();LoadDisplaySettings();Theme=EditorialTheme();
        _careers=new CareerStore(ProjectSettings.GlobalizePath("user://careers"));
        StartTimeline();
        _backdrop=new ColorRect{Color=Wash,MouseFilter=MouseFilterEnum.Ignore};_backdrop.SetAnchorsPreset(LayoutPreset.FullRect);AddChild(_backdrop);
        var margin=new MarginContainer();margin.SetAnchorsPreset(LayoutPreset.FullRect);foreach(var e in new[]{"left","right","top","bottom"})margin.AddThemeConstantOverride("margin_"+e,16);AddChild(margin);
        var layout=new HBoxContainer();margin.AddChild(layout);
        var railPanel=new PanelContainer{ThemeTypeVariation="FloatingPanel"};layout.AddChild(railPanel);
        var railScroll=new ScrollContainer{HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};railPanel.AddChild(railScroll);
        _rail=new VBoxContainer{CustomMinimumSize=new(148,0)};railScroll.AddChild(_rail);
        Words(_rail,"MANGAKA\nDAYS",21).AutowrapMode=TextServer.AutowrapMode.Off;
        Words(_rail,"マンガカ・デイズ",12);
        _shell=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};_shell.AddThemeConstantOverride("separation",4);layout.AddChild(_shell);
        var header=new HFlowContainer();_managementHeader=header;_shell.AddChild(header);header.AddThemeConstantOverride("v_separation",4);
        _status=Words(header,"",14);_status.CustomMinimumSize=new(150,0);_status.SizeFlagsVertical=SizeFlags.ShrinkCenter;
        _status.AutowrapMode=TextServer.AutowrapMode.Off;
        var funds=new PanelContainer{ThemeTypeVariation="HeaderFunds",SizeFlagsVertical=SizeFlags.ShrinkCenter};header.AddChild(funds);
        BuildMoneyHeader(funds);
        _inboxShortcut=HeaderButton(header,"0 unread",()=>{if(_homeOffice.Visible)OpenOfficeSidebar("Inbox");else Navigate("Inbox");});_inboxShortcut.TooltipText="Open Inbox";
        foreach(var speed in new[]{0,1,2,4,8,32}){var s=speed;var button=HeaderButton(header,s==0?"Ⅱ":$"{s}×",()=>{if(GameKeysAvailable()){if(s==0)TogglePause();else ChooseSpeed(s);}});button.ToggleMode=true;_speedButtons[s]=button;button.TooltipText="Space: pause / resume · 1: slower · 2: faster";}
        HeaderButton(header,"Save",()=>OpenSaveMenu());
        _overnightSpeedBadge=Words(header,"▶▶ 32× NIGHT",14);_overnightSpeedBadge.SizeFlagsVertical=SizeFlags.ShrinkCenter;_overnightSpeedBadge.Hide();
        _overnightSpeedBadge.ThemeTypeVariation="SectionLabel";
        _navigation["Office"]=ActionButton(_rail,"⌂  Office",ShowOffice);
        foreach(var (page,icon) in new[]{("Goals","★"),("Inbox","✉"),("Series","▤"),("Books","▥"),("Staff","♙"),("Finances","¥"),("Studios","▦"),("Industry","◇"),("Help","?")}){var name=page;_navigation[name]=ActionButton(_rail,icon+"  "+name,()=>Navigate(name));}
        foreach(var button in _navigation.Values){button.ToggleMode=true;button.ThemeTypeVariation="NavigationButton";button.Alignment=HorizontalAlignment.Left;}
        _viewOfficeHeading=Words(_rail,"VIEWING STUDIO",12);
        // The current name determines the selector's minimum width at the chosen
        // text scale. Keep the district separate rather than clipping both together.
        _viewOffice=new OptionButton{FitToLongestItem=false,ClipText=false};_rail.AddChild(_viewOffice);
        _viewOfficeDistrict=Words(_rail,"",12);_viewOfficeDistrict.ThemeTypeVariation="QuietLabel";
        _viewOffice.ItemSelected+=_=>{_viewLocation=_viewOffice.GetSelectedId();RefreshManagement();};
        ActionButton(_rail,"Menu",ShowMenu);
        BuildCurrentProgress();
        var body=new HBoxContainer{SizeFlagsVertical=SizeFlags.ExpandFill};_shell.AddChild(body);
        _homeOffice=new OfficeView{ShowCompanion=true};body.AddChild(_homeOffice);
        _officeView.ShowCompanion=true;_homeOffice.HelperClicked+=()=>Navigate("Help");
        _homeOffice.PersonClicked+=id=>{_selectedPersonId=id;Navigate("Person",id);};
        _side=new PanelContainer{CustomMinimumSize=new(440,0)};body.AddChild(_side);var sideBox=new VBoxContainer();_side.AddChild(sideBox);
        var sideBar=new HFlowContainer();sideBox.AddChild(sideBar);
        ActionButton(sideBar,"← Back",GoBack);_sideTitle=Words(sideBar,"Inbox",24);_sideTitle.AutowrapMode=TextServer.AutowrapMode.Off;
        _pageRefresh=ActionButton(sideBar,"Refresh",()=>{BuildManagementPage();Notify("Updated to the current simulation time.");});
        _sidebarToggle=ActionButton(sideBar,"Beside office",()=>{_officeSidebar=!_officeSidebar;ApplyPageLayout();BuildManagementPage();});
        ActionButton(sideBar,"×",ShowOffice).TooltipText="Close this page and return to the office";
        _sideScroll=new ScrollContainer{SizeFlagsVertical=SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};sideBox.AddChild(_sideScroll);
        var reading=new GuiReadingColumn{SizeFlagsHorizontal=SizeFlags.ExpandFill};_sideScroll.AddChild(reading);
        _sideContent=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};reading.AddChild(_sideContent);
        _notice=Words(_shell,"Your studio, one page at a time.",14);
        _report=new PanelContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};body.AddChild(_report);
        var reportBox=new VBoxContainer();_report.AddChild(reportBox);var reportNav=new HBoxContainer();reportBox.AddChild(reportNav);
        ActionButton(reportNav,"← Back",GoBack);_workspaceTitle=Words(reportNav,"Production",24);ActionButton(reportNav,"Office",ShowOffice);
        _workspaceActions=new HFlowContainer();reportBox.AddChild(_workspaceActions);
        _mainTabs.Reparent(reportBox);_mainTabs.TabsVisible=false;_report.Hide();
        // A workspace shows notices under its own form, so the bottom bar steps aside rather than repeat them (finding B7).
        _report.VisibilityChanged+=()=>_notice.Visible=!_report.Visible;
        var production=(Control)_mainTabs.GetChild(0);_mainTabs.RemoveChild(production);
        var productionScroll=new ScrollContainer{Name="Production"};_mainTabs.AddChild(productionScroll);_mainTabs.MoveChild(productionScroll,0);productionScroll.AddChild(production);
        production.SizeFlagsHorizontal=production.SizeFlagsVertical=SizeFlags.ExpandFill;_mainTabs.CurrentTab=0;
        _workbenchNotice=Words(reportBox,"Changes use the selected studio, person and series.",14);
        // The developer event stream is not part of the player-facing workbench.
        _log.Hide();
        var shade=new ColorRect{Color=new Color(0,0,0,.30f)};shade.SetAnchorsPreset(LayoutPreset.FullRect);AddChild(shade);
        _menu=new PanelContainer();_menu.SetAnchorsPreset(LayoutPreset.FullRect);_menu.OffsetLeft=180;_menu.OffsetTop=90;_menu.OffsetRight=-180;_menu.OffsetBottom=-70;AddChild(_menu);
        // The notice sits outside the rebuilt page, so a failed save or load on any sub-page is seen (final review).
        var menuBox=new VBoxContainer();_menu.AddChild(menuBox);_menuNotice=Words(menuBox,"",15);_menuNotice.Hide();
        var menuScroll=new ScrollContainer{HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,SizeFlagsVertical=SizeFlags.ExpandFill};menuBox.AddChild(menuScroll);_menuContent=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};menuScroll.AddChild(_menuContent);
        _pauseMenuContent=_menuContent;
        _helperPopup=new PanelContainer();_helperPopup.SetAnchorsPreset(LayoutPreset.Center);_helperPopup.Position=new(-420,-260);_helperPopup.Size=new(840,520);AddChild(_helperPopup);_helperPopup.Hide();
        _menu.VisibilityChanged+=()=>shade.Visible=_menu.Visible||_helperPopup.Visible;
        _helperPopup.VisibilityChanged+=()=>{shade.Visible=_menu.Visible||_helperPopup.Visible;_dirty=true;};
        BuildAlpha();BuildSpeedFeedback();PrepareWorkspaceForms();
        BuildFloatingOffice(margin,railPanel,body,shade);
        BuildWorkFeedback();
        _side.VisibilityChanged+=RefreshNavigation;_report.VisibilityChanged+=RefreshNavigation;
        GetViewport().SizeChanged+=ResizeGui;_managementReady=true;ApplyInterfaceSize();
        GetTree().AutoAcceptQuit=false; // the close button keeps a safety save first (HandleCloseRequest)
        _viewLocation=_state.Protagonist.Employment!.LocationId;RefreshManagement();OpenTitle();
        // A real launch starts black behind the start-up screens; the title screen fades up when they end.
        BuildCurtain(startBlack:!SmokeRun);
        if(!SmokeRun)BeginStartup(!_audioSettings.SetupDone||OS.GetCmdlineUserArgs().Contains("--first-launch"));
    }
    private void Notify(string text)
    {
        if(!_managementReady){LogLine(text);return;}
        _notice.Text=text;_workbenchNotice.Text=text;_workbenchNotice.Show();
        if(_titleNotice is not null){_titleNotice.Text=text;_titleNotice.Visible=text.Length>0;Callable.From(LayoutTitle).CallDeferred();}
        if(_titlePageNotice is not null&&_titlePage is {Visible:true}){_titlePageNotice.Text=text;_titlePageNotice.Visible=text.Length>0;}
        if(_menu.Visible&&_menuNotice is not null){_menuNotice.Text=text;_menuNotice.Visible=text.Length>0;}
    }
    public override void _UnhandledKeyInput(InputEvent ev)
    {
        if(_startup is not null||Fading)return; // keys belong to the start-up screens, and wait for fades
        if(TitleOpen){if(ev is InputEventKey{Keycode:Key.Escape})GetViewport().SetInputAsHandled();return;} // nothing to go back to
        if(!_managementReady||ev is not InputEventKey{Pressed:true,Echo:false,Keycode:Key.Escape})return;
        if(_storyOpen){Notify("Choose an answer, Read later or Skip to close this conversation.");return;}
        if(_helperPopup.Visible)_helperPopup.Hide();
        else if(OfficeEditing)Notify("Apply or discard your furniture changes first.");
        else if(_menu.Visible){_menu.Hide();_inMenu=false;}
        else if(_report.Visible||_side.Visible)ShowOffice();
        else ShowMenu();
        GetViewport().SetInputAsHandled();
    }
    private void Navigate(string page,int detail=0)
    {
        if(OfficeEditing){Notify("Apply or discard your furniture changes before navigating.");return;}
        RevealPage(page);
        LogTimeline("screen "+page);
        _back.Push((_page,_detailId,_scopeLocation,WorkspaceScroll(),_officeSidebar,_progressSeriesId,SelectedPerson.Id));
        _officeSidebar=false;_report.Hide();
        _page=page;_detailId=detail;_side.Show();ApplyPageLayout();BuildManagementPage();_sideScroll.ScrollVertical=0;
        if(page is "Series details" or "Sell online" or "Print doujin" or "Showcase")SelectSeriesForWorkbench(detail);
        OpenManagementWorkbench();
        PageEntrance(_side);
        // On a cramped screen the page needs the phone's column, so the phone folds to its icon.
        if(_phoneOpen&&PhoneCrampsPage())ClosePhone();
        if(_presentation.Tips&&_presentation.Tutorials.Add(page)&&TutorialText(page) is {} tip)Notify("Helper-Chan: "+tip);
    }
    private void OpenManagementWorkbench()
    {
        if(WorkspaceTabs.ContainsKey(_page))ShowWorkspace(_page);
    }
    private void ApplyPageLayout()
    {
        _expanded=!_officeSidebar;_homeOffice.Show();_side.SizeFlagsHorizontal=_expanded?SizeFlags.ExpandFill:SizeFlags.Fill;
        ResizeGui();
    }
    private void GoBack()
    {
        if(OfficeEditing){Notify("Apply or discard your furniture changes first.");return;}
        if(_back.Count==0){ShowOffice();return;}
        _report.Hide();_side.Show();_officeSidebar=false;
        var entry=_back.Pop();_page=entry.Page;_detailId=entry.Detail;_scopeLocation=entry.Scope;_officeSidebar=entry.Sidebar;
        if(_state.ControlledStaff.Any(p=>p.Id==entry.Person)&&_selectedPersonId!=entry.Person){_selectedPersonId=entry.Person;ResetPersonInputs();}
        if(ManagedSeries.Any(s=>s.Id==entry.Series))SelectSeriesForWorkbench(entry.Series);
        if(_page=="Office"){ShowOffice();return;}
        ApplyPageLayout();BuildManagementPage();OpenManagementWorkbench();RestoreWorkspaceScroll(entry.Scroll);
        if(_page is "Series details" or "Sell online" or "Print doujin" or "Showcase")SelectSeriesForWorkbench(_detailId);
    }
    private void Workbench(int tab)
    {
        if(OfficeEditing&&tab!=5){Notify("Your furniture draft is still open.");return;}
        OpenWorkspace(WorkspaceTabs.First(p=>p.Value==tab).Key);
    }
    private void RefreshManagement()
    {
        if(!_managementReady)return;
        RefreshGuiAuthority();
        RefreshGuidance();
        RefreshCurrentProgress();
        RefreshOfficeDashboard();
        RefreshDashboardGoals();
        if(_navigation.TryGetValue("Goals",out var goalsButton))goalsButton.Text=GoalsRailText();
        RefreshDisclosure();
        RefreshCandidateComparison();
        if(_side.Visible&&_page=="Print doujin")_refreshPrintPanel?.Invoke();
        if(_side.Visible&&_page=="Conventions")_refreshConvention?.Invoke();
        if(_side.Visible&&_page=="Sell online")_refreshOnlinePanel?.Invoke();
        AchievementDelivery.Deliver(_state,_achievementSink);
        RefreshSpeedFeedback();
        RefreshCompactHeader();
        RefreshMoneyHeader();
        var locations=_state.Locations.Where(l=>l.BusinessId==_state.ControlledBusinessId&&!l.Closed).ToArray();
        if(!locations.Any(l=>l.Id==_viewLocation))_viewLocation=_state.Protagonist.Employment!.LocationId;
        SyncOptions(_viewOffice,locations.Select(l=>(l.Id,l.Name)),_viewLocation);
        // With one studio there is nothing to switch between, so the picker reads as a plain label (T1.8).
        _viewOffice.Disabled=locations.Length<=1;_viewOfficeHeading.Text=locations.Length<=1?"YOUR STUDIO":"VIEWING STUDIO";
        var viewedLocation=locations.FirstOrDefault(l=>l.Id==_viewLocation);
        _viewOfficeDistrict.Text=viewedLocation?.District??"";
        _viewOffice.TooltipText=viewedLocation is null?"Choose a studio":$"Viewing {viewedLocation.Name} · {viewedLocation.District}";
        _homeOffice.Bind(_state,_viewLocation);_homeOffice.Speed=OfficePlaybackSpeed;_homeOffice.Effect=_officeView.Effect;
        if(_lastUiEvents!=_state.Events.Count){_lastUiEvents=_state.Events.Count;if(_side.Visible){_pageRefresh.ThemeTypeVariation="PrimaryAction";_pageRefresh.TooltipText="New activity is available. Refresh when you are ready; your current controls stay in place.";}}
        foreach(var update in _pageLiveValues)update();
        if(!_inMenu&&!OfficeEditing&&!_storyOpen&&_state.Clock.Now.Date>_lastAutosave)
        {
            try{SaveCareer("Daily autosave",true);_lastAutosave=_state.Clock.Now.Date;}
            catch(Exception ex)when(ex is System.IO.IOException or System.IO.InvalidDataException or UnauthorizedAccessException){_lastAutosave=_state.Clock.Now.Date;LogTimeline("error "+TimelineRedactor.Clean(ex.Message,_state));Notify("Autosave failed: "+ex.Message);}
        }
        if(_overnightTarget is null&&!_inMenu&&!OfficeEditing&&!_helperPopup.Visible&&_popupEvents.Count>0){var id=_popupEvents.Dequeue();if(!_presentation.ReadEvents.Contains(id))ShowEvent(id);}
        // The held recap follows the notices; if the player resumed time instead, skip the summary but still end the day.
        if(_pendingRecap is {} held&&(_speed>0||_overnightTarget is not null)){_pendingRecap=null;if(_overnightTarget is null)BeginOvernight();}
        else if(_pendingRecap is {} waiting&&!_inMenu&&!_helperPopup.Visible&&_popupEvents.Count==0){_pendingRecap=null;ShowRecap(waiting);}
    }
    private void RefreshCompactHeader()
    {
        var compact=GetViewportRect().Size.Y/Math.Max(1,_presentation.UiScale)<600;
        var speed=_speed==0?"Paused":$"{_speed}×";
        _status.CustomMinimumSize=new(compact?125:150,0);
        _managementHeader.AddThemeConstantOverride("h_separation",compact?4:_presentation.CompactUi?6:8);
        _status.Text=compact?$"{_state.Clock.Now:d MMM yyyy}\n{_state.Clock.Now:HH:mm} · {speed}":$"{_state.Clock.Now:ddd d MMM yyyy}\n{_state.Clock.Now:HH:mm} · {speed}";
        _status.TooltipText=$"{_state.Clock.Now:dddd d MMMM yyyy · HH:mm} · {speed}";
        _inboxShortcut.Text=compact?$"✉ {UnreadCount()}":$"{UnreadCount()} unread";
        _inboxShortcut.TooltipText=$"Open Inbox · {UnreadCount()} unread messages";
    }
    private static string? TutorialText(string page)=>page switch
    {
        "Series"=>"Start a manga here. Open its card to see work, deadlines, publishing and its showcase.",
        "Books"=>"Choose a finished book to print. Delivery puts copies in local shops, where they sell through the day.",
        "Staff"=>"People are grouped around their series. Select someone for skills, wellbeing and work controls.",
        "Finances"=>"Available money already protects wages and reserved commitments. Personal savings are separate.",
        "Studios"=>"Look around a location without changing your management filters. Furniture changes need Apply or Discard.",
        "Industry"=>"Only current opportunities appear here. Scout rival staff before making an approach.",
        "Inbox"=>"Reading a message does not resolve its decision. Use its action to respond before the deadline.",
        _=>null
    };
}
