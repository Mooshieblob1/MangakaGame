using System.Text.Json.Nodes;
using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class OfficeTests
{
    internal static void Furnish(GameState s,int location)
    {var a=s.ArrangeOffice(location,true,true);s.Apply(new ApplyOfficeLayoutCommand(location,s.OfficeRevision,a.Placements,a.Purchases,[]));}
    private static GameState Moved()
    {var s=GameState.NewGame(12);s.Apply(new StudioActionCommand(StudioAction.Move,1));return s;}
    private static ApplyOfficeLayoutCommand Draft(GameState s,int? loc=null)
    {var id=loc??s.Protagonist.Employment!.LocationId;return new(id,s.OfficeRevision,s.OfficeAt(id).Placements.ToList(),[],[]);}
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)]
    [InlineData(9)] [InlineData(10)] [InlineData(11)] [InlineData(12)] [InlineData(13)] [InlineData(14)] [InlineData(15)] [InlineData(16)]
    public void Every_property_can_fit_advertised_capacity(int offerId)
    {
        var p=TokyoProperties.All.Single(p=>p.Id==offerId);var location=new StudioLocation{Seats=p.Seats,PropertyOfferId=p.Id,IsFamilyHome=false};
        var a=OfficeAutoArrange.Arrange(OfficeCatalog.Plan(location),[],[],p.Seats,true,true);
        var inventory=a.Purchases.ToDictionary(p=>p.TemporaryId,p=>new OfficeFurniture{Id=p.TemporaryId,Kind=p.Kind});
        Assert.Equal(p.Seats,OfficeLayoutRules.Validate(OfficeCatalog.Plan(location),a.Placements,inventory).Count);
        Assert.Equal(p.Seats*10000,a.Purchases.Sum(p=>OfficeCatalog.Get(p.Kind).Price));
    }
    [Fact] public void Family_furnishing_is_free_assigned_and_not_saleable()
    {
        var s=GameState.NewGame();Assert.Equal(300000,s.Money);Assert.Equal(0,s.Locations[0].MonthlyRent);Assert.Equal(2,s.UsableWorkspaces(s.Locations[0].Id));Assert.True(s.HasDesk(s.ProtagonistPersonId));
        var draft=Draft(s);var id=s.Furniture[0].Id;var before=s.ToJson();
        Assert.Throws<InvalidCommandException>(()=>s.Apply(draft with{Placements=[],Sell=[id]}));Assert.Equal(before,s.ToJson());
    }
    [Fact] public void Move_keeps_family_assets_and_purchases_only_required_workstation()
    {
        var s=Moved();var current=s.Locations.Single(l=>!l.Closed&&l.BusinessId==s.ControlledBusinessId);
        Assert.Equal(1,s.UsableWorkspaces(current.Id));Assert.Equal(88000,s.Money);
        Assert.DoesNotContain(s.OfficeAt(current.Id).Placements,p=>s.Furniture.Single(i=>i.Id==p.ItemId).Owner==FurnitureOwner.Family);
        Assert.Equal(s.ToJson(),GameState.FromJson(s.ToJson()).ToJson());
    }
    [Fact] public void Preview_does_not_spend_and_caller_lists_cannot_mutate_history()
    {
        var s=Moved();var a=s.ArrangeOffice(s.Protagonist.Employment!.LocationId,true,true);var d=new ApplyOfficeLayoutCommand(s.Protagonist.Employment.LocationId,s.OfficeRevision,a.Placements,a.Purchases,[]);
        var before=s.ToJson();Assert.Equal(30000,s.QuoteOffice(d).Purchases);Assert.Equal(before,s.ToJson());
        s.Apply(d);var after=s.ToJson();a.Placements.Clear();a.Purchases.Clear();Assert.Equal(after,s.ToJson());
    }
    [Fact] public void Break_room_command_materialises_two_seat_set()
    {
        var s=Moved();var l=s.Locations.Single(l=>!l.Closed&&l.BusinessId==s.ControlledBusinessId);s.Apply(new StudioActionCommand(StudioAction.BreakRoom,l.Id));
        Assert.Equal(4,l.BreakSeats);Assert.Contains(s.Furniture,i=>i.Kind=="break");Assert.Equal(s.ToJson(),GameState.FromJson(s.ToJson()).ToJson());
    }
    [Fact] public void Stale_preview_is_rejected_atomically()
    {
        var s=GameState.NewGame();var d=Draft(s);s.Advance(1);var before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(d));Assert.Equal(before,s.ToJson());
    }
    [Fact] public void Occupied_desk_cannot_be_removed_or_chair_disconnected()
    {
        var s=GameState.NewGame();var d=Draft(s);var assigned=s.OfficeAt(d.LocationId).Assignments[0].DeskId;var before=s.ToJson();
        Assert.Throws<InvalidCommandException>(()=>s.Apply(d with{Placements=[]}));
        var chair=d.Placements.Single(p=>p.DeskId==assigned);d.Placements[d.Placements.IndexOf(chair)]=chair with{X=0};
        Assert.Throws<InvalidCommandException>(()=>s.Apply(d));Assert.Equal(before,s.ToJson());
    }
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void Rotated_pairs_preserve_access_and_footprint(int rotation)
    {
        var plan=OfficeCatalog.Plan(new StudioLocation{Seats=8});var desk=new OfficePlacement(1,12,12,rotation);var chair=OfficeLayoutRules.ChairAt(desk,2);
        var inventory=new Dictionary<int,OfficeFurniture>{{1,new(){Id=1,Kind="desk"}},{2,new(){Id=2,Kind="chair"}}};
        Assert.Single(OfficeLayoutRules.Validate(plan,[desk,chair],inventory));
        Assert.Throws<InvalidCommandException>(()=>OfficeLayoutRules.Validate(plan,[desk,chair with{X=desk.X,Z=desk.Z}],inventory));
    }
    [Fact] public void Door_and_stock_area_are_protected()
    {
        var plan=OfficeCatalog.Plan(new StudioLocation{Seats=4});var inventory=new Dictionary<int,OfficeFurniture>{{1,new(){Id=1,Kind="plant"}}};
        Assert.Throws<InvalidCommandException>(()=>OfficeLayoutRules.Validate(plan,[new(1,plan.Width/2,plan.Depth-2)],inventory));
        Assert.Throws<InvalidCommandException>(()=>OfficeLayoutRules.Validate(plan,[new(1,plan.Width-4,plan.Depth-5)],inventory));
    }
    [Fact] public void Arrangement_is_stable_preserves_locks_and_does_not_touch_rng()
    {
        var s=GameState.NewGame(7);var l=s.Protagonist.Employment!.LocationId;var office=s.OfficeAt(l);var p=office.Placements[0];office.Placements[0]=p with{Locked=true};
        var before=s.ToJson();var a=s.ArrangeOffice(l);var b=s.ArrangeOffice(l);
        Assert.Equal(a.Placements,b.Placements);Assert.Contains(office.Placements[0],a.Placements);Assert.Equal(before,s.ToJson());
    }
    [Fact] public void Sale_returns_half_price_once_and_family_items_cannot_be_stolen()
    {
        var s=Moved();Furnish(s,s.Protagonist.Employment!.LocationId);var d=Draft(s);var spare=d.Placements.First(p=>s.Furniture.Single(i=>i.Id==p.ItemId).Kind=="desk"&&!s.OfficeAt(d.LocationId).Assignments.Any(a=>a.DeskId==p.ItemId));
        var chair=d.Placements.Single(p=>p.DeskId==spare.ItemId);var amount=s.Money;
        s.Apply(d with{Placements=d.Placements.Where(p=>p.ItemId!=spare.ItemId&&p.ItemId!=chair.ItemId).ToList(),Sell=[spare.ItemId,chair.ItemId]});Assert.Equal(amount+5000,s.Money);
        var before=s.ToJson();Assert.Throws<InvalidCommandException>(()=>s.Apply(Draft(s) with{Sell=[spare.ItemId]}));Assert.Equal(before,s.ToJson());
    }
    [Fact] public void Cannot_hire_without_furnishing_an_available_property_slot()
    {
        var s=Moved();var c=s.Candidates[0];var l=s.Protagonist.Employment!.LocationId;
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new HireStaffCommand(c.Id,l,c.ExpectedSalary)));
        // Provide payroll funding without bypassing furnishing.
        s.Apply(new ContributeFundsCommand(100000));Furnish(s,l);s.Apply(new HireStaffCommand(c.Id,l,c.ExpectedSalary));Assert.True(s.HasDesk(c.Id));
    }
    [Fact] public void Career_keeps_business_assets_and_reuses_family_items()
    {
        var s=Moved();var owned=s.Furniture.Where(i=>i.Owner==FurnitureOwner.Business).Select(i=>i.Id).ToArray();var old=s.ControlledBusinessId;
        s.Apply(new StudioActionCommand(StudioAction.CareerHome));s.Advance(24);
        Assert.All(owned,id=>Assert.Equal(old,s.Furniture.Single(i=>i.Id==id).BusinessId));
        Assert.Equal(4,s.Furniture.Count(i=>i.Owner==FurnitureOwner.Family));Assert.True(s.HasDesk(s.ProtagonistPersonId));
        Assert.Equal(s.ToJson(),GameState.FromJson(s.ToJson()).ToJson());
    }
    [Fact] public void Appearance_is_cosmetic_persistent_and_does_not_consume_gameplay_rng()
    {
        var a=GameState.NewGame(10);var b=GameState.NewGame(10);a.Apply(new SetAppearanceCommand(new(2,1,4,2,true,2,2)));
        Assert.Equal(b.StaffRng.State,a.StaffRng.State);Assert.Equal(b.NextId,a.NextId);
        a.Apply(new CreateSeriesCommand("Same","drama",Cadence.Weekly,19));b.Apply(new CreateSeriesCommand("Same","drama",Cadence.Weekly,19));a.Advance(48);b.Advance(48);
        Assert.Equal(b.Series[0].Chapters[0].Stages.Select(s=>s.HoursDone),a.Series[0].Chapters[0].Stages.Select(s=>s.HoursDone));
        Assert.Equal(a.Protagonist.Appearance,GameState.FromJson(a.ToJson()).Protagonist.Appearance);
    }
    [Fact] public void Older_appearance_recipes_default_to_cardigans_without_changing_existing_choices()
    {
        var state=GameState.NewGame(10);var saved=JsonNode.Parse(state.ToJson())!;
        foreach(var person in saved["People"]!.AsArray())person!["Appearance"]!.AsObject().Remove("Wardrobe");
        var restored=GameState.FromJson(saved.ToJsonString());
        foreach(var person in restored.People)
            Assert.Equal(state.FindPerson(person.Id)!.Appearance! with {Wardrobe=0},person.Appearance);
        Assert.Equal(state.StaffRng.State,restored.StaffRng.State);
        Assert.Equal(restored.ToJson(),GameState.FromJson(restored.ToJson()).ToJson());
    }
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Invalid_wardrobe_is_rejected(int wardrobe)
    {
        var state=GameState.NewGame();var before=state.ToJson();
        Assert.Throws<InvalidCommandException>(()=>state.Apply(new SetAppearanceCommand(new(Wardrobe:wardrobe))));
        Assert.Equal(before,state.ToJson());
    }
    [Fact] public void Import_v3_preserves_money_rng_and_provides_no_resale_windfall()
    {
        var s=Moved();var node=JsonNode.Parse(s.ToJson())!;node["Version"]=3;foreach(var key in new[]{"Furniture","Offices","OfficeActivities","NextFurnitureId","OfficeRevision"})node.AsObject().Remove(key);
        var json=node.ToJsonString();var migrated=GameState.ImportStudioV3(json);
        Assert.Equal(s.Money,migrated.Money);Assert.True(migrated.NextId>s.NextId);Assert.All(s.People,p=>Assert.NotNull(migrated.FindPerson(p.Id)));Assert.Equal(s.StaffRng.State,migrated.StaffRng.State);Assert.All(migrated.Furniture,i=>Assert.Equal(0,i.Paid));
        Assert.Equal(4,migrated.UsableWorkspaces(migrated.Protagonist.Employment!.LocationId));Assert.Equal(migrated.ToJson(),GameState.FromJson(migrated.ToJson()).ToJson());
        node.AsObject().Remove("PrintRuns");Assert.Throws<System.IO.InvalidDataException>(()=>GameState.ImportStudioV3(node.ToJsonString()));
    }
    [Fact] public void Office_commands_replay_with_same_item_ids_cash_and_activity()
    {
        var s=Moved();Furnish(s,s.Protagonist.Employment!.LocationId);s.Advance(30);
        var replay=GameState.NewGame(12);foreach(var entry in s.CommandLog){replay.Advance(replay.Clock.HoursUntil(entry.Time));replay.Apply(entry.Command);}replay.Advance(replay.Clock.HoursUntil(s.Clock.Now));
        Assert.Equal(s.ToJson(),replay.ToJson());
    }
    [Fact] public void Activity_records_break_and_actual_work_not_merely_the_queued_task()
    {
        var s=GameState.NewGame();s.Apply(new CreateSeriesCommand("Ink","drama",Cadence.Monthly,19));s.Protagonist.Food=10;s.Advance(1);
        Assert.Equal(OfficeActivityKind.Break,s.OfficeActivities.Single(a=>a.PersonId==s.ProtagonistPersonId).Kind);
        s.Advance(1);Assert.Equal(OfficeActivityKind.Work,s.OfficeActivities.Single(a=>a.PersonId==s.ProtagonistPersonId).Kind);
    }
    [Theory] [InlineData("Furniture")] [InlineData("Offices")] [InlineData("OfficeActivities")]
    public void Missing_office_save_collections_are_rejected(string name)
    {
        var node=JsonNode.Parse(GameState.NewGame().ToJson())!;node.AsObject().Remove(name);Assert.Throws<System.IO.InvalidDataException>(()=>GameState.FromJson(node.ToJsonString()));
    }

    [Fact] public void Combined_move_rejects_bad_layout_without_a_lease_or_cost_and_replays()
    {
        var s=GameState.NewGame(12);var preview=s.PreviewOfficeMove(1);var d=Draft(preview);var before=s.ToJson();
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new RelocateOfficeCommand(1,s.OfficeRevision,d with{Placements=[]})));Assert.Equal(before,s.ToJson());
        var quoted=s.QuoteOfficeRelocation(new(1,s.OfficeRevision,d));s.Apply(new RelocateOfficeCommand(1,s.OfficeRevision,d));Assert.Equal(300000-quoted,s.Money);
        var replay=GameState.NewGame(12);foreach(var command in s.CommandLog)replay.Apply(command.Command);Assert.Equal(s.ToJson(),replay.ToJson());
    }
    [Fact] public void Purchased_support_chair_has_a_small_comfort_benefit_without_speed_bonus()
    {
        var s=GameState.NewGame(6);var d=Draft(s);var desk=s.OfficeAt(d.LocationId).Assignments[0].DeskId;var chair=d.Placements.Single(p=>p.DeskId==desk);
        s.Apply(d with{Placements=d.Placements.Select(p=>p==chair?p with{ItemId=-1}:p).ToList(),Purchases=[new(-1,"chair-support")]});
        var basic=GameState.NewGame(6);foreach(var state in new[]{s,basic}){state.Apply(new CreateSeriesCommand("Work","drama",Cadence.Monthly,19));state.Advance(1);}
        Assert.Equal(.5,s.Protagonist.Comfort-basic.Protagonist.Comfort,5);
        Assert.Equal(basic.Series[0].Chapters[0].Stages.Select(x=>x.HoursDone),s.Series[0].Chapters[0].Stages.Select(x=>x.HoursDone));
        Assert.Contains(s.Furniture,i=>i.Id==chair.ItemId&&!i.Sold); // Family chair is stored, never sold.
    }
    [Fact] public void Wage_reserves_and_employer_gross_budget_cannot_be_spent_on_furniture()
    {
        var s=GameState.NewGame();s.Apply(new StudioActionCommand(StudioAction.SetFounderSalary,Amount:900000));var d=Draft(s);var before=s.ToJson();
        Assert.Throws<InvalidCommandException>(()=>s.Apply(d with{Purchases=Enumerable.Range(1,30).Select(i=>new OfficePurchase(-i,"desk-better")).ToList()}));Assert.Equal(before,s.ToJson());
        s=GameState.NewGame();s.Apply(new StudioActionCommand(StudioAction.CareerEmployer,Value:1));s.Advance(24);d=Draft(s);before=s.ToJson();
        Assert.Throws<InvalidCommandException>(()=>s.Apply(d with{Purchases=[new(-1,"desk-better"),new(-2,"desk-better")]}));Assert.Equal(before,s.ToJson());
        Assert.Throws<InvalidCommandException>(()=>s.Apply(Draft(s,s.Locations.First(l=>l.Id!=d.LocationId).Id)));
    }
    [Fact] public void Wall_prints_cannot_overlap_and_fake_desk_links_cannot_inflate_capacity()
    {
        var plan=OfficeCatalog.Plan(new StudioLocation{Seats=4});var items=new Dictionary<int,OfficeFurniture>{{1,new(){Id=1,Kind="print"}},{2,new(){Id=2,Kind="print"}}};
        Assert.Throws<InvalidCommandException>(()=>OfficeLayoutRules.Validate(plan,[new(1,0,0),new(2,1,0)],items));
        Assert.Throws<InvalidCommandException>(()=>OfficeLayoutRules.Validate(plan,[new(1,0,0,DeskId:9)],items));
    }
    [Theory] [InlineData("Clock")] [InlineData("RngSeed")] [InlineData("LastSalesAt")]
    public void Legacy_import_does_not_default_missing_old_fields(string key)
    {
        var node=JsonNode.Parse(GameState.NewGame().ToJson())!;node["Version"]=3;node.AsObject().Remove(key);
        Assert.Throws<System.IO.InvalidDataException>(()=>GameState.ImportStudioV3(node.ToJsonString()));
    }
    [Theory] [InlineData("Placements")] [InlineData("Assignments")]
    public void Null_layout_entries_are_cleanly_rejected(string field)
    {
        var node=JsonNode.Parse(GameState.NewGame().ToJson())!;node["Offices"]![0]![field]![0]=null;
        Assert.Throws<System.IO.InvalidDataException>(()=>GameState.FromJson(node.ToJsonString()));
    }
    [Fact] public void Missing_property_metadata_and_null_command_entries_are_rejected()
    {
        var s=GameState.NewGame();var node=JsonNode.Parse(s.ToJson())!;node["Locations"]![0]!.AsObject().Remove("FixedBreakSeats");
        Assert.Throws<System.IO.InvalidDataException>(()=>GameState.FromJson(node.ToJsonString()));var before=s.ToJson();
        Assert.Throws<InvalidCommandException>(()=>s.Apply(Draft(s) with{Purchases=[null!]}));
        Assert.Throws<InvalidCommandException>(()=>s.Apply(new RelocateOfficeCommand(1,s.OfficeRevision,null!)));Assert.Equal(before,s.ToJson());
    }

    [Fact] public void Legacy_import_handles_closed_home_return_and_existing_break_upgrades()
    {
        var s=Moved();s.Apply(new StudioActionCommand(StudioAction.BreakRoom,s.Protagonist.Employment!.LocationId));
        s.Apply(new StudioActionCommand(StudioAction.CareerHome));s.Advance(24);
        var node=JsonNode.Parse(s.ToJson())!;node["Version"]=3;
        foreach(var key in new[]{"Furniture","Offices","OfficeActivities","NextFurnitureId","OfficeRevision"})node.AsObject().Remove(key);
        foreach(var location in node["Locations"]!.AsArray())foreach(var key in new[]{"PropertyOfferId","FloorPlanId","OfficeRevision","BaseAtmosphere","FixedBreakSeats"})location!.AsObject().Remove(key);
        foreach(var person in node["People"]!.AsArray())person!.AsObject().Remove("Appearance");
        var migrated=GameState.ImportStudioV3(node.ToJsonString());
        Assert.Equal(s.Locations.Select(l=>l.BreakSeats),migrated.Locations.Where(l=>s.Locations.Any(old=>old.Id==l.Id)).Select(l=>l.BreakSeats));
        Assert.Equal(s.Money,migrated.Money);Assert.Equal(s.StaffRng.State,migrated.StaffRng.State);Assert.Equal(4,migrated.Furniture.Count(i=>i.Owner==FurnitureOwner.Family));
        Assert.Equal(migrated.ToJson(),GameState.FromJson(migrated.ToJson()).ToJson());
    }
}
