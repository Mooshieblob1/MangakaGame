using MangakaSim;
using Xunit;

namespace MangakaSim.Tests.Rules;

public class StaffRulesTests
{
    private static readonly StaffCatalog Catalog = StaffCatalog.LoadDefault();
    private static Amenity A(string id) => Catalog.RequireAmenity(id);

    [Fact]
    public void Needs_deplete_per_hour_and_recover_on_breaks()
    {
        var fresh = new Needs();
        var regular = NeedsRules.Deplete(fresh, overtime: false, comfortRate: 3);
        Assert.Equal(95, regular.Hunger);
        Assert.Equal(94, regular.Thirst);
        Assert.Equal(97, regular.Comfort);
        var overtime = NeedsRules.Deplete(fresh, overtime: true, comfortRate: 3);
        Assert.Equal(92, overtime.Hunger);
        Assert.Equal(91, overtime.Thirst);
        Assert.Equal(94, overtime.Comfort);
        Assert.Equal(99, NeedsRules.Deplete(fresh, false, 1).Comfort);

        Assert.Equal(3, NeedsRules.ComfortRate(Array.Empty<Amenity>()));
        Assert.Equal(3, NeedsRules.ComfortRate(new[] { A("folding-chairs") }));
        Assert.Equal(2, NeedsRules.ComfortRate(new[] { A("sofa") }));
        Assert.Equal(1, NeedsRules.ComfortRate(new[] { A("office-chairs"), A("sofa") }));

        Assert.False(NeedsRules.NeedsBreak(new Needs { Hunger = 25 }));
        Assert.True(NeedsRules.NeedsBreak(new Needs { Thirst = 24.9 }));
        Assert.Equal("thirst", new Needs { Hunger = 30, Thirst = 20, Comfort = 25 }.Lowest);

        var hungry = new Needs { Hunger = 20, Thirst = 50, Comfort = 95 };
        var afterBreak = NeedsRules.Recover(hungry, "hunger", NeedsRules.Recovery("hunger", new[] { A("fridge") }));
        Assert.Equal(90, afterBreak.Hunger);
        Assert.Equal(60, afterBreak.Thirst);
        Assert.Equal(100, afterBreak.Comfort);
        Assert.Equal(20, NeedsRules.Recovery("hunger", Array.Empty<Amenity>()));
        Assert.Equal(70, NeedsRules.Recovery("thirst", new[] { A("kettle"), A("water-cooler") }));
    }

    [Fact]
    public void Happiness_equilibrium_and_drift()
    {
        Assert.Equal(50, HappinessRules.Equilibrium(1, 0, 0, 0, 0), 9);
        Assert.Equal(62, HappinessRules.Equilibrium(1.4, 0, 0, 0, 0), 9);
        Assert.Equal(38, HappinessRules.Equilibrium(0.6, 0, 0, 0, 0), 9);
        Assert.Equal(56, HappinessRules.Equilibrium(1, 12, 0, 0, 0), 9);
        Assert.Equal(47, HappinessRules.Equilibrium(1, 0, 0.2, 0, 0), 9);
        Assert.Equal(40, HappinessRules.Equilibrium(1, 0, 0, 1, 0), 9);
        Assert.Equal(60, HappinessRules.Equilibrium(1, 0, 0, 0, 80), 9);
        Assert.Equal(23, HappinessRules.Step(20, 50), 9);
        Assert.Equal(0, HappinessRules.Equilibrium(0.6, -40, 1, 3, 0), 9);

        var garage = Catalog.RequirePremises("garage");
        Assert.Equal(-10, HappinessRules.Atmosphere(garage, Array.Empty<Amenity>(), 1, 2), 9);
        Assert.Equal(-10 + 2 + 4 - 8, HappinessRules.Atmosphere(garage, new[] { A("fridge"), A("office-chairs") }, 3, 2), 9);
    }

    [Fact]
    public void Moonlighting_and_quitting_chances()
    {
        Assert.Equal(0.15, MoonlightRules.StartChance(39), 9);
        Assert.Equal(0, MoonlightRules.StartChance(40), 9);
        Assert.True(MoonlightRules.Stops(55));
        Assert.False(MoonlightRules.Stops(54.9));
        Assert.Equal(0.2, MoonlightRules.QuitChance(10, 0), 9);
        Assert.Equal(0.06, MoonlightRules.QuitChance(10, 20), 9);
        Assert.Equal(0.1, MoonlightRules.QuitChance(20, 0), 9);
        Assert.Equal(0, MoonlightRules.QuitChance(30, 0), 9);
    }

    [Fact]
    public void Pay_rules()
    {
        Assert.Equal(220_000, PayRules.MarketSalary(new[] { 50, 50, 50, 50, 50 }, 1.0));
        Assert.Equal(280_000, PayRules.MarketSalary(new[] { 80, 80, 80, 80, 80 }, 1.0));
        Assert.Equal(330_000, PayRules.MarketSalary(new[] { 80, 80, 80, 80, 80 }, 1.18)); // 330,400 to the nearest 1,000
        Assert.Equal(1.4, PayRules.PayFactor(500_000, 220_000), 9);
        Assert.Equal(0.6, PayRules.PayFactor(1, 220_000), 9);
        Assert.Equal(1.0, PayRules.PayFactor(220_000, 220_000), 9);
        var rng = Rng.FromSeed(3);
        for (var i = 0; i < 200; i++)
        {
            var asking = PayRules.AskingSalary(220_000, rng);
            Assert.InRange(asking, 198_000, 253_000);
            Assert.Equal(0, asking % 1000);
        }
        Assert.True(PayRules.Accepts(176_000, 220_000));
        Assert.False(PayRules.Accepts(175_000, 220_000));
    }

    [Fact]
    public void Hiring_rules()
    {
        var rng = Rng.FromSeed(5);
        for (var i = 0; i < 100; i++)
        {
            Assert.InRange(HiringRules.Level(0, rng), 25, 45);
            Assert.InRange(HiringRules.Level(100, rng), 50, 70);
            var skills = HiringRules.Skills(40, rng);
            Assert.Equal(5, skills.Count);
            Assert.All(skills.Values, v => Assert.InRange(v, 5, 95));
            Assert.InRange(skills[Stage.Name], 15, 45);
            Assert.InRange(skills[Stage.Inks], 25, 55);
        }
        Assert.Equal(60, HiringRules.StartingHappiness(220_000, 220_000), 9);
        Assert.Equal(56, HiringRules.StartingHappiness(176_000, 220_000), 9);
        Assert.Equal(70, HiringRules.StartingHappiness(330_000, 220_000), 9);
    }

    [Fact]
    public void Assignment_score_prefers_the_free_person()
    {
        Assert.Equal(0.64, AssignmentRules.Score(1.6, 30), 9);
        Assert.Equal(1.2, AssignmentRules.Score(1.2, 0), 9);
        Assert.Equal(2, AssignmentRules.Best(new[] { (1, 0.64), (2, 1.2) }));
        Assert.Equal(1, AssignmentRules.Best(new[] { (2, 1.0), (1, 1.0) })); // ties to the lower id
        Assert.Null(AssignmentRules.Best(Array.Empty<(int, double)>()));
    }

    [Fact]
    public void Premises_rules()
    {
        Assert.Equal(250_000, PremisesRules.MoveCost(Catalog.RequirePremises("office"), 1.0));
        Assert.Equal(0, PremisesRules.MoveCost(Catalog.RequirePremises("garage"), 1.18));
        var charge = PremisesRules.Charge(Catalog.RequirePremises("apartment"), new[] { A("fridge"), A("kettle") }, online: true, priceIndex: 1.0);
        Assert.Equal(80_000, charge.Rent);
        Assert.Equal(4_500, charge.Upkeep);
        Assert.Equal(3_000, charge.Provider);
        Assert.Equal(87_500, charge.Total);
        Assert.Equal(0, PremisesRules.Charge(Catalog.RequirePremises("garage"), Array.Empty<Amenity>(), false, 1.0).Total);
    }

    [Fact]
    public void Promotion_fans_per_hour()
    {
        Assert.Equal(0.3, PromotionRules.FansPerHour(50, 0.1), 9);
        Assert.Equal(3.6, PromotionRules.FansPerHour(80, 1.0), 9);
    }
}
