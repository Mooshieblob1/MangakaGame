namespace MangakaSim;

public partial class GameState
{
    // ---------------------------------------------------------------- candidate pool

    private bool NameInUse(string name) =>
        People.Any(p => p.Name == name) || FormerPeople.Any(p => p.Name == name) || Candidates.Any(c => c.Name == name);

    private string DrawName()
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var name = StaffData.GivenNames[Rng.NextInt(StaffData.GivenNames.Count)] + " " +
                       StaffData.FamilyNames[Rng.NextInt(StaffData.FamilyNames.Count)];
            if (!NameInUse(name)) return name;
        }
        return $"Assistant {AllocateId()}";
    }

    /// <summary>Monthly: expired candidates leave, returning and scheduled ones join, the pool is topped up to four.</summary>
    internal void RefreshCandidatePool()
    {
        Candidates.RemoveAll(c => c.AvailableUntil <= Clock.Now);

        foreach (var note in Departures.Where(d => d.Quit && !d.Returned && d.LeftAt.AddMonths(HiringRules.ReturnAfterMonths) <= Clock.Now).ToList())
        {
            note.Returned = true;
            if (People.Any(p => p.Name == note.Name) || Candidates.Any(c => c.Name == note.Name)) continue;
            Candidates.Add(new Candidate
            {
                Id = AllocateId(),
                Name = note.Name,
                Skills = new Dictionary<Stage, int>(note.Skills),
                AskingSalary = PayRules.RoundToThousand(note.Salary * PayRules.ReturningSalaryMultiplier),
                AvailableUntil = Clock.Now.AddMonths(2),
                IsReturning = true,
                Note = $"left the studio in {note.LeftAt:MMMM yyyy}",
            });
        }

        foreach (var scheduled in StaffData.ScheduledCandidates)
        {
            if (scheduled.AppearsAt > Clock.Now || ScheduledCandidatesShown.Contains(scheduled.Name)) continue;
            ScheduledCandidatesShown.Add(scheduled.Name);
            var market = PayRules.MarketSalary(scheduled.Skills.Values, PriceIndexNow);
            var candidate = new Candidate
            {
                Id = AllocateId(),
                Name = scheduled.Name,
                Skills = new Dictionary<Stage, int>(scheduled.Skills),
                AskingSalary = PayRules.RoundToThousand(market * scheduled.AskingMultiplier),
                AvailableUntil = Clock.Now.AddMonths(scheduled.MonthsAvailable),
                IsScheduled = true,
                Note = scheduled.Note,
            };
            Candidates.Add(candidate);
            Emit(EventType.CandidateAppeared,
                $"{candidate.Name} is looking for a studio ({scheduled.Note}); asking {candidate.AskingSalary:N0} yen a month until {candidate.AvailableUntil:d MMM yyyy}.",
                new EventContext(Amount: candidate.AskingSalary));
        }

        while (Candidates.Count(c => !c.IsScheduled && !c.IsReturning) < HiringRules.PoolSize)
        {
            var level = HiringRules.Level(StudioTrackRecord, Rng);
            var skills = HiringRules.Skills(level, Rng);
            var market = PayRules.MarketSalary(skills.Values, PriceIndexNow);
            Candidates.Add(new Candidate
            {
                Id = AllocateId(),
                Name = DrawName(),
                Skills = skills,
                AskingSalary = PayRules.AskingSalary(market, Rng),
                AvailableUntil = Clock.Now.AddMonths(Rng.NextInt(1, 2)),
            });
        }

        Emit(EventType.CandidatePoolRefreshed,
            "Assistants looking for work: " + string.Join(", ", Candidates.Select(c => $"{c.Name} ({c.AskingSalary:N0})")) + ".");
    }

    public Candidate? FindCandidate(int id) => Candidates.FirstOrDefault(c => c.Id == id);

    // ---------------------------------------------------------------- commands

    private Person RequireAssistant(int personId)
    {
        var person = RequirePerson(personId);
        if (person.Role != PersonRole.Assistant)
            throw new InvalidCommandException($"{person.Name} runs the studio; that command only applies to assistants.");
        return person;
    }

    private void ApplyHire(HireCommand c)
    {
        var candidate = FindCandidate(c.CandidateId) ?? throw new InvalidCommandException($"No candidate with id {c.CandidateId}.");
        if (candidate.AvailableUntil <= Clock.Now)
            throw new InvalidCommandException($"{candidate.Name} is no longer looking; they leave the pool at the next refresh.");
        if (c.Salary < 0) throw new InvalidCommandException("Salary must not be negative.");
        if (!PayRules.Accepts(c.Salary, candidate.AskingSalary))
            throw new InvalidCommandException($"{candidate.Name} won't work for {c.Salary:N0} yen; the asking salary is {candidate.AskingSalary:N0}.");
        var capacity = CurrentPremises.Capacity;
        if (People.Count >= capacity)
            throw new InvalidCommandException($"No desk for {candidate.Name}: {CurrentPremises.Name} holds {capacity}.");

        var person = new Person
        {
            Id = candidate.Id,
            Name = candidate.Name,
            Role = PersonRole.Assistant,
            Skills = new Dictionary<Stage, int>(candidate.Skills),
            Salary = c.Salary,
            HiredAt = Clock.Now,
            Reputation = 5,
            Happiness = HiringRules.StartingHappiness(c.Salary, candidate.AskingSalary),
            Schedule = new Schedule { WorkStartHour = 8, WorkEndHour = 18, DaysOff = { DayOfWeek.Sunday } },
            OvertimeAllowed = true,
            AllowedStages = StageOrder.All.Where(s => s != Stage.Name).ToHashSet(),
        };
        Candidates.Remove(candidate);
        People.Add(person);
        Emit(EventType.StaffHired,
            $"{person.Name} joins the studio at {c.Salary:N0} yen a month.",
            new EventContext(PersonId: person.Id, Amount: c.Salary));
    }

    /// <summary>Removes a person from the studio, keeping their record for history and reassigning their work.</summary>
    private void RemovePerson(Person person, bool quit)
    {
        People.Remove(person);
        person.Queue.Clear();
        person.Pins.Clear();
        person.ManualOrder = null;
        person.CurrentTask = null;
        person.OnBreak = false;
        person.PromotionSeriesId = null;
        FormerPeople.Add(person);
        Departures.Add(new FormerStaffNote
        {
            PersonId = person.Id,
            Name = person.Name,
            Skills = new Dictionary<Stage, int>(person.Skills),
            Salary = person.Salary,
            LeftAt = Clock.Now,
            Quit = quit,
        });
        foreach (var work in Series.SelectMany(s => s.Chapters).SelectMany(ch => ch.Stages))
        {
            if (!work.IsDone && work.AssignedTo == person.Id) work.AssignedTo = null; // finished stages keep their history
            if (work.ManualAssignee == person.Id) work.ManualAssignee = null;
        }
        foreach (var series in Series.Where(s => s.LeadId == person.Id)) series.LeadId = Mangaka.Id;
        foreach (var colleague in People) AdjustHappiness(colleague, HappinessRules.ColleagueQuit);
    }

    private void ApplyFire(FireCommand c)
    {
        var person = RequireAssistant(c.PersonId);
        var severance = (long)person.Salary;
        if (severance > 0) AddLedger(-severance, "severance", personId: person.Id);
        RemovePerson(person, quit: false);
        Emit(EventType.StaffFired,
            $"{person.Name} is let go with {severance:N0} yen of severance.",
            new EventContext(PersonId: person.Id, Amount: -severance));
    }

    private void ApplySetSalary(SetSalaryCommand c)
    {
        var person = RequireAssistant(c.PersonId);
        if (c.Salary < 0) throw new InvalidCommandException("Salary must not be negative.");
        var old = person.Salary;
        person.Salary = c.Salary;
        if (c.Salary >= old * 1.1 && c.Salary > old) AdjustHappiness(person, HappinessRules.Raise);
        else if (c.Salary < old) AdjustHappiness(person, HappinessRules.PayCut);
        Emit(EventType.SalaryChanged,
            $"{person.Name}'s salary goes from {old:N0} to {c.Salary:N0} yen a month.",
            new EventContext(PersonId: person.Id, Amount: c.Salary));
    }

    private void ApplySetAllowedStages(SetAllowedStagesCommand c)
    {
        var person = RequirePerson(c.PersonId);
        if (c.Stages is null || c.Stages.Count == 0 || c.Stages.Any(s => !Enum.IsDefined(s)))
            throw new InvalidCommandException("Allowed stages must be a non-empty set of valid stages.");
        if (person.IsMangaka && !c.Stages.Contains(Stage.Name))
            throw new InvalidCommandException($"{person.Name} must keep writing the Name.");
        if (!c.Stages.Contains(Stage.Name) && Series.Any(s => s.LeadId == person.Id))
            throw new InvalidCommandException($"{person.Name} leads a series and must keep the Name stage, or hand the lead over first.");
        person.AllowedStages = new HashSet<Stage>(c.Stages);
        foreach (var work in Series.SelectMany(s => s.Chapters).SelectMany(ch => ch.Stages))
        {
            if (work.ManualAssignee == person.Id && !c.Stages.Contains(work.Stage)) work.ManualAssignee = null;
        }
    }

    private void ApplySetSeriesLead(SetSeriesLeadCommand c)
    {
        var series = RequireSeries(c.SeriesId);
        var person = RequirePerson(c.PersonId);
        if (!person.MayWork(Stage.Name))
            throw new InvalidCommandException($"{person.Name} is not allowed to write the Name and cannot lead '{series.Title}'.");
        series.LeadId = person.Id;
    }

    private void ApplyAssignStage(AssignStageCommand c)
    {
        var chapter = RequireChapter(c.ChapterId);
        RequireStage(c.Stage);
        var work = chapter.StageWork(c.Stage);
        if (work.IsDone) throw new InvalidCommandException($"{c.Stage} of chapter {chapter.Number} is already {work.Status}.");
        if (c.PersonId is { } id)
        {
            var person = RequirePerson(id);
            if (!person.MayWork(c.Stage))
                throw new InvalidCommandException($"{person.Name} is not allowed to work on {c.Stage}.");
            work.ManualAssignee = id;
        }
        else work.ManualAssignee = null;
    }
}
