using System.Text.Json;
using Xunit;
namespace MangakaSim.Tests;

public class MusicMomentsTests
{
    static (GameState State, Series Owned) Career()
    {
        var s = GameState.NewGame(0);
        s.Apply(new CreateSeriesCommand("Owned", "adventure", Cadence.Monthly, 16));
        return (s, s.Series.Last());
    }
    static GameEvent E(GameState s, EventType type, int? series, int? person = null) =>
        new() { Type = type, SeriesId = series, PersonId = person, Time = s.Clock.Now, ActivityDate = s.Clock.Now.Date };

    [Fact] public void Offers_and_acceptances_are_good_news()
    {
        var (s, owned) = Career();
        Assert.Equal([MusicMoment.GoodNews], MusicMoments.Classify(s, [E(s, EventType.SerializationOffered, owned.Id)], []));
        Assert.Equal([MusicMoment.GoodNews], MusicMoments.Classify(s, [E(s, EventType.OfferAccepted, owned.Id)], []));
    }

    [Fact] public void Cancellations_and_rejections_are_setbacks()
    {
        var (s, owned) = Career();
        Assert.Equal([MusicMoment.Setback], MusicMoments.Classify(s, [E(s, EventType.SeriesCancelled, owned.Id)], []));
        Assert.Equal([MusicMoment.Setback], MusicMoments.Classify(s, [E(s, EventType.PitchRejected, owned.Id)], []));
    }

    [Fact] public void Other_studios_events_do_not_count()
    {
        var (s, owned) = Career();
        owned.BusinessId = s.ControlledBusinessId + 1000;
        Assert.Empty(MusicMoments.Classify(s, [E(s, EventType.SeriesCancelled, owned.Id)], []));
    }

    [Fact] public void Only_an_award_with_a_prize_is_good_news()
    {
        var (s, owned) = Career();
        s.Progression.Awards.Add(new() { Id = 9001, SeriesId = owned.Id, ResolvedAt = s.Clock.Now, Prize = 300_000 });
        Assert.Equal([MusicMoment.GoodNews], MusicMoments.Classify(s, [E(s, EventType.AwardResult, owned.Id)], []));
        s.Progression.Awards[^1].Prize = 0;
        Assert.Empty(MusicMoments.Classify(s, [E(s, EventType.AwardResult, owned.Id)], []));
    }

    [Fact] public void The_first_sale_milestone_is_good_news()
    {
        var (s, _) = Career();
        Assert.Equal([MusicMoment.GoodNews], MusicMoments.Classify(s, [], ["first-sale"]));
        Assert.Empty(MusicMoments.Classify(s, [], ["later-sale"]));
    }

    [Fact] public void A_chapter_at_risk_matters_only_under_a_magazine_contract()
    {
        var (s, owned) = Career();
        Assert.Empty(MusicMoments.Classify(s, [E(s, EventType.ChapterAtRisk, owned.Id)], []));
        owned.Contract = new(9002, s.PublisherCatalog.Magazines[0].Id, 5000, s.Clock.Now, s.Clock.Now.AddDays(30));
        Assert.Equal([MusicMoment.Deadline], MusicMoments.Classify(s, [E(s, EventType.ChapterAtRisk, owned.Id)], []));
    }

    [Fact] public void Attending_a_convention_today_is_a_convention_moment()
    {
        var (s, _) = Career();
        s.Bookings.Add(new() { Id = 9003, BusinessId = s.ControlledBusinessId, PersonId = s.ProtagonistPersonId, Date = s.Clock.Now.Date });
        Assert.Equal([MusicMoment.Convention], MusicMoments.Classify(s, [], []));
        s.Bookings[^1].Cancelled = true;
        Assert.Empty(MusicMoments.Classify(s, [], []));
    }

    [Fact] public void Music_volume_defaults_to_half_and_older_settings_get_the_default()
    {
        Assert.Equal(0.5, new CareerPresentation().MusicVolume);
        Assert.Equal(0.5, JsonSerializer.Deserialize<CareerPresentation>("{}")!.MusicVolume);
    }

    [Fact] public void A_music_volume_outside_0_to_1_is_refused()
    {
        var root = Path.Combine(Path.GetTempPath(), "music-volume-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CareerStore(root);
            // The career validator refuses it when saving, as it does for the other volumes.
            Assert.Throws<InvalidDataException>(() => store.Save(Guid.NewGuid().ToString("N"), "Loud", GameState.NewGame(0), new CareerPresentation { MusicVolume = 2 }));
            Assert.Empty(store.List());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
