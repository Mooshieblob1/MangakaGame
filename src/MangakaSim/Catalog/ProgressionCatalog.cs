namespace MangakaSim;

// Fictional content and balancing values; not contemporary Japanese contract rates.
public record PartnerProfile(string Name, string Specialty, double Reliability);
public record AchievementDefinition(string Key, string ApiName, string Requirement);
public static class ProgressionCatalog
{
    public const int Version = 1;
    public static readonly PartnerProfile[] AnimationPartners =
    [new("Paper Lantern Animation", "drama", .88), new("Blue Kite Pictures", "action", .78), new("Clover Frame", "comedy", .82)];
    public static readonly PartnerProfile[] GoodsPartners =
    [new("Momo Goods", "romance", .85), new("Paper Street Licensing", "drama", .88), new("Little Star Collectibles", "fantasy", .76)];
    public static readonly AchievementDefinition[] Achievements =
    [
        new("first_publication", "MKG_FIRST_PUBLICATION", "Publish a manga chapter or release a volume."),
        new("contest_placement", "MKG_CONTEST_PLACEMENT", "Earn a paid newcomer-contest placement."),
        new("annual_award", "MKG_ANNUAL_AWARD", "Win the annual Manga Craft Award."),
        new("anime_release", "MKG_ANIME_RELEASE", "Release a first anime adaptation."),
        new("anime_followup", "MKG_ANIME_FOLLOWUP", "Release a follow-up anime with reception at least 75."),
        new("merchandise_license", "MKG_MERCHANDISE", "Release licensed merchandise and receive the release payment."),
        new("readers_10000", "MKG_READERS_10000", "Reach 10,000 readers on a protagonist-led title."),
        new("readers_million", "MKG_READERS_MILLION", "Reach 1,000,000 readers on a protagonist-led title."),
        new("iconic_series", "MKG_ICONIC_SERIES", "Earn iconic status through the existing impact and readership rules."),
        new("sustainable_studio", "MKG_SUSTAINABLE_STUDIO", "Earn more than one million yen operating profit over 365 days, with no overdue bills or unpaid wages.")
    ];
}
