using System.Text.Json.Serialization;

namespace MangakaSim;

public sealed record StoryCommand(string Scene, int Answer = -1, bool Defer = false) : ICommand;
public sealed record StoryEntry(string Scene, int Answer, DateTime At, string Text);
public sealed record StoryScene(string Id, string Title, string Text, string First, string Second, string Expression = "neutral", string? Illustration = null);
public sealed record SalesSample(DateTime At, int Business, int Series, int Volume, long Physical, long Digital, long Overseas);
public sealed record RankingSample(DateTime At, string Magazine, List<RankEntry> Rows);
public sealed class CareerHistory
{
    [JsonRequired] public DateTime AvailableFrom { get; set; } = GameClock.Start;
    [JsonRequired] public DateTime LastCheck { get; set; } = GameClock.Start;
    [JsonRequired] public DateTime NextOffer { get; set; } = GameClock.Start;
    [JsonRequired] public Rng NarrativeRng { get; set; } = Rng.FromSeed(1);
    [JsonRequired] public string? PendingScene { get; set; }
    [JsonRequired] public DateTime? DeferredUntil { get; set; }
    [JsonRequired] public int LastBusiness { get; set; }
    [JsonRequired] public DateTime? MovedAt { get; set; }
    [JsonRequired] public List<StoryEntry> Journal { get; set; } = new();
    [JsonRequired] public List<SalesSample> Sales { get; set; } = new();
    [JsonRequired] public List<RankingSample> Rankings { get; set; } = new();
}

public static class HelperStories
{
    public static readonly string[] Arc = ["beside", "page", "reader", "same", "ordinary", "still"];
    public static readonly string[] Everyday = ["margin", "shelf", "tea", "migration", "desk_moment"];
    public static bool Known(string id) => Arc.Contains(id) || Everyday.Contains(id);
    public static StoryScene Describe(GameState state, string id)
    {
        var journal = state.Career.Journal;
        int Answer(string scene) => journal.LastOrDefault(e => e.Scene == scene)?.Answer ?? -1;
        var wish = Answer("beside") == 0 ? "making something you're proud of" : Answer("beside") == 1 ? "reaching someone who needs your story" : "finding your own way";
        var archive = Answer("page") == 0 ? "the rough draft beside the finished page" : "the first finished page";
        var support = Answer("same") == 0 ? "We can talk it through, just like before." : Answer("same") == 1 ? "I'll stay beside you. We don't have to fill the silence." : "I'm here whenever you need me.";
        return id switch
        {
            "beside" => new(id,"A place beside yours", (state.Career.AvailableFrom > GameClock.Start ? "You already have a story behind you. I'll keep the next part safe, too.\n\n" : "") + "The desk is ready. So am I. What should I remind you of when things get difficult?", "Make something I'm proud of", "Reach someone who needs it"),
            "page" => new(id,"The first page worth keeping","I kept a copy of the first finished page. Official archival duties, obviously.","Keep the rough draft too","Start with the finished page","happy"),
            "reader" => new(id,"Your first reader", (state.Series.Any(s=>s.Volumes.Any(v=>v.ReleasedAt is not null)||s.Contract is not null) ? "I was here before the reviews." : "Even while it's still taking shape, your work matters to me.") + $" You said this was about {wish}. I'd like to hear what you think of it.","I'm proud of the work","I want to do better next time","happy"),
            "same" => new(id,"Same clipboard, different day",(state.Career.MovedAt is not null ? "Different room. Same place beside you." : "The clipboard has room for a difficult chapter, too.") + " What would help right now?","Talk it through","Just stay beside me for a bit","concerned"),
            "ordinary" => new(id,"An archive of ordinary days",$"It was supposed to be a work log. Somehow, the ordinary days took up most of it. I still have {archive}. " + (Answer("reader")==1?"And space for everything you want to improve.":Answer("reader")==0?"You sounded proud that day. I was, too.":"There's no hurry to decide what it all means."),"Keep the little moments","Make room for what comes next","happy"),
            "still" => new(id,"The first reader, still",$"I kept {archive}. There's plenty of room after it. You wanted to keep {wish}. " + support,"Let's keep going","Thank you for staying","determined"),
            "margin" => new(id,"Clipboard margin","These are notes. The tiny cheering figure is also a note. " + (Answer("margin")==0?"You wanted to see the last one, so I drew another.":""),"Let me see it","You can keep that one","happy"),
            "desk_moment" => new(id,"A moment at her desk","Oh, taking a break? I can make room. For you, anyway. The hair stays. " +
                (Answer("desk_moment")==0?"I saved another little story to tell you.":Answer("desk_moment")==1?"We can enjoy the quiet again. I'm happy you're here.":""),
                "Tell me about your day","Let's sit here for a while","happy","desk-conversation"),
            "shelf" => new(id,"Reference shelf",$"I've organized the references. The favorites were harder to put back. I remember what you said about {wish}. " + (Answer("shelf")==0?"I left the technique books within reach.":Answer("shelf")==1?"I bookmarked the reader letters again.":""),"Talk about the craft","Talk about the readers"),
            "tea" => new(id,"Tea beside the draft","A pause is allowed. I checked. " + support + (Answer("ordinary")==0?" This can be one of the little moments.":Answer("ordinary")==1?" We can think about what comes next.":""),"Stay and chat","Enjoy the quiet","concerned"),
            "migration" => new(id,"Desk migration","The important equipment survived. Clipboard, glasses, and an unreasonable amount of hair. " + (Answer("migration")==0?"And yes, the archive is safe again.":Answer("migration")==1?"I found another good view from the desk.":""),"How is the archive?","How is the new view?","happy"),
            _ => throw new InvalidCommandException("Unknown conversation.")
        };
    }
}
