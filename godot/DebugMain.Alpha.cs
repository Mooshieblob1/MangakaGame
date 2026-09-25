using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private PanelContainer _guidanceCard=null!;
    private Label _guidanceTitle=null!,_guidanceText=null!;
    private OfficeAudio _audio=null!;
    private Button _showGuidance=null!,_guidanceRoutes=null!;
    private TextureRect _guidancePortrait=null!;
    private OptionButton _alphaPrinter=null!;
    private SpinBox _alphaCopies=null!;
    private byte[]? _reportScreen;
    private void CaptureReportScreen()
    {
        if(DisplayServer.GetName()=="headless")return;
        using var captured=GetViewport().GetTexture().GetImage();_reportScreen=captured.SavePngToBuffer();
    }
    private void BuildAlpha()
    {
        _audio=new OfficeAudio();AddChild(_audio);
        _guidanceCard=new PanelContainer();_shell.AddChild(_guidanceCard);_shell.MoveChild(_guidanceCard,_shell.GetChildCount()-2);
        _guidanceCard.AddThemeStyleboxOverride("panel",Surface(Hover,8));
        var row=new HBoxContainer();_guidanceCard.AddChild(row);
        _guidancePortrait=HelperPortrait(96,108);row.AddChild(_guidancePortrait);
        var copy=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};row.AddChild(copy);
        _guidanceTitle=Words(copy,"",17);_guidanceText=Words(copy,"",14);
        var actions=new VBoxContainer{SizeFlagsVertical=SizeFlags.ShrinkCenter};row.AddChild(actions);
        _showGuidance=ActionButton(actions,"Show me",ShowGuidance);_showGuidance.Name="GuidanceShowMe";
        _guidanceRoutes=ActionButton(actions,"What should I do?",()=>Navigate("Guidance"));
        ActionButton(row,"×",()=>{_presentation.Guidance.Visible=false;RefreshGuidance();});
    }
    private void RefreshGuidance()
    {
        if(_guidanceCard is null)return;
        CareerGuidance.Observe(_state,_presentation.Guidance);
        _guidanceCard.Visible=_presentation.Guidance.Visible;
        var step=CareerGuidance.Evaluate(_state,_presentation.Guidance);
        _guidanceTitle.Text="Helper-Chan · "+step.Title;_guidanceText.Text=step.Text;
        _guidanceTitle.TooltipText=step.Text+"\nOpen Help for all guidance.";
    }
    private void ShowGuidance()
    {
        if(_inMenu||_helperPopup.Visible||OfficeEditing){Notify("Finish or close the current dialog or furniture draft first.");return;}
        var step=CareerGuidance.Evaluate(_state,_presentation.Guidance);
        if(step.Project>0)SelectSeriesForWorkbench(step.Project);
        Control? focus=null;
        switch(step.Target)
        {
            case "create":Navigate("New doujin");focus=GetNodeOrNull<LineEdit>("%DoujinTitle");break;
            case "production":OpenWorkspace("Production");focus=_seriesOption;break;
            case "printing":
                OpenPrinting(step.Project);focus=_alphaCopies?.GetLineEdit();break;
            case "employment":OpenWorkspace("Career moves");focus=FindChildren("*","Button",true,false).OfType<Button>().FirstOrDefault(b=>b.Name=="GuidanceEmployment");break;
            case "staff":Navigate("Staff");break;
            case "awards":Navigate("Awards");break;
            default:Navigate("Guidance");break;
        }
        focus??=_sideContent.GetChildren().OfType<Button>().FirstOrDefault();
        if(focus is not null)
        {
            focus.GrabFocus();focus.Modulate=new Color(.75f,1,.8f);
            for(Node? parent=focus.GetParent();parent is not null;parent=parent.GetParent())
                if(parent is ScrollContainer scroll)scroll.CallDeferred(ScrollContainer.MethodName.EnsureControlVisible,focus);
            var target=focus;GetTree().CreateTimer(2).Timeout+=()=>{if(IsInstanceValid(target))target.Modulate=Colors.White;};
        }
        Notify("Helper-Chan: "+step.Text);
    }
    private void GuidancePage()
    {
        Words(_sideContent,"Choose what you would like to work toward. Every career action remains available.");
        foreach(var (route,label) in new[]{("opening","Opening guidance"),("doujin","Grow the doujin business"),("contest","Enter a contest"),("employment","Seek studio employment")})
        {var id=route;ActionButton(_sideContent,label,()=>{_presentation.Guidance.Route=id;_presentation.Guidance.Visible=true;RefreshGuidance();});}
        Words(_sideContent,"Project to follow");var projects=new OptionButton();projects.AddItem("Choose automatically",0);
        foreach(var s in _state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId&&(_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)))projects.AddItem(s.Title,s.Id);
        projects.Select(Math.Max(0,projects.GetItemIndex(_presentation.Guidance.Project)));_sideContent.AddChild(projects);
        projects.ItemSelected+=_=>{_presentation.Guidance.Project=projects.GetSelectedId();RefreshGuidance();};
        ActionButton(_sideContent,_presentation.Guidance.Visible?"Hide guidance":"Resume guidance",()=>{_presentation.Guidance.Visible=!_presentation.Guidance.Visible;RefreshGuidance();BuildManagementPage();});
        ActionButton(_sideContent,"Create a one-shot doujin",()=>Navigate("New doujin"));
        Words(_sideContent,"Observed progress: "+string.Join(", ",_presentation.Guidance.Completed.Select(Humanize)),14);
    }
    private void NewDoujinPage()
    {
        Words(_sideContent,"One-shot means one complete story: one chapter becomes one printable book, then production stops. It will not generate chapter 2. Choose an ongoing series for a continuing story. Your target date has no missed-deadline penalty.");
        var title=new LineEdit{Name="DoujinTitle",PlaceholderText="Your story's title",MaxLength=120};_sideContent.AddChild(title);title.Owner=this;title.UniqueNameInOwner=true;
        Words(_sideContent,"Genre",14);var genre=MakeGenreOption();_sideContent.AddChild(genre);
        Words(_sideContent,"Story pages · 16 recommended to begin");var pages=new SpinBox{MinValue=8,MaxValue=64,Step=4,Value=16};_sideContent.AddChild(pages);
        var estimate=Words(_sideContent,"");
        void Quote(){int printed=4*(int)Math.Ceiling((pages.Value+4)/4);estimate.Text=$"{printed} printed pages including covers. Example: 10 copy-shop copies cost ¥{GameState.PrintingCost(PrintTier.CopyShop,printed,10):N0}, delivered in one day after ordering. Available business cash: ¥{_state.AvailableBusinessCash:N0}. Printing is not ordered here.";}
        pages.ValueChanged+=_=>Quote();Quote();
        ActionButton(_sideContent,"Create one-shot doujin",()=>
        {
            _state.Apply(new CreateDoujinCommand(title.Text,genre.GetItemText(genre.Selected),(int)pages.Value));
            var s=_state.Series.Last();_presentation.Guidance.Project=s.Id;_dirty=true;ScanEvents();Navigate("Series details",s.Id);RefreshGuidance();
        });
    }
    private void AlphaSettings(Control parent)
    {
        var dark=new CheckBox{Text="Dark mode",ButtonPressed=_darkMode};parent.AddChild(dark);dark.Toggled+=SetDarkMode;
        Words(parent,"Controls: WASD or middle drag to pan · wheel to zoom · right drag to rotate. Space pauses/resumes; 1 slows down; 2 speeds up. Shortcuts stay off while typing or in dialogs.",14);
        var guidance=new CheckBox{Text="Helper-Chan's next-step card",ButtonPressed=_presentation.Guidance.Visible};parent.AddChild(guidance);guidance.Toggled+=v=>{_presentation.Guidance.Visible=v;RefreshGuidance();};
        foreach(var ambience in new[]{true,false})
        {
            Words(parent,ambience?"Office ambience · 0 mutes":"Sound effects · 0 mutes");
            var slider=new HSlider{MinValue=0,MaxValue=1,Step=.05,Value=ambience?_presentation.AmbienceVolume:_presentation.EffectsVolume};parent.AddChild(slider);
            slider.ValueChanged+=v=>{if(ambience)_presentation.AmbienceVolume=v;else _presentation.EffectsVolume=v;};
        }
        Words(parent,"Sounds stay natural at every speed. Office ambience pauses in menus and when the window is unfocused.",14);
    }
    private void AlphaPrintingPage()
    {
        var series=ManagedSeries.FirstOrDefault(s=>s.Id==_detailId);
        var books=series?.Volumes.Where(v=>v.IsDoujin&&v.BusinessId==_state.ControlledBusinessId).ToArray()??[];
        if(books.Length==0)
        {
            Words(_sideContent,"No finished doujin book is ready to print yet.");
            if(series is not null){Words(_sideContent,series.StandaloneDoujin?"Finish all five drawing stages for this one-shot, then order copies here.":"An ongoing series can print each finished chapter as a numbered issue. Five chapters also make a collected book.");ActionButton(_sideContent,"View production",()=>Navigate("Series details",series.Id));}
            ActionButton(_sideContent,"Browse all books",()=>Navigate("Books"));return;
        }
        Words(_sideContent,series!.Title,22);
        var choice=new OptionButton();foreach(var book in books)choice.AddItem($"{GameState.EditionName(book)} · {book.PrintedPages} pages",book.Id);
        choice.Select(_printingBookId>0&&choice.GetItemIndex(_printingBookId)>=0?choice.GetItemIndex(_printingBookId):books.Length-1);_sideContent.AddChild(choice);
        var status=Words(_sideContent,"",22);status.Name="DistributionStatus";
        var distribution=Words(_sideContent,"",14);var stock=Words(_sideContent,"",17);
        ActionButton(_sideContent,"Sell online · no upfront cost",()=>OpenOnline(series.Id,choice.GetSelectedId()));
        Words(_sideContent,"Order copies → wait for delivery → automatic local sales",16);
        _alphaPrinter=new OptionButton();foreach(var name in new[]{"7-Twelve copy shop · 1 day","Local printer · 4 days","Bulk printer · 7 days"})_alphaPrinter.AddItem(name);_sideContent.AddChild(_alphaPrinter);
        Words(_sideContent,"Copies to order");_alphaCopies=new SpinBox{MinValue=1,MaxValue=100,Value=10};_sideContent.AddChild(_alphaCopies);
        var quote=Words(_sideContent,"");var feedback=Words(_sideContent,"");Button? order=null;
        void Quote()
        {
            var book=books.Single(v=>v.Id==choice.GetSelectedId());_printingBookId=book.Id;
            var details=_state.DescribeDoujin(book.Id);status.Text=details.Status;distribution.Text=details.LocalSales;
            stock.Text=$"In stock: {details.Stock:N0} · Physical copies sold: {details.Sold:N0}";
            var cost=GameState.PrintingCost((PrintTier)_alphaPrinter.Selected,book.PrintedPages,(int)_alphaCopies.Value);
            var pending=_state.PrintRuns.Any(r=>r.VolumeId==book.Id&&!r.Delivered);
            var storage=_state.Locations.Any(l=>l.BusinessId==book.BusinessId&&!l.Closed&&l.Storage-_state.PrintRuns.Where(r=>r.LocationId==l.Id).Sum(r=>r.Remaining)>=_alphaCopies.Value);
            var reason=pending?"A print order is already underway.":cost>_state.AvailableBusinessCash?$"Not enough available {ProductionFundsCaption.ToLowerInvariant()}.":!storage?"Not enough storage in an open workplace.":"";
            quote.Text=$"PRINT ORDER TOTAL\n¥{cost:N0}\nPaid from: {ProductionFundsCaption}\nAvailable after commitments: ¥{_state.AvailableBusinessCash:N0}\nCover price: ¥{book.Price:N0}"+(reason.Length>0?"\n"+reason:"");
            var localNet=(long)Math.Floor(book.Price*.7);var breakEven=(long)Math.Ceiling(cost/(double)Math.Max(1,localNet));
            quote.Text+=$"\nLocal receipts per sold copy: ¥{localNet:N0}\nPrint bill recovered after {breakEven:N0} copies sell"+(breakEven>_alphaCopies.Value?" · MORE THAN THIS RUN":"")+". Before creator shares, wages and other costs; sales are not guaranteed.";
            if(order is not null){order.Disabled=reason.Length>0;order.TooltipText=reason;}
        }
        choice.ItemSelected+=_=>Quote();_alphaCopies.ValueChanged+=_=>Quote();
        _alphaPrinter.ItemSelected+=i=>{_alphaCopies.MinValue=i==0?1:i==1?50:300;_alphaCopies.MaxValue=i==0?100:i==1?1000:5000;Quote();};
        order=ActionButton(_sideContent,"Order this print run",()=>
        {
            _state.Apply(new StudioActionCommand(StudioAction.Print,choice.GetSelectedId(),Amount:(long)_alphaCopies.Value,Value:_alphaPrinter.Selected));
            feedback.Text="Order placed. Continue time for delivery.";_dirty=true;ScanEvents();Quote();
        });order.Name="GuidancePrintOrder";order.ThemeTypeVariation="PrimaryAction";
        _sideContent.MoveChild(feedback,_sideContent.GetChildCount()-1);_sideContent.MoveChild(distribution,_sideContent.GetChildCount()-1);
        _refreshPrintPanel=Quote;Quote();
        Words(_sideContent,"Sales and profit vary. A small batch limits the money tied up in stock. Status updates automatically as time passes.",14);
        var channels=Words(_sideContent,"");var refresh=_refreshPrintPanel;_refreshPrintPanel=()=>{refresh();channels.Text=_state.DescribeDoujin(choice.GetSelectedId()).Channels;};_refreshPrintPanel();
        ActionButton(_sideContent,"Browse all books",()=>Navigate("Books"));
        ActionButton(_sideContent,"View production",()=>Navigate("Series details",series.Id));
        ActionButton(_sideContent,"Send to convention",()=>Navigate("Conventions",series.Id));
        ActionButton(_sideContent,"Automatic printing and operations",()=>{SelectSeriesForWorkbench(series.Id);OpenWorkspace("Distribution settings");});
        ActionButton(_sideContent,"Digital & overseas agreements",()=>{SelectSeriesForWorkbench(series.Id);OpenWorkspace("Industry contacts");});
        ActionPageColumns("PRINT A PHYSICAL EDITION",status,stock,quote,feedback,distribution,channels);
    }
    private void ReportProblem()
    {
        // Capture the current view before replacing the menu; attachments remain opt-in.
        if(_reportScreen is null)CaptureReportScreen();
        byte[]? picture=_reportScreen;
        Pause();_inMenu=true;_menu.Show();Empty(_menuContent);Words(_menuContent,"Report a problem",30);
        Words(_menuContent,"Write what happened and what you expected. This tool only saves a local file for you to review and share manually.");
        var note=new TextEdit{CustomMinimumSize=new(0,150),PlaceholderText="What were you doing? What went wrong?"};_menuContent.AddChild(note);
        var screenshot=new CheckBox{Text="Attach this screen",Disabled=picture is null};_menuContent.AddChild(screenshot);
        var save=new CheckBox{Text="Attach career (includes full history, entered text and custom artwork)"};_menuContent.AddChild(save);
        var contents=Words(_menuContent,"");
        void Preview()=>contents.Text=$"Contents: your note, build {ProblemReport.Build}, format versions, game date, difficulty/Sandbox status and record counts"+(screenshot.ButtonPressed?", screenshot":"")+(save.ButtonPressed?", portable career":"")+". No automatic upload.";
        screenshot.Toggled+=_=>Preview();save.Toggled+=_=>Preview();Preview();
        ActionButton(_menuContent,"Export local report",()=>
        {
            var bytes=ProblemReport.Create(note.Text,_state,screenshot.ButtonPressed?picture:null,
                save.ButtonPressed?_careers.ExportSnapshot(_careerId,_state,_presentation):null);
            ChooseFile("Save problem report",FileDialog.FileModeEnum.SaveFile,["*.zip ; Problem report"],path=>
            {ProblemReport.Write(path,bytes);contents.Text="Report saved to "+path+". Review it before sharing.";});
        });
        ActionButton(_menuContent,"Cancel",ShowMenu);
    }
}
