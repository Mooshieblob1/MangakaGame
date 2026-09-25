namespace MangakaSim.Rules;

public static class OfficeLayoutRules
{
    public static (int W,int D) Size(FurnitureDefinition f,int rotation) => rotation%2==0?(f.Width,f.Depth):(f.Depth,f.Width);
    public static OfficeCell Transform(OfficePlacement p, FurnitureDefinition f,int x,int z) => p.Rotation switch
    {
        0=>new(p.X+x,p.Z+z),1=>new(p.X+f.Depth-1-z,p.Z+x),
        2=>new(p.X+f.Width-1-x,p.Z+f.Depth-1-z),3=>new(p.X+z,p.Z+f.Width-1-x),
        _=>throw new InvalidCommandException("Furniture rotates in quarter turns.")
    };
    public static OfficePlacement ChairAt(OfficePlacement desk,int id,bool locked=false)
    {
        var anchor=Transform(desk,OfficeCatalog.Get("desk"),1,3);
        return desk.Rotation switch
        {
            0=>new(id,anchor.X,anchor.Z,0,locked,desk.ItemId),
            1=>new(id,anchor.X-2,anchor.Z,1,locked,desk.ItemId),
            2=>new(id,anchor.X-2,anchor.Z-2,2,locked,desk.ItemId),
            _=>new(id,anchor.X,anchor.Z-2,3,locked,desk.ItemId)
        };
    }
    public static OfficeCell Access(OfficePlacement p,FurnitureDefinition f) => Transform(p,f,f.Width/2,f.Depth+1);
    public static HashSet<OfficeCell> Occupied(IEnumerable<OfficePlacement> placements,IReadOnlyDictionary<int,OfficeFurniture> inventory)
    {
        var set=new HashSet<OfficeCell>();
        foreach(var p in placements)
        {
            var f=OfficeCatalog.Get(inventory[p.ItemId].Kind);var (w,d)=Size(f,p.Rotation);
            if(f.Art=="print")continue;
            for(var x=0;x<w;x++)for(var z=0;z<d;z++)set.Add(new(p.X+x,p.Z+z));
        }
        return set;
    }
    public static HashSet<OfficeCell> Reachable(FloorPlanDefinition plan,HashSet<OfficeCell> occupied)
    {
        occupied=ClearanceMask(plan,occupied);
        var result=new HashSet<OfficeCell>();var q=new Queue<OfficeCell>();
        if(!occupied.Contains(plan.Entrance)){q.Enqueue(plan.Entrance);result.Add(plan.Entrance);}
        while(q.TryDequeue(out var p))foreach(var n in Neighbours(p))
            if(n.X>=1&&n.Z>=1&&n.X<plan.Width-1&&n.Z<plan.Depth&&!occupied.Contains(n)&&result.Add(n))q.Enqueue(n);
        return result;
    }
    public static HashSet<OfficeCell> ClearanceMask(FloorPlanDefinition plan,HashSet<OfficeCell> solids)
    {
        var result=new HashSet<OfficeCell>();
        for(var x=plan.Width-5;x<plan.Width;x++)for(var z=plan.Depth-6;z<plan.Depth-2;z++)solids.Add(new(x,z));
        foreach(var cell in solids)for(var x=-1;x<=1;x++)for(var z=-1;z<=1;z++)result.Add(new(cell.X+x,cell.Z+z));
        return result;
    }
    public static IEnumerable<OfficeCell> Neighbours(OfficeCell p)
    {yield return new(p.X+1,p.Z);yield return new(p.X-1,p.Z);yield return new(p.X,p.Z+1);yield return new(p.X,p.Z-1);}
    public static List<int> Validate(FloorPlanDefinition plan,List<OfficePlacement> placements,IReadOnlyDictionary<int,OfficeFurniture> inventory)
    {
        if(placements is null||placements.Any(p=>p is null)||placements.Count>512 || placements.Select(p=>p.ItemId).Distinct().Count()!=placements.Count)
            throw new InvalidCommandException("An item can only be placed once.");
        var occupied=new HashSet<OfficeCell>();var wallCells=new HashSet<int>();
        foreach(var p in placements)
        {
            if(!inventory.TryGetValue(p.ItemId,out var item)||item.Sold)throw new InvalidCommandException("This furniture is unavailable.");
            var f=OfficeCatalog.Get(item.Kind);if(p.DeskId is not null&&!f.Chair)throw new InvalidCommandException("Only work chairs can be paired with desks.");if(p.Rotation is <0 or >3)throw new InvalidCommandException("Invalid furniture rotation.");
            var(w,d)=Size(f,p.Rotation);
            if(p.X<0||p.Z<0||p.X>plan.Width-w||p.Z>plan.Depth-d)throw new InvalidCommandException("Keep furniture inside the workroom.");
            if(f.Art=="print")
            {if(p.Z!=0||p.Rotation!=0)throw new InvalidCommandException("Wall prints belong on the back wall.");
                for(var x=p.X;x<p.X+w;x++)if(!wallCells.Add(x))throw new InvalidCommandException("Wall prints overlap.");continue;}
            for(var x=0;x<w;x++)for(var z=0;z<d;z++)
                if(plan.Protected(p.X+x,p.Z+z)||!occupied.Add(new(p.X+x,p.Z+z)))
                    throw new InvalidCommandException("Furniture overlaps an item, doorway or stock area.");
        }
        var reachable=Reachable(plan,occupied);var desks=new List<int>();
        foreach(var p in placements)
        {
            var f=OfficeCatalog.Get(inventory[p.ItemId].Kind);
            if(f.Chair && p.DeskId is { } id)
            {
                var desk=placements.SingleOrDefault(x=>x.ItemId==id);
                if(desk is null || !OfficeCatalog.Get(inventory[id].Kind).Desk)throw new InvalidCommandException("Pair this chair with a placed desk.");
                var expected=ChairAt(desk,p.ItemId,p.Locked);
                if(expected!=p)throw new InvalidCommandException("Place the chair directly behind its desk.");
                if(!desks.Contains(id))desks.Add(id);else throw new InvalidCommandException("A desk has only one work chair.");
            }
            if(f.Art=="print" || f.Desk || f.Art=="plant")continue;
            var a=Access(p,f);
            // Three clear cells across the approach prevent one-cell pinch points at the seat.
            if(!reachable.Contains(a))
                throw new InvalidCommandException("Keep a clear approach to chairs and shared facilities.");
        }
        if(desks.Count>plan.Capacity)throw new InvalidCommandException("This property cannot support more workspaces.");
        return desks.OrderBy(id=>id).ToList();
    }
    public static List<OfficeCell> Path(FloorPlanDefinition plan,List<OfficePlacement> placements,IReadOnlyDictionary<int,OfficeFurniture> inventory,OfficeCell from,OfficeCell to)
    {
        var blocked=ClearanceMask(plan,Occupied(placements,inventory));blocked.Remove(from);blocked.Remove(to);
        var parent=new Dictionary<OfficeCell,OfficeCell>();var q=new Queue<OfficeCell>();q.Enqueue(from);parent[from]=from;
        while(q.TryDequeue(out var p))
        {
            if(p==to){var path=new List<OfficeCell>{to};while(path[^1]!=from)path.Add(parent[path[^1]]);path.Reverse();return path;}
            foreach(var n in Neighbours(p))if(n.X>=0&&n.Z>=0&&n.X<plan.Width&&n.Z<plan.Depth&&!blocked.Contains(n)&&!parent.ContainsKey(n)){parent[n]=p;q.Enqueue(n);}
        }
        return new();
    }
}
