namespace MangakaSim;

// Fictional content and balancing values; not contemporary Japanese contract rates.
public record PartnerProfile(string Name, string Specialty, double Reliability);
// Name and Requirement are also the Steam display name and description (Q69); the ten from 2026-09-26 come first.
public record AchievementDefinition(string Key, string ApiName, string Name, string Requirement);
public static class ProgressionCatalog
{
    public const int Version = 1;
    public static readonly PartnerProfile[] AnimationPartners =
    [new("Paper Lantern Animation", "drama", .88), new("Blue Kite Pictures", "action", .78), new("Clover Frame", "comedy", .82)];
    public static readonly PartnerProfile[] GoodsPartners =
    [new("Momo Goods", "romance", .85), new("Paper Street Licensing", "drama", .88), new("Little Star Collectibles", "fantasy", .76)];
    public static readonly AchievementDefinition[] Achievements =
    [
        new("first_publication", "MKG_FIRST_PUBLICATION", "In Print", "Publish a chapter or release a book."),
        new("contest_placement", "MKG_CONTEST_PLACEMENT", "Judges Noticed", "Place in a newcomer contest."),
        new("annual_award", "MKG_ANNUAL_AWARD", "Manga Craft Award", "Win the annual Manga Craft Award."),
        new("anime_release", "MKG_ANIME_RELEASE", "On Screen", "Release a first anime adaptation."),
        new("anime_followup", "MKG_ANIME_FOLLOWUP", "Second Season", "Release a follow-up anime with reception of 75 or more."),
        new("merchandise_license", "MKG_MERCHANDISE", "In Their Hands", "Release licensed merchandise for one of your series."),
        new("readers_10000", "MKG_READERS_10000", "A Real Hit", "Reach 10,000 readers on one of your series."),
        new("readers_million", "MKG_READERS_MILLION", "National Hit", "Reach 1,000,000 readers on one of your series."),
        new("iconic_series", "MKG_ICONIC_SERIES", "Manga History", "Make one of your series iconic."),
        new("sustainable_studio", "MKG_SUSTAINABLE_STUDIO", "Books Balanced", "Earn over ¥1,000,000 operating profit in a year with every bill and wage paid on time."),
        new("first_sale", "MKG_FIRST_SALE", "First Reader", "Sell your first copy."),
        new("first_convention", "MKG_FIRST_CONVENTION", "Behind the Table", "Sell copies at a convention."),
        new("serialization", "MKG_SERIALIZATION", "Serialized", "Accept a magazine serialization offer."),
        new("ranking_top3", "MKG_RANKING_TOP3", "Top Three", "Reach the top three in a magazine ranking."),
        new("deadline_streak", "MKG_DEADLINE_STREAK", "Never Late", "Deliver ten magazine chapters in a row without a missed deadline or issue."),
        new("comeback", "MKG_COMEBACK", "Comeback", "Win a new serialization after a series is cancelled."),
        new("first_hire", "MKG_FIRST_HIRE", "First Assistant", "Hire your first assistant."),
        new("full_team", "MKG_FULL_TEAM", "Full Team", "Employ four assistants at once."),
        new("move_out", "MKG_MOVE_OUT", "A Studio of My Own", "Move into a studio outside the family home."),
        new("incorporate", "MKG_INCORPORATE", "Incorporated", "Incorporate your business."),
        new("second_studio", "MKG_SECOND_STUDIO", "Two Studios", "Run two studios at once."),
        new("copies_100k", "MKG_COPIES_100K", "100,000 Copies", "Sell 100,000 copies of one series."),
        new("copies_1m", "MKG_COPIES_1M", "A Million Copies", "Sell 1,000,000 copies across your career."),
        new("overseas_deal", "MKG_OVERSEAS_DEAL", "Beyond the Shop", "Sign a digital or overseas deal."),
        new("ten_years", "MKG_TEN_YEARS", "Ten Years at the Desk", "Still creating manga ten years after your start date.")
    ];
}
