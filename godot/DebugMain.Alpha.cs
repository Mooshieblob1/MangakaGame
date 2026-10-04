using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private PhoneFrame _phone=null!;
    private PhoneIcon _phoneIcon=null!;
    private ScrollContainer _phoneScroll=null!;
    private Label _phoneDate=null!;
    private OfficeAudio _audio=null!;
    private MusicPlayer _music=null!;
    private Button _showGuidance=null!;
    private Tween? _phoneTween;
    private bool _phoneOpen;
    private float _phoneSlide;
    private string _phoneKey="",_phoneLatest="";
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
        LoadAudioSettings();
        _audio=new OfficeAudio();AddChild(_audio);
        _music=new MusicPlayer();AddChild(_music);_music.UseFolder(System.Environment.TickCount);
        _music.Failed=id=>LogTimeline("error music "+id+" could not be loaded");_music.Changed=LogTimeline;
        // Helper-Chan texts the player. Both controls join the floating layer in BuildFloatingOffice.
        _phone=new PhoneFrame{Name="HelperPhone",Visible=false,MouseFilter=MouseFilterEnum.Stop};
        _phoneIcon=new PhoneIcon{Name="HelperPhoneIcon",Visible=false};_phoneIcon.Pressed+=()=>OpenPhone(false);
    }
    private static string LatestKey(GuidancePreferences prefs)=>prefs.Thread.LastOrDefault() is { } last
        ?$"{prefs.Thread.Count}|{last.Time.Ticks}|{string.Join("|",last.Texts)}":"";
    private void BuildPhone(bool modern,float scale)
    {
        _phone.Configure(modern,_darkMode,scale);Empty(_phone);
        _phoneIcon.Modern=modern;_phoneIcon.TextScale=scale;_phoneIcon.QueueRedraw();
        var box=new VBoxContainer();box.AddThemeConstantOverride("separation",(int)(6*scale));_phone.AddChild(box);
        Label Text(Control parent,string text,int size)
        {
            // Explicit sizes so the phone grows with the text setting even before it joins the tree.
            var label=Words(parent,text,size);label.AddThemeFontSizeOverride("font_size",(int)(size*scale));
            label.AddThemeColorOverride("font_color",_phone.TextColor);return label;
        }
        StyleBoxFlat Bubble(Color color,float radius)
        {
            var bubble=new StyleBoxFlat{BgColor=color,BorderColor=_phone.BubbleEdge,AntiAliasing=true};bubble.SetBorderWidthAll(1);
            bubble.SetCornerRadiusAll((int)(radius*scale));bubble.SetContentMarginAll(7*scale);return bubble;
        }
        Button Reply(Control parent,string title,Action action)
        {
            var button=ActionButton(parent,title,action);button.SizeFlagsHorizontal=SizeFlags.ExpandFill;
            button.AddThemeFontSizeOverride("font_size",(int)(14*scale));button.CustomMinimumSize=new(0,32*scale);
            foreach(var state in new[]{"normal","hover","pressed","focus"})
            {
                var style=Bubble(state is "hover" or "pressed"?_phone.BubbleEdge:_phone.BubbleColor,modern?16:4);
                if(state=="focus"){style.DrawCenter=false;style.SetBorderWidthAll(2);style.BorderColor=_phone.TextColor;}
                button.AddThemeStyleboxOverride(state,style);
            }
            foreach(var colour in new[]{"font_color","font_hover_color","font_pressed_color","font_focus_color"})button.AddThemeColorOverride(colour,_phone.TextColor);
            return button;
        }
        var header=new HBoxContainer();header.AddThemeConstantOverride("separation",(int)(6*scale));box.AddChild(header);
        header.AddChild(HelperPortrait(34*scale,34*scale));
        var names=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};names.AddThemeConstantOverride("separation",0);header.AddChild(names);
        Text(names,modern?"Helper-Chan":"HELPER-CHAN",15);_phoneDate=Text(names,$"{_state.Clock.Now:ddd d MMM yyyy, HH:mm}",11);
        var close=Reply(header,"×",ClosePhone);close.SizeFlagsHorizontal=SizeFlags.ShrinkEnd;close.CustomMinimumSize=new(32*scale,32*scale);close.TooltipText="Put the phone away";
        box.AddChild(new ColorRect{Color=_phone.BubbleEdge,CustomMinimumSize=new(0,1)});
        _phoneScroll=new ScrollContainer{SizeFlagsVertical=SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};box.AddChild(_phoneScroll);
        var thread=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};thread.AddThemeConstantOverride("separation",(int)(10*scale));_phoneScroll.AddChild(thread);
        var messages=_presentation.Guidance.Thread;
        foreach(var message in messages.Skip(Math.Max(0,messages.Count-50)))
        {
            var row=new HBoxContainer();row.AddThemeConstantOverride("separation",(int)(6*scale));thread.AddChild(row);
            var face=HelperPortrait(28*scale,28*scale);face.SizeFlagsVertical=SizeFlags.ShrinkBegin;row.AddChild(face);
            var texts=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};texts.AddThemeConstantOverride("separation",(int)(4*scale));row.AddChild(texts);
            Text(texts,$"{message.Time:ddd d MMM, HH:mm}",11).Modulate=new Color(1,1,1,.72f);
            foreach(var text in message.Texts)
            {
                var bubble=new PanelContainer();bubble.AddThemeStyleboxOverride("panel",Bubble(_phone.BubbleColor,modern?14:3));texts.AddChild(bubble);
                Text(bubble,text,14);
            }
        }
        if(messages.Count==0)Text(thread,"No messages yet.",13);
        if(ArrearsCoverable)Reply(box,$"Cover from savings (¥{_state.WageArrears:N0})",CoverArrears).Name="GuidanceCoverArrears";
        var replies=new HBoxContainer();replies.AddThemeConstantOverride("separation",(int)(6*scale));box.AddChild(replies);
        var current=CareerGuidance.Evaluate(_state,_presentation.Guidance);
        if(current.Id=="sell-more")
        {
            // After the first sale she offers the two ways to sell more, instead of one Show me (spec 2026-10-03).
            Reply(replies,"Sell online",()=>{ClosePhone();HighlightGuidance(RouteGuidance("online",current.Project).Focus);}).Name="GuidanceSellOnline";
            Reply(replies,"Conventions",()=>{ClosePhone();HighlightGuidance(RouteGuidance("conventions",current.Project).Focus);}).Name="GuidanceConventions";
            _showGuidance=Reply(replies,"Show me",ShowGuidance);_showGuidance.Visible=false;
        }
        else{_showGuidance=Reply(replies,"Show me",ShowGuidance);_showGuidance.Name="GuidanceShowMe";}
        Reply(replies,"Later",ClosePhone);
    }
    private bool ArrearsCoverable=>_state.WageArrears>0&&_state.PersonalMoney>=_state.WageArrears&&_state.Control==ControlMode.OwnerDirector;
    private void CoverArrears()
    {
        // Only on the player's tap: personal savings are never used for the business automatically.
        var owed=_state.WageArrears;
        try{_state.Apply(new ContributeFundsCommand(owed));}
        catch(InvalidCommandException ex){Notify(ex.Message);return;}
        CareerGuidance.Say(_presentation.Guidance,_state.Clock.Now,$"Done! I moved ¥{owed:N0} from your savings into the business. The wages are paid within the hour.");
        _dirty=true;RefreshGuidance();
    }
    private void OpenPhone(bool buzz)
    {
        if(_phone is null||_floatingUi is null)return;
        var wasOpen=_phoneOpen;_phoneOpen=true;_phone.Visible=true;_phoneIcon.Visible=false;
        if(!wasOpen)LogTimeline("phone");
        _phoneLatest=LatestKey(_presentation.Guidance);
        if(buzz)_audio.Buzz();
        ResizeFloatingOffice();
        if(!wasOpen&&!_presentation.ReducedUiMotion)
        {
            // Slide up from below the window edge. The offset keeps the slide correct if the window resizes meanwhile.
            _phoneTween?.Kill();_phoneTween=CreateTween();
            _phoneTween.TweenMethod(Callable.From<float>(v=>{_phoneSlide=v;ResizeFloatingOffice();}),1f,0f,.25).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }
        CareerGuidance.MarkRead(_presentation.Guidance);_phoneIcon.Unread=0;
        ScrollPhoneToEnd();
    }
    private void ClosePhone()
    {
        if(_phone is null)return;
        _phoneTween?.Kill();_phoneTween=null;_phoneSlide=0;_phoneOpen=false;_phone.Visible=false;
        _phoneIcon.Visible=_floatingUi is not null&&_managementReady;_phoneIcon.Unread=CareerGuidance.Unread(_presentation.Guidance);
        if(_floatingUi is not null)ResizeFloatingOffice();
    }
    private void ScrollPhoneToEnd()
    {
        var scroll=_phoneScroll;
        Callable.From(()=>{if(IsInstanceValid(scroll))scroll.ScrollVertical=(int)scroll.GetVScrollBar().MaxValue;}).CallDeferred();
    }
    private void SetGuidanceVisible(bool visible)
    {
        _presentation.Guidance.Visible=visible;
        if(!visible)ClosePhone();
        RefreshGuidance();
    }
    private void RefreshGuidance()
    {
        if(_phone is null||_floatingUi is null)return;
        var prefs=_presentation.Guidance;
        CareerGuidance.Observe(_state,prefs);
        LogNewGuidance(prefs);
        var scale=(float)_presentation.UiScale;var modern=_state.Clock.Now.Year>=2010;
        var latest=LatestKey(prefs);
        var key=$"{latest}|{modern}|{scale}|{_darkMode}|{(ArrearsCoverable?_state.WageArrears:0)}";
        if(key!=_phoneKey){_phoneKey=key;BuildPhone(modern,scale);if(_phoneOpen){ResizeFloatingOffice();ScrollPhoneToEnd();}}
        _phoneDate.Text=$"{_state.Clock.Now:ddd d MMM yyyy, HH:mm}";
        var blocked=_inMenu||_helperPopup.Visible||OfficeEditing||!_managementReady;
        var unread=CareerGuidance.Unread(prefs);
        if(unread>0&&!blocked)
        {
            if(!_phoneOpen&&prefs.Visible&&!(PageOpen&&PhoneCrampsPage()))OpenPhone(true);
            else if(_phoneOpen&&latest!=_phoneLatest){_audio.Buzz();_phoneLatest=latest;ScrollPhoneToEnd();}
        }
        if(_phoneOpen&&!blocked){CareerGuidance.MarkRead(prefs);unread=0;}
        _phoneIcon.Unread=unread;_phoneIcon.Visible=!_phoneOpen&&_managementReady;
    }
    private void ShowGuidance()
    {
        if(_inMenu||_helperPopup.Visible||OfficeEditing){Notify("Finish or close the current dialog or furniture draft first.");return;}
        var step=CareerGuidance.Evaluate(_state,_presentation.Guidance);
        ClosePhone();
        if(_state.WageArrears>0&&CareerGuidance.LastPathMessage(_presentation.Guidance)?.Step==CareerGuidance.ArrearsStep)
            step=step with{Target="arrears",Project=0};
        if(step.Project>0)SelectSeriesForWorkbench(step.Project);
        var (focus,hint)=RouteGuidance(step.Target,step.Project);
        HighlightGuidance(focus);
        CareerGuidance.Say(_presentation.Guidance,_state.Clock.Now,hint);CareerGuidance.MarkRead(_presentation.Guidance);
        RefreshGuidance();
        Notify("Helper-Chan: "+hint);
    }
    // Opens the screen for a guidance target and says what to do there; shared by "Show me" and the goals board's How? (spec 2026-10-01).
    private (Control? Focus,string Hint) RouteGuidance(string target,int project)
    {
        Control? focus=null;string hint;
        switch(target)
        {
            case "create":Navigate("New doujin");focus=GetNodeOrNull<LineEdit>("%DoujinTitle");
                hint="Here's the New doujin page! A 16-page one-shot is a good first book: one story, then stop.\nGive it a title, pick a genre, then press Create.";break;
            case "production":OpenWorkspace("Production");focus=_seriesOption;
                hint="This is Production. Pages move through each stage by themselves while time runs.";break;
            case "printing":OpenPrinting(project);focus=_alphaCopies?.GetLineEdit();
                hint="Here's printing. Ten copy-shop copies is a safe first order.";break;
            case "series":Navigate("Series details",project);focus=VisibleButton("Continue as ongoing series");
                hint="Press \"Continue as ongoing series\" here. Magazines only take ongoing series.";break;
            case "publishing":OpenWorkspace("Publishing");if(_state.FindSeries(project) is {} pitching)PreselectSuggestedMagazine(pitching);focus=_pitchButton;
                hint="Choose the magazine I named, then press Pitch. My estimate for each one is shown here.";break;
            case "employment":OpenWorkspace("Career moves");focus=FindChildren("*","Button",true,false).OfType<Button>().FirstOrDefault(b=>b.Name=="GuidanceEmployment");
                hint="Studio jobs are listed here. Pick one that suits you.";break;
            case "arrears":Navigate("Staff");
                hint="Unpaid wages show here. Letting someone go stops new wages; borrowing is on the Finances page.";break;
            case "staff":Navigate("Staff");hint="Your team and recruitment live here.";break;
            case "recruitment":OpenWorkspace("Recruitment");focus=_recruitButton;
                hint="Compare candidates here. The runway line shows what each wage does to your funds.";break;
            case "furniture":OpenDeskFix();
                hint="Add a desk and chair from the catalogue, then press Apply. Fill all desks can buy the missing chairs.";break;
            case "conventions":Navigate("Conventions",project);focus=VisibleButton("Confirm convention booking");
                hint="Choose the event I named, check the copies to bring, then press Confirm convention booking.";break;
            case "finances":Navigate("Finances");
                focus=FindChildren("*","Button",true,false).OfType<Button>().FirstOrDefault(b=>b.ToggleMode&&b.Text.EndsWith("Part-time work & personal contributions"));
                if(focus is Button{ButtonPressed:false} section)section.ButtonPressed=true;
                hint="Part-time work is in this section. You can stop the job any time if chapters slip.";break;
            case "awards":Navigate("Awards");hint="Contests and awards are listed here.";break;
            case "sold":ShowOffice();focus=_currentCopies;
                hint="Sold counts up here as the shops sell your copies, from 10:00 to 20:00.";break;
            case "online":if(_state.FindSeries(project)?.Volumes.LastOrDefault(v=>v.IsDoujin) is {} listing)OpenOnline(project,listing.Id);else Navigate("Books");
                hint="List the book online here. It costs nothing up front.";break;
            case "books":Navigate("Books");hint="Your books, print runs and online listings are here.";break;
            case "studios":Navigate("Studios");hint="Studios for rent are listed here, with their desks and monthly rent.";break;
            case "licenses":Navigate("Licenses");hint="Licence offers for your series appear here. Compare the terms before you accept.";break;
            case "business":Navigate("Finances");hint="Funding, loans and incorporation are in Finances.";break;
            default:Navigate("Guidance");hint="Here are all my suggestions. Pick a direction whenever you like.";break;
        }
        return (focus??_sideContent.GetChildren().OfType<Button>().FirstOrDefault(),hint);
    }
    private void HighlightGuidance(Control? focus)
    {
        if(focus is null)return;
        focus.GrabFocus();focus.Modulate=new Color(.75f,1,.8f);
        for(Node? parent=focus.GetParent();parent is not null;parent=parent.GetParent())
            if(parent is ScrollContainer scroll)scroll.CallDeferred(ScrollContainer.MethodName.EnsureControlVisible,focus);
        var target=focus;GetTree().CreateTimer(2).Timeout+=()=>{if(IsInstanceValid(target))target.Modulate=Colors.White;};
    }
    private Button? VisibleButton(string text)=>FindChildren("*","Button",true,false).OfType<Button>()
        .FirstOrDefault(b=>b.Text==text&&b.IsVisibleInTree()&&!b.IsQueuedForDeletion());
    private void GuidancePage()
    {
        Words(_sideContent,"Choose what you would like to work toward. Every career action remains available.");
        Words(_sideContent,"A contest or employment is a side trip: Helper-Chan still texts you about offers, deadlines and setbacks, and a contest hands back to the career path once your manuscript is entered.",14);
        foreach(var (route,label) in new[]{("career","Follow the career path"),("contest","Enter a contest"),("employment","Seek studio employment")})
        {var id=route;ActionButton(_sideContent,label,()=>{_presentation.Guidance.Route=id;if(id=="contest")RevealPage("Awards");else if(id=="employment")RevealPage("Career moves");SetGuidanceVisible(true);});}
        Words(_sideContent,"Project to follow");var projects=new OptionButton();projects.AddItem("Choose automatically",0);
        foreach(var s in _state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId&&(_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)))projects.AddItem(s.Title,s.Id);
        projects.Select(Math.Max(0,projects.GetItemIndex(_presentation.Guidance.Project)));_sideContent.AddChild(projects);
        projects.ItemSelected+=_=>{_presentation.Guidance.Project=projects.GetSelectedId();RefreshGuidance();};
        ActionButton(_sideContent,"Open Helper-Chan's messages",()=>OpenPhone(false));
        ActionButton(_sideContent,_presentation.Guidance.Visible?"Hide guidance":"Resume guidance",()=>{SetGuidanceVisible(!_presentation.Guidance.Visible);BuildManagementPage();});
        Words(_sideContent,"Hiding guidance stops the phone popping up. The phone icon stays in the corner with her messages.",14);
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
    private void AlphaSettings(Control parent,bool career=true)
    {
        DisplayControls(parent);
        var dark=new CheckBox{Text="Dark mode",ButtonPressed=_darkMode};parent.AddChild(dark);dark.Toggled+=SetDarkMode;
        Words(parent,"Controls: WASD or middle drag to pan · wheel to zoom · right drag to rotate. Space pauses/resumes; 1 slows down; 2 speeds up. Shortcuts stay off while typing or in dialogs.",14);
        if(career){var guidance=new CheckBox{Text="Helper-Chan's phone pops up for new messages",ButtonPressed=_presentation.Guidance.Visible};parent.AddChild(guidance);guidance.Toggled+=SetGuidanceVisible;}
        AudioSliders(parent);
        var unfocused=new CheckBox{Text="Play sound even while unfocused",ButtonPressed=_audioSettings.PlayWhileUnfocused};parent.AddChild(unfocused);
        unfocused.Toggled+=on=>{_audioSettings.PlayWhileUnfocused=on;SaveAudioSettings();};
        Words(parent,"These settings apply to every career on this computer. Office ambience pauses in menus.",14);
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
        ActionButton(_sideContent,"Digital & overseas agreements",()=>{SelectSeriesForWorkbench(series.Id);OpenWorkspace("Industry contacts");}).Visible=PartShown("industry");
        ActionPageColumns("PRINT A PHYSICAL EDITION",status,stock,quote,feedback,distribution,channels);
    }
    private void ReportProblem()
    {
        // Capture the current view before replacing the menu; attachments remain opt-in.
        if(_reportScreen is null)CaptureReportScreen();
        byte[]? picture=_reportScreen;
        OpenMenuPanel();Empty(_menuContent);Words(_menuContent,"Report a problem",30);
        Words(_menuContent,"Write what happened and what you expected. This tool only saves a local file for you to review and share manually.");
        var note=new TextEdit{CustomMinimumSize=new(0,150),PlaceholderText="What were you doing? What went wrong?"};_menuContent.AddChild(note);
        var screenshot=new CheckBox{Text="Attach this screen",Disabled=picture is null};_menuContent.AddChild(screenshot);
        var save=new CheckBox{Text="Attach career (includes full history, entered text and custom artwork)"};_menuContent.AddChild(save);
        if(TitleOpen){save.Disabled=true;save.TooltipText="No career is open on the title screen.";}
        var timeline=new CheckBox{Text="Attach session timeline",ButtonPressed=true};_menuContent.AddChild(timeline);
        var contents=Words(_menuContent,"");
        void Preview()=>contents.Text=$"Contents: your note, build {ProblemReport.Build}, format versions, game date, difficulty/Sandbox status and record counts"+(screenshot.ButtonPressed?", screenshot":"")+(save.ButtonPressed?", portable career":"")+(timeline.ButtonPressed?", session timeline":"")+". No automatic upload.";
        screenshot.Toggled+=_=>Preview();save.Toggled+=_=>Preview();timeline.Toggled+=_=>Preview();Preview();
        ActionButton(_menuContent,"Export local report",()=>
        {
            var bytes=ProblemReport.Create(note.Text,_state,screenshot.ButtonPressed?picture:null,
                save.ButtonPressed?_careers.ExportSnapshot(_careerId,_state,_presentation):null,
                timeline:timeline.ButtonPressed?_timeline?.Files():null);
            ChooseFile("Save problem report",FileDialog.FileModeEnum.SaveFile,["*.zip ; Problem report"],path=>
            {ProblemReport.Write(path,bytes);contents.Text="Report saved to "+path+". Review it before sharing.";});
        });
        ActionButton(_menuContent,"Cancel",ShowMenu);
    }
}
