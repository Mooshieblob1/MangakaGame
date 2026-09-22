using System.Text.Json.Nodes;
using MangakaSim;
using Xunit;

namespace MangakaSim.Tests;

public class StaffCatalogTests
{
    private static string Json() =>
        new StreamReader(typeof(GameState).Assembly.GetManifestResourceStream("MangakaSim.Data.staff.json")!).ReadToEnd();

    [Fact]
    public void Default_catalog_matches_the_spec_tables()
    {
        var catalog = StaffCatalog.LoadDefault();
        Assert.Equal(new[] { "garage", "apartment", "office" }, catalog.Premises.Select(p => p.Id));
        Assert.Equal(2, catalog.RequirePremises("garage").Capacity);
        Assert.Equal(0, catalog.RequirePremises("garage").MonthlyRent);
        Assert.Equal(-10, catalog.RequirePremises("garage").Atmosphere);
        Assert.Equal(80_000, catalog.RequirePremises("apartment").MonthlyRent);
        Assert.Equal(8, catalog.RequirePremises("office").Capacity);
        Assert.Equal(9, catalog.Amenities.Count);
        var fridge = catalog.RequireAmenity("fridge");
        Assert.Equal("hunger", fridge.Need);
        Assert.Equal(90_000, fridge.Cost);
        Assert.Equal(4_000, fridge.MonthlyUpkeep);
        Assert.Equal(70, fridge.RecoveryPerBreak);
        Assert.Equal("none", catalog.RequireAmenity("radio").Need);
        Assert.Equal(2, catalog.ScheduledCandidates.Count);
        var oga = catalog.ScheduledCandidates[0];
        Assert.Equal("Eiichido Oga", oga.Name);
        Assert.Equal(new DateTime(1997, 7, 1), oga.AppearsAt);
        Assert.Equal(75, oga.Skills[Stage.Inks]);
        Assert.Equal(1.2, catalog.ScheduledCandidates[1].AskingMultiplier);
        Assert.True(catalog.GivenNames.Count >= 60);
        Assert.True(catalog.FamilyNames.Count >= 60);
        Assert.Contains("Tokiyama", catalog.FamilyNames);
        Assert.Null(catalog.FindPremises("penthouse"));
        Assert.Throws<InvalidDataException>(() => catalog.RequireAmenity("jacuzzi"));
        Assert.Same(catalog, StaffCatalog.LoadDefault());
    }

    [Theory]
    [InlineData("premises", 1, "id", "garage", "duplicate")]
    [InlineData("premises", 0, "capacity", 0, "capacity")]
    [InlineData("premises", 1, "monthlyRent", -1, "negative")]
    [InlineData("amenities", 0, "need", "sleep", "need")]
    [InlineData("amenities", 0, "recoveryPerBreak", 101, "recovery")]
    [InlineData("amenities", 1, "id", "kettle", "duplicate")]
    [InlineData("scheduledCandidates", 0, "monthsAvailable", 0, "available")]
    public void Validation_rejects_bad_entries(string list, int index, string field, object value, string expected)
    {
        var json = JsonNode.Parse(Json())!;
        json[list]![index]![field] = JsonValue.Create(value);
        var ex = Assert.Throws<InvalidDataException>(() => StaffCatalog.FromJson(json.ToJsonString()));
        Assert.Contains(expected, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validation_rejects_short_name_pools_missing_garage_and_bad_skills()
    {
        var json = JsonNode.Parse(Json())!;
        json["givenNames"] = new JsonArray(JsonValue.Create("Aki"));
        Assert.Contains("given names", Assert.Throws<InvalidDataException>(() => StaffCatalog.FromJson(json.ToJsonString())).Message);

        json = JsonNode.Parse(Json())!;
        json["premises"]![0]!["id"] = "shed";
        Assert.Contains("garage", Assert.Throws<InvalidDataException>(() => StaffCatalog.FromJson(json.ToJsonString())).Message);

        json = JsonNode.Parse(Json())!;
        json["scheduledCandidates"]![0]!["skills"]!["Inks"] = 120;
        Assert.Contains("skill", Assert.Throws<InvalidDataException>(() => StaffCatalog.FromJson(json.ToJsonString())).Message);
    }
}
