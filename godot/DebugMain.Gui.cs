using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    // The developer scene keeps its tabs. Player routes have stable names and a shared surface.
    private static readonly Dictionary<string,int> WorkspaceTabs=new()
    {
        ["Production"]=0,["Publishing"]=1,["Recruitment"]=2,["Studio actions"]=3,
        ["Tokyo map"]=4,["Furniture"]=5,["Industry contacts"]=6,
        ["Team settings"]=3,["Business actions"]=3,["Distribution settings"]=3,
        ["Career moves"]=3,["Properties"]=3,
    };
    private VBoxContainer _rail=null!;
    private Label _workspaceTitle=null!;
    private HFlowContainer _workspaceActions=null!;
    private Button _pageRefresh=null!;
    private Button _sidebarToggle=null!;
    private bool _officeSidebar;
    private string _guiAuthority="";
    private Tween? _pageTween;
    private readonly List<Action> _pageLiveValues=new();
    private string _inboxFilter="Needs attention";
    private int _inboxPage;
    private Tree? _candidateComparison;
    private OptionButton? _recruitWorkplace;

    private void ShowOffice()
    {
        if(OfficeEditing){Notify("Apply or discard your furniture changes first.");return;}
        LogTimeline("screen Office");
        _page="Office";_detailId=0;_report.Hide();_side.Hide();_homeOffice.Show();_officeSidebar=false;_expanded=false;
        RefreshNavigation();RefreshManagement();ResizeGui();
    }
    private void RefreshGuiAuthority()
    {
        var current=$"{_state.ControlledBusinessId}:{_state.Control}:{_state.ProtagonistPersonId}";
        if(_guiAuthority.Length==0){_guiAuthority=current;return;}
        if(_guiAuthority==current)return;
        _guiAuthority=current;_pageLiveValues.Clear();_refreshPrintPanel=null;_refreshOnlinePanel=null;_refreshConvention=null;
        _back.Clear();_scopeLocation=0;_detailId=0;_printingBookId=0;_progressSeriesId=0;_inboxPage=0;
        _selectedPersonId=_state.ProtagonistPersonId;_viewLocation=_state.Protagonist.Employment!.LocationId;
        _page="Office";_officeSidebar=false;_expanded=false;_report.Hide();_side.Hide();_homeOffice.Show();ResetPersonInputs();RefreshNavigation();
        Notify("Your management view now follows the mangaka's current workplace.");
    }
    private void OpenOfficeSidebar(string page)
    {
        if(OfficeEditing){Notify("Apply or discard your furniture changes first.");return;}
        Navigate(page);_officeSidebar=true;ApplyPageLayout();BuildManagementPage();
    }
    private void OpenWorkspace(string route)
    {
        if(!ManagementInterface){_mainTabs.CurrentTab=WorkspaceTabs[route];return;}
        Navigate(route);
    }
    private int WorkspaceScroll()=>_report.Visible&&_mainTabs.GetTabControl(_mainTabs.CurrentTab) is ScrollContainer scroll?scroll.ScrollVertical:_sideScroll.ScrollVertical;
    private void RestoreWorkspaceScroll(int position)
    {
        if(_report.Visible&&_mainTabs.GetTabControl(_mainTabs.CurrentTab) is ScrollContainer scroll)scroll.SetDeferred("scroll_vertical",position);
        else _sideScroll.SetDeferred("scroll_vertical",position);
    }
    private void ShowWorkspace(string route)
    {
        _mainTabs.CurrentTab=WorkspaceTabs[route];_report.Show();_side.Hide();_homeOffice.Show();
        _workspaceTitle.Text=route switch{"Publishing"=>"Magazine publishing","Production"=>"Production & work schedules","Business actions"=>"Funding & business decisions",_=>route};Empty(_workspaceActions);
        foreach(var (label,destination) in WorkspaceLinks(route))
        {var target=destination;ActionButton(_workspaceActions,label,()=>Navigate(target));}
        if(WorkspaceTabs[route]==3)
            StudioSection(route switch{"Team settings"=>"Team","Business actions"=>"Money","Distribution settings"=>"Publishing","Career moves"=>"Career","Properties"=>"Locations",_=>"Overview"});
        _workbenchNotice.Text=route=="Publishing"?PublishingWorkbenchHint:
            GenericWorkbenchHint;
        UpdateWorkbenchNotice();
        RefreshNavigation();_dirty=true;PageEntrance(_report);
    }
    private static (string Label,string Route)[] WorkspaceLinks(string route)=>route switch
    {
        "Production"=>[("Series overview","Series"),("Team assignments","Recruitment"),("Pipeline & overtime","Team settings")],
        "Publishing"=>[("Series overview","Series"),("Books & sales","Books"),("Industry overview","Industry")],
        "Recruitment" or "Team settings"=>[("Staff overview","Staff"),("Recruit & assign","Recruitment"),("Pipeline & overtime","Team settings"),("Pay & employment","Business actions"),("Work schedules","Production")],
        "Industry contacts"=>[("Industry overview","Industry"),("Magazine rankings","Publishing"),("Awards","Awards"),("Adaptations","Licenses")],
        "Business actions"=>[("Finance overview","Finances"),("Staff overview","Staff"),("Career moves","Career moves")],
        "Distribution settings"=>[("Books & sales","Books"),("Conventions","Conventions"),("Series overview","Series")],
        _=>[("Studios overview","Studios"),("Properties & branches","Properties"),("Tokyo map","Tokyo map"),("Business finances","Business actions"),("Career moves","Career moves")],
    };
    // The standing hints give way to the form itself on short windows with large text (Publishing keeps its Books & sales link); action results stay.
    private const string GenericWorkbenchHint="Choose the named person, series or studio in this form. Costs use the account shown beside the action.";
    private const string PublishingWorkbenchHint="Pitch to magazines and manage contracts here. For self-published printing or downloads, open Books & sales.";
    private void UpdateWorkbenchNotice()=>_workbenchNotice.Visible=_workbenchNotice.Text is not (GenericWorkbenchHint or PublishingWorkbenchHint)||GetViewportRect().Size.Y/_presentation.UiScale>=560;
    private void ResizeGui()
    {
        if(_rail is null||_side is null)return;
        if(_workbenchNotice is not null)UpdateWorkbenchNotice();
        var width=GetViewportRect().Size.X;
        _rail.CustomMinimumSize=new(width<1500?120:148,0);
        _side.CustomMinimumSize=new(_officeSidebar?Math.Min(460,width*.40f):0,0);
        // The old debug viewport minimum (640 x 400) would force the shell off
        // a 720p screen once navigation, larger text and a sidebar are present.
        _homeOffice.CustomMinimumSize=new(240,180);_officeView.CustomMinimumSize=new(240,180);
        if(_pageRefresh is not null)_pageRefresh.Text=_officeSidebar?"↻":"Refresh";
        if(_sidebarToggle is not null){_sidebarToggle.Visible=_page is "Inbox" or "Series";_sidebarToggle.Text=_officeSidebar?"Full page":"Beside office";}
        foreach(var b in _navigation.Values)b.CustomMinimumSize=new(0,_presentation.CompactUi?34:44);
        if(_menu is not null)
        {
            // The pause menu is a small centred panel; its sub-pages use the full panel.
            var ui=(float)_presentation.UiScale;var height=GetViewportRect().Size.Y;
            var insetX=Math.Max(24,(width-(_menuCompact?460*ui:1040))/2);var insetY=_menuCompact?Math.Max(40,(height-600*ui)/2):40;
            _menu.OffsetLeft=insetX;_menu.OffsetRight=-insetX;_menu.OffsetTop=insetY;_menu.OffsetBottom=-insetY;
        }
        LayoutTitle();
        ResizeFloatingOffice();
    }
    private void PageEntrance(Control target)
    {
        _pageTween?.Kill();_side.Modulate=Colors.White;_report.Modulate=Colors.White;
        if(_presentation.ReducedUiMotion)return;
        target.Modulate=new Color(1,1,1,.82f);_pageTween=CreateTween();_pageTween.TweenProperty(target,"modulate:a",1f,.12);
    }
    private TextureRect HelperPortrait(float width=76,float height=84)
    {
        var texture=GD.Load<Texture2D>("res://Assets/Helper/neutral.png");
        // Crop only the displayed region. The original transparent illustration is untouched.
        var portrait=new AtlasTexture{Atlas=texture,Region=new Rect2(180,0,470,470)};
        return new TextureRect{Texture=portrait,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,CustomMinimumSize=new(width,height),MouseFilter=MouseFilterEnum.Ignore};
    }
    private static void SyncOptions(OptionButton choice,IEnumerable<(int Id,string Text)> entries,int selected)
    {
        var rows=entries.ToArray();
        if(choice.ItemCount!=rows.Length||rows.Where((row,i)=>choice.GetItemId(i)!=row.Id||choice.GetItemText(i)!=row.Text).Any())
        {choice.Clear();foreach(var row in rows)choice.AddItem(row.Text,row.Id);}
        if(rows.Length>0)choice.Select(Math.Max(0,choice.GetItemIndex(selected)));
    }
    private VBoxContainer FormSection(Control parent,string title,IEnumerable<Control> controls,bool advanced=false)
    {
        var card=StudioCard(parent,title);
        var contents=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};card.AddChild(contents);
        foreach(var control in controls.ToArray())control.Reparent(contents);
        if(advanced)
        {
            contents.Hide();var toggle=ActionButton(card,"Show options",()=>contents.Visible=!contents.Visible);card.MoveChild(toggle,1);
            contents.VisibilityChanged+=()=>toggle.Text=contents.Visible?"Hide options":"Show options";
        }
        return contents;
    }
    private void PrepareWorkspaceForms()
    {
        // Keep the original controls and handlers alive: unsent inputs survive navigation.
        var production=(Container)_mainTabs.GetChild(0).GetChild(0);
        var left=production.GetChild<Control>(0);var person=production.GetChild<Control>(1);
        var flow=new HFlowContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};production.AddChild(flow);production.MoveChild(flow,0);
        left.Reparent(flow);person.Reparent(flow);left.CustomMinimumSize=new(460,0);person.CustomMinimumSize=new(290,0);
        left.SizeFlagsStretchRatio=2;person.SizeFlagsStretchRatio=1;
        // Creation has focused routes; production starts with the selected title.
        left.GetChild<Control>(1).Hide();
        foreach(var row in left.GetChildren().OfType<HBoxContainer>().ToArray())
        {
            var wrap=new HFlowContainer();left.AddChild(wrap);left.MoveChild(wrap,row.GetIndex());
            wrap.Visible=row.Visible;foreach(var child in row.GetChildren().OfType<Control>().ToArray())child.Reparent(wrap);row.QueueFree();
        }
        var chapterScroll=_chapterGrid.GetParent<ScrollContainer>();chapterScroll.CustomMinimumSize=new(0,360);
        FormSection(flow,"PRODUCTION · selected title",[left]);
        FormSection(flow,"WORK SCHEDULE · selected employee",[person]);

        var publishing=(Control)_mainTabs.GetChild(1);_mainTabs.RemoveChild(publishing);
        var publishingScroll=new ScrollContainer{Name="Publishing",HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};
        _mainTabs.AddChild(publishingScroll);_mainTabs.MoveChild(publishingScroll,1);
        var bounded=new GuiReadingColumn{SizeFlagsHorizontal=SizeFlags.ExpandFill};publishingScroll.AddChild(bounded);bounded.AddChild(publishing);
        // The tab bar hid this page while it was an inactive tab; it is now shown by its scroll container instead.
        publishing.Show();
        var publishingChildren=publishing.GetChildren().OfType<Control>().Where(c=>c!=_marketTabs).ToArray();
        FormSection(publishing,"PUBLISHING · title and magazine",publishingChildren);
        publishing.MoveChild(publishing.GetChild(publishing.GetChildCount()-1),0);
        _marketTabs.CustomMinimumSize=new(0,430);

        var staff=(Control)_mainTabs.GetChild(2).GetChild(0);
        var staffChildren=staff.GetChildren().OfType<Control>().ToArray();
        staffChildren[2].Hide(); // New-career creation belongs to the main menu.
        var recruitment=FormSection(staff,"RECRUITMENT · production budget",staffChildren.Skip(4).Take(3));
        Words(recruitment,"Workplace for the new hire",14);
        _recruitWorkplace=new OptionButton{FitToLongestItem=false,ClipText=true};recruitment.AddChild(_recruitWorkplace);
        _recruitWorkplace.ItemSelected+=_=>{_workplaceChoice.Select(_workplaceChoice.GetItemIndex(_recruitWorkplace.GetSelectedId()));_dirty=true;};
        FormSection(staff,"TEAM · select an employee below",staffChildren.Skip(7).Take(3));
        FormSection(staff,"EMPLOYMENT · contributions and notice",new[]{staffChildren[3]}.Concat(staffChildren.Skip(10)),true);
        _candidateComparison=Table("Candidate","Known skills · N / P / I / B / T","Monthly salary","Available until");
        _candidateComparison.CustomMinimumSize=new(0,170);staff.AddChild(_candidateComparison);
        _candidateComparison.ItemSelected+=()=>{if(_candidateComparison.GetSelected() is {} row){var id=(int)row.GetMetadata(0);var index=_candidateOption.GetItemIndex(id);if(index>=0){_candidateOption.Select(index);SetCandidateOffer();_dirty=true;}}};

        var industry=(Control)_mainTabs.GetChild(6).GetChild(0);
        var children=industry.GetChildren().OfType<Control>().ToArray();
        VBoxContainer? section=null;
        foreach(var child in children)
        {
            if(child is Label label&&(label.Text.StartsWith("Rival staff")||label.Text.StartsWith("Temporary assistants")||label.Text.StartsWith("Recruitment decisions")||label.Text.StartsWith("Release channels")))
                section=StudioCard(industry,label.Text.Split('·')[0].Trim());
            if(section is not null)child.Reparent(section);
            if(child is Button button&&button.Text.StartsWith("Import previous"))child.Hide();
        }
        foreach(var choice in FindChildren("*","OptionButton",true,false).OfType<OptionButton>())
        {
            // Only choices that stretch across their row may clip; a fixed-size choice sizes to its longest item so its text never vanishes.
            var stretches=(choice.SizeFlagsHorizontal&SizeFlags.Expand)!=0;
            choice.FitToLongestItem=!stretches;choice.ClipText=stretches&&choice!=_viewOffice;
        }
    }
    private void RefreshCandidateComparison()
    {
        if(_recruitWorkplace is not null)SyncOptions(_recruitWorkplace,_state.Locations.Where(l=>!l.Closed&&l.BusinessId==_state.ControlledBusinessId).Select(l=>(l.Id,l.Name+" · "+l.District)),_workplaceChoice.GetSelectedId());
        if(_candidateComparison is null||!_candidateComparison.IsVisibleInTree())return;
        var candidates=_state.Candidates.Where(c=>c.IntroductionBusinessId is null||c.IntroductionBusinessId==_state.ControlledBusinessId).ToArray();
        var key=string.Join(";",candidates.Select(c=>$"{c.Id}:{c.HiddenTalent}:{c.ExpectedSalary}"));
        if(_candidateComparison.HasMeta("rows")&&_candidateComparison.GetMeta("rows").AsString()==key)return;
        _candidateComparison.SetMeta("rows",key);_candidateComparison.Clear();var root=_candidateComparison.CreateItem();
        foreach(var c in candidates)
        {var row=Row(_candidateComparison,root,c.Name,c.HiddenTalent?"Unknown talent":string.Join(" / ",StageOrder.All.Select(s=>c.Skills[s])),$"¥{c.ExpectedSalary:N0}",$"{c.ExpiresAt:d MMM}");row.SetMetadata(0,c.Id);}
    }
    private void ActionPageColumns(string title,params Control[] summary)
    {
        var controls=_sideContent.GetChildren().OfType<Control>().ToArray();
        var flow=new HFlowContainer();_sideContent.AddChild(flow);
        var inputs=StudioCard(flow,title);inputs.GetParent<Control>().CustomMinimumSize=new(400,0);
        var quote=StudioCard(flow,"YOUR QUOTE & STATUS");quote.GetParent<Control>().CustomMinimumSize=new(280,0);
        foreach(var control in controls)control.Reparent(summary.Contains(control)?quote:inputs);
    }
    private ConfirmationDialog ConfirmPlayerAction(string title,string detail,Action commit,string ok="Confirm",string cancel="Keep editing")
    {
        var dialog=new ConfirmationDialog{Title=title,DialogText=detail,OkButtonText=ok,CancelButtonText=cancel,Exclusive=true};AddChild(dialog);
        dialog.Confirmed+=()=>{try{commit();}finally{dialog.QueueFree();}};dialog.Canceled+=()=>dialog.QueueFree();
        dialog.PopupCentered(new Vector2I(580,260));
        return dialog;
    }
}
