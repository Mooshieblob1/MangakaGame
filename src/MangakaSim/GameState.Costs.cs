namespace MangakaSim;

public partial class GameState
{
    // ---------------------------------------------------------------- monthly charges

    /// <summary>Rent, upkeep and the provider on the 1st at 09:00; payroll on the 25th at 09:00.</summary>
    internal void CostsStep()
    {
        if (TickStart.Hour != 9) return;
        var month = new DateTime(TickStart.Year, TickStart.Month, 1);
        if (TickStart.Day == 1 && month > LastCostsMonth)
        {
            LastCostsMonth = month;
            ChargeMonthlyCosts();
        }
        if (TickStart.Day == 25 && month > LastPayrollMonth)
        {
            LastPayrollMonth = month;
            RunPayroll();
        }
    }

    private void ChargeMonthlyCosts()
    {
        var charge = PremisesRules.Charge(CurrentPremises, OwnedAmenities, HasInternet, PriceIndexNow);
        if (charge.Total <= 0) return;
        if (charge.Rent > 0) AddLedger(-charge.Rent, "rent");
        if (charge.Upkeep > 0) AddLedger(-charge.Upkeep, "upkeep");
        if (charge.Provider > 0) AddLedger(-charge.Provider, "internet provider");
        Emit(EventType.MonthlyCostsPaid,
            $"Monthly costs: rent {charge.Rent:N0}, upkeep {charge.Upkeep:N0}, provider {charge.Provider:N0} yen.",
            new EventContext(Amount: -charge.Total));
    }

    private void RunPayroll()
    {
        var assistants = People.Where(p => p.Role == PersonRole.Assistant).ToList();
        if (assistants.Count == 0) return;
        foreach (var person in assistants) person.MonthsEmployed++;
        var total = assistants.Sum(p => (long)p.Salary);
        if (total <= 0) return;

        if (Money < total)
        {
            Studio.MissedPayrolls++;
            foreach (var person in assistants) AdjustHappiness(person, HappinessRules.PayrollMissed);
            Emit(EventType.PayrollMissed,
                $"Payroll of {total:N0} yen could not be met with {Money:N0} in the bank; nobody was paid this month.",
                new EventContext(Amount: -total));
            return;
        }

        foreach (var person in assistants)
            AddLedger(-person.Salary, "salary", personId: person.Id);
        Studio.MissedPayrolls = 0;
        Emit(EventType.PayrollPaid,
            $"Payroll: {total:N0} yen to {assistants.Count} assistant{(assistants.Count == 1 ? "" : "s")}.",
            new EventContext(Amount: -total));
    }

    // ---------------------------------------------------------------- premises and amenities

    private void ApplyMovePremises(MovePremisesCommand c)
    {
        if (c.PremisesId is null) throw new InvalidCommandException("Premises id must not be null.");
        var target = StaffData.FindPremises(c.PremisesId) ?? throw new InvalidCommandException($"No premises with id '{c.PremisesId}'.");
        var current = CurrentPremises;
        if (target.Id == current.Id) throw new InvalidCommandException($"The studio is already in {current.Name}.");
        if (People.Count > target.Capacity)
            throw new InvalidCommandException($"{target.Name} holds {target.Capacity}; the studio has {People.Count} people.");
        var cost = PremisesRules.MoveCost(target, PriceIndexNow);
        if (Money < cost) throw new InvalidCommandException($"Moving to {target.Name} costs {cost:N0} yen up front; the studio has {Money:N0}.");

        if (cost > 0) AddLedger(-cost, "moving");
        Studio.PremisesId = target.Id;
        Studio.MovedInAt = Clock.Now;
        if (target.Atmosphere > current.Atmosphere)
            foreach (var person in People) AdjustHappiness(person, HappinessRules.BetterPremises);
        Emit(EventType.PremisesMoved,
            $"The studio moves from {current.Name} to {target.Name} ({target.Capacity} desks, {target.MonthlyRent:N0} yen a month at 1996 prices).",
            new EventContext(Amount: -cost));
    }

    private void ApplyBuyAmenity(BuyAmenityCommand c)
    {
        if (c.AmenityId is null) throw new InvalidCommandException("Amenity id must not be null.");
        var amenity = StaffData.FindAmenity(c.AmenityId) ?? throw new InvalidCommandException($"No amenity with id '{c.AmenityId}'.");
        if (Studio.Amenities.Contains(amenity.Id)) throw new InvalidCommandException($"The studio already has a {amenity.Name.ToLowerInvariant()}.");
        var cost = Economy.Inflate(amenity.Cost, PriceIndexNow);
        if (Money < cost) throw new InvalidCommandException($"A {amenity.Name.ToLowerInvariant()} costs {cost:N0} yen; the studio has {Money:N0}.");
        if (cost > 0) AddLedger(-cost, "amenity");
        Studio.Amenities.Add(amenity.Id);
        Emit(EventType.AmenityBought,
            $"Bought a {amenity.Name.ToLowerInvariant()} for {cost:N0} yen.",
            new EventContext(Amount: -cost));
    }

    // ---------------------------------------------------------------- promotion

    private void ApplySetPromotion(SetPromotionCommand c)
    {
        var person = RequirePerson(c.PersonId);
        if (c.SeriesId is { } target)
        {
            if (!HasInternet) throw new InvalidCommandException("Promotion needs the internet; get online first.");
            if (target != PromotionRules.WholeStudio)
            {
                var series = RequireSeries(target);
                if (series.Publishing != PublishingStatus.Unpublished)
                    throw new InvalidCommandException($"'{series.Title}' has a publisher to promote it; promotion is for doujin work.");
            }
            person.PromotionSeriesId = target;
            Emit(EventType.PromotionAssigned,
                target == PromotionRules.WholeStudio
                    ? $"{person.Name} will promote the studio's doujin work in idle hours."
                    : $"{person.Name} will promote {FindSeries(target)!.Title} in idle hours.",
                new EventContext(PersonId: person.Id, SeriesId: target == PromotionRules.WholeStudio ? null : target));
        }
        else
        {
            person.PromotionSeriesId = null;
            Emit(EventType.PromotionAssigned, $"{person.Name} stops promotion duty.", new EventContext(PersonId: person.Id));
        }
    }

    /// <summary>An idle working hour online: fans for the person's promotion target.</summary>
    private void Promote(Person person)
    {
        if (!HasInternet || person.PromotionSeriesId is not { } target) return;
        var fans = PromotionRules.FansPerHour(person.Skills.Values.DefaultIfEmpty(0).Average(), Economy.InternetReach(TrendData, Clock.Now));
        if (target == PromotionRules.WholeStudio)
        {
            var doujin = Series.Where(s => s.Status == SeriesStatus.Active && s.Publishing == PublishingStatus.Unpublished).ToList();
            foreach (var series in doujin) series.Fanbase += fans / doujin.Count;
        }
        else if (FindSeries(target) is { Publishing: PublishingStatus.Unpublished } series)
        {
            series.Fanbase += fans;
        }
    }
}
