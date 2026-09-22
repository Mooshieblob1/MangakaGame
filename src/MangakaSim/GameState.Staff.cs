namespace MangakaSim;

public partial class GameState
{
    // ---------------------------------------------------------------- studio helpers

    public Premises CurrentPremises => StaffData.RequirePremises(Studio.PremisesId);

    public List<Amenity> OwnedAmenities => Studio.Amenities.Select(StaffData.RequireAmenity).ToList();

    public double Atmosphere =>
        HappinessRules.Atmosphere(CurrentPremises, OwnedAmenities, People.Count, CurrentPremises.Capacity);

    internal void AdjustHappiness(Person person, double delta) =>
        person.Happiness = HappinessRules.Clamp(person.Happiness + delta);

    // ---------------------------------------------------------------- needs and breaks

    /// <summary>
    /// Runs before WorkStep. Resets needs on the first working hour of the day, turns the hour into a break
    /// when a need is below the threshold, otherwise depletes the needs for the hour about to be worked.
    /// </summary>
    internal void NeedsStep()
    {
        var owned = OwnedAmenities;
        var comfortRate = NeedsRules.ComfortRate(owned);
        foreach (var person in People)
        {
            person.OnBreak = false;
            if (!IsWorkingHour(person, TickStart, out var overtime)) continue;

            if (person.HoursWorkedToday == 0 && person.BreaksToday == 0 && IsRegularHour(person, TickStart))
                person.Needs = new Needs();

            if (NeedsRules.NeedsBreak(person.Needs))
            {
                var lowest = person.Needs.Lowest;
                person.Needs = NeedsRules.Recover(person.Needs, lowest, NeedsRules.Recovery(lowest, owned));
                person.OnBreak = true;
                person.BreaksToday++;
                if (overtime) person.OvertimeHoursToday++;
                if (person.BreaksToday == 1)
                {
                    Emit(EventType.TookBreak, $"{person.Name} takes a break ({lowest}).", new EventContext(PersonId: person.Id));
                }
                continue;
            }

            var before = person.Needs.Min;
            person.Needs = NeedsRules.Deplete(person.Needs, overtime, comfortRate);
            // Unreachable at the shipped rates (a break always comes first), kept for balance changes.
            if (before > 0 && person.Needs.Min <= 0)
            {
                Emit(EventType.NeedCritical,
                    $"{person.Name} is running on empty ({person.Needs.Lowest}).",
                    new EventContext(PersonId: person.Id));
                AdjustHappiness(person, HappinessRules.NeedZero);
            }
        }
    }

    // ---------------------------------------------------------------- midnight

    /// <summary>Runs at the day rollover before the daily counters reset: fatigue and the seven-day windows.</summary>
    private void StaffNewDay()
    {
        var yesterday = Clock.Now.AddDays(-1);
        foreach (var person in People)
        {
            var regular = person.RegularHoursToday;
            var overtimeWorked = person.HoursWorkedToday - regular;
            var dayOff = person.Schedule.IsDayOff(yesterday);
            person.Fatigue = FatigueRules.Clamp(person.Fatigue + FatigueRules.Accrue(overtimeWorked, regular)
                                                - FatigueRules.Recover(dayOff));
            Append(person.RecentOvertime, overtimeWorked);
            Append(person.RecentRegular, regular);
            Append(person.RecentBreaks, person.BreaksToday);
            person.BreaksToday = 0;
            person.OnBreak = false;
        }
        HappinessStep();
        MoonlightStep();
        if (Clock.Now.Day == 1) QuitRolls();
    }

    private static void Append(List<int> window, int value)
    {
        window.Add(value);
        while (window.Count > 7) window.RemoveAt(0);
    }

    /// <summary>Overtime hours over regular hours in the last seven days (0 when nothing was worked).</summary>
    internal static double OvertimeShare(Person person)
    {
        var regular = person.RecentRegular.Sum();
        return regular <= 0 ? 0 : (double)person.RecentOvertime.Sum() / regular;
    }

    /// <summary>Breaks per working day over the last seven days.</summary>
    internal static double BreaksPerDay(Person person)
    {
        var workingDays = person.RecentRegular.Count(h => h > 0);
        return workingDays <= 0 ? 0 : (double)person.RecentBreaks.Sum() / workingDays;
    }

    // ---------------------------------------------------------------- happiness

    internal double PayFactorOf(Person person) =>
        person.IsMangaka ? 1.0 : PayRules.PayFactor(person.Salary, PayRules.MarketSalary(person.Skills.Values, PriceIndexNow));

    public double EquilibriumOf(Person person) =>
        HappinessRules.Equilibrium(PayFactorOf(person), Atmosphere, OvertimeShare(person), BreaksPerDay(person), StudioTrackRecord);

    /// <summary>Daily: everyone drifts a tenth of the way toward their equilibrium.</summary>
    private void HappinessStep()
    {
        var atmosphere = Atmosphere;
        foreach (var person in People)
        {
            var equilibrium = HappinessRules.Equilibrium(PayFactorOf(person), atmosphere, OvertimeShare(person),
                BreaksPerDay(person), StudioTrackRecord);
            person.Happiness = HappinessRules.Step(person.Happiness, equilibrium);
        }
    }

    /// <summary>Applies a happiness shock to everyone who worked on the chapters.</summary>
    internal void ShockContributors(IEnumerable<Chapter> chapters, double delta)
    {
        foreach (var personId in HourShares(chapters).Keys)
        {
            if (FindPerson(personId) is { } person) AdjustHappiness(person, delta);
        }
    }

    // ---------------------------------------------------------------- moonlighting and quitting

    private void MoonlightStep()
    {
        foreach (var person in People.Where(p => p.Role == PersonRole.Assistant))
        {
            if (!person.IsMoonlighting)
            {
                var chance = MoonlightRules.StartChance(person.Happiness);
                if (chance <= 0 || Rng.NextDouble() >= chance) continue;
                person.IsMoonlighting = true;
                Emit(EventType.MoonlightingStarted,
                    $"{person.Name} has started taking outside work and leaves two hours early.",
                    new EventContext(PersonId: person.Id));
            }
            else if (MoonlightRules.Stops(person.Happiness))
            {
                person.IsMoonlighting = false;
                Emit(EventType.MoonlightingStopped,
                    $"{person.Name} has dropped the outside work.",
                    new EventContext(PersonId: person.Id));
            }
        }
    }

    /// <summary>This month's quit chance for an assistant; doubled after two consecutive missed payrolls.</summary>
    internal double QuitChanceFor(Person person) =>
        MoonlightRules.QuitChance(person.Happiness, person.MonthsEmployed) * (Studio.MissedPayrolls >= 2 ? 2 : 1);

    private void QuitRolls()
    {
        foreach (var person in People.Where(p => p.Role == PersonRole.Assistant).ToList())
        {
            var chance = QuitChanceFor(person);
            if (chance <= 0 || Rng.NextDouble() >= chance) continue;
            Emit(EventType.StaffQuit,
                $"{person.Name} quits (happiness {person.Happiness:0}).",
                new EventContext(PersonId: person.Id));
            RemovePerson(person, quit: true);
        }
    }
}
