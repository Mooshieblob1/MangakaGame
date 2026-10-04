namespace MangakaSim.Rules;

public static class OfficeAutoArrange
{
    public static OfficeArrangement Arrange(FloorPlanDefinition plan,IEnumerable<OfficeFurniture> available,
        IEnumerable<OfficePlacement> previous,int required,bool purchaseMissing=false,bool fillCapacity=false,IReadOnlyList<int>? deskOrder=null)
    {
        var inventory=available.ToList();var old=previous.ToList();var results=new List<OfficeArrangement>();string failure="No valid arrangement fits.";
        foreach(var reverse in new[]{false,true})
        {
            try{results.Add(ArrangeCore(plan,inventory,old,required,purchaseMissing,fillCapacity,deskOrder,reverse));}
            catch(InvalidCommandException ex){failure=ex.Message;}
        }
        if(results.Count==0)throw new InvalidCommandException(failure);
        long GroupDistance(OfficeArrangement a)
        {
            if(deskOrder is null)return 0;
            var desks=deskOrder.Select(id=>a.Placements.FirstOrDefault(p=>p.ItemId==id)).Where(p=>p is not null).ToArray();
            return desks.Zip(desks.Skip(1),(x,y)=>(long)Math.Abs(x!.X-y!.X)+Math.Abs(x.Z-y.Z)).Sum();
        }
        return results.OrderByDescending(a=>a.Placements.Count(p=>p.DeskId is not null))
            .ThenBy(a=>a.Purchases.Sum(p=>OfficeCatalog.Get(p.Kind).Price)).ThenBy(a=>a.Stored.Count)
            .ThenBy(GroupDistance).ThenBy(a=>a.Placements.Count(p=>old.Any(o=>o.ItemId==p.ItemId&&o!=p)))
            .ThenBy(a=>a.Placements.Where(p=>p.DeskId is not null).Sum(p=>Math.Abs(p.X-plan.Entrance.X)+Math.Abs(p.Z-plan.Entrance.Z))).First();
    }
    private static OfficeArrangement ArrangeCore(FloorPlanDefinition plan,IEnumerable<OfficeFurniture> available,
        IEnumerable<OfficePlacement> previous,int required,bool purchaseMissing,bool fillCapacity,IReadOnlyList<int>? deskOrder,bool reverse)
    {
        var inventory=available.Where(i=>!i.Sold).OrderBy(i=>i.Id).ToDictionary(i=>i.Id);
        var old=previous.ToList();var placed=old.Where(p=>p.Locked).ToList();
        foreach(var p in placed.ToArray())
        {
            var partner=old.FirstOrDefault(x=>x.DeskId==p.ItemId || p.DeskId==x.ItemId);
            if(partner is not null&&!placed.Any(x=>x.ItemId==partner.ItemId))placed.Add(partner);
        }
        OfficeLayoutRules.Validate(plan,placed,inventory);
        var purchases=new List<OfficePurchase>();var next=-1;while(inventory.ContainsKey(next))next--;
        OfficeFurniture New(string kind)
        {var item=new OfficeFurniture{Id=next--,Kind=kind};inventory[item.Id]=item;purchases.Add(new(item.Id,kind));return item;}
        var goal=Math.Min(plan.Capacity,fillCapacity?plan.Capacity:Math.Max(required,inventory.Values.Count(i=>OfficeCatalog.Get(i.Kind).Desk)));
        var workspaces=OfficeLayoutRules.Validate(plan,placed,inventory);
        var deskItems=inventory.Values.Where(i=>OfficeCatalog.Get(i.Kind).Desk&&!placed.Any(p=>p.ItemId==i.Id)).ToList();
        while(deskItems.Count+workspaces.Count<goal && purchaseMissing)deskItems.Add(New("desk"));
        if(deskOrder is not null)deskItems=deskItems.OrderBy(i=>deskOrder.Contains(i.Id)?Array.IndexOf(deskOrder.ToArray(),i.Id):int.MaxValue).ThenBy(i=>i.Id).ToList();
        var tries=0;
        bool TryAdd(params OfficePlacement[] group)
        {
            if(++tries>1000)return false;
            try{OfficeLayoutRules.Validate(plan,placed.Concat(group).ToList(),inventory);placed.AddRange(group);return true;}
            catch(InvalidCommandException){return false;}
        }
        foreach(var desk in deskItems.Take(plan.Capacity-workspaces.Count))
        {
            var chair=inventory.Values.Where(i=>OfficeCatalog.Get(i.Kind).Chair&&!placed.Any(p=>p.ItemId==i.Id)).OrderByDescending(i=>old.Any(p=>p.ItemId==i.Id&&p.DeskId==desk.Id)).ThenBy(i=>i.Id).FirstOrDefault();
            if(chair is null&&purchaseMissing)chair=New("chair");
            if(chair is null)continue;
            var slots=(reverse?plan.DeskSlots.Reverse():plan.DeskSlots).Select(c=>new OfficePlacement(desk.Id,c.X,c.Z,c.Rotation));
            var candidates=deskOrder is null?old.Where(p=>p.ItemId==desk.Id).Concat(slots):slots.Concat(old.Where(p=>p.ItemId==desk.Id));
            var done=false;
            foreach(var p in candidates)if(TryAdd(p with{Locked=false},OfficeLayoutRules.ChairAt(p,chair.Id))){done=true;break;}
            if(!done)
                for(var z=1;z<plan.Depth-7&&!done&&tries<1000;z+=2)
                for(var x=1;x<plan.Width-6&&!done&&tries<1000;x+=2)
                {var p=new OfficePlacement(desk.Id,x,z);done=TryAdd(p,OfficeLayoutRules.ChairAt(p,chair.Id));}
        }
        var count=OfficeLayoutRules.Validate(plan,placed,inventory).Count;
        if(count<required)throw new InvalidCommandException($"The arrangement fits {count} workspaces; {required} are required. Unlock furniture or add desk/chair pairs.");
        foreach(var item in inventory.Values.Where(i=>!placed.Any(p=>p.ItemId==i.Id)&&!OfficeCatalog.Get(i.Kind).Desk&&!OfficeCatalog.Get(i.Kind).Chair).OrderBy(i=>OfficeCatalog.Get(i.Kind).Break?0:1).ThenBy(i=>i.Id))
        {
            var done=false;var definition=OfficeCatalog.Get(item.Kind);
            foreach(var p in old.Where(p=>p.ItemId==item.Id))if(TryAdd(p)){done=true;break;}
            for(var z=definition.Art=="print"?0:1;z<(definition.Art=="print"?1:plan.Depth-2)&&!done&&tries<1000;z++)
            for(var x=0;x<plan.Width&&!done&&tries<1000;x++)done=TryAdd(new OfficePlacement(item.Id,x,z));
        }
        // Never quote purchases that the solver failed to place.
        purchases.RemoveAll(p=>!placed.Any(x=>x.ItemId==p.TemporaryId));
        var stored=inventory.Keys.Where(id=>id>0&&!placed.Any(p=>p.ItemId==id)).OrderBy(id=>id).ToList();
        return new(placed,purchases,stored,$"{count} usable workspaces; {stored.Count} items remain in storage. Doors and workstation approaches stay clear.");
    }
}
