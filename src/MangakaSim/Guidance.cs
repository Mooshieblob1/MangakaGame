namespace MangakaSim;

public sealed class GuidancePreferences
{
    public bool Visible { get; set; } = true;
    public string Route { get; set; } = "opening";
    public int Project { get; set; }
    public HashSet<string> Completed { get; set; } = new();
}

public sealed record GuidanceStep(string Id, string Title, string Text, string Target, int Project = 0);

/// <summary>Read-only suggestions. Only presentation preferences remember observed progress.</summary>
public static class CareerGuidance
{
    public static readonly string[] Routes = ["opening", "doujin", "contest", "employment"];
    public static GuidanceStep Evaluate(GameState state, GuidancePreferences preferences)
    {
        var owned = state.Series.Where(s => s.BusinessId == state.ControlledBusinessId && !s.AwaitingCreatorDestination &&
            (state.Control == ControlMode.OwnerDirector || s.LeadPersonId == state.ProtagonistPersonId)).ToArray();
        var protagonistTitles = state.Series.Where(s => WasCreator(state,s)).ToArray();
        bool sale = protagonistTitles.Any(s => s.Volumes.Any(v => v.CopiesSold > 0)) ||
            state.Career.Sales.Any(x => protagonistTitles.Any(s => s.Id == x.Series) && x.Physical + x.Digital + x.Overseas > 0);
        bool established = protagonistTitles.Any(s => s.Chapters.Any(c => c.PublishedAt is not null)) || sale || preferences.Completed.Contains("first-sale");
        if (preferences.Route == "employment")
            return state.Control == ControlMode.OwnerDirector
                ? new("employment", "Explore studio employment", "Review the employer, pay and title rights before confirming a move. Your career follows the mangaka.", "employment")
                : new("employed", "Settle into your studio", "You already have an employer. Review your team and work schedule, or choose another guidance direction.", "staff");
        if (preferences.Route == "contest")
        {
            var manuscript = state.Progression.Manuscripts.LastOrDefault(m => !m.Released && owned.Any(s => s.Id == m.SeriesId));
            return manuscript is null
                ? new("contest-create", "Prepare an unpublished manuscript", "Create a separate contest manuscript. A doujin already collected or sold cannot enter as unpublished work.", "awards")
                : new("contest-review", "Review your contest manuscript", "Finish its ordinary production, then review eligibility, the deadline and any pending result in Awards & contests.", "awards", manuscript.SeriesId);
        }
        if (preferences.Route == "opening" && established)
            return new("direction", "Where shall we go next?", "Your career already has published work or readers. Choose doujin growth, a contest or studio employment below.", "routes");

        var project = owned.FirstOrDefault(s => s.Id == preferences.Project) ??
            owned.LastOrDefault(s => s.StandaloneDoujin) ?? owned.LastOrDefault(s => s.Publishing == PublishingStatus.Unpublished &&
                !state.Progression.Manuscripts.Any(m => m.SeriesId == s.Id && !m.Released));
        if (project is null)
            return new("create", "Make a small doujin", "Start with a 16-page one-shot: one story, one book, then stop. Ongoing series continue in short numbered issues.", "create");
        var book = project.Volumes.LastOrDefault(v => v.IsDoujin && v.BusinessId == state.ControlledBusinessId);
        if (book is null)
            return new("produce", "Finish the pages", "The planner assigns ordinary work automatically. Review the production queue and use the normal speed controls when ready.", "production", project.Id);
        if ((book.CopiesSold > 0 || state.DoujinDownloadsSold(book.Id)>0) && preferences.Route == "doujin")
            return new("grow", "Plan your next book or print run", "Compare remaining stock, sales and printing cost before ordering more. You can also start another one-shot story.", "printing", project.Id);
        if (book.CopiesSold > 0 || state.DoujinDownloadsSold(book.Id)>0)
            return new("direction", "Your first readers", "A real sale is recorded. Choose your next direction below.", "routes", project.Id);
        if(state.DoujinOnlineListed(book.Id))return new("sell-online","Find your online readers","Your download is on sale. Purchases settle on Mondays, based on quality and current genre interest. No printed stock is needed.","printing",project.Id);
        var pending = state.PrintRuns.FirstOrDefault(r => r.VolumeId == book.Id && !r.Delivered);
        if (pending is not null)
            return new("delivery", "Copies are on their way", $"Delivery is due {pending.DueAt:d MMM · HH:mm}. Keep working or use normal speed controls; no second order is needed.", "printing", project.Id);
        if (state.Stock(book.Id) == 0)
            return new("print", "Print a small batch", "Choose printing or a no-upfront-cost online release. The copy shop accepts 1–100 copies; downloads need no printed stock.", "printing", project.Id);
        return new("sell", "Find your first readers", "Stock is ready. Ordinary local sales settle on Mondays; conventions offer another route. Sales and profit are not guaranteed.", "printing", project.Id);
    }

    public static void Observe(GameState state, GuidancePreferences preferences)
    {
        // Presentation-only and idempotent; history remains authoritative and no popup backlog is created.
        var titles = state.Series.Where(s => WasCreator(state,s)).ToArray();
        if (titles.Length > 0) preferences.Completed.Add("create");
        if (titles.Any(s => s.Chapters.Any(c => c.Status == ChapterStatus.Complete))) preferences.Completed.Add("produce");
        if (titles.Any(s => s.Volumes.Any(v => state.PrintRuns.Any(r => r.VolumeId == v.Id)))) preferences.Completed.Add("print");
        if (titles.Any(s => s.Volumes.Any(v => v.CopiesSold > 0)) || state.Career.Sales.Any(x => titles.Any(s => s.Id == x.Series) && x.Physical + x.Digital + x.Overseas > 0))
            preferences.Completed.Add("first-sale");
    }
    private static bool WasCreator(GameState state,Series s) => s.LeadPersonId==state.ProtagonistPersonId || s.RightsLeadPersonId==state.ProtagonistPersonId ||
        s.Chapters.Any(c=>c.CreatorPersonId==state.ProtagonistPersonId) || s.Volumes.Any(v=>v.CreatorShares.ContainsKey(state.ProtagonistPersonId));
}
