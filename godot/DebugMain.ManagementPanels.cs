using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private int _chartDays=90;
    private bool _personalAccount;
    private IEnumerable<Series> ManagedSeries => _state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId&&
        (_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)&&(_scopeLocation==0||s.LocationId==_scopeLocation));
    private IEnumerable<Person> ManagedPeople => _state.ControlledStaff.Where(p=>(_scopeLocation==0||p.Employment!.LocationId==_scopeLocation)&&
        (_state.Control==ControlMode.OwnerDirector||p.Employment!.LocationId==_state.Protagonist.Employment!.LocationId));
    private VBoxContainer Card(string title,string subtitle)
    {
        return StudioCard(_sideContent,title,subtitle);
    }
    private void ScopeFilter()
    {
        QuietWords(_sideContent,"STUDIO FILTER",12);
        var filter=new OptionButton();filter.AddItem(_state.Control==ControlMode.OwnerDirector?"All studios · whole business":"Authorized team",0);
        foreach(var location in _state.Locations.Where(l=>l.BusinessId==_state.ControlledBusinessId&&!l.Closed))filter.AddItem(location.Name+" · "+location.District,location.Id);
        filter.Select(Math.Max(0,filter.GetItemIndex(_scopeLocation)));_sideContent.AddChild(filter);
        filter.ItemSelected+=_=>{_scopeLocation=filter.GetSelectedId();BuildManagementPage();};
    }
    private void BuildManagementPage()
    {
        if(!_managementReady)return;
        _refreshPrintPanel=null;_refreshConvention=null;_refreshOnlinePanel=null;_pageLiveValues.Clear();RefreshNavigation();Empty(_sideContent);_sideTitle.Text=_page;_pageRefresh.ThemeTypeVariation="Button";_pageRefresh.TooltipText="Refresh this overview from the current simulation state.";ResizeGui();
        switch(_page)
        {
            case "Guidance":GuidancePage();break;
            case "Goals":BuildGoals();break;
            case "New doujin":NewDoujinPage();break;
            case "New series":NewOngoingPage();break;
            case "Conventions":ConventionPage();break;
            case "Print doujin":AlphaPrintingPage();break;
            case "Sell online":DoujinOnlinePage();break;
            case "Inbox":BuildInbox();break;
            case "Books":BuildBooks();break;
            case "Awards":BuildAwards();break;
            case "Licenses":BuildLicenses();break;
            case "Legacy":BuildLegacy();break;
            case "Series":BuildSeriesOverview();break;
            case "Series details":BuildSeriesDetails();break;
            case "Staff":BuildStaffOverview();break;
            case "Person":
                var employee=ManagedPeople.FirstOrDefault(p=>p.Id==_detailId);
                if(employee is null){Words(_sideContent,"This person is no longer in your management scope.");break;}
                _selectedPersonId=employee.Id;ResetPersonInputs();
                BuildEmployeeDetails(employee);break;
            case "Finances":BuildFinance();break;
            case "Studios":BuildStudioOverview();break;
            case "Industry":BuildIndustryOverview();break;
            case "Showcase":BuildShowcase();break;
            case "Help":BuildHelp();break;
        }
        _pageRefresh.Visible=_page is not ("New doujin" or "New series" or "Print doujin" or "Sell online" or "Conventions");
    }
    private void BuildSeriesDetails()
    {
        var s=ManagedSeries.FirstOrDefault(s=>s.Id==_detailId);if(s is null){Words(_sideContent,"This series is outside your current authority.");return;}
        Words(_sideContent,s.Title,28);QuietWords(_sideContent,$"{s.Genre} · {(s.StandaloneDoujin?"One-shot doujin":s.Publishing==PublishingStatus.Unpublished?"Ongoing self-published series":"Ongoing series")} · Lead: {_state.FindPerson(s.LeadPersonId)?.Name}");
        SeriesNextAction(_sideContent,s);
        PublishingStatusCard(_sideContent,s);
        var current=SidebarChapter(s);
        if(current is not null)
        {
            var work=StudioCard(_sideContent,"CURRENT WORK",$"Chapter {current.Number} · {ProductionTarget(current)}");
            ChapterProgress(work,current);
            var stages=Disclosure(work,"Stage breakdown");
            foreach(var stage in current.Stages)LiveWords(stages,()=>$"{(stage.Stage==Stage.Name?"Storyboard":stage.Stage)}   {stage.HoursDone:0.#} / {stage.HoursRequired:0.#}h"+(stage.IsDone?" ✓":""),14);
        }
        var release=StudioCard(_sideContent,"READERS & RELEASES");
        LiveWords(release,()=>$"{s.Fanbase:N0} title fans · {s.ChaptersPublished:N0} published chapters",18);
        LiveWords(release,()=>SeriesSalesText(s),14);
        var buttons=new HFlowContainer();_sideContent.AddChild(buttons);
        ActionButton(buttons,"Production & schedule",()=>{SelectSeriesForWorkbench(s.Id);OpenWorkspace("Production");});
        var pitch=ActionButton(buttons,"Magazine pitch & contract"+(PartIsNew("publishing")?"  · New":""),()=>{SelectSeriesForWorkbench(s.Id);OpenWorkspace("Publishing");});pitch.Visible=PartShown("publishing");
        ActionButton(buttons,"Print & sell",()=>OpenPrinting(s.Id)).Visible=PartShown("books");
        Words(_sideContent,s.StandaloneDoujin?"One-shot: one complete story, one book. Production ends after this chapter.":s.Publishing==PublishingStatus.Unpublished?$"Ongoing doujin · {(_state.ChaptersTowardCollection(s)%5)} / 5 finished chapters toward the next collected book. "+(s.ReleaseShortIssues?"Each finished chapter is also printable as a numbered issue.":"Enable short issues below to print individual chapters before the collection is ready."):s.Publishing is PublishingStatus.Pitching or PublishingStatus.Offered?"The pitch sample is separate from your doujin chapters. Publisher deadlines begin after you accept a contract.":"Magazine serialization follows the publisher's issue deadlines.",14);
        if(!s.StandaloneDoujin&&s.Publishing==PublishingStatus.Unpublished&&!s.ReleaseShortIssues)ActionButton(_sideContent,"Enable short numbered issues",()=>ProgressionAction(new SetDoujinIssuesCommand(s.Id)));
        foreach(var book in s.Volumes.Where(v=>v.IsDoujin&&v.BusinessId==_state.ControlledBusinessId).TakeLast(1))
        {var distribution=_state.DescribeDoujin(book.Id);Words(_sideContent,$"{GameState.EditionName(book)} · {distribution.Status}\n{distribution.Stock:N0} in stock · {distribution.Sold:N0} physical copies sold",18);}
        if(s.ReleaseShortIssues&&s.Volumes.Count(v=>v.Format==VolumeFormat.DoujinIssue&&v.ReleasedAt is null)>=s.MasterLimit)Words(_sideContent,"Print your ready issues to make room for more production. Collected editions are optional.",14);
        if(s.StandaloneDoujin)
        {
            var continuation=StudioCard(_sideContent,"CONTINUE THE STORY","Keep this title, genre and readers. Existing books stay intact; new chapters become numbered issues.");
            ActionButton(continuation,"Continue as ongoing series",()=>ContinueOneShotFromSidebar(s.Id));
        }
        var extras=Disclosure(_sideContent,"Promotion, showcase & adaptations");
        var extraActions=new HFlowContainer();extras.AddChild(extraActions);
        ActionButton(extraActions,"Send to convention",()=>Navigate("Conventions",s.Id)).Visible=PartShown("books");
        ActionButton(extraActions,"Digital & overseas",()=>{SelectSeriesForWorkbench(s.Id);OpenWorkspace("Industry contacts");}).Visible=PartShown("industry");
        ActionButton(extraActions,"Showcase",()=>Navigate("Showcase",s.Id));
        ActionButton(extraActions,"Awards & contests",()=>Navigate("Awards")).Visible=PartShown("contests");
        ActionButton(extraActions,"Adaptations & merchandise",()=>Navigate("Licenses")).Visible=PartShown("industry");
        var earlier=s.Chapters.Where(c=>c!=current).TakeLast(8).Reverse().ToArray();
        var history=Disclosure(_sideContent,"Chapter & ranking history",earlier.Length==0?"No earlier chapters yet.":"Up to eight recent chapters are shown below.");
        foreach(var chapter in earlier)
        {
            var card=StudioCard(history,$"Chapter {chapter.Number}",$"{Humanize(chapter.Status.ToString())} · {ProductionTarget(chapter)}");
            ChapterProgress(card,chapter);
        }
        var samples=_state.Career.Rankings.Where(r=>r.At>=ChartStart()).SelectMany(r=>r.Rows.Where(x=>x.SeriesId==s.Id).Select(x=>(r.At,(double)x.Rank))).ToArray();
        PeriodSelector(history);
        AddPlot("Reader ranking · lower is better",samples,true,history);QuietWords(history,$"Ranking history available from {_state.Career.AvailableFrom:d MMM yyyy}.");
    }
    private void SelectSeriesForWorkbench(int id)
    {_progressSeriesId=id;foreach(var option in new[]{_seriesOption,_publishingSeries,_channelSeries,_teamOption}){var index=option.GetItemIndex(id);if(index>=0)option.Select(index);}
        // The cadence and page controls show the chosen title's own values, not the form defaults.
        if(_state.FindSeries(id) is {} chosen){_setPagesSpin.Value=chosen.PagesPerChapter;_setCadenceOption.Select(_setCadenceOption.GetItemIndex((int)chosen.Cadence));}
        RefreshCurrentProgress();_dirty=true;}
    private DateTime ChartStart()=>_chartDays==0?GameClock.Start:_state.Clock.Now.AddDays(-_chartDays);
    private void PeriodSelector(Control? parent=null)
    {
        parent??=_sideContent;QuietWords(parent,"PERIOD",12);
        var row=new HFlowContainer();parent.AddChild(row);
        foreach(var (label,days) in new[]{("Last 30 days",30),("Last 90 days",90),("Last year",365),("All time",0)})
        {var n=days;ChoiceButton(row,label,_chartDays==n,()=>{_chartDays=n;BuildManagementPage();});}
    }
    private void AddPlot(string title,(DateTime At,double Value)[] points,bool rank=false,Control? parent=null)
    {
        parent??=_sideContent;
        Words(parent,title,18);if(points.Length==0){QuietWords(parent,"No recorded observations in this period.");return;}
        parent.AddChild(new ReportPlot(points,rank){DarkMode=_darkMode,CustomMinimumSize=new(0,170),SizeFlagsHorizontal=SizeFlags.ExpandFill});
        QuietWords(parent,$"{points[0].At:d MMM yyyy} – {points[^1].At:d MMM yyyy} · current period may be incomplete",13);
        ActionButton(parent,"Read figures · "+title,()=>ShowFigures(title,points));
    }
    private void ShowFigures(string title,(DateTime At,double Value)[] points)
    {
        var dialog=new AcceptDialog{Title=title,Size=new(640,540)};AddChild(dialog);
        var scroll=new ScrollContainer{CustomMinimumSize=new(590,450)};dialog.AddChild(scroll);
        var content=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(content);
        var page=0;void Fill(){Empty(content);Words(content,title,20);foreach(var p in points.Reverse().Skip(page*100).Take(100))Words(content,$"{p.At:d MMM yyyy · HH:mm}     {p.Value:N0}");var row=new HBoxContainer();content.AddChild(row);if(page>0)ActionButton(row,"Newer",()=>{page--;Fill();});if((page+1)*100<points.Length)ActionButton(row,"Older",()=>{page++;Fill();});}Fill();
        dialog.Confirmed+=()=>dialog.QueueFree();dialog.Canceled+=()=>dialog.QueueFree();dialog.PopupCentered();
    }
    private void BuildFinance()
    {
        var row=new HFlowContainer();_sideContent.AddChild(row);
        ChoiceButton(row,"Personal savings",_personalAccount,()=>{_personalAccount=true;BuildManagementPage();});
        ChoiceButton(row,ProductionFundsCaption,!_personalAccount,()=>{_personalAccount=false;BuildManagementPage();});
        QuietWords(_sideContent,_personalAccount?"Your personal savings. Outside work and personal borrowing arrive here.":
            _state.Control==ControlMode.EmployedLead?"Your employer's money. Spending is limited by your role.":
            "Production money for printing, equipment and staff. Available money excludes reserved commitments.");
        var account=_personalAccount?_state.Protagonist.PersonalAccount:_state.ControlledBusiness.Account;
        var metrics=new HFlowContainer();_sideContent.AddChild(metrics);
        Metric(metrics,"ACCOUNT BALANCE",()=>$"¥{account.Balance:N0}");
        if(!_personalAccount)
        {
            Metric(metrics,"AVAILABLE TO SPEND",()=>$"¥{_state.AvailableBusinessCash:N0}","After reserved commitments");
            Metric(metrics,"RESERVED WAGES",()=>$"¥{_state.ReservedWages:N0}");
            Metric(metrics,"UNPAID BILLS",()=>$"¥{_state.Bills.Where(b=>b.BusinessId==_state.ControlledBusinessId).Sum(b=>b.Remaining):N0}");
        }
        var fundingButton=ActionButton(_sideContent,"Funding, loans & incorporation"+(PartIsNew("money")?"  · New":""),()=>OpenWorkspace("Business actions"));fundingButton.Visible=PartShown("money");PeriodSelector();
        var entries=account.Entries.Where(e=>e.Time>=ChartStart()).ToArray();
        var balance=account.OpeningBalance;var balances=new List<(DateTime,double)>();
        foreach(var group in account.Entries.GroupBy(e=>e.Time.Date).OrderBy(g=>g.Key)){balance+=group.Sum(e=>e.Amount);if(group.Key>=ChartStart().Date)balances.Add((group.Key,balance));}
        var income=entries.Where(e=>e.Amount>0&&(e.Kind is AccountEntryKind.Publishing or AccountEntryKind.Salary or AccountEntryKind.PersonalIncome or AccountEntryKind.AwardPrize or AccountEntryKind.LicenseIncome || e.Kind==AccountEntryKind.Transfer&&e.Reason.EndsWith("creator share",StringComparison.Ordinal))).GroupBy(e=>e.Time.Date).Select(g=>(g.Key,(double)g.Sum(e=>e.Amount))).ToArray();
        var spending=entries.Where(e=>e.Amount<0&&(e.Kind is AccountEntryKind.Expense or AccountEntryKind.Salary || e.Kind==AccountEntryKind.Transfer&&e.Reason.EndsWith("creator share",StringComparison.Ordinal))).GroupBy(e=>e.Time.Date).Select(g=>(g.Key,(double)-g.Sum(e=>e.Amount))).ToArray();
        var summary=new HFlowContainer();_sideContent.AddChild(summary);
        Metric(summary,"EARNED IN THIS PERIOD",()=>$"¥{income.Sum(e=>e.Item2):N0}","Excludes loans and capital transfers");
        Metric(summary,"OPERATING SPENDING",()=>$"¥{spending.Sum(e=>e.Item2):N0}","Includes creator shares paid out");
        QuietWords(_sideContent,$"Period figures updated {_state.Clock.Now:d MMM · HH:mm}. Refresh to include newer activity.",12);
        var recent=StudioCard(_sideContent,"RECENT TRANSACTIONS");
        if(entries.Length==0)QuietWords(recent,"No transactions in this period yet.");
        foreach(var entry in entries.TakeLast(8).Reverse())CashEntryRow(recent,entry);
        ActionButton(recent,"Open full account ledger",()=>ShowLedger(account.Entries));
        var charts=Disclosure(_sideContent,"Cash flow & history","Recorded figures for the selected period. Loans and capital transfers are not earned income.");
        AddPlot("Closing cash balance · yen",balances.ToArray(),parent:charts);
        AddPlot("Earned income · yen (includes creator shares)",income,parent:charts);AddPlot("Operating spending · yen (includes creator shares)",spending,parent:charts);
        QuietWords(charts,$"Sandbox subsidies in this period: ¥{entries.Where(e=>e.Kind==AccountEntryKind.SandboxSubsidy).Sum(e=>e.Amount):N0}.",13);
        if(!_personalAccount)
        {
            var sales=_state.Career.Sales.Where(s=>s.Business==_state.ControlledBusinessId&&s.At>=ChartStart()).ToArray();
            AddPlot("Physical copies",sales.GroupBy(s=>s.At.Date).Select(g=>(g.Key,(double)g.Sum(s=>s.Physical))).ToArray(),parent:charts);
            AddPlot("Domestic digital copies",sales.GroupBy(s=>s.At.Date).Select(g=>(g.Key,(double)g.Sum(s=>s.Digital))).ToArray(),parent:charts);
            AddPlot("Overseas copies",sales.GroupBy(s=>s.At.Date).Select(g=>(g.Key,(double)g.Sum(s=>s.Overseas))).ToArray(),parent:charts);
            QuietWords(charts,$"Sales samples begin {_state.Career.AvailableFrom:d MMM yyyy}; earlier unrecorded periods are unavailable.",13);
        }
        var funding=Disclosure(_sideContent,"Part-time work & personal contributions");OutsideWorkCard(funding);
    }
    private void ShowLedger(IReadOnlyList<LedgerEntry> entries)
    {
        var dialog=new AcceptDialog{Title="Account ledger",Size=new(720,580)};AddChild(dialog);
        var scroll=new ScrollContainer{CustomMinimumSize=new(650,480)};dialog.AddChild(scroll);var content=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(content);
        var page=0;void Fill(){Empty(content);Words(content,$"All transactions · page {page+1}",20);foreach(var e in entries.Reverse().Skip(page*50).Take(50))CashEntryRow(content,e,true);var row=new HBoxContainer();content.AddChild(row);if(page>0)ActionButton(row,"Newer",()=>{page--;Fill();});if((page+1)*50<entries.Count)ActionButton(row,"Older",()=>{page++;Fill();});}Fill();
        dialog.Confirmed+=()=>dialog.QueueFree();dialog.Canceled+=()=>dialog.QueueFree();dialog.PopupCentered();
    }
    private bool BelongsInInbox(GameEvent e)=>(e.SeriesId is not {} series||_state.FindSeries(series)?.BusinessId==_state.ControlledBusinessId||_state.FindSeries(series)?.LeadPersonId==_state.ProtagonistPersonId)&&
        (e.PersonId is not {} person||person==_state.ProtagonistPersonId||_state.FindPerson(person)?.Employment?.BusinessId==_state.ControlledBusinessId||e.Type==EventType.StaffDeparted);
    private void QueueImportantEvent(GameEvent e,int index)
    {
        if(!ImportantEvent(e)||!BelongsInInbox(e))return;
        if(e.Type==EventType.VolumeReleased&&_state.Series.SelectMany(s=>s.Volumes).Count(v=>v.BusinessId==_state.ControlledBusinessId&&v.ReleasedAt is not null)>1)return;
        if(_popupEvents.Any(i=>_state.Events[i].Type==e.Type&&_state.Events[i].SeriesId==e.SeriesId&&_state.Events[i].PersonId==e.PersonId))return;
        _popupEvents.Enqueue(index);
    }
    private int UnreadCount()=>_state.Events.Select((e,i)=>(e,i)).Count(x=>InboxEvent(x.e)&&BelongsInInbox(x.e)&&!_presentation.ReadEvents.Contains(x.i));
    private static bool ImportantEvent(GameEvent e)=>e.Type is EventType.AwardResult or EventType.AwardNomination or EventType.LicenseOffered or EventType.LicenseDecision or EventType.LicenseReleased or EventType.CareerMilestone or EventType.GoalChapterCompleted or EventType.IndustryDecision or EventType.StaffNotice or EventType.WageArrears or EventType.SerializationOffered or EventType.OfferAccepted or EventType.SeriesBecameIconic or EventType.DeadlineMissed or EventType.CancellationWarning or EventType.SeriesCancelled or EventType.VolumeReleased;
    private static bool InboxEvent(GameEvent e)=>ImportantEvent(e)||e.Type is EventType.IndustryNews or EventType.VolumeReleased or EventType.SeriesCancelled or EventType.DeadlineMissed or EventType.DailyRecap or EventType.RankingPublished;
    private void BuildInbox()
    {
        var filters=new HFlowContainer();_sideContent.AddChild(filters);
        foreach(var name in new[]{"Needs attention","Results & milestones","Routine","All"})
        {var filter=name;var button=ActionButton(filters,name,()=>{_inboxFilter=filter;_inboxPage=0;BuildManagementPage();});button.ToggleMode=true;button.SetPressedNoSignal(name==_inboxFilter);}
        Words(_sideContent,"Unread means unseen. Reading a notice does not accept an offer or resolve a decision.",14);
        if(_inboxFilter is "Needs attention" or "All")
        {
        // Decision cards reflect current state, independently of read/unread message history.
        foreach(var project in _state.Progression.Projects.Where(p=>p.BusinessId==_state.ControlledBusinessId&&
            (p.Phase==LicensePhase.Offer&&p.DueAt>_state.Clock.Now||p.Decision.Length>0)))
        {var card=Card("License decision · "+_state.FindSeries(project.SeriesId)!.Title,project.Outcome);ActionButton(card,"Review license decision",()=>Navigate("Licenses"));}
        foreach(var s in _state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId&&s.PendingOffer is not null&&(_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)).OrderBy(s=>s.PendingOffer!.ExpiresAt))
        {var series=s;var card=Card("Decision needed · "+s.Title,$"Serialization offer · expires {s.PendingOffer!.ExpiresAt:d MMM · HH:mm}");ActionButton(card,"Review publishing offer",()=>{SelectSeriesForWorkbench(series.Id);OpenWorkspace("Publishing");});}
        foreach(var offer in _state.World.Offers.Where(o=>o.Status==NegotiationStatus.Pending&&(o.ToBusiness==_state.ControlledBusinessId||o.FromBusiness==_state.ControlledBusinessId)).OrderBy(o=>o.EndsAt))
        {var card=Card("Pending staff negotiation",$"{_state.FindPerson(offer.PersonId)?.Name} · decision {offer.EndsAt:d MMM · HH:mm}");ActionButton(card,"Review staff decision",()=>OpenWorkspace("Industry contacts"));}
        if(_presentation.Stories&&_state.Career.PendingScene is {} scene&&(_state.Career.DeferredUntil is null||_state.Career.DeferredUntil<=_state.Clock.Now))
        {var card=Card("Helper-Chan",HelperStories.Describe(_state,scene).Title);ActionButton(card,"Read conversation",()=>ShowStory(scene));}
        }
        var markRead=ActionButton(_sideContent,"Mark displayed messages read",()=>{foreach(var (_,i) in InboxItems())_presentation.ReadEvents.Add(i);BuildManagementPage();RefreshManagement();});
        var messages=InboxItems().ToArray();
        markRead.Disabled=messages.All(m=>_presentation.ReadEvents.Contains(m.Index));
        markRead.TooltipText=markRead.Disabled?"No unread messages in the displayed page.":"Marks only the messages on this page as read; decisions stay pending.";
        if(messages.Length==0)Words(_sideContent,"No messages in this view. Pending decisions are shown above; All includes your older history.",14);
        if(_inboxFilter=="Routine")
        {
            foreach(var group in messages.GroupBy(x=>(x.Event.Time.Date,x.Event.Type)))
            {
                var card=Card(Humanize(group.Key.Type.ToString())+$" · {group.Count()} updates",$"{group.Key.Date:d MMM yyyy}");
                var details=new VBoxContainer();card.AddChild(details);details.Hide();
                ActionButton(card,"Show / hide updates",()=>details.Visible=!details.Visible);
                foreach(var (e,i) in group){var id=i;Words(details,e.Message,14);ActionButton(details,"Read · "+e.Time.ToString("HH:mm"),()=>ShowEvent(id));}
            }
        }
        else foreach(var (e,i) in messages)
        {
            var id=i;var card=Card((_presentation.ReadEvents.Contains(i)?"":"● ")+Humanize(e.Type.ToString()),$"{e.Time:d MMM yyyy · HH:mm}");
            Words(card,e.Message);ActionButton(card,"Read message",()=>ShowEvent(id));
        }
        var pages=new HFlowContainer();_sideContent.AddChild(pages);
        if(_inboxPage>0)ActionButton(pages,"Newer messages",()=>{_inboxPage--;BuildManagementPage();_sideScroll.ScrollVertical=0;});
        if(AllInboxItems().Skip((_inboxPage+1)*50).Any())ActionButton(pages,"Older messages",()=>{_inboxPage++;BuildManagementPage();_sideScroll.ScrollVertical=0;});
    }
    private static bool AttentionNotice(GameEvent e)=>e.Type is EventType.IndustryDecision or EventType.StaffNotice or EventType.WageArrears or EventType.SerializationOffered or EventType.LicenseOffered or EventType.LicenseDecision or EventType.DeadlineMissed or EventType.CancellationWarning;
    private IEnumerable<(GameEvent Event,int Index)> InboxItems()=>AllInboxItems().Skip(_inboxPage*50).Take(50);
    private IEnumerable<(GameEvent Event,int Index)> AllInboxItems()=>_state.Events.Select((e,i)=>(e,i)).Where(x=>InboxEvent(x.e)&&BelongsInInbox(x.e)&&(_inboxFilter switch
    {"Needs attention"=>AttentionNotice(x.e)&&!_presentation.ReadEvents.Contains(x.i),"Results & milestones"=>ImportantEvent(x.e)&&!AttentionNotice(x.e),"Routine"=>!ImportantEvent(x.e),_=>true}))
        .OrderByDescending(x=>x.i);
    private HBoxContainer HelperBody(string expression,string title,string text,out VBoxContainer copy)
    {
        Empty(_helperPopup);
        _helperPopup.Size=new(Math.Min(840,GetViewportRect().Size.X-40),Math.Min(520,GetViewportRect().Size.Y-40));
        _helperPopup.Position=(GetViewportRect().Size-_helperPopup.Size)/2;
        var body=new HBoxContainer();_helperPopup.AddChild(body);
        var portrait=new TextureRect{Texture=GD.Load<Texture2D>($"res://Assets/Helper/{expression}.png"),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,CustomMinimumSize=new(265,420)};body.AddChild(portrait);
        var scroll=new ScrollContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsVertical=SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};body.AddChild(scroll);
        copy=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(copy);Words(copy,"HELPER-CHAN",14);Words(copy,title,25);Words(copy,text,18);_helperPopup.Show();return body;
    }
    private void ShowEvent(int id)
    {
        if(id<0||id>=_state.Events.Count)return;var e=_state.Events[id];_presentation.ReadEvents.Add(id);
        if(ImportantEvent(e))Pause();
        HelperBody(ImportantEvent(e)&&e.Type!=EventType.GoalChapterCompleted?"concerned":"happy",Humanize(e.Type.ToString()),e.Message,out var copy);
        if(e.Type==EventType.SerializationOffered&&e.SeriesId is {} offerSeries&&_state.FindSeries(offerSeries)?.PendingOffer is null)Words(copy,"This offer has already been resolved or expired.",14);
        ActionButton(copy,"Open relevant controls",()=>{_helperPopup.Hide();if(e.Type==EventType.GoalChapterCompleted)Navigate("Goals");else if(e.Type is EventType.AwardResult or EventType.AwardNomination)Navigate("Awards");else if(e.Type is EventType.LicenseOffered or EventType.LicenseDecision or EventType.LicenseReleased)Navigate("Licenses");else if(e.Type==EventType.CareerMilestone)Navigate("Legacy");else if(e.Type==EventType.IndustryDecision)OpenWorkspace("Industry contacts");else if(e.Type==EventType.WageArrears)Navigate("Finances");else if(e.PersonId is {} person){_selectedPersonId=person;OpenWorkspace("Recruitment");}else if(e.SeriesId is {} series){Navigate("Series details",series);}else OpenWorkspace("Industry contacts");});
        ActionButton(copy,"Keep in inbox",()=>{_helperPopup.Hide();RefreshManagement();});RefreshManagement();
    }
    private void ShowStory(string id)
    {
        if(OfficeEditing||_popupEvents.Count>0){Notify("Resolve the urgent notice or finish editing first.");return;}
        var scene=HelperStories.Describe(_state,id);_storyResume=_speed;Pause();_storyOpen=true;
        VBoxContainer copy;
        if(scene.Illustration is {} illustration)copy=IllustratedStoryBody(illustration,scene.Title,scene.Text);
        else HelperBody(scene.Expression,scene.Title,scene.Text,out copy);
        void Respond(int answer,bool defer=false){_state.Apply(new StoryCommand(id,answer,defer));_dirty=true;_storyOpen=false;_helperPopup.Hide();BuildManagementPage();if(_popupEvents.Count==0&&!_recapDialog.Visible&&_pendingRecap is null&&_storyResume>0)SetSpeed(_storyResume);}
        ActionButton(copy,scene.First,()=>Respond(0));ActionButton(copy,scene.Second,()=>Respond(1));ActionButton(copy,"Read later",()=>Respond(-1,true));ActionButton(copy,"Skip this conversation",()=>Respond(-1));
    }
    private void BuildHelp()
    {
        Words(_sideContent,"Your first reader, always beside you.",22);ActionButton(_sideContent,"Objectives and direction",()=>Navigate("Guidance"));ActionButton(_sideContent,"Tutorial and notification settings",SettingsMenu);
        foreach(var page in new[]{"Inbox","Series","Staff","Finances","Studios","Industry"})Words(Card(page,"Guide"),TutorialText(page)!);
        if(_state.Career.PendingScene is {} scene)ActionButton(_sideContent,"Talk to Helper-Chan",()=>ShowStory(scene));
        Words(_sideContent,"Our conversations",23);foreach(var entry in _state.Career.Journal.AsEnumerable().Reverse().Take(40))
        {
            var archivedScene=HelperStories.Describe(_state,entry.Scene);var card=Card(archivedScene.Title,$"{entry.At:d MMM yyyy}");Words(card,entry.Text);
            if(archivedScene.Illustration is {} illustration)ActionButton(card,"Revisit illustrated conversation",()=>
            {Pause();var copy=IllustratedStoryBody(illustration,archivedScene.Title,entry.Text);ActionButton(copy,"Close",()=>_helperPopup.Hide());});
        }
    }
    private VBoxContainer IllustratedStoryBody(string illustration,string title,string text)
    {
        Empty(_helperPopup);
        var size=new Vector2(Math.Min(920,GetViewportRect().Size.X-40),Math.Min(820,GetViewportRect().Size.Y-40));
        _helperPopup.Size=size;_helperPopup.Position=(GetViewportRect().Size-size)/2;
        var layout=new VBoxContainer();_helperPopup.AddChild(layout);
        var art=new TextureRect{Texture=GD.Load<Texture2D>($"res://Assets/Helper/{illustration}.png"),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,CustomMinimumSize=new(0,size.Y*.46f),SizeFlagsVertical=SizeFlags.ExpandFill};layout.AddChild(art);
        var scroll=new ScrollContainer{CustomMinimumSize=new(0,size.Y*.40f),HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};layout.AddChild(scroll);
        var copy=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(copy);Words(copy,title,23);Words(copy,text,17);_helperPopup.Show();return copy;
    }
}

