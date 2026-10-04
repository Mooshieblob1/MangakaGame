namespace MangakaSim;

/// <summary>A part of the interface that stays hidden until the career reaches it (progressive disclosure spec 2026-10-02).
/// Backstop is the goals chapter index that opens it at the latest; Announcement null means no tag and no text.</summary>
public sealed record PartDefinition(string Id, string Name, int Backstop, string? Announcement, Func<GameState, bool> Moment);

public static class DisclosureCatalog
{
    public static readonly PartDefinition[] Parts =
    [
        new("books", "Books", 1, "Books is in the menu now: printing, online sales and conventions.",
            s => s.GoalDoujinFinished),
        new("quiet-speed", "32x speed", 1, null, s => s.GoalCopiesSold > 0),
        new("publishing", "Publishing", 1, "An ongoing series can go to a magazine. Publishing is open now: pitches and contracts.",
            s => s.GoalTitles.Any(t => !t.StandaloneDoujin)),
        new("contests", "Contests", 1, "Newcomer contests are open in Awards. A placing impresses editors.", s => false),
        new("staff", "Staff", 2, "Staff is in the menu now: recruitment, teams and overtime.", s => s.GoalStaff > 0),
        new("studios", "Studios", 3, "Studios is in the menu now: desks and furniture, and later bigger premises.", s => s.GoalStaff > 0 || s.GoalWorksFromStudio),
        new("industry", "Industry", 2, "Industry is in the menu now: rankings, rival studios and licence offers.",
            s => s.GoalSerializedOrPlaced || s.Progression.Awards.Any(a => a.ManuscriptId > 0 && a.ResolvedAt is not null && s.GoalTitle(a.SeriesId))),
        new("money", "Business money", 3, "Loans, card advances and incorporation are in Finances, under Funding, loans & incorporation.",
            s => s.WageArrears > 0 || s.Loans.Count > 0 || s.ControlledBusiness.Incorporated),
    ];

    public static PartDefinition Get(string id) => Parts.First(p => p.Id == id);
    public static bool Known(string id) => Parts.Any(p => p.Id == id);

    /// <summary>The part a page or workspace belongs to, or null for pages that are always there.</summary>
    public static string? PartFor(string page) => page switch
    {
        "Books" or "Print doujin" or "Sell online" or "Conventions" or "Distribution settings" => "books",
        "Publishing" => "publishing",
        "Awards" => "contests",
        // Person is left out: clicking your own character must not open Staff (final review).
        "Staff" or "Recruitment" or "Team settings" => "staff",
        "Studios" or "Studio actions" or "Tokyo map" or "Furniture" or "Career moves" or "Properties" => "studios",
        "Industry" or "Industry contacts" or "Licenses" or "Legacy" => "industry",
        // Business actions also holds salaries, so it is always there; its loans and incorporation wait for the money part.
        _ => null,
    };
}
