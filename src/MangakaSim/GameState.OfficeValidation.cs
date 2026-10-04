using System.Text.Json;
using System.Text.Json.Nodes;
using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    private void ValidateOfficeSave()
    {
        void Check(bool value,string field){if(!value)throw new InvalidDataException($"Save file has invalid office {field}.");}
        Check(Furniture is not null&&Offices is not null&&OfficeActivities is not null,"collections");
        Check(OfficeRevision>=0&&NextFurnitureId>0,"revision");
        var items=Furniture!;var offices=Offices!;
        Check(items.Count<=100000&&items.All(i=>i is not null&&i.Id>0&&Enum.IsDefined(i.Owner)&&OfficeCatalog.Furniture.Any(f=>f.Id==i.Kind)&&i.Paid>=0&&i.Paid<=OfficeCatalog.Get(i.Kind).Price&&(i.Owner==FurnitureOwner.Business||i.Paid==0)&&Businesses.Any(b=>b.Id==i.BusinessId)),"inventory");
        Check(items.Select(i=>i.Id).Distinct().Count()==items.Count&&NextFurnitureId>items.Select(i=>i.Id).DefaultIfEmpty(0).Max(),"item IDs");
        Check(offices.Count==Locations.Count&&offices.All(o=>o is not null&&o.Placements is not null&&o.Assignments is not null&&o.Placements.All(p=>p is not null)&&o.Assignments.All(a=>a is not null))&&offices.Select(o=>o.LocationId).Distinct().Count()==offices.Count,"locations");
        Check(offices.SelectMany(o=>o.Placements).Select(p=>p.ItemId).Distinct().Count()==offices.Sum(o=>o.Placements.Count),"duplicate placement");
        var inventory=items.ToDictionary(i=>i.Id);
        foreach(var l in Locations)
        {
            var expected=l.IsFamilyHome?"family-home":BusinessOf(l.BusinessId).EmployerTier>0?$"employer-{l.Seats}":$"property-{l.PropertyOfferId}";
            Check(l.FloorPlanId==expected&&double.IsFinite(l.BaseAtmosphere)&&l.BaseAtmosphere is >=-100 and <=100&&l.OfficeRevision>=0&&l.FixedBreakSeats>=2&&l.FixedBreakSeats<=1002&&(!l.StudioIsland||l.IsFamilyHome),"property identity");
            var o=offices.SingleOrDefault(o=>o.LocationId==l.Id);Check(o is not null,"location reference");
            foreach(var p in o!.Placements)
            {
                Check(inventory.TryGetValue(p.ItemId,out var item)&&!item.Sold,"placed item");
                Check(item!.Owner==FurnitureOwner.Business?item.BusinessId==l.BusinessId:item.SiteKey==Site(l),"ownership");
            }
            List<int> desks;
            try{desks=OfficeLayoutRules.Validate(OfficeCatalog.Plan(l),o.Placements,inventory);}
            catch(InvalidCommandException ex){throw new InvalidDataException($"Save file has invalid office layout: {ex.Message}",ex);}
            var staff=People.Where(p=>p.Employment?.LocationId==l.Id).Select(p=>p.Id).ToHashSet();
            Check(o.Assignments.Select(a=>a.PersonId).Distinct().Count()==o.Assignments.Count&&o.Assignments.Select(a=>a.DeskId).Distinct().Count()==o.Assignments.Count,"desk reservations");
            Check(o.Assignments.All(a=>staff.Contains(a.PersonId)&&desks.Contains(a.DeskId))&&(!l.Closed?o.Assignments.Count==staff.Count:o.Assignments.Count==0),"staff desks");
            var breaks=l.FixedBreakSeats+o.Placements.Count(p=>OfficeCatalog.Get(inventory[p.ItemId].Kind).Break)*2;
            var decor=o.Placements.Select(p=>inventory[p.ItemId].Kind).Where(k=>k is "plant" or "print").Distinct().Count();
            Check(l.Closed||l.BreakSeats==breaks&&l.Atmosphere==l.BaseAtmosphere+decor,"facility totals");
        }
        Check(People.All(p=>p.Appearance is {Valid:true}),"appearance");
        Check(OfficeActivities!.Count<=People.Count&&OfficeActivities.All(a=>a is not null)&&OfficeActivities.Select(a=>a.PersonId).Distinct().Count()==OfficeActivities.Count&&OfficeActivities.All(a=>
            People.Any(p=>p.Id==a.PersonId)&&Locations.Any(l=>l.Id==a.LocationId)&&Enum.IsDefined(a.Kind)&&a.At>=GameClock.Start&&a.At<=Clock.Now&&a.At.Ticks%TimeSpan.TicksPerHour==0&&
            (a.Stage is null||Enum.IsDefined(a.Stage.Value))&&(a.PartnerId is null||People.Any(p=>p.Id==a.PartnerId))&&(a.SeatIndex is null||a.Kind==OfficeActivityKind.Break&&a.SeatIndex>=0&&a.SeatIndex<1002)&&(a.FacilityId is null||a.Kind==OfficeActivityKind.Break&&items.Any(i=>i.Id==a.FacilityId&&OfficeCatalog.Get(i.Kind).Break))),"activity observations");
    }

    /// <summary>Explicit conversion; never overwrites an original file or advances gameplay randomness.</summary>
    public static GameState ImportStudioV3(string json)
    {
        try
        {
            var node=JsonNode.Parse(json)?.AsObject()??throw new InvalidDataException("Save is empty.");
            if(node[nameof(Version)]?.GetValue<int>()!=3)throw new InvalidDataException("Select a completed Sub-project 3 version-3 save.");
            foreach(var name in new[]{nameof(Clock),nameof(People),nameof(Series),nameof(Events),nameof(Rng),nameof(RngSeed),nameof(Settings),nameof(NextId),nameof(CommandLog),nameof(RecapWindowStart),nameof(RecapFiredToday),nameof(Businesses),nameof(Locations),nameof(Markets),nameof(Trends),nameof(DoujinCopiesThisMonth),nameof(DoujinFansThisMonth)})
                if(node[name] is null)throw new InvalidDataException($"Save file is missing {name}.");
            foreach(var name in new[]{nameof(LastTrendUpdateMonth),nameof(LastSalesAt)})if(!node.ContainsKey(name))throw new InvalidDataException($"Save file is missing {name}.");
            foreach(var name in new[]{nameof(Furniture),nameof(Offices),nameof(OfficeActivities)})node[name]=new JsonArray();
            node[nameof(World)]=JsonSerializer.SerializeToNode(new TimelineWorld(),JsonOptions);
            node[nameof(NextFurnitureId)]=1;node[nameof(OfficeRevision)]=0;
            node[nameof(Career)]=JsonSerializer.SerializeToNode(new CareerHistory(),JsonOptions);
            node[nameof(Progression)]=JsonSerializer.SerializeToNode(new ProgressionState(),JsonOptions);
            var state=JsonSerializer.Deserialize<GameState>(node.ToJsonString(),JsonOptions)??throw new InvalidDataException("Save is empty.");
            state.ValidateSave(); // Old required members and all old invariants are still checked.
            foreach(var l in state.Locations)
            {
                if(!l.IsFamilyHome&&state.BusinessOf(l.BusinessId).EmployerTier==0)
                {
                    var offer=TokyoProperties.All.FirstOrDefault(p=>p.District==l.District&&p.Seats==l.Seats&&p.Rent==l.MonthlyRent&&p.Tier==l.PropertyTier);
                    if(offer is null)throw new InvalidDataException("This legacy property is not in the supported catalog. Keep the original save.");
                    l.PropertyOfferId=offer.Id;
                }
                var legacyBreaks=l.BreakSeats;
                if(!l.Closed)l.BreakSeats=2+legacyBreaks%2;
                state.InitializeOffice(l,true);
                if(!l.Closed&&legacyBreaks>l.FixedBreakSeats)
                {
                    var owner=state.BusinessOf(l.BusinessId).EmployerTier>0?FurnitureOwner.Landlord:FurnitureOwner.Business;
                    var added=new List<int>();
                    for(var seats=l.FixedBreakSeats;seats<legacyBreaks;seats+=2)
                    {
                        var id=state.NextFurnitureId++;added.Add(id);
                        state.Furniture.Add(new(){Id=id,Kind="break",BusinessId=l.BusinessId,Owner=owner,SiteKey=Site(l)});
                    }
                    var layout=OfficeAutoArrange.Arrange(OfficeCatalog.Plan(l),state.AvailableFurniture(l.Id),state.OfficeAt(l.Id).Placements,l.Seats);
                    if(added.Any(id=>!layout.Placements.Any(p=>p.ItemId==id)))throw new InvalidDataException("This legacy break capacity cannot fit its office. Keep the original save.");
                    state.OfficeAt(l.Id).Placements=layout.Placements;
                }
            }
            state.Version=4;state.RefreshOfficeAssignments();state.ValidateSave();return ImportTimelineV4(state.ToJson());
        }
        catch(Exception ex) when(ex is JsonException or InvalidOperationException or ArgumentException or InvalidCommandException)
        {throw new InvalidDataException($"Could not import this version-3 save: {ex.Message}",ex);}
    }

    private void AddBreakFurniture(StudioLocation location)
    {
        var inventory=AvailableFurniture(location.Id).ToList();
        var temp=new OfficeFurniture{Id=-1,Kind="break",BusinessId=location.BusinessId};inventory.Add(temp);
        var arrangement=OfficeAutoArrange.Arrange(OfficeCatalog.Plan(location),inventory,OfficeAt(location.Id).Placements,People.Count(p=>p.Employment?.LocationId==location.Id));
        if(!arrangement.Placements.Any(p=>p.ItemId==-1))throw new InvalidCommandException("There is no room for this break set. Rearrange the office first.");
        ExecuteOffice(new(location.Id,OfficeRevision,arrangement.Placements,[new(-1,"break")],[]));
    }
    private void ObserveBreak(Person p,StudioLocation location,int index)
    {
        var sets=OfficeAt(location.Id).Placements.Where(p=>OfficeCatalog.Get(Furniture.Single(i=>i.Id==p.ItemId).Kind).Break).OrderBy(p=>p.ItemId).ToList();
        var slot=index-location.FixedBreakSeats;
        Observe(p,OfficeActivityKind.Break,facility:slot<0?null:sets[slot/2].ItemId,seat:slot<0?index:slot%2);
    }
    private double BreakComfortBonus(int location,int queueIndex)
    {
        var l=Locations.Single(l=>l.Id==location);if(queueIndex<l.FixedBreakSeats)return 0;
        var sets=OfficeAt(location).Placements.Where(p=>OfficeCatalog.Get(Furniture.Single(i=>i.Id==p.ItemId).Kind).Break).OrderBy(p=>p.ItemId).ToList();
        var index=(queueIndex-l.FixedBreakSeats)/2;
        return index<sets.Count&&Furniture.Single(i=>i.Id==sets[index].ItemId).Kind=="break-comfort"?3:0;
    }
}
