using System.Text.Json.Nodes;
using MangakaSim.Rules;
using Xunit;

namespace MangakaSim.Tests;

public class ProgressionTests
{
    private static void RoundTrip(GameState s) => Assert.Equal(s.ToJson(), GameState.FromJson(s.ToJson()).ToJson());
    private static void Reject(GameState s, ICommand command)
    { var before = s.ToJson(); Assert.Throws<InvalidCommandException>(() => s.Apply(command)); Assert.Equal(before, s.ToJson()); }
    private static void Complete(GameState s, Chapter chapter, double quality = 90)
    {
        foreach (var w in chapter.Stages)
        { w.Status = StageStatus.Complete; w.HoursDone = w.HoursRequired; w.HoursByPerson[s.ProtagonistPersonId] = 1; w.QualityWeightedWork = w.HoursRequired * quality / 100; w.Contribution = QualityRules.Weight(w.Stage) * quality; }
        chapter.CreatorPersonId = s.ProtagonistPersonId;
        var series = s.Series.Single(s => s.Chapters.Contains(chapter));
        series.LifetimeHoursByPerson[s.ProtagonistPersonId] = series.LifetimeHoursByPerson.GetValueOrDefault(s.ProtagonistPersonId) + chapter.Stages.Count;
        s.CompleteChapterIfDone(chapter);
    }
    private static (GameState State, ContestManuscript Manuscript) Manuscript()
    {
        var s = GameState.NewGame(8);
        s.Apply(new RecognitionCommand(RecognitionAction.CreateManuscript, Text: "A little masterpiece"));
        var m = s.Progression.Manuscripts.Single();
        Complete(s, s.FindChapter(m.ChapterId)!);
        return (s, m);
    }
    private static (GameState State, LicenseProject Project) Offer(LicenseKind kind = LicenseKind.Anime)
    {
        var (s, m) = Manuscript();
        s.Apply(new RecognitionCommand(RecognitionAction.ReleaseManuscript, m.Id));
        var title = s.FindSeries(m.SeriesId)!;
        title.Fanbase = 20000;
        var p = new LicenseProject { Id = s.AllocateId(), SeriesId = title.Id, BusinessId = title.BusinessId,
            CreatorId = title.RightsLeadPersonId, Kind = kind, Partner = "Test Animation", CreatedAt = s.Clock.Now,
            DueAt = s.Clock.Now.AddDays(30), Payment = 100000, CreatorPercent = 30, Control = kind == LicenseKind.Anime ? 2 : 0,
            Reliability = .85, Fit = 85, Roll = .5, Weeks = 26, SourceChapters = 24 };
        s.Progression.Projects.Add(p); return (s, p);
    }
    private sealed class Recorder : IAchievementSink
    { public List<string> Keys { get; } = new(); public void Unlock(string key) => Keys.Add(key); }

    [Theory]
    [InlineData(SandboxAssist.PersonalFunds)] [InlineData(SandboxAssist.BusinessFunds)]
    [InlineData(SandboxAssist.InstantProduction)] [InlineData(SandboxAssist.InstantDelivery)]
    [InlineData(SandboxAssist.UnlockLocations)] [InlineData(SandboxAssist.UnlockEquipment)]
    [InlineData(SandboxAssist.NoStress)] [InlineData(SandboxAssist.NoDeadlinePenalties)] [InlineData(SandboxAssist.FutureTechnology)]
    public void Every_assist_permanently_disables_platform_achievements(SandboxAssist assist)
    {
        var s = GameState.NewGame();
        s.Apply(new DifficultyCommand(CareerDifficulty.Custom, assist));
        Assert.True(s.Progression.EverSandbox);
        s.Apply(new DifficultyCommand(CareerDifficulty.Standard));
        var copy = GameState.FromJson(s.ToJson());
        var recorder = new Recorder(); AchievementDelivery.Deliver(copy, recorder);
        Assert.False(AchievementDelivery.Eligible(copy)); Assert.Empty(recorder.Keys);
        Assert.True(AchievementDelivery.Eligible(GameState.NewGame()));
        RoundTrip(s);
    }
    [Fact] public void Sandbox_mode_alone_is_ineligible_and_removes_unsent_evidence()
    {
        var s = GameState.NewGame();
        s.Progression.Milestones.Add(new("example", s.ProtagonistPersonId, s.Clock.Now, "Example"));
        s.Progression.Achievements.Add(new("example", s.Clock.Now));
        var clean = s.ToJson();
        s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox));
        Assert.Empty(s.Progression.Achievements); Assert.Single(s.Progression.Milestones);
        Assert.True(AchievementDelivery.Eligible(GameState.FromJson(clean)));
        RoundTrip(s);
    }
    [Fact] public void Difficulty_never_changes_prodigy_skills_or_reissues_starting_money()
    {
        var s = GameState.NewGame(); var skills = s.Protagonist.Skills.ToArray();
        s.Apply(new DifficultyCommand(CareerDifficulty.Relaxed));
        Assert.Equal(400000, s.PersonalMoney); Assert.Equal(600000, s.Money);
        s.Apply(new DifficultyCommand(CareerDifficulty.Challenging));
        Assert.Equal(400000, s.PersonalMoney); Assert.Equal(600000, s.Money);
        Assert.Equal(skills, s.Protagonist.Skills.ToArray()); Assert.True(s.Protagonist.IsProdigy);
        Assert.True(AchievementDelivery.Eligible(s)); RoundTrip(s);
        Assert.Equal(s.ToJson(), s.ReplayTimeline().ToJson());
    }
    [Fact] public void Invalid_settings_are_atomic()
    { var s = GameState.NewGame(); Reject(s, new DifficultyCommand(CareerDifficulty.Custom, (SandboxAssist)1024)); Reject(s, new DifficultyCommand(CareerDifficulty.Custom, Pressure: 9)); }
    [Fact] public void Personal_subsidy_is_separate_and_reconciles_after_large_contribution()
    {
        var s = GameState.NewGame(); s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox, SandboxAssist.PersonalFunds));
        s.Apply(new ContributeFundsCommand(1000000));
        Assert.Equal(0, s.PersonalMoney); Assert.Equal(1300000, s.Money);
        Assert.Equal(800000, s.Protagonist.PersonalAccount.Entries.Single(e => e.Kind == AccountEntryKind.SandboxSubsidy).Amount);
        Assert.DoesNotContain(s.ControlledBusiness.Account.Entries, e => e.Kind == AccountEntryKind.SandboxSubsidy); RoundTrip(s);
    }
    [Fact] public void Instant_production_completes_finite_work_without_invented_hours_or_xp()
    {
        var s = GameState.NewGame(); s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox, SandboxAssist.InstantProduction));
        s.Apply(new RecognitionCommand(RecognitionAction.CreateManuscript, Text: "Quick sketch"));
        s.Advance(1); var m = s.Progression.Manuscripts.Single();
        Assert.Equal(ChapterStatus.Complete, s.FindChapter(m.ChapterId)!.Status);
        Assert.Single(s.FindSeries(m.SeriesId)!.Chapters);
        Assert.InRange(s.Protagonist.HoursWorkedToday, 0, 1); RoundTrip(s);
    }
    [Fact] public void Future_unlock_changes_access_without_advancing_historical_events()
    {
        var s = GameState.NewGame(); var processed = s.World.Processed.ToArray();
        Assert.False(s.ChannelAvailable(ReleaseChannel.DomesticDigital)); Assert.False(s.EquipmentAvailable("desk-digital"));
        s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox, SandboxAssist.FutureTechnology | SandboxAssist.UnlockEquipment));
        Assert.True(s.ChannelAvailable(ReleaseChannel.DomesticDigital)); Assert.True(s.EquipmentAvailable("desk-digital"));
        Assert.Equal(processed, s.World.Processed.ToArray()); Assert.Equal(GameClock.Start, s.Clock.Now);
        s.Apply(new DifficultyCommand(CareerDifficulty.Standard)); Assert.False(s.ChannelAvailable(ReleaseChannel.DomesticDigital)); RoundTrip(s);
    }
    [Fact] public void Contest_entries_are_frozen_and_only_one_can_be_active()
    {
        var (s, m) = Manuscript(); s.Apply(new RecognitionCommand(RecognitionAction.Submit, m.Id));
        var a = s.Progression.Awards.Single(); var quality = a.Quality;
        Reject(s, new RecognitionCommand(RecognitionAction.Submit, m.Id));
        Reject(s, new RecognitionCommand(RecognitionAction.Revise, m.Id));
        Reject(s, new RecognitionCommand(RecognitionAction.ReleaseManuscript, m.Id));
        Assert.Equal(quality, a.Quality); RoundTrip(s);
    }
    [Fact] public void Contest_manuscript_cannot_be_misread_as_a_publisher_sample()
    {
        var (s, m) = Manuscript();
        Reject(s, new PitchSeriesCommand(m.SeriesId, s.PublisherCatalog.Magazines.First().Id));
        s.Advance(48); Assert.Single(s.FindSeries(m.SeriesId)!.Chapters); RoundTrip(s);
    }
    [Fact] public void Jury_scores_favor_craft_over_audience_size()
    { Assert.True(GameState.JuryScore(95, 90, 80, -5) > GameState.JuryScore(60, 50, 90, 5)); }
    [Fact] public void Contest_win_pays_personal_account_once_and_retains_feedback()
    {
        var (s, m) = Manuscript(); s.Apply(new RecognitionCommand(RecognitionAction.Submit, m.Id));
        var a = s.Progression.Awards.Single(); a.Competitors = [20, 30]; a.Jury = 5; a.Originality = 95; a.ResolvesAt = s.Clock.Now.AddHours(1);
        var personal = s.PersonalMoney; s.Advance(1);
        Assert.Equal("Winner", a.Result); Assert.Equal(personal + 300000, s.PersonalMoney); Assert.NotEmpty(a.Feedback);
        Assert.Single(s.Protagonist.PersonalAccount.Entries, e => e.Kind == AccountEntryKind.AwardPrize);
        s.Advance(48); Assert.Single(s.Protagonist.PersonalAccount.Entries, e => e.Kind == AccountEntryKind.AwardPrize); RoundTrip(s);
    }
    [Fact] public void Unsuccessful_work_requires_a_new_completed_revision()
    {
        var (s, m) = Manuscript(); s.Apply(new RecognitionCommand(RecognitionAction.Submit, m.Id));
        var a = s.Progression.Awards.Single(); a.Quality = 20; a.ResolvesAt = s.Clock.Now.AddHours(1); s.Advance(1);
        Reject(s, new RecognitionCommand(RecognitionAction.Submit, m.Id));
        s.Apply(new RecognitionCommand(RecognitionAction.Revise, m.Id));
        Assert.Equal(2, m.Revision); Assert.Equal(ChapterStatus.NotStarted, s.FindChapter(m.ChapterId)!.Status);
        Reject(s, new RecognitionCommand(RecognitionAction.Submit, m.Id)); RoundTrip(s);
    }
    [Fact] public void Contract_receipts_freeze_beneficiaries_and_create_creator_obligations()
    {
        var (s, p) = Offer(); var before = s.Money;
        s.Apply(new LicenseCommand(LicenseAction.Accept, p.Id));
        Assert.Equal(before + 20000, s.Money); Assert.Equal(6000, s.Bills.Single(b => b.Reason == "Anime creator share").Remaining);
        Assert.Equal(s.ProtagonistPersonId, p.CreatorId); Assert.Equal(LicensePhase.PreProduction, p.Phase);
        Reject(s, new LicenseCommand(LicenseAction.Accept, p.Id)); RoundTrip(s);
    }
    [Fact] public void Counteroffers_are_bounded_and_never_reroll_on_view()
    {
        var (s, p) = Offer();
        s.Apply(new LicenseCommand(LicenseAction.Payment, p.Id)); s.Apply(new LicenseCommand(LicenseAction.Schedule, p.Id));
        Assert.Equal(2, p.Rounds); Assert.Equal(2, p.Negotiations.Count);
        Reject(s, new LicenseCommand(LicenseAction.Payment, p.Id)); RoundTrip(s);
    }
    [Fact] public void Catchup_options_require_contractual_rights()
    {
        var (s, p) = Offer(); s.Apply(new LicenseCommand(LicenseAction.Accept, p.Id));
        p.Phase = LicensePhase.Production; p.Decision = "Source material"; p.DecisionAt = s.Clock.Now; p.Control = 0;
        Reject(s, new LicenseCommand(LicenseAction.OriginalEnding, p.Id));
        s.Apply(new LicenseCommand(LicenseAction.Wait, p.Id)); Assert.True(p.ShortSeason); Assert.Empty(p.Decision); RoundTrip(s);
    }
    [Fact] public void Anime_reception_does_not_rewrite_manga_quality()
    {
        var (s, p) = Offer(); s.Apply(new LicenseCommand(LicenseAction.Accept, p.Id));
        p.Phase = LicensePhase.Production; p.DueAt = s.Clock.Now.AddHours(1); p.WarningChecked = true;
        var chapter = s.FindSeries(p.SeriesId)!.Chapters.First(); var quality = chapter.Quality;
        s.Advance(1); Assert.Equal(LicensePhase.Released, p.Phase); Assert.Equal(quality, chapter.Quality);
        Assert.Equal(2, s.Progression.Receipts.Count); Assert.Contains(s.Progression.Milestones, m => m.Key == "anime_release"); RoundTrip(s);
    }
    [Fact] public void Cancellation_retains_only_signed_payment()
    {
        var (s, p) = Offer(); s.Apply(new LicenseCommand(LicenseAction.Accept, p.Id));
        p.Phase = LicensePhase.Production; p.DueAt = s.Clock.Now.AddHours(1); p.WarningChecked = true; p.Roll = .01;
        s.Advance(1); Assert.Equal(LicensePhase.Cancelled, p.Phase); Assert.Single(s.Progression.Receipts); RoundTrip(s);
    }
    [Fact] public void Merchandise_releases_without_an_anime_and_settles_monthly()
    {
        var (s, p) = Offer(LicenseKind.Stationery); s.Apply(new LicenseCommand(LicenseAction.Accept, p.Id));
        p.Phase = LicensePhase.Production; p.DueAt = s.Clock.Now.AddHours(1); s.Advance(1);
        Assert.Equal(LicensePhase.Released, p.Phase); Assert.DoesNotContain(s.Progression.Projects, x => x.Kind == LicenseKind.Anime);
        s.Advance(24); Assert.Equal(3, s.Progression.Receipts.Count); RoundTrip(s);
    }
    [Fact] public void Illustration_preserves_choices_and_is_not_a_business_bonus()
    {
        var s = GameState.NewGame(); s.Apply(new StoryCommand("beside", 0)); s.Career.PendingScene = "desk_moment";
        var money = s.Money; var skills = s.Protagonist.Skills.ToArray();
        Assert.Equal("desk-conversation", HelperStories.Describe(s, "desk_moment").Illustration);
        s.Apply(new StoryCommand("desk_moment", 1)); Assert.Contains("quiet again", HelperStories.Describe(s, "desk_moment").Text);
        Assert.Equal(money, s.Money); Assert.Equal(skills, s.Protagonist.Skills.ToArray()); RoundTrip(s);
    }
    [Fact] public void V6_import_keeps_original_state_and_establishes_a_checkpoint()
    {
        var s = GameState.NewGame(17); s.Advance(24); var node = JsonNode.Parse(s.ToJson())!.AsObject();
        node["Version"] = 6; node.Remove("Progression"); var source = node.ToJsonString();
        var imported = GameState.ImportSupported(source);
        Assert.Equal(GameState.CurrentVersion, imported.Version); Assert.Equal(s.Money, imported.Money); Assert.Equal(s.Career.Journal.Count, imported.Career.Journal.Count);
        Assert.NotNull(imported.World.ReplayCheckpoint); imported.Advance(24); RoundTrip(imported);
        Assert.Equal(imported.ToJson(), imported.ReplayTimeline().ToJson());
    }
    [Fact] public void Missing_eligibility_fields_or_a_cleared_sandbox_flag_are_rejected()
    {
        var s = GameState.NewGame(); s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox));
        var node = JsonNode.Parse(s.ToJson())!; node["Progression"]!["EverSandbox"] = false;
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(node.ToJsonString()));
        node = JsonNode.Parse(s.ToJson())!; node["Progression"]!.AsObject().Remove("EverSandbox");
        Assert.Throws<InvalidDataException>(() => GameState.FromJson(node.ToJsonString()));
    }
    [Fact] public void New_commands_replay_and_tick_batching_agree()
    {
        var s = GameState.NewGame(15); s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox, SandboxAssist.InstantProduction));
        s.Apply(new RecognitionCommand(RecognitionAction.CreateManuscript, Text: "A replayable tale")); s.Advance(2);
        s.Apply(new RecognitionCommand(RecognitionAction.Submit, s.Progression.Manuscripts.Single().Id));
        var copy = GameState.FromJson(s.ToJson()); s.Advance(48); for (int i = 0; i < 48; i++) copy.Advance(1);
        Assert.Equal(s.ToJson(), copy.ToJson()); Assert.Equal(s.ToJson(), s.ReplayTimeline().ToJson()); RoundTrip(s);
    }

    [Fact] public void Custom_starting_cushion_is_independent_and_cannot_be_reissued()
    {
        var s=GameState.NewGame();s.Apply(new DifficultyCommand(CareerDifficulty.Custom,Pressure:2,Recovery:0,Cushion:2));
        Assert.Equal(400000,s.PersonalMoney);Assert.Equal(600000,s.Money);Assert.Equal(2,s.Progression.Pressure);
        s.Apply(new DifficultyCommand(CareerDifficulty.Custom,Cushion:0));Assert.Equal(400000,s.PersonalMoney);RoundTrip(s);
    }
    [Fact] public void Active_manuscripts_cannot_be_deleted_by_ending_the_title()
    {
        var (s,m)=Manuscript();Reject(s,new EndSeriesCommand(m.SeriesId));
        s.Apply(new RecognitionCommand(RecognitionAction.ReleaseManuscript,m.Id));s.Apply(new EndSeriesCommand(m.SeriesId));RoundTrip(s);
    }
    [Fact] public void Award_sales_lift_is_capped_and_expires()
    {
        var (s,m)=Manuscript();s.Apply(new RecognitionCommand(RecognitionAction.Submit,m.Id));var a=s.Progression.Awards.Single();
        for(var i=0;i<5;i++)s.Progression.Effects.Add(new(a.Id,m.SeriesId,s.Clock.Now,s.Clock.Now.AddHours(1),.15));
        Assert.Equal(1.25,s.RecognitionLift(m.SeriesId));s.Advance(1);Assert.Equal(1,s.RecognitionLift(m.SeriesId));RoundTrip(s);
    }
    [Fact] public void Achievement_delivery_deduplicates_and_filters_each_originating_snapshot()
    {
        var s=GameState.NewGame();s.Progression.Milestones.Add(new("first_publication",s.ProtagonistPersonId,s.Clock.Now,"Published"));
        s.Progression.Achievements.Add(new("first_publication",s.Clock.Now));
        var recorder=new Recorder();var session=new AchievementSession(recorder);
        AchievementDelivery.Deliver(s,session);AchievementDelivery.Deliver(s,session);Assert.Equal(["MKG_FIRST_PUBLICATION"],recorder.Keys);
        s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox));session.AccountChanged();AchievementDelivery.Deliver(s,session);
        Assert.Single(recorder.Keys);RoundTrip(s);
    }
    [Fact] public void Consultation_consumes_real_manga_work_time_without_double_bookings()
    {
        var (s,p)=Offer();s.Apply(new LicenseCommand(LicenseAction.Accept,p.Id));s.Apply(new LicenseCommand(LicenseAction.Involvement,p.Id,Value:2));
        s.Apply(new CreateSeriesCommand("Manga alongside the anime","drama",Cadence.Monthly,4));
        var control=GameState.FromJson(s.ToJson());control.Apply(new LicenseCommand(LicenseAction.Involvement,p.Id,Value:0));
        s.Advance(1);control.Advance(1);
        Assert.Equal(1,p.ConsultationHours);Assert.Equal(1,s.Protagonist.HoursWorkedToday);
        var hours=s.Series.SelectMany(x=>x.Chapters).SelectMany(c=>c.Stages).Sum(w=>w.HoursByPerson.Values.Sum());
        var ordinary=control.Series.SelectMany(x=>x.Chapters).SelectMany(c=>c.Stages).Sum(w=>w.HoursByPerson.Values.Sum());
        Assert.True(hours<ordinary);RoundTrip(s);
    }
    [Fact] public void Full_merchandise_term_settles_once_and_expires()
    {
        var (s,p)=Offer(LicenseKind.Figures);s.Apply(new LicenseCommand(LicenseAction.Accept,p.Id));
        s.Advance(24*(28+p.Weeks*7+368));Assert.Equal(LicensePhase.Completed,p.Phase);
        Assert.InRange(s.Progression.Receipts.Count,13,15);var total=s.Progression.Receipts.Where(r=>r.ProjectId==p.Id).Sum(r=>r.Gross);
        s.Advance(24*35);Assert.Equal(total,s.Progression.Receipts.Where(r=>r.ProjectId==p.Id).Sum(r=>r.Gross));RoundTrip(s);
    }
    [Fact] public void Moving_home_preserves_signed_beneficiaries_and_settlement()
    {
        var (s,p)=Offer();s.Apply(new LicenseCommand(LicenseAction.Accept,p.Id));var business=p.BusinessId;var creator=p.CreatorId;
        s.Apply(new StudioActionCommand(StudioAction.CareerHome,Amount:10000));s.Advance(24);
        Assert.NotEqual(business,s.ControlledBusinessId);Assert.Equal(business,p.BusinessId);Assert.Equal(creator,p.CreatorId);
        p.Phase=LicensePhase.Production;p.WarningChecked=true;p.DueAt=s.Clock.Now.AddHours(1);s.Advance(24);
        Assert.Equal(LicensePhase.Released,p.Phase);Assert.Equal(100000,s.Businesses.Single(b=>b.Id==business).Account.Entries.Where(e=>e.Kind==AccountEntryKind.LicenseIncome).Sum(e=>e.Amount));RoundTrip(s);
    }
    [Fact] public void Employed_creator_needs_employer_approval_before_signing()
    {
        var s=GameState.NewGame();s.Apply(new StudioActionCommand(StudioAction.CareerEmployer,Value:1));s.Advance(24);
        s.Apply(new CreateSeriesCommand("Employer title","drama",Cadence.Monthly,1));var title=s.Series.Single();
        var p=new LicenseProject{Id=s.AllocateId(),SeriesId=title.Id,BusinessId=title.BusinessId,CreatorId=title.RightsLeadPersonId,
            Partner="Clover Frame",CreatedAt=s.Clock.Now,DueAt=s.Clock.Now.AddDays(30),Payment=100000,CreatorPercent=30,Control=1,Weeks=26,Roll=.5,Fit=80,Reliability=.9};
        s.Progression.Projects.Add(p);s.Apply(new LicenseCommand(LicenseAction.Accept,p.Id));Assert.Empty(s.Progression.Receipts);
        Reject(s,new LicenseCommand(LicenseAction.Accept,p.Id));s.Advance(24*8);Assert.True(p.EmployerApproved);
        s.Apply(new LicenseCommand(LicenseAction.Accept,p.Id));Assert.Single(s.Progression.Receipts);RoundTrip(s);
    }
    [Fact] public void Annual_awards_use_published_work_and_resolve_on_the_calendar()
    {
        var s=GameState.NewGame(12);s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox,SandboxAssist.InstantProduction|SandboxAssist.InstantDelivery));
        s.Apply(new CreateSeriesCommand("The yearly book","drama",Cadence.Monthly,1));
        PublishingTests.Until(s,()=>s.Series[0].Volumes.Count>0,24*14);
        s.Apply(new StudioActionCommand(StudioAction.Print,s.Series[0].Volumes[0].Id,Amount:10));s.Advance(1);
        Assert.True(s.PrintRuns[0].Delivered);Assert.NotNull(s.Series[0].Volumes[0].ReleasedAt);
        s.Apply(new DifficultyCommand(CareerDifficulty.Standard));s.Apply(new EndSeriesCommand(s.Series[0].Id));
        s.Advance(s.Clock.HoursUntil(new DateTime(1997,1,1,9,0,0)));
        var award=Assert.Single(s.Progression.Awards,a=>a.Award=="annual:1996");Assert.Null(award.ResolvedAt);
        s.Advance(24*15);Assert.NotNull(award.ResolvedAt);Assert.NotEmpty(award.Feedback);RoundTrip(s);
    }
    [Fact] public void Natural_scene_scheduler_can_offer_the_desk_scene_without_a_management_trigger()
    {
        var offered=false;
        for(var seed=0;seed<20&&!offered;seed++)
        {
            var s=GameState.NewGame(seed);s.Apply(new StoryCommand("beside",0));
            for(var week=0;week<26&&!offered;week++)
            {
                s.Advance(24*7);offered=s.Career.PendingScene=="desk_moment";
                if(s.Career.PendingScene is {} scene&&!offered)s.Apply(new StoryCommand(scene,-1));
            }
        }
        Assert.True(offered);
    }
    [Fact] public void Business_assist_pays_obligations_without_adding_personal_subsidies()
    {
        var s=GameState.NewGame();s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox,SandboxAssist.BusinessFunds));
        s.Apply(new StudioActionCommand(StudioAction.Move,16));s.Advance(24*40);
        Assert.Contains(s.ControlledBusiness.Account.Entries,e=>e.Kind==AccountEntryKind.SandboxSubsidy);
        Assert.DoesNotContain(s.Protagonist.PersonalAccount.Entries,e=>e.Kind==AccountEntryKind.SandboxSubsidy);
        Assert.DoesNotContain(s.Bills,b=>b.BusinessId==s.ControlledBusinessId&&b.DueAt<=s.Clock.Now&&b.Remaining>0);RoundTrip(s);
    }
    [Fact] public void Location_unlock_waives_progression_but_not_price_or_authority()
    {
        var s=GameState.NewGame();Reject(s,new StudioActionCommand(StudioAction.Lease,1));
        s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox,SandboxAssist.UnlockLocations));
        Reject(s,new StudioActionCommand(StudioAction.Lease,16));s.Apply(new StudioActionCommand(StudioAction.Lease,1));
        Assert.Equal(2,s.Locations.Count(l=>l.BusinessId==s.ControlledBusinessId&&!l.Closed));
        s.Apply(new DifficultyCommand(CareerDifficulty.Standard));Reject(s,new StudioActionCommand(StudioAction.Lease,1));RoundTrip(s);
    }
    [Fact] public void Stress_assistance_keeps_morale_without_erasing_needs()
    {
        var s=GameState.NewGame();s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox,SandboxAssist.NoStress));
        s.Protagonist.Happiness=10;s.Protagonist.Food=10;s.Advance(1);
        Assert.True(s.Protagonist.Food<100);s.Advance(23);Assert.True(s.Protagonist.Happiness>=70);RoundTrip(s);
    }
    [Fact] public void Future_digital_contract_continues_when_assist_is_disabled()
    {
        var s=PublishingTests.Started();PublishingTests.Until(s,()=>s.Series[0].Volumes.Count>0);
        s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox,SandboxAssist.FutureTechnology));
        s.Apply(new GetOnlineCommand());s.Apply(new TimelineCommand(TimelineAction.RequestDigital,s.Series[0].Id));
        Assert.Equal(0,s.DigitalPreference);s.Apply(new DifficultyCommand(CareerDifficulty.Standard));s.Advance(24*8);
        Assert.True(s.World.Receipts.Sum(r=>r.Units)>0);Assert.False(s.ChannelAvailable(ReleaseChannel.DomesticDigital));RoundTrip(s);
    }
    [Fact] public void Deadline_assistance_preserves_calendar_without_missed_issue_penalties()
    {
        var s=SimulationFixture.Serialized();var series=s.Series[0];
        s.Apply(new DifficultyCommand(CareerDifficulty.Sandbox,SandboxAssist.NoDeadlinePenalties));
        s.Apply(new SetScheduleCommand(s.ProtagonistPersonId,8,18,Enum.GetValues<DayOfWeek>().ToHashSet()));
        s.Advance(s.Clock.HoursUntil(series.Contract!.FirstIssueClose.AddDays(35)));
        Assert.Contains(s.Events,e=>e.Type==EventType.IssueMissed);Assert.Empty(series.Strikes);
        Assert.Equal(0,series.ChaptersPublished);RoundTrip(s);
    }
    [Theory]
    [InlineData(CareerDifficulty.Relaxed,3)] [InlineData(CareerDifficulty.Standard,3)] [InlineData(CareerDifficulty.Challenging,3)]
    [InlineData(CareerDifficulty.Relaxed,19)] [InlineData(CareerDifficulty.Standard,19)] [InlineData(CareerDifficulty.Challenging,19)]
    public void Ordinary_home_and_employed_routes_do_not_need_award_luck(CareerDifficulty mode,int seed)
    {
        foreach(var employed in new[]{false,true})
        {
            var s=GameState.NewGame(seed);s.Apply(new DifficultyCommand(mode));
            if(employed){s.Apply(new StudioActionCommand(StudioAction.CareerEmployer,Value:1));s.Advance(24);}
            s.Apply(new CreateSeriesCommand("A modest career","drama",Cadence.Monthly,4));s.Advance(24*90);
            Assert.True(s.Series[0].Chapters.Count(c=>c.Status==ChapterStatus.Complete)>0);
            Assert.True(s.PersonalMoney>0);Assert.False(s.Locations.Single(l=>l.Id==s.Protagonist.Employment!.LocationId).Closed);
            Assert.Empty(s.Progression.Awards);Assert.Empty(s.Progression.Projects);Assert.True(AchievementDelivery.Eligible(s));RoundTrip(s);
        }
    }
    [Fact] public void Poor_adaptation_spillover_recovers_without_changing_manga_quality()
    {
        var (s,p)=Offer();s.Apply(new LicenseCommand(LicenseAction.Accept,p.Id));
        s.Apply(new EndSeriesCommand(p.SeriesId));
        p.Phase=LicensePhase.Production;p.DueAt=s.Clock.Now.AddHours(1);p.WarningChecked=true;p.Reliability=.1;p.Fit=0;p.Roll=.4;
        var reputation=s.Protagonist.Reputation;var quality=s.FindSeries(p.SeriesId)!.Chapters.First().Quality;
        s.Advance(1);Assert.True(p.Reception<45);Assert.Equal(reputation-2,s.Protagonist.Reputation);Assert.Equal(quality,s.FindSeries(p.SeriesId)!.Chapters.First().Quality);
        s.Advance(24*184);Assert.Equal(reputation,s.Protagonist.Reputation);Assert.Null(p.ReputationRecoveryAt);RoundTrip(s);
    }
}
