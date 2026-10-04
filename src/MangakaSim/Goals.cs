namespace MangakaSim;

/// <summary>How far a goal has come, its target and the words the board shows (career goals spec 2026-10-01).</summary>
public sealed record GoalProgress(double Have, double Need, string Text)
{
    public bool Done => Have >= Need;
    public double Fraction => Need <= 0 ? 1 : Math.Clamp(Have / Need, 0, 1);
}

/// <summary>A one-off reward (Q49): cash, fans, an item, an unlock, an opportunity or a scene. Never a permanent bonus.</summary>
public sealed record GoalReward(long Cash = 0, int Fans = 0, string? Furniture = null, string? Unlock = null,
    bool FreeConventionTable = false, bool PitchBoost = false, string? Scene = null)
{
    public string Describe()
    {
        var parts = new List<string>();
        if (Cash > 0) parts.Add($"¥{Cash:N0}");
        if (Fans > 0) parts.Add($"{Fans:N0} fans for your newest series");
        if (Furniture is { } item) parts.Add("a " + OfficeCatalog.Get(item).Name.ToLowerInvariant());
        if (Unlock is { } unlock) parts.Add($"the {OfficeCatalog.Get(unlock).Name.ToLowerInvariant()} unlocked");
        if (FreeConventionTable) parts.Add("a free convention table");
        if (PitchBoost) parts.Add("a better chance on your next pitch");
        if (Scene is not null) parts.Add("a scene with Helper-Chan");
        return string.Join(", ", parts);
    }
}

public sealed record GoalDefinition(string Id, int Chapter, string Title, string Why, string Target, string Tip, GoalReward Reward, Func<GameState, GoalProgress> Measure);
public sealed record GoalChapter(int Index, string Name, string Blurb, GoalReward Reward);

/// <summary>Five career chapters, then optional mastery goals (Q50). Targets are Helper-Chan's guidance routes.</summary>
public static class GoalCatalog
{
    private static readonly GoalReward Trophy = new(Furniture: "trophy");

    public static readonly GoalChapter[] Chapters =
    [
        new(0, "Doujin Days", "Your parents' house, your first pages and your first readers.", new(Furniture: "trophy-shelf", FreeConventionTable: true, Scene: "goal-doujin-days")),
        new(1, "Rookie", "Getting noticed: editors, contests and a first chance in a magazine.", new(Unlock: "desk-studio", PitchBoost: true, Scene: "goal-rookie")),
        new(2, "Serialized", "Real deadlines, rankings and your first collected volume.", new(Cash: 100_000, Furniture: "award-plaque", Scene: "goal-serialized")),
        new(3, "Studio Head", "Your own studio, a team and a company.", new(Cash: 300_000, Furniture: "display-cabinet", Scene: "goal-studio-head")),
        new(4, "Legend", "The milestones that put a mangaka in manga history.", new(Scene: "goal-legend")),
        new(5, "Mastery", "Optional goals for a long career. Each gives a trophy.", new()),
    ];

    public static readonly GoalDefinition[] Goals =
    [
        new("doujin-finished", 0, "Finish your first doujin", "A finished book is something you can print, sell and pitch.", "production",
            "Pages move through each stage by themselves while time runs. Speed up time and watch Production.", new(Cash: 5_000),
            s => GameState.GoalFlag(s.GoalDoujinFinished, "Not finished yet")),
        new("first-copy", 0, "Sell your first copy", "Your first reader! Every sale also brings fans.", "books",
            "Print ten copies at the copy shop, or list the book online for free. Copies sell through shop hours once they arrive.", new(Furniture: "print"),
            s => GameState.GoalCount(s.GoalCopiesSold, 1)),
        new("convention", 0, "Attend a convention", "Conventions sell printed copies fast and bring new fans.", "conventions",
            "Book a table on the Conventions page and bring printed copies. You sell them on the day and meet new readers.", new(Cash: 10_000),
            s => GameState.GoalCount(s.GoalConventions, 1)),
        new("fans-100", 0, "Reach 100 fans", "Fans buy your next book and impress editors.", "books",
            "Fans grow with every copy sold and every convention. Good quality and a popular genre help most.", new(Furniture: "plant"),
            s => GameState.GoalCount(s.GoalFans, 100)),
        new("sales-50000", 0, "Earn ¥50,000 from your own sales", "Your own sales fund printing, conventions and your first hire.", "books",
            "Reprint books that sell out and keep an online listing going. Every copy adds up.", new(Cash: 10_000),
            s => GameState.GoalYen(s.GoalOwnSales, 50_000)),

        new("pitch-or-contest", 1, "Pitch a magazine or enter a contest", "Magazines bring page fees and many more readers.", "publishing",
            "Make a doujin an ongoing series in its Series details, then pitch it on Publishing. Or enter a contest in Awards.", new(Cash: 10_000),
            s => GameState.GoalFlag(s.GoalPitchedOrEntered, "Not yet")),
        new("editor-verdict", 1, "Hear an editor's verdict", "Every answer teaches you something, even a no.", "publishing",
            "Editors answer a pitch within a few weeks. Keep drawing meanwhile; a rejection says what to improve.", new(Furniture: "framed-letter"),
            s => GameState.GoalFlag(s.GoalVerdict, "Waiting for a pitch")),
        new("serialization-or-placement", 1, "Win a serialization or place in a contest", "This is your start as a professional.", "publishing",
            "A strong sample in a fitting magazine has the best chance. Contest placings count too, and impress editors.", new(Cash: 30_000),
            s => GameState.GoalFlag(s.GoalSerializedOrPlaced, "Not yet")),
        new("fans-1000", 1, "Reach 1,000 fans", "A bigger audience makes every pitch and book easier.", "books",
            "Keep books in print and online, and go to conventions. A serialization grows fans fastest.", new(Fans: 200),
            s => GameState.GoalCount(s.GoalFans, 1_000)),

        new("chapters-10", 2, "Publish 10 magazine chapters", "Steady chapters keep your series safe in the rankings.", "production",
            "Deliver each chapter by its issue close. Keep a chapter or two ready ahead for slow weeks.", new(Cash: 30_000),
            s => GameState.GoalCount(s.GoalMagazineChapters, 10)),
        new("top-10", 2, "Reach the top 10", "A top-10 series sits well clear of the cancellation line.", "production",
            "Quality and steady chapters lift your ranking. A genre in fashion helps too.", new(Furniture: "ranking-chart"),
            s => s.GoalTopRank(10)),
        new("first-volume", 2, "Release your first collected volume", "Collected volumes earn royalties for years.", "production",
            "Every five magazine chapters make a collected volume. Keep the chapters coming and it follows.", new(Cash: 50_000),
            s => GameState.GoalFlag(s.GoalCollectedVolume, "Not yet")),
        new("first-hire", 2, "Hire your first assistant", "An assistant takes Backgrounds and Tones so you keep up.", "recruitment",
            "Hire once funds cover about three months of wages. The runway line shows what each wage does.", new(Furniture: "chair-support"),
            s => GameState.GoalCount(s.GoalStaff, 1)),
        new("readers-10000", 2, "Reach 10,000 readers", "Ten thousand readers is a real hit.", "production",
            "Steady quality and a long run grow readers. Rankings and collected volumes bring more.", new(Fans: 1_000),
            s => s.GoalMilestoneOr("readers_10000", GameState.GoalCount(s.GoalTopReaders, 10_000))),

        new("moved-out", 3, "Move out of your parents' house", "Your own studio has room for a team.", "studios",
            "Studios on the Studios page show rent and desks. Move once income covers the rent with room to spare.", new(Furniture: "plant-set"),
            s => GameState.GoalFlag(s.GoalWorksFromStudio, "Still at home")),
        new("staff-3", 3, "Employ 3 staff", "A team can run two series.", "recruitment",
            "Each hire needs a desk and wages. Check the runway before every offer.", new(Cash: 50_000),
            s => GameState.GoalCount(s.GoalStaff, 3)),
        new("incorporated", 3, "Incorporate", "A company can borrow and grow.", "business",
            "Incorporation costs ¥200,000 and opens business credit after 90 days. Find it in Finances.", new(Furniture: "company-sign"),
            s => GameState.GoalFlag(s.ControlledBusiness.Incorporated, "Not yet")),
        new("two-series", 3, "Run two series at once", "Two series double your chances in the rankings.", "series",
            "Start a second ongoing series once your team has spare desks. Assign staff to each title.", new(Cash: 80_000),
            s => GameState.GoalCount(s.GoalActiveTitles, 2)),
        new("award-shortlist", 3, "Be shortlisted for the Manga Craft Award", "A shortlist brings readers and respect.", "awards",
            "Each January the award shortlists the year's best work. High quality chapters are what count.", new(Furniture: "gold-frame"),
            s => GameState.GoalFlag(s.GoalShortlisted, "Not yet")),

        new("anime", 4, "An anime adaptation", "Anime brings readers who never read manga.", "licenses",
            "Licence offers come to popular series. Compare fit and terms on the Licenses page.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("anime_release"), "Not yet")),
        new("merchandise", 4, "A merchandise deal", "Merchandise keeps your characters in people's hands.", "licenses",
            "Merchandise offers follow popular series too. A steady fanbase makes them likelier.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("merchandise_license"), "Not yet")),
        new("readers-million", 4, "A million readers", "A million readers is a national hit.", "production",
            "Long runs, collected volumes and adaptations all grow readers.", Trophy,
            s => s.GoalMilestoneOr("readers_million", GameState.GoalCount(s.GoalTopReaders, 1_000_000))),
        new("iconic", 4, "An iconic series", "An iconic series is part of manga history.", "production",
            "Iconic series combine a huge readership with lasting cultural impact. Awards and anime help.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("iconic_series"), "Not yet")),
        new("sustainable", 4, "A sustainable studio year", "A profitable year with every bill paid.", "business",
            "Keep costs below income for a full year and pay every bill and loan on time.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("sustainable_studio"), "Not yet")),

        new("annual-award", 5, "Win the Manga Craft Award", "The highest honour of the year.", "awards",
            "The award goes to the year's best work. Top quality chapters all year give you the best chance.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("annual_award"), "Not yet")),
        new("anime-followup", 5, "A second anime season", "A second season means the first one worked.", "licenses",
            "A successful first anime brings follow-up offers. Keep the series strong meanwhile.", Trophy,
            s => GameState.GoalFlag(s.GoalMilestone("anime_followup"), "Not yet")),
        new("five-books", 5, "Release five books", "A shelf of your own books.", "production",
            "Doujin and collected volumes both count. Keep finishing work and it adds up.", Trophy,
            s => GameState.GoalCount(s.GoalReleasedBooks, 5)),
    ];

    public static IEnumerable<GoalDefinition> In(int chapter) => Goals.Where(g => g.Chapter == chapter);
    public static GoalDefinition Get(string id) => Goals.First(g => g.Id == id);
    /// <summary>The chapter whose reward unlocks a furniture kind, or null.</summary>
    public static string? UnlockedBy(string kind) => Chapters.FirstOrDefault(c => c.Reward.Unlock == kind)?.Name;
}
