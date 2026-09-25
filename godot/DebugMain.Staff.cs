using System;

using System.Linq;

using Godot;

using MangakaSim;



namespace MangakaGame;



public partial class DebugMain

{

    private OptionButton _personOption = null!;

    private OptionButton _candidateOption = null!;

    private OptionButton _recruitSpecialty = null!;

    private OptionButton _teamOption = null!;

    private OptionButton _assignmentOption = null!;

    private OptionButton _ownershipOption = null!;

    private SpinBox _salaryOffer = null!;

    private SpinBox _contribution = null!;

    private Label _staffSummary = null!;

    private Label _candidateSummary = null!;

    private Label _staffFeedback = null!;

    private Tree _staffTable = null!;

    private RichTextLabel _financeText = null!;

    private int _selectedPersonId;
    private Button _recruitButton = null!;

    private Person SelectedPerson => _state.ControlledStaff.FirstOrDefault(p => p.Id == _selectedPersonId) ?? _state.Protagonist;



    private Control BuildStaffPanel()

    {

        var scroll = new ScrollContainer { Name = "Staff and studio" };

        var panel = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

        panel.AddThemeConstantOverride("separation", 10);

        scroll.AddChild(panel);

        _staffSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };

        panel.AddChild(_staffSummary);

        _staffFeedback = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };

        panel.AddChild(_staffFeedback);



        var newGame = new HFlowContainer();

        newGame.AddChild(new Label { Text = "New game's title ownership" });

        _ownershipOption = new OptionButton();

        _ownershipOption.AddItem("Studio retention (other leads)", (int)OwnershipMode.StudioRetention);

        _ownershipOption.AddItem("Creator retention (all leads)", (int)OwnershipMode.CreatorRetention);

        newGame.AddChild(_ownershipOption);

        var restart = new Button { Text = "New game…" };

        var dialog = new ConfirmationDialog { Title = "Start a new game?", DialogText = "Unsaved progress will be replaced. Existing save files are kept.\nThe protagonist keeps their future lead titles in either ownership mode." };

        AddChild(dialog);

        dialog.Confirmed += () =>

        {

            _state = GameState.NewGame(Random.Shared.Next(), (OwnershipMode)_ownershipOption.GetSelectedId());

            _selectedPersonId = _state.ProtagonistPersonId;

            _scanIndex = _state.Events.Count;

            _log.Clear(); _recapDialog.Hide(); Pause(); ResetPersonInputs(); _dirty = true;

        };

        restart.Pressed += () => dialog.PopupCentered();

        newGame.AddChild(restart); panel.AddChild(newGame);



        var money = new HFlowContainer();

        money.AddChild(new Label { Text = "Personal contribution (yen)" });

        _contribution = new SpinBox { MinValue = 0, MaxValue = 1000000000, Step = 1000, Value = 10000 };

        money.AddChild(_contribution);

        StaffButton(money, "Contribute savings", () => new ContributeFundsCommand((long)_contribution.Value));

        panel.AddChild(money);



        var recruiting = new HFlowContainer();

        _recruitSpecialty = new OptionButton();

        _recruitSpecialty.AddItem("General search", 0);

        foreach (var stage in StageOrder.All) _recruitSpecialty.AddItem(stage.ToString(), (int)stage + 1);

        recruiting.AddChild(_recruitSpecialty);

        _recruitButton = StaffButton(recruiting, "Recruit — ¥20,000 / 7 days", () => new RecruitStaffCommand(

            _recruitSpecialty.GetSelectedId() == 0 ? null : (Stage)(_recruitSpecialty.GetSelectedId() - 1)));

        panel.AddChild(recruiting);



        var hiring = new HFlowContainer();

        _candidateOption = new OptionButton { CustomMinimumSize = new Vector2(240, 0) };

        _candidateOption.ItemSelected += _ => { SetCandidateOffer(); _dirty = true; };

        hiring.AddChild(_candidateOption);

        _salaryOffer = new SpinBox { MinValue = StudioRules.MinimumMonthlySalary, MaxValue = 1000000, Step = 1000, Value = StudioRules.MinimumMonthlySalary };

        hiring.AddChild(new Label { Text = "Monthly salary ¥" }); hiring.AddChild(_salaryOffer);

        StaffButton(hiring, "Hire at selected workplace", () => new HireStaffCommand(_candidateOption.GetSelectedId(),

            _workplaceChoice.GetSelectedId(), (long)_salaryOffer.Value));

        panel.AddChild(hiring);

        _candidateSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };

        panel.AddChild(_candidateSummary);



        _staffTable = Table("Person", "Skills N / P / I / B / T", "Monthly pay", "Start / notice", "Personal cash");

        _staffTable.CustomMinimumSize = new Vector2(0, 155);

        _staffTable.ItemSelected += () =>

        {

            if (_staffTable.GetSelected() is { } row)

            { _selectedPersonId = (int)row.GetMetadata(0); ResetPersonInputs(); _dirty = true; }

        };

        panel.AddChild(_staffTable);



        var teams = new HFlowContainer();

        _teamOption = new OptionButton { CustomMinimumSize = new Vector2(220, 0) };

        teams.AddChild(_teamOption);

        StaffButton(teams, "Assign selected staff to team", () => new AssignStaffCommand(SelectedPerson.Id, _teamOption.GetSelectedId()));

        var lead = new Button { Text = "Make selected staff lead…" };

        var leadDialog = new ConfirmationDialog { Title = "Change future title rights?", DialogText = "The selected employee becomes the permanent lead and future rights holder. Existing chapters keep their original creator attribution." };

        AddChild(leadDialog);

        int leadPerson = 0, leadSeries = 0;

        lead.Pressed += () => { leadPerson = SelectedPerson.Id; leadSeries = _teamOption.GetSelectedId(); leadDialog.PopupCentered(); };

        leadDialog.Confirmed += () => ApplyStaffCommand(new AssignStaffCommand(leadPerson, leadSeries, true));

        teams.AddChild(lead); panel.AddChild(teams);



        var assignments = new HFlowContainer();

        _assignmentOption = new OptionButton { CustomMinimumSize = new Vector2(300, 0) };

        assignments.AddChild(_assignmentOption);

        StaffButton(assignments, "Assign stage to selected staff", () => StageAssignment(SelectedPerson.Id));

        StaffButton(assignments, "Restore automatic assignment", () => StageAssignment(null));

        panel.AddChild(assignments);

        var notices = new HFlowContainer();

        StaffButton(notices, "Give selected staff 30 days' paid notice", () => new DismissStaffCommand(SelectedPerson.Id));

        panel.AddChild(notices);

        _financeText = new RichTextLabel { CustomMinimumSize = new Vector2(0, 150), SelectionEnabled = true,

            FitContent = true, ScrollActive = false };

        panel.AddChild(_financeText);

        panel.AddChild(new Label { Text = "The family home has one spare desk. Select a colleague above to manage their work; their schedule and queue are in Production.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        return scroll;

    }



    private ICommand StageAssignment(int? personId)

    {

        if (_assignmentOption.ItemCount == 0) throw new InvalidCommandException("There is no unfinished stage to assign.");

        var fields = _assignmentOption.GetItemMetadata(_assignmentOption.Selected).AsString().Split(':');

        return new AssignStageCommand(int.Parse(fields[0]), (Stage)int.Parse(fields[1]), personId);

    }



    private Button StaffButton(Container parent, string text, Func<ICommand> command)

    {

        var button = new Button { Text = text };

        button.Pressed += () =>

        {

            try
            {
                var request=command();
                if(ManagementInterface&&request is DismissStaffCommand)
                    ConfirmPlayerAction("Give paid notice",$"{SelectedPerson.Name} will receive 30 days' paid notice. The business remains responsible for those wages.",()=>ApplyStaffCommand(request));
                else ApplyStaffCommand(request);
            }

            catch (InvalidCommandException ex) { _staffFeedback.Text = ex.Message; }

        };

        parent.AddChild(button);
        return button;

    }



    private void ApplyStaffCommand(ICommand command)

    {

        try { _state.Apply(command); _staffFeedback.Text = "Done."; _dirty = true; ScanEvents(); }

        catch (InvalidCommandException ex) { _staffFeedback.Text = ex.Message; }

    }



    private void SetCandidateOffer()

    {

        if (_state.Candidates.FirstOrDefault(c => c.Id == _candidateOption.GetSelectedId()) is { } candidate)

            _salaryOffer.Value = candidate.ExpectedSalary;

    }



    private void RefreshStaff()

    {

        _recruitButton.Text=$"Recruit — ¥{_state.RecruitmentFee:N0} / 7 days";
        var home = _state.Locations.Single(l => l.Id == _state.Protagonist.Employment!.LocationId);

        _staffSummary.Text = $"{_state.ControlledBusiness.Name} • {home.Name}, {home.District} • Rent ¥{home.MonthlyRent:N0} • {_state.ControlledStaff.Count()}/{home.Seats} desks\n" +
            $"Personal ¥{_state.PersonalMoney:N0} • Business ¥{_state.Money:N0} • Reserved pay ¥{_state.ReservedWages:N0} • Arrears ¥{_state.WageArrears:N0}\n" +

            $"Ownership: {(_state.Ownership == OwnershipMode.CreatorRetention ? "Creator retention" : "Studio retention")}. Selected employee: {SelectedPerson.Name}. " +

            (_state.Recruitment is { } search ? $"Recruitment ready {search.ReadyAt:d MMM HH:mm}." : "Pool refreshes first and third Mondays.");

        var candidateId = _candidateOption.GetSelectedId();

        SyncOptions(_candidateOption,_state.Candidates.Where(c=>c.IntroductionBusinessId is null||c.IntroductionBusinessId==_state.ControlledBusinessId).Select(c=>(c.Id,$"{c.Name} · {(c.HiddenTalent?"Unknown talent":c.Profile.ToString())}")),candidateId);
        if (_candidateOption.ItemCount > 0)

        {

            var selection = _candidateOption.GetItemIndex(candidateId);

            _candidateOption.Select(Math.Max(0, selection));

            if (selection < 0) SetCandidateOffer();

        }

        var candidate = _state.Candidates.FirstOrDefault(c => c.Id == _candidateOption.GetSelectedId());

        _candidateSummary.Text = candidate is null ? "No candidates currently available." : candidate.HiddenTalent ? $"Talent unknown • Expected ¥{candidate.ExpectedSalary:N0}/month • Available for seven days." :

            $"{string.Join(" • ", StageOrder.All.Select(s => $"{s}: {candidate.Skills[s]}"))}\nExpected ¥{candidate.ExpectedSalary:N0}/month • Expires {candidate.ExpiresAt:d MMM} • Seven days' pay reserved on hiring.";

        SyncOptions(_teamOption,_state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId&&(!ManagementInterface||_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId)).Select(s=>(s.Id,s.Title)),_teamOption.GetSelectedId());
        var assignment=_assignmentOption.ItemCount==0?"":_assignmentOption.GetItemMetadata(_assignmentOption.Selected).AsString();
        var stages=_state.Series.Where(s=>s.BusinessId==_state.ControlledBusinessId&&s.Status==SeriesStatus.Active&&(!ManagementInterface||_state.Control==ControlMode.OwnerDirector||s.LeadPersonId==_state.ProtagonistPersonId))
            .SelectMany(s=>s.Chapters.SelectMany(c=>c.Stages.Where(w=>!w.IsDone).Select(w=>(Key:$"{c.Id}:{(int)w.Stage}",Text:$"{s.Title} ch.{c.Number} {w.Stage}")))).ToArray();
        if(_assignmentOption.ItemCount!=stages.Length||stages.Where((v,i)=>_assignmentOption.GetItemMetadata(i).AsString()!=v.Key||_assignmentOption.GetItemText(i)!=v.Text).Any())
        {
            _assignmentOption.Clear();foreach(var stage in stages){_assignmentOption.AddItem(stage.Text);var i=_assignmentOption.ItemCount-1;_assignmentOption.SetItemMetadata(i,stage.Key);if(stage.Key==assignment)_assignmentOption.Select(i);}
        }

        var staff=_state.ControlledStaff.ToArray();
        var staffKey=string.Join(";",staff.Select(p=>p.Id));
        if(!_staffTable.HasMeta("people")||_staffTable.GetMeta("people").AsString()!=staffKey)
        {
            _staffTable.SetMeta("people",staffKey);_staffTable.Clear();var root=_staffTable.CreateItem();
            foreach(var person in staff){var row=_staffTable.CreateItem(root);row.SetMetadata(0,person.Id);}
        }
        for(var row=_staffTable.GetRoot()?.GetFirstChild();row is not null;row=row.GetNext())
        {
            var person=staff.Single(p=>p.Id==(int)row.GetMetadata(0));var job=person.Employment!;
            var cells=new[]{person.Name+(person.IsProdigy&&!person.HiddenTalent?" ★":""),person.HiddenTalent?"Unknown":string.Join(" / ",StageOrder.All.Select(person.Skill)),
                $"¥{job.MonthlySalary:N0}",job.NoticeEndsAt is {} end?$"Notice until {end:d MMM}":$"Starts {job.StartsAt:d MMM}",$"¥{person.PersonalAccount.Balance:N0}"};
            for(var column=0;column<cells.Length;column++)row.SetText(column,cells[column]);
        }

        _financeText.Text = "Recent business entries\n" + string.Join("\n", _state.Ledger.TakeLast(8).Reverse().Select(e => $"{e.Time:d MMM} {e.Reason}: ¥{e.Amount:N0}")) +

            "\n\nOutstanding wages\n" + string.Join("\n", _state.WageObligations.Where(w => w.Remaining > 0).Select(w => $"{_state.FindPerson(w.PersonId)!.Name}: ¥{w.Remaining:N0} due {w.DueAt:d MMM}"));

    }

}

