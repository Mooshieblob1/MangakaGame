using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private Label _operationsFeedback = null!;
    private OptionButton _propertyChoice = null!, _workplaceChoice = null!, _bookChoice = null!, _loanChoice = null!, _bookingChoice = null!, _proposalChoice = null!;
    private ItemList _followerChoices=null!;
    private SpinBox _printCopies = null!;
    private OptionButton _printerChoice = null!;

    private Control BuildOperationsPanel()
    {
        var scroll=new ScrollContainer{Name="Studio management",HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};
        _studioScroll=scroll;
        var panel=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};panel.AddThemeConstantOverride("separation",16);scroll.AddChild(panel);
        BuildStudioDashboard(panel);
        var section=_studioSections["Team"];
        var actionRows=new System.Collections.Generic.Dictionary<Control,HFlowContainer>();
        HFlowContainer Row(string title,string description="")
        {
            var card=StudioCard(section,title,description);var fields=new HFlowContainer();card.AddChild(fields);
            var actions=new HFlowContainer();card.AddChild(actions);actionRows.Add(fields,actions);return fields;
        }
        Control Field(Control row,string label)
        {
            var field=new VBoxContainer{CustomMinimumSize=new(190,0)};row.AddChild(field);
            if(label.Length>0)Words(field,label,14);return field;
        }
        SpinBox Number(Control row,double min,double max,double value,double step=1,string label="")
        {
            var n=new SpinBox{MinValue=min,MaxValue=max,Value=value,Step=step,SizeFlagsHorizontal=SizeFlags.ExpandFill};Field(row,label).AddChild(n);n.ValueChanged+=_=>_dirty=true;return n;
        }
        OptionButton Choice(Control row,params string[] labels)
        {
            var o=new OptionButton{CustomMinimumSize=new(290,0),FitToLongestItem=false,ClipText=true};foreach(var l in labels)o.AddItem(l);
            row.AddChild(o);o.ItemSelected+=_=>_dirty=true;return o;
        }
        Button Action(Control row,string label,Func<ICommand> command)
        {
            var b=new Button{Text=label,MouseDefaultCursorShape=CursorShape.PointingHand};actionRows[row].AddChild(b);b.Pressed+=()=>
            {
                var request=command();
                void Commit(){try{_state.Apply(request);_operationsFeedback.Text=label+": done.";_dirty=true;ScanEvents();}catch(InvalidCommandException ex){_operationsFeedback.Text=ex.Message;}_operationsFeedback.Show();if(_managementReady)Notify(_operationsFeedback.Text);}
                if(ManagementInterface&&request is StudioActionCommand c&&c.Action is StudioAction.BorrowPersonal or StudioAction.BorrowCard or StudioAction.BorrowBusiness or StudioAction.DismissBuyout or StudioAction.Incorporate or StudioAction.Lease or StudioAction.CloseLocation or StudioAction.ReleaseRights)
                    ConfirmPlayerAction(label,OperationConfirmation(c),Commit);
                else Commit();
            };return b;
        }
        var workload=Row("Production limits","Controls how much work the selected series queues ahead.");
        var pipe=Number(workload,1,3,2,label:"Chapters in progress"); var buffer=Number(workload,0,4,2,label:"Finished chapter buffer"); var masters=Number(workload,1,3,1,label:"Unprinted masters");
        Action(workload,"Set title limits",()=>new StudioActionCommand(StudioAction.SetPipeline,_teamOption.GetSelectedId(),(int)buffer.Value,(long)masters.Value,(int)pipe.Value));
        var wellbeing=Row("Employee overtime","Set the maximum overtime for the employee selected above."); var day=Number(wellbeing,0,2,2,label:"Hours per day"); var week=Number(wellbeing,0,12,6,label:"Hours per week");
        Action(wellbeing,"Set employee limits",()=>new StudioActionCommand(StudioAction.SetWellbeing,SelectedPerson.Id,(int)week.Value,Value:(int)day.Value));
        var moon=Row("Personal projects","Choose a studio-wide policy or override it for the selected employee."); var policy=Choice(moon,"Allowed","Limited","Prohibited");
        Action(moon,"Studio policy",()=>new StudioActionCommand(StudioAction.SetMoonlighting,Value:policy.Selected));
        Action(moon,"Employee override",()=>new StudioActionCommand(StudioAction.SetMoonlighting,SelectedPerson.Id,Value:policy.Selected));
        Action(moon,"Use studio policy",()=>new StudioActionCommand(StudioAction.SetMoonlighting,SelectedPerson.Id,Enabled:true));
        section=_studioSections["Money"];
        var pay=Row("Salaries & paid work","Employee actions use the employee selected above. Founder salary is paid from business cash to personal funds."); var salary=Number(pay,0,1000000,0,1000,label:"Monthly salary · ¥");
        Action(pay,"Set salary",()=>new StudioActionCommand(StudioAction.SetFounderSalary,Amount:(long)salary.Value));
        Action(pay,"Set selected employee salary",()=>new StudioActionCommand(StudioAction.SetEmployeeSalary,SelectedPerson.Id,Amount:(long)salary.Value));
        Action(pay,"Four-hour paid commission",()=>new StudioActionCommand(StudioAction.RecoveryCommission));
        _moneyControls.Add(Action(pay,"Dismiss employee with 30-day buyout",()=>new StudioActionCommand(StudioAction.DismissBuyout,SelectedPerson.Id)));
        var credit=Row("Borrowing","Personal: 24% APR · incorporated business: 8% APR. These are game terms. Personal borrowing stays in your personal account."); var amount=Number(credit,1000,3000000,100000,1000,label:"Borrow amount · ¥");
        _moneyControls.Add(credit.GetParent().GetParent<Control>()); // borrowing waits for the money part (progressive disclosure)
        Action(credit,"Personal loan",()=>new StudioActionCommand(StudioAction.BorrowPersonal,Amount:(long)amount.Value));
        Action(credit,"Card cash advance",()=>new StudioActionCommand(StudioAction.BorrowCard,Amount:(long)amount.Value));
        Action(credit,"Business loan",()=>new StudioActionCommand(StudioAction.BorrowBusiness,Amount:(long)amount.Value));
        var debt=Row("Existing debt"); _loanChoice=Choice(debt); var repay=Number(debt,1,3000000,10000,1000,label:"Repayment · ¥");
        _moneyControls.Add(debt.GetParent().GetParent<Control>());
        Action(debt,"Repay",()=>new StudioActionCommand(StudioAction.RepayLoan,_loanChoice.GetSelectedId(),Amount:(long)repay.Value));
        Action(debt,"Incorporate — ¥200,000 setup",()=>new StudioActionCommand(StudioAction.Incorporate));
        section=_studioSections["Locations"];
        var properties=Row("Find your next studio","Preview a move before committing. A first additional branch needs six staff and two active series; later branches need four more staff each."); _propertyChoice=Choice(properties);
        _studioPropertyInfo=Words(properties.GetParent<Control>(),"",16);
        foreach(var p in TokyoProperties.All) _propertyChoice.AddItem($"Tier {p.Tier} • {p.District} • {p.Seats} desks • ¥{p.Rent:N0}/month",p.Id);
        var moveOffice=new Button{Text="Move main studio"};actionRows[properties].AddChild(moveOffice);moveOffice.Pressed+=()=>
        {try{if(ManagementInterface)OpenWorkspace("Furniture");else _mainTabs.CurrentTab=5;BeginOfficeEditor(_propertyChoice.GetSelectedId());}catch(InvalidCommandException ex){_operationsFeedback.Text=ex.Message;_operationsFeedback.Show();if(_managementReady)Notify(_operationsFeedback.Text);}};
        Action(properties,"Open additional studio",()=>new StudioActionCommand(StudioAction.Lease,_propertyChoice.GetSelectedId()));
        var places=Row("Manage a workplace","Transfer the selected employee, improve break seating, or close an empty location."); _workplaceChoice=Choice(places);
        Action(places,"Transfer selected employee",()=>new StudioActionCommand(StudioAction.TransferStaff,_workplaceChoice.GetSelectedId(),SelectedPerson.Id));
        Action(places,"Add two break seats — ¥10,000",()=>new StudioActionCommand(StudioAction.BreakRoom,_workplaceChoice.GetSelectedId()));
        Action(places,"Close empty workplace",()=>new StudioActionCommand(StudioAction.CloseLocation,_workplaceChoice.GetSelectedId()));
        section=_studioSections["Publishing"];
        if(ManagementInterface)
        {
            var quick=StudioCard(section,"Reach your readers","Guided menus show print deliveries, online sales, event dates, and copies reserved for conventions.");var shortcuts=new HFlowContainer();quick.AddChild(shortcuts);
            ActionButton(shortcuts,"Books & online sales",()=>Navigate("Books"));
            ActionButton(shortcuts,"Plan a convention",()=>Navigate("Conventions",_teamOption.GetSelectedId())).ThemeTypeVariation="PrimaryAction";
        }
        var print=Row("Print a doujin","Choose a finished edition, a printer, and your quantity. Delivered copies go on sale locally."); _bookChoice=Choice(print); _printerChoice=Choice(print,"7-Twelve copy shop (1 day)","Local printer (4 days)","Bulk printer (7 days)");
        _printCopies=Number(print,1,5000,50,label:"Copies to print");
        _studioPrintQuote=Words(print.GetParent<Control>(),"",18);
        Action(print,"Order copies",()=>new StudioActionCommand(StudioAction.Print,_bookChoice.GetSelectedId(),Amount:(long)_printCopies.Value,Value:_printerChoice.Selected));
        var auto=Row("Automatic reprints","Keep the selected series stocked within a spending limit."); var target=Number(auto,1,5000,50,label:"Target copies"); var budget=Number(auto,0,1000000,20000,1000,label:"28-day budget · ¥");
        var tiers=Choice(auto,"All printers","Copy shop only","Local only","Bulk only");
        Action(auto,"Enable for selected title",()=>new StudioActionCommand(StudioAction.AutoPrint,_teamOption.GetSelectedId(),tiers.Selected==0?7:1<<(tiers.Selected-1),(long)budget.Value,(int)target.Value,true));
        Action(auto,"Disable",()=>new StudioActionCommand(StudioAction.AutoPrint,_teamOption.GetSelectedId(),7,(long)budget.Value,(int)target.Value));
        var conventions=Row("Quick convention booking","The selected employee attends. Local events are usually 2–5 days away; use Plan a convention to see dates and reserve copies."); var scale=Choice(conventions,"Free neighbourhood event","Regional event — ¥5,000","Summer / winter — ¥8,000");
        Action(conventions,"Book solo",()=>new StudioActionCommand(StudioAction.BookConvention,SelectedPerson.Id,Value:scale.Selected));
        Action(conventions,"Book with creator",()=>new StudioActionCommand(StudioAction.BookConvention,SelectedPerson.Id,_state.ProtagonistPersonId,Value:scale.Selected));
        var booked=Row("Bookings"); _bookingChoice=Choice(booked); Action(booked,"Cancel booking",()=>new StudioActionCommand(StudioAction.CancelConvention,_bookingChoice.GetSelectedId()));
        var marketing=Row("Promotion for selected title");
        Action(marketing,"Use spare employee hours",()=>new StudioActionCommand(StudioAction.Promote,_teamOption.GetSelectedId(),SelectedPerson.Id));
        Action(marketing,"Prioritise promotion",()=>new StudioActionCommand(StudioAction.Promote,_teamOption.GetSelectedId(),SelectedPerson.Id,Enabled:true));
        Action(marketing,"Stop promotion",()=>new StudioActionCommand(StudioAction.Promote,_teamOption.GetSelectedId()));
        Action(marketing,"14-day campaign — up to ¥5,000",()=>new StudioActionCommand(StudioAction.Campaign,_teamOption.GetSelectedId()));
        var proposals=Row("Staff story proposals"); _proposalChoice=Choice(proposals);
        Action(proposals,"Approve new title",()=>new StudioActionCommand(StudioAction.AcceptProposal,_proposalChoice.GetSelectedId()));
        Action(proposals,"Let title follow its lead",()=>new StudioActionCommand(StudioAction.ReleaseRights,_teamOption.GetSelectedId(),Enabled:true));
        section=_studioSections["Career"];
        var followers=StudioCard(section,"Who will come with you?","Select colleagues to invite; Ctrl-click to select several. They may decline, and the destination must have space and accept them.");
        _followerChoices=new ItemList{SelectMode=ItemList.SelectModeEnum.Multi,CustomMinimumSize=new Vector2(0,130)};followers.AddChild(_followerChoices);
        var career=Row("Your next chapter","The camera follows your mangaka. Review what moves with you before confirming; the change happens at midnight."); var contribution=Number(career,0,10000000,0,10000,label:"Personal startup contribution · ¥");
        Words(section,"Founding a studio uses the property selected under Locations. Your old business keeps its money, debts, stock, and released books.",14);
        ActionButton(section,"Choose a startup property",()=>StudioSection("Locations"));
        var employer=Choice(career,"Entry employer","Established employer (30 reputation)","Leading employer (60 reputation)");
        void Career(string label,Func<ICommand> command)
        {
            var button=new Button{Text=label}; actionRows[career].AddChild(button);
            if(label=="Join employer")button.Name="GuidanceEmployment";
            var confirm=new ConfirmationDialog{Title="Change career?",DialogText="Your perspective follows the creator. The old business keeps its money, debts, stock and released books. Colleagues may choose to follow if there is space and pay. The move happens at midnight."}; AddChild(confirm);
            confirm.Confirmed+=()=>{try{_state.Apply(command());_operationsFeedback.Text="Move arranged for midnight.";_operationsFeedback.Show();if(_managementReady)Notify(_operationsFeedback.Text);_dirty=true;}catch(InvalidCommandException ex){_operationsFeedback.Text=ex.Message;_operationsFeedback.Show();if(_managementReady)Notify(_operationsFeedback.Text);}};
            button.Pressed+=()=>
            {
                try
                {
                    Pause(); var move=(StudioActionCommand)command();
                    _state.Apply(new StudioActionCommand(StudioAction.QuoteCareer,(int)move.Action,move.Target,move.Amount,move.Value,Followers:move.Followers));
                    confirm.DialogText=_state.CareerPreview();confirm.PopupCentered(new Vector2I(740,360));_dirty=true;
                }
                catch(InvalidCommandException ex){_operationsFeedback.Text=ex.Message;_operationsFeedback.Show();if(_managementReady)Notify(_operationsFeedback.Text);}
            };
        }
        System.Collections.Generic.List<int> Invited()=>_followerChoices.GetSelectedItems().Select(i=>(int)_followerChoices.GetItemMetadata(i)).ToList();
        Career("Return to parents' home",()=>new StudioActionCommand(StudioAction.CareerHome,Amount:(long)contribution.Value,Followers:Invited()));
        Career("Found studio at selected property",()=>new StudioActionCommand(StudioAction.CareerStudio,_propertyChoice.GetSelectedId(),Amount:(long)contribution.Value,Followers:Invited()));
        Career("Join employer",()=>new StudioActionCommand(StudioAction.CareerEmployer,Value:employer.Selected+1,Followers:Invited()));
        foreach(var button in panel.FindChildren("*","Button",true,false).OfType<Button>())
        {
            if(button.Text is "Order copies")StudioRequirement(button,()=>_bookChoice.GetSelectedId()>0,"Finish a one-shot or chapter issue to unlock printing.");
            if(button.Text is "Repay")StudioRequirement(button,()=>_loanChoice.GetSelectedId()>0,"There is no outstanding loan to repay.");
            if(button.Text is "Cancel booking")StudioRequirement(button,()=>_bookingChoice.GetSelectedId()>0,"There are no upcoming bookings.");
            if(button.Text is "Approve new title")StudioRequirement(button,()=>_proposalChoice.GetSelectedId()>0,"No employee has proposed a new title yet.");
            if(button.Text is "Set title limits" or "Enable for selected title" or "Disable" or "Use spare employee hours" or "Prioritise promotion" or "Stop promotion" or "14-day campaign — up to ¥5,000" or "Let title follow its lead")
                StudioRequirement(button,()=>_teamOption.GetSelectedId()>0,"Create or select a series first.");
        }
        return scroll;
    }
    private void RefreshOperations()
    {
        void Fill(OptionButton choice,System.Collections.Generic.IEnumerable<(int Id,string Text)> values)
        {var rows=values.ToArray();choice.Disabled=rows.Length==0;SyncOptions(choice,rows.Length==0?new[]{(0,"None available")}:rows,choice.GetSelectedId());}
        Fill(_workplaceChoice,_state.Locations.Where(l=>l.BusinessId==_state.ControlledBusinessId&&!l.Closed).Select(l=>(l.Id,$"{l.Name} ({l.District})")));
        Fill(_bookChoice,_state.Series.SelectMany(s=>s.Volumes.Where(v=>v.IsDoujin&&v.BusinessId==_state.ControlledBusinessId).Select(v=>(v.Id,$"{s.Title} · {GameState.EditionName(v)} • {v.PrintedPages}p • stock {_state.Stock(v.Id)} • ¥{v.Price}"))));
        Fill(_loanChoice,_state.Loans.Where(l=>l.Principal>0&&(l.BusinessId==_state.ControlledBusinessId||l.BusinessId is null&&l.PersonId==_state.ProtagonistPersonId)).Select(l=>(l.Id,$"{(l.BusinessId is null?"Personal":"Business")} ¥{l.Principal:N0} + ¥{l.Interest:N0} interest")));
        Fill(_bookingChoice,_state.Bookings.Where(b=>b.BusinessId==_state.ControlledBusinessId&&!b.Settled&&!b.Cancelled).Select(b=>(b.Id,$"{b.Date:d MMM} {b.District}")));
        Fill(_proposalChoice,_state.StaffProposals.Where(p=>p.BusinessId==_state.ControlledBusinessId).Select(p=>(p.Id,p.Title)));
        var colleagues=_state.ControlledStaff.Where(person=>person.Id!=_state.ProtagonistPersonId).ToArray();
        if(_followerChoices.ItemCount!=colleagues.Length || colleagues.Where((person,i)=>i>=_followerChoices.ItemCount||(int)_followerChoices.GetItemMetadata(i)!=person.Id).Any())
        {
            _followerChoices.Clear();foreach(var colleague in colleagues){_followerChoices.AddItem(colleague.Name);_followerChoices.SetItemMetadata(_followerChoices.ItemCount-1,colleague.Id);}
        }
        RefreshStudioDashboard();
    }
    private string OperationConfirmation(StudioActionCommand command)=>command.Action switch
    {
        StudioAction.BorrowPersonal or StudioAction.BorrowCard=>$"Borrow ¥{command.Amount:N0} into your personal account.\n24% APR under the game's loan terms. Personal debt follows the mangaka.",
        StudioAction.BorrowBusiness=>$"Borrow ¥{command.Amount:N0} into the incorporated business account.\n8% APR under the game's loan terms. This debt belongs to the business.",
        StudioAction.DismissBuyout=>$"Dismiss {_state.FindPerson(command.Target)?.Name}.\nThe business pays a 30-day wage buyout. Their work assignments will end.",
        StudioAction.Incorporate=>"Incorporate this business for ¥200,000 from business funds. Future business borrowing uses the incorporated business account.",
        StudioAction.Lease=>TokyoProperties.All.FirstOrDefault(p=>p.Id==command.Target) is {} property?$"Open an additional studio in {property.District}.\n¥{property.Rent*3:N0} deposit and first rent from business funds.\nOngoing rent: ¥{property.Rent:N0}/month. Furnish it before assigning staff.":"This property is no longer available.",
        StudioAction.CloseLocation=>$"Close {_state.Locations.FirstOrDefault(l=>l.Id==command.Target)?.Name}.\nThe workplace must be empty. Its capacity will no longer be available.",
        StudioAction.ReleaseRights=>$"Release the studio's rights to {_state.FindSeries(command.Target)?.Title}. Review the selected title before confirming.",
        _=>"Confirm this business change.",
    };
}
