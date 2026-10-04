using System.Text.Json;
using System.Text.Json.Serialization;
using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    [JsonRequired] public List<OfficeFurniture> Furniture { get; set; } = new();
    [JsonRequired] public List<OfficeLayout> Offices { get; set; } = new();
    [JsonRequired] public List<OfficeActivity> OfficeActivities { get; set; } = new();
    [JsonRequired] public int NextFurnitureId { get; set; } = 1;
    [JsonRequired] public int OfficeRevision { get; set; }
    public OfficeLayout OfficeAt(int location) => Offices.Single(o=>o.LocationId==location);
    public IReadOnlyDictionary<int,OfficeFurniture> OfficeInventory => Furniture.ToDictionary(i=>i.Id);
    public IEnumerable<OfficeFurniture> AvailableFurniture(int location)
    {
        var l=Locations.Single(x=>x.Id==location);
        var elsewhere=Offices.Where(o=>o.LocationId!=location&&!Locations.Single(x=>x.Id==o.LocationId).Closed).SelectMany(o=>o.Placements).Select(p=>p.ItemId).ToHashSet();
        return Furniture.Where(i=>!i.Sold&&!elsewhere.Contains(i.Id)&&(i.Owner==FurnitureOwner.Business?i.BusinessId==l.BusinessId:i.SiteKey==Site(l)));
    }
    private static string Site(StudioLocation l)=>l.IsFamilyHome?"parents-home":$"location-{l.Id}";
    private void InitializeOffice(StudioLocation l,bool materialize=false)
    {
        if(Offices.Any(o=>o.LocationId==l.Id))return;
        if(l.FloorPlanId.Length==0)l.FloorPlanId=l.IsFamilyHome?"family-home":BusinessOf(l.BusinessId).EmployerTier>0?$"employer-{l.Seats}":$"property-{l.PropertyOfferId}";
        l.BaseAtmosphere=l.Atmosphere;l.FixedBreakSeats=l.BreakSeats;
        var office=new OfficeLayout{LocationId=l.Id};Offices.Add(office);
        if(l.Closed)return;
        if(l.IsFamilyHome)
        {
            l.StudioIsland=true;
            foreach(var prior in Offices.Where(o=>o!=office&&Locations.Single(x=>x.Id==o.LocationId).IsFamilyHome))
                prior.Placements.RemoveAll(p=>Furniture.Single(i=>i.Id==p.ItemId).Owner==FurnitureOwner.Family);
            if(!Furniture.Any(i=>i.Owner==FurnitureOwner.Family))
                AddInitialFurniture(l,FurnitureOwner.Family,2);
            ArrangeInitial(l);
        }
        else if(materialize||BusinessOf(l.BusinessId).EmployerTier>0)
        {AddInitialFurniture(l,BusinessOf(l.BusinessId).EmployerTier>0?FurnitureOwner.Landlord:FurnitureOwner.Business,l.Seats);ArrangeInitial(l);}
    }
    private void AddInitialFurniture(StudioLocation l,FurnitureOwner owner,int count)
    {
        for(var n=0;n<count;n++)foreach(var kind in new[]{"desk","chair"})
            Furniture.Add(new(){Id=NextFurnitureId++,Kind=kind,Owner=owner,BusinessId=l.BusinessId,SiteKey=Site(l)});
    }
    private void ArrangeInitial(StudioLocation l)
    {
        var arrangement=OfficeAutoArrange.Arrange(OfficeCatalog.Plan(l),AvailableFurniture(l.Id),[],Math.Min(l.Seats,People.Count(p=>p.Employment?.LocationId==l.Id)));
        OfficeAt(l.Id).Placements=arrangement.Placements;
    }
    public int UsableWorkspaces(int location)
    {
        var office=Offices.FirstOrDefault(o=>o.LocationId==location);
        if(office is null)return 0;
        return office.Placements.Count(p=>p.DeskId is not null);
    }
    public bool HasDesk(int person) => Offices.Any(o=>o.Assignments.Any(a=>a.PersonId==person));
    public List<int> OfficeDeskOrder(int location)=>OfficeAt(location).Assignments.OrderBy(a=>FindPerson(a.PersonId)?.MainSeriesId??int.MaxValue).ThenBy(a=>a.PersonId).Select(a=>a.DeskId).ToList();
    public OfficeArrangement ArrangeOffice(int location,bool purchaseMissing=false,bool fillCapacity=false)
    {
        var l=RequireOffice(location);
        return OfficeAutoArrange.Arrange(OfficeCatalog.Plan(l),AvailableFurniture(location),OfficeAt(location).Placements,
            People.Count(p=>p.Employment?.LocationId==location),purchaseMissing,fillCapacity,OfficeDeskOrder(location));
    }
    private StudioLocation RequireOffice(int location) => Locations.FirstOrDefault(l=>l.Id==location&&!l.Closed&&l.BusinessId==ControlledBusinessId&&
        (Control!=ControlMode.EmployedLead||Protagonist.Employment?.LocationId==location))??throw new InvalidCommandException("This office is outside your authority.");
    private GameState OfficeCopy()=>JsonSerializer.Deserialize<GameState>(ToJson(),JsonOptions)!;
    public GameState PreviewOfficeMove(int offer)
    {var copy=OfficeCopy();copy.Apply(new StudioActionCommand(StudioAction.Move,offer));return copy;}
    public long QuoteOfficeRelocation(RelocateOfficeCommand c)
    {var copy=OfficeCopy();var before=copy.Money;copy.ExecuteOfficeRelocation(c);return before-copy.Money;}
    private void ApplyOfficeRelocation(RelocateOfficeCommand c)
    {QuoteOfficeRelocation(c);ExecuteOfficeRelocation(c);}
    private void ExecuteOfficeRelocation(RelocateOfficeCommand c)
    {
        if(c.Layout is null)throw new InvalidCommandException("A move needs a furnishing preview.");
        if(c.Revision!=OfficeRevision)throw new InvalidCommandException("Refresh this move preview; the business has changed.");
        ExecuteStudioAction(new StudioActionCommand(StudioAction.Move,c.OfferId));RefreshOfficeAssignments();OfficeRevision++;
        if(c.Layout.LocationId!=Protagonist.Employment!.LocationId)throw new InvalidCommandException("The layout must belong to the move destination.");
        ExecuteOffice(c.Layout);
    }
    public OfficeQuote QuoteOffice(ApplyOfficeLayoutCommand command)=>OfficeCopy().ExecuteOffice(command);
    private void ApplyOffice(ApplyOfficeLayoutCommand command){QuoteOffice(command);ExecuteOffice(command);}
    private OfficeQuote ExecuteOffice(ApplyOfficeLayoutCommand c)
    {
        var l=RequireOffice(c.LocationId);var office=OfficeAt(l.Id);
        if(c.Revision!=OfficeRevision)throw new InvalidCommandException("The office changed. Refresh this preview before applying.");
        if(c.Placements is null||c.Purchases is null||c.Sell is null||c.Placements.Any(p=>p is null)||c.Purchases.Any(p=>p is null)||c.Assignments?.Any(a=>a is null)==true||c.Placements.Count>512||c.Purchases.Count>256||c.Sell.Count>512)
            throw new InvalidCommandException("Invalid furniture batch.");
        if(c.Purchases.Any(p=>p.TemporaryId>=0)||c.Purchases.Select(p=>p.TemporaryId).Distinct().Count()!=c.Purchases.Count||c.Sell.Distinct().Count()!=c.Sell.Count)
            throw new InvalidCommandException("Duplicate furniture references.");
        var available=AvailableFurniture(l.Id).ToDictionary(i=>i.Id);
        var items=available.ToDictionary(x=>x.Key,x=>x.Value);
        long purchases=0,sales=0;
        foreach(var p in c.Purchases)
        {
            if(OfficeCatalog.Get(p.Kind).RewardOnly)throw new InvalidCommandException("That item is a goal reward and cannot be bought.");
            if(!EquipmentAvailable(p.Kind))throw new InvalidCommandException("This workstation needs more studio reputation or the digital era. Sandbox unlocks are available in Settings.");
            var f=OfficeCatalog.Get(p.Kind);purchases=checked(purchases+f.Price);
            items.Add(p.TemporaryId,new(){Id=p.TemporaryId,Kind=p.Kind,Owner=FurnitureOwner.Business,BusinessId=l.BusinessId,Paid=f.Price});
        }
        foreach(var id in c.Sell)
        {
            if(!items.TryGetValue(id,out var item)||item.Owner!=FurnitureOwner.Business||item.BusinessId!=l.BusinessId||Control==ControlMode.EmployedLead&&item.Paid==0)
                throw new InvalidCommandException("You can only sell this business's purchased movable furniture.");
            if(c.Placements.Any(p=>p.ItemId==id))throw new InvalidCommandException("Remove the item from the layout before selling it.");
            sales=checked(sales+item.Paid/2);items.Remove(id);
        }
        var desks=OfficeLayoutRules.Validate(OfficeCatalog.Plan(l),c.Placements,items);
        var staff=People.Where(p=>p.Employment?.LocationId==l.Id).OrderBy(p=>p.Id).ToList();
        if(desks.Count<staff.Count)throw new InvalidCommandException($"{staff.Count} staff need desks; this layout has {desks.Count}.");
        if(c.Placements.Count(p=>OfficeCatalog.Get(items[p.ItemId].Kind).Break)*2+l.FixedBreakSeats>Math.Max(l.Seats,l.BreakSeats))
            throw new InvalidCommandException("This property already has enough break seating.");
        var assign=c.Assignments?.ToList()??office.Assignments.Where(a=>desks.Contains(a.DeskId)&&staff.Any(p=>p.Id==a.PersonId)).ToList();
        if(assign.Select(a=>a.PersonId).Distinct().Count()!=assign.Count||assign.Select(a=>a.DeskId).Distinct().Count()!=assign.Count||assign.Any(a=>!desks.Contains(a.DeskId)||!staff.Any(p=>p.Id==a.PersonId)))
            throw new InvalidCommandException("Assign each employee to one different desk in their workplace.");
        if(purchases>FreeCash(l.BusinessId)+sales || purchases>TeamBudgetLeft(l.BusinessId))throw new InvalidCommandException("Keep enough money for wages, bills and the employer's team budget.");
        // All checks above happen on a candidate copy first; the committed replay uses the same sequence.
        if(sales>0)AccountPost(BusinessOf(l.BusinessId).Account,sales,"furniture resale",AccountEntryKind.Expense);
        if(purchases>0)Spend(l.BusinessId,purchases,"office furniture");
        var ids=new Dictionary<int,int>();
        foreach(var p in c.Purchases)
        {
            var item=items[p.TemporaryId];item.Id=NextFurnitureId++;Furniture.Add(item);ids[p.TemporaryId]=item.Id;
        }
        foreach(var id in c.Sell)Furniture.Single(i=>i.Id==id).Sold=true;
        int Resolve(int id)=>ids.GetValueOrDefault(id,id);
        office.Placements=c.Placements.Select(p=>p with{ItemId=Resolve(p.ItemId),DeskId=p.DeskId is {} d?Resolve(d):null}).ToList();
        office.Assignments=assign.Select(a=>a with{DeskId=Resolve(a.DeskId)}).ToList();
        OfficeRevision++;l.OfficeRevision++;RefreshOfficeAssignments();
        var stored=AvailableFurniture(l.Id).Count(i=>!office.Placements.Any(p=>p.ItemId==i.Id));
        return new(purchases,sales,desks.Count,stored,$"Buy ¥{purchases:N0} · sell ¥{sales:N0} · total ¥{purchases-sales:N0} · {desks.Count} workspaces · {stored} stored");
    }
    private void RefreshOfficeAssignments()
    {
        foreach(var l in Locations)
        {
            InitializeOffice(l);
            var o=OfficeAt(l.Id);
            if(l.Closed){o.Assignments.Clear();continue;}
            var staff=People.Where(p=>p.Employment?.LocationId==l.Id).OrderBy(p=>p.MainSeriesId??int.MaxValue).ThenBy(p=>p.Id).ToArray();
            var desks=o.Placements.Where(p=>p.DeskId is not null).Select(p=>p.DeskId!.Value).ToHashSet();
            o.Assignments.RemoveAll(a=>!staff.Any(p=>p.Id==a.PersonId)||!desks.Contains(a.DeskId));
            foreach(var p in staff.Where(p=>!o.Assignments.Any(a=>a.PersonId==p.Id)))
            {
                var desk=desks.OrderBy(id=>id).FirstOrDefault(id=>!o.Assignments.Any(a=>a.DeskId==id));
                if(desk>0)o.Assignments.Add(new(p.Id,desk));
            }
            l.BreakSeats=l.FixedBreakSeats+o.Placements.Count(p=>OfficeCatalog.Get(Furniture.Single(i=>i.Id==p.ItemId).Kind).Break)*2;
            l.Atmosphere=l.BaseAtmosphere+o.Placements.Select(p=>Furniture.Single(i=>i.Id==p.ItemId).Kind).Where(k=>k is "plant" or "print").Distinct().Count();
        }
        foreach(var p in People)p.Appearance??=AppearanceRecipe.Generate(RngSeed,p.Id);
    }
    private void AssignDesk(AssignDeskCommand c)
    {
        RequireOffice(c.LocationId);var office=OfficeAt(c.LocationId);
        var person=RequirePerson(c.PersonId);
        if(person.Employment!.LocationId!=c.LocationId||!office.Placements.Any(p=>p.DeskId==c.DeskId))throw new InvalidCommandException("Choose a usable desk in this person's workplace.");
        var old=office.Assignments.SingleOrDefault(a=>a.PersonId==c.PersonId);
        var other=office.Assignments.SingleOrDefault(a=>a.DeskId==c.DeskId);
        office.Assignments.RemoveAll(a=>a.PersonId==c.PersonId||a.DeskId==c.DeskId);
        office.Assignments.Add(new(c.PersonId,c.DeskId));
        if(other is not null&&other.PersonId!=c.PersonId&&old is not null)office.Assignments.Add(new(other.PersonId,old.DeskId));
        OfficeRevision++;RefreshOfficeAssignments();
    }
    private double ChairComfort(Person p)
    {
        var office=Offices.FirstOrDefault(o=>o.LocationId==p.Employment?.LocationId);
        var desk=office?.Assignments.FirstOrDefault(a=>a.PersonId==p.Id)?.DeskId;
        var chair=office?.Placements.FirstOrDefault(x=>x.DeskId==desk&&x.DeskId is not null);
        return chair is not null&&Furniture.Single(i=>i.Id==chair.ItemId).Kind=="chair-support"?.5:0;
    }
    private void Observe(Person p,OfficeActivityKind kind,Stage? stage=null,int? partner=null,int? facility=null,int? seat=null)
    {
        if(p.Employment is not{} job)return;
        OfficeActivities.RemoveAll(a=>a.PersonId==p.Id);
        OfficeActivities.Add(new(p.Id,job.LocationId,kind,TickStart,stage,partner,facility,seat));
    }
    private void StartOfficeHour()
    {
        OfficeActivities.Clear();
        foreach(var p in People.Where(p=>p.Employment is not null))
            Observe(p,p.Employment!.StartsAt>TickStart||!p.Schedule.IsRegularHour(TickStart)?OfficeActivityKind.OffDuty:
                p.BusyUntil>TickStart?OfficeActivityKind.Travel:OfficeActivityKind.Idle);
    }
    private void FundOfficeFor(StudioLocation destination,int required,bool keepAll=false)
    {
        InitializeOffice(destination);
        var arrangement=OfficeAutoArrange.Arrange(OfficeCatalog.Plan(destination),AvailableFurniture(destination.Id),OfficeAt(destination.Id).Placements,required,true,keepAll);
        var total=arrangement.Purchases.Sum(p=>OfficeCatalog.Get(p.Kind).Price);
        if(total>0)Spend(destination.BusinessId,total,"office furniture");
        var map=new Dictionary<int,int>();
        foreach(var buy in arrangement.Purchases){var id=NextFurnitureId++;map[buy.TemporaryId]=id;Furniture.Add(new(){Id=id,Kind=buy.Kind,BusinessId=destination.BusinessId,Owner=FurnitureOwner.Business,Paid=OfficeCatalog.Get(buy.Kind).Price});}
        OfficeAt(destination.Id).Placements=arrangement.Placements.Select(p=>p with{ItemId=map.GetValueOrDefault(p.ItemId,p.ItemId),DeskId=p.DeskId is{} id?map.GetValueOrDefault(id,id):null}).ToList();
    }
    private void StoreOffice(StudioLocation l)
    {
        var office=Offices.FirstOrDefault(o=>o.LocationId==l.Id);if(office is null)return;
        office.Assignments.Clear();office.Placements.RemoveAll(p=>Furniture.Single(i=>i.Id==p.ItemId).Owner==FurnitureOwner.Business);
    }
}
