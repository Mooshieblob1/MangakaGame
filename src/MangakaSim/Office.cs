using System.Text.Json.Serialization;

namespace MangakaSim;

public enum FurnitureOwner { Business, Family, Landlord }
public enum OfficeActivityKind { Idle, Work, Break, Waiting, Mentoring, Promotion, Recovery, Travel, Convention, Relocating, OffDuty, OutsideJob }
public sealed record AppearanceRecipe(int Skin = 0, int Hair = 0, int Outfit = 0, int Style = 0, bool Glasses = false, int Build = 0, int Wardrobe = 0)
{
    public bool Valid => Skin is >=0 and <4 && Hair is >=0 and <4 && Outfit is >=0 and <6 && Style is >=0 and <3 && Build is >=0 and <3 && Wardrobe is >=0 and <3;
    public static AppearanceRecipe Generate(int seed, int id)
    {
        uint n=unchecked((uint)(seed*397)^((uint)id*2654435761u)^0x415254u);
        int Pick(int max){n^=n<<13;n^=n>>17;n^=n<<5;return (int)(n%(uint)max);}
        return new(Pick(4),Pick(4),Pick(6),Pick(3),Pick(4)==0,Pick(3),Pick(3));
    }
}
public sealed partial class StudioLocation
{
    public int PropertyOfferId { get; set; }
    public string FloorPlanId { get; set; } = "";
    public int OfficeRevision { get; set; }
    public int BaseAtmosphere { get; set; }
    public int FixedBreakSeats { get; set; } = 2;
    /// <summary>The parents' home uses the studio island (Q59, 2026-10-03). False in older saves until EnsureStudioIsland.</summary>
    public bool StudioIsland { get; set; }
}
public partial class Person { public AppearanceRecipe? Appearance { get; set; } }
// Candidate was originally a non-partial class; its appearance is an independent stable recipe at hire time.
public sealed class OfficeFurniture
{
    [JsonRequired] public int Id { get; set; }
    [JsonRequired] public string Kind { get; set; } = "desk";
    [JsonRequired] public FurnitureOwner Owner { get; set; }
    [JsonRequired] public int BusinessId { get; set; }
    [JsonRequired] public string SiteKey { get; set; } = "";
    [JsonRequired] public long Paid { get; set; }
    [JsonRequired] public bool Sold { get; set; }
}
public sealed record OfficePlacement(int ItemId, int X, int Z, int Rotation = 0, bool Locked = false, int? DeskId = null);
public sealed record DeskAssignment(int PersonId, int DeskId);
public sealed class OfficeLayout
{
    [JsonRequired] public int LocationId { get; set; }
    [JsonRequired] public List<OfficePlacement> Placements { get; set; } = new();
    [JsonRequired] public List<DeskAssignment> Assignments { get; set; } = new();
}
public sealed record OfficeActivity(int PersonId, int LocationId, OfficeActivityKind Kind, DateTime At, Stage? Stage = null, int? PartnerId = null, int? FacilityId = null, int? SeatIndex = null);
public sealed record FurnitureDefinition(string Id, string Name, int Width, int Depth, long Price, string Art, int Variant = 0, bool RewardOnly = false)
{
    public bool Desk => Art == "desk";
    public bool Chair => Art == "chair";
    public bool Break => Art == "break";
}
public readonly record struct OfficeCell(int X, int Z);
public sealed record FloorPlanDefinition(string Id, int Width, int Depth, int Capacity, int Variation, bool Home, bool Employer, bool Island = false)
{
    /// <summary>Studio island: Helper-Chan's desk (quarter turn, facing the spare desk) and her chair. Presentation only, but kept clear.</summary>
    public static readonly OfficePlacement IslandHelperDesk = new(0, 2, 2, 1);
    public OfficeCell Entrance => new(Width/2,Depth-2);
    public bool HelperReserved(int x,int z) => Island && (x is >=2 and <=4 && z is >=2 and <=6 || x is >=0 and <=1 && z is >=3 and <=5);
    public bool Protected(int x,int z) => z>=Depth-2 || x>=Width-5 && z>=Depth-6 || HelperReserved(x,z);
    public IEnumerable<(int X,int Z,int Rotation)> DeskSlots
    {
        get
        {
            // Aki heads the island facing down it; the spare desk faces Helper-Chan across it.
            if(Island&&Capacity==2){yield return (3,7,0);yield return (5,2,3);yield break;}
            var columns=Capacity<=4?2:Capacity<=16?4:6;
            for(var row=0;row<(Capacity+columns-1)/columns;row++)
            for(var col=0;col<columns;col++)
                if(row*columns+col<Capacity)yield return (2+col*(Capacity==2?7:9),2+row*9,0);
        }
    }
}
public static class OfficeCatalog
{
    public static readonly FurnitureDefinition[] Furniture =
    [
        new("desk","Drawing desk",5,3,8000,"desk"),new("desk-better","Better drawing desk",5,3,16000,"desk",1),
        new("desk-studio","Professional drawing desk",5,3,24000,"desk",1),
        new("desk-digital","Digital drawing workstation (digital era, reputation 40)",5,3,40000,"desk",1),
        new("chair","Work chair",3,3,2000,"chair"),new("chair-support","Supportive chair",3,3,8000,"chair",1),
        new("break","Two-seat break set",6,6,10000,"break"),new("break-comfort","Comfortable break set",6,6,18000,"break",1),
        new("cabinet","Tea and food cabinet",3,2,6000,"cabinet"),new("shelf","Reference shelf",4,2,4000,"shelf"),
        new("plant","Plant",2,2,1000,"plant"),new("print","Wall print",4,1,1000,"print"),
        // Goal rewards (spec 2026-10-01): given, never bought, and kept last so the buy list keeps its indexes.
        new("trophy-shelf","Trophy shelf",4,2,6000,"trophy-shelf",RewardOnly:true),
        new("framed-letter","Framed editor's letter",2,1,1000,"framed-letter",RewardOnly:true),
        new("ranking-chart","Ranking chart",3,1,1000,"ranking-chart",RewardOnly:true),
        new("award-plaque","Award plaque",2,1,3000,"award-plaque",RewardOnly:true),
        new("plant-set","Housewarming plants",4,2,3000,"plant-set",RewardOnly:true),
        new("company-sign","Company sign",4,1,5000,"company-sign",RewardOnly:true),
        new("gold-frame","Gold frame",2,1,5000,"gold-frame",RewardOnly:true),
        new("display-cabinet","Award display cabinet",4,2,20000,"display-cabinet",RewardOnly:true),
        new("trophy","Trophy",1,1,10000,"trophy",RewardOnly:true)
    ];
    public static FurnitureDefinition Get(string id) => Furniture.FirstOrDefault(f=>f.Id==id) ?? throw new InvalidCommandException("Unknown furniture type.");
    public static FloorPlanDefinition Plan(StudioLocation l)
    {
        var seats=l.Seats;
        var variation=Math.Max(0,l.PropertyOfferId-1)%4;
        var size=seats<=2?(16,16):seats<=4?new[]{(20,28),(22,24),(20,26),(22,25)}[variation]:seats<=8?new[]{(38,26),(38,24),(36,28),(36,26)}[variation]:seats<=16?new[]{(40,44),(42,44),(40,42),(42,42)}[variation]:new[]{(58,58),(60,56),(58,56),(56,58)}[variation];
        return new(l.FloorPlanId,size.Item1,size.Item2,seats,l.PropertyOfferId%4,l.IsFamilyHome,l.FloorPlanId.StartsWith("employer-",StringComparison.Ordinal),l.IsFamilyHome&&l.StudioIsland);
    }
}
public sealed record ApplyOfficeLayoutCommand(int LocationId, int Revision, List<OfficePlacement> Placements,
    List<OfficePurchase> Purchases, List<int> Sell, List<DeskAssignment>? Assignments = null) : ICommand;
public sealed record OfficePurchase(int TemporaryId, string Kind);
public sealed record SetAppearanceCommand(AppearanceRecipe Appearance) : ICommand;
public sealed record AssignDeskCommand(int LocationId, int PersonId, int DeskId) : ICommand;
public sealed record RelocateOfficeCommand(int OfferId, int Revision, ApplyOfficeLayoutCommand Layout) : ICommand;
public sealed record OfficeQuote(long Purchases, long Sales, int Workspaces, int Stored, string Description);
public sealed record OfficeArrangement(List<OfficePlacement> Placements, List<OfficePurchase> Purchases, List<int> Stored, string Explanation);
