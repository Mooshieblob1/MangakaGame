namespace MangakaSim.Catalog;

public sealed record RivalDefinition(string Id, string Title, string Genre, string Magazine, DateTime Start,
    DateTime? End, string Precision, string[] Sources, DateTime? Hiatus, DateTime? Breakthrough);
public sealed record IndustryDefinition(string Id, DateTime At, string Message, string Source, string Precision);
public sealed record CreatorDefinition(string Id, string Name, DateTime Start, DateTime Departure, int[] Skills);
public sealed record TimelineData(int Revision, RivalDefinition[] Rivals, IndustryDefinition[] Events, CreatorDefinition[] Creators);
public static class TimelineCatalog
{
    public static readonly DateTime Cutoff = new(2026,1,1);
    public static TimelineData Default { get; } = Load(CatalogJson.Resource("timeline.json"));
    public static TimelineData Load(string json)
    {
        var data=CatalogJson.Parse<TimelineData>(json);
        CatalogJson.Require(data.Revision==1 && data.Rivals is {Length:13} && data.Events is not null && data.Creators is {Length:2},"timeline structure");
        CatalogJson.Require(data.Rivals.All(r=>r is not null)&&data.Events.All(e=>e is not null)&&data.Creators.All(c=>c is not null)&&data.Rivals.Select(r=>r.Id).Distinct().Count()==data.Rivals.Length && data.Events.Select(e=>e.Id).Distinct().Count()==data.Events.Length,"timeline IDs");
        foreach(var r in data.Rivals)
            CatalogJson.Require(!string.IsNullOrWhiteSpace(r.Id)&&!string.IsNullOrWhiteSpace(r.Title)&&TrendCatalog.LoadDefault().Genres.Contains(r.Genre)&&
                PublisherCatalog.LoadDefault().Magazines.Any(m=>m.Id==r.Magazine)&&r.Start<Cutoff&&r.Start.Hour==0&&
                (r.End is null||r.End>r.Start&&r.End<Cutoff)&&r.Sources is {Length:>0}&&r.Sources.All(s=>Uri.TryCreate(s,UriKind.Absolute,out var u)&&u.Scheme=="https")&&
                r.Precision is "day" or "month" or "year"&&(r.Hiatus is null||r.Hiatus>r.Start&&r.Hiatus<Cutoff)&&(r.Breakthrough is null||r.Breakthrough>=r.Start&&r.Breakthrough<Cutoff),"historical rival evidence");
        foreach(var e in data.Events)CatalogJson.Require(e.At>=GameClock.Start&&e.At<Cutoff&&e.At.Hour==0&&Uri.TryCreate(e.Source,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&e.Precision is "day" or "month" or "year"&&!string.IsNullOrWhiteSpace(e.Message),"industry event");
        foreach(var c in data.Creators)CatalogJson.Require(c.Start>=GameClock.Start.Date&&c.Departure>c.Start&&c.Departure<Cutoff&&c.Skills.Length==StageOrder.All.Length&&c.Skills.All(s=>s is >=0 and <=100),"creator window");
        foreach(var magazine in PublisherCatalog.LoadDefault().Magazines)
        foreach(var date in data.Rivals.Select(r=>r.Start))
            CatalogJson.Require(data.Rivals.Count(r=>r.Magazine==magazine.Id&&r.Start<=date&&(r.End is null||r.End>date))<magazine.RosterSize-1,"historical slot capacity");
        return data;
    }
}
