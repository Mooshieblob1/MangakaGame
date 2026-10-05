using MangakaSim.Rules;

namespace MangakaSim;

public partial class GameState
{
    private static bool LiveLicense(LicenseProject p) => p.Phase is LicensePhase.PreProduction or LicensePhase.Production or LicensePhase.Released;
    private int SourceCount(Series s) => s.Chapters.Count(c => c.Status == ChapterStatus.Complete &&
        (c.PublishedAt is not null || s.Volumes.Any(v => v.ReleasedAt is not null && v.ChapterIds.Contains(c.Id))));
    private bool HasSequelMaterial(Series s, LicenseKind kind) => kind != LicenseKind.Anime ||
        SourceCount(s) > Progression.Projects.Where(p => p.SeriesId == s.Id && p.Kind == kind && p.Phase == LicensePhase.Completed)
            .Select(p => p.SourceStart + p.SourceChapters).DefaultIfEmpty(0).Max();
    private void ApplyLicense(LicenseCommand c)
    {
        if (!Enum.IsDefined(c.Action) || !Enum.IsDefined(c.Kind)) throw new InvalidCommandException("Choose a license action.");
        if (c.Action == LicenseAction.Pitch)
        {
            var s = RequireSeries(c.Target);
            if (s.Fanbase < 1000 && !Progression.Awards.Any(a => a.SeriesId == s.Id && a.Prize > 0))
                throw new InvalidCommandException("Build an audience of 1,000 readers or earn a contest prize before approaching licensing partners.");
            if (SourceCount(s) == 0) throw new InvalidCommandException("Publish source material before seeking a license.");
            if (!HasSequelMaterial(s,c.Kind)) throw new InvalidCommandException("Publish new source material before pitching a follow-up anime season.");
            var key = $"{s.Id}:{c.Kind}";
            if (Progression.PitchCooldowns.GetValueOrDefault(key) > Clock.Now ||
                Progression.Projects.Any(p => p.SeriesId == s.Id && p.Kind == c.Kind && (LiveLicense(p) || p.Phase == LicensePhase.Offer)))
                throw new InvalidCommandException("This license is active, offered or in its ninety-day pitch cooldown.");
            if (Protagonist.BusyUntil > Clock.Now) throw new InvalidCommandException("Finish the current appointment first.");
            Progression.PitchCooldowns[key] = Clock.Now.AddDays(90);
            Protagonist.BusyUntil = Clock.Now.AddHours(2);
            var prior = Progression.Projects.LastOrDefault(p => p.SeriesId == s.Id && p.Kind == c.Kind && p.Phase == LicensePhase.Completed);
            var chance = Math.Clamp(.25 + s.Fanbase / 200000 + FindPerson(s.LeadPersonId)!.Reputation / 200 +
                (prior is null ? 0 : (prior.Reception - 60) / 200) + (c.Kind == LicenseKind.Anime && SourceCount(s) >= 24 ? .05 : 0), .25, .9);
            if (Progression.LicensingRng.NextDouble() < chance) CreateLicenseOffer(s, c.Kind);
            else Emit(EventType.IndustryNews, $"Partners passed on {s.Title} for now. Build readership or recognition before approaching again.", s.Id);
            return;
        }
        var p = Progression.Projects.FirstOrDefault(p => p.Id == c.Target) ?? throw new InvalidCommandException("Choose a current project.");
        var series = RequireSeries(p.SeriesId);
        if (p.BusinessId != ControlledBusinessId || p.CreatorId != series.RightsLeadPersonId)
            throw new InvalidCommandException("This agreement belongs to its original parties. New deals use the current rights.");
        if (c.Action == LicenseAction.Involvement)
        {
            if (p.Kind != LicenseKind.Anime || !LiveLicense(p) || c.Value is < 0 or > 2 || c.Value > p.Control)
                throw new InvalidCommandException("Choose involvement allowed by this active anime contract.");
            p.Involvement = c.Value; return;
        }
        if (c.Action is LicenseAction.Wait or LicenseAction.SideStories or LicenseAction.OriginalEnding or LicenseAction.RespondDelay)
        {
            if (p.Decision.Length == 0 || !LiveLicense(p)) throw new InvalidCommandException("There is no production decision waiting.");
            if (p.Decision == "Source material" && c.Action == LicenseAction.RespondDelay ||
                p.Decision != "Source material" && c.Action != LicenseAction.RespondDelay)
                throw new InvalidCommandException("Choose a response matching this production issue.");
            if (c.Action is LicenseAction.SideStories or LicenseAction.OriginalEnding && p.Control < 1)
                throw new InvalidCommandException("This contract does not grant approval of original story material.");
            if (c.Action == LicenseAction.Wait) { p.ShortSeason = true; p.SourceChapters = Math.Max(1, SourceCount(series) - p.SourceStart); }
            if (c.Action is LicenseAction.SideStories or LicenseAction.OriginalEnding)
            { p.Original = true; p.OriginalEnding = c.Action == LicenseAction.OriginalEnding; p.SourceChapters = Math.Max(1, SourceCount(series) - p.SourceStart); p.DueAt = p.DueAt.AddDays(28); }
            if (c.Action == LicenseAction.RespondDelay) { p.DueAt = p.DueAt.AddDays(56); p.Reliability = Math.Min(1, p.Reliability + .1); }
            p.Outcome = c.Action == LicenseAction.Wait ? "Finish this shorter season; wait for more manga before a sequel" :
                c.Action == LicenseAction.SideStories ? "Original side stories approved" : c.Action == LicenseAction.OriginalEnding ? "Anime-original ending approved" : "Revised production schedule agreed";
            p.Decision = ""; return;
        }
        if (p.Phase != LicensePhase.Offer || p.DueAt <= Clock.Now) throw new InvalidCommandException("This offer is no longer open.");
        if (c.Action == LicenseAction.Decline) { p.Phase = LicensePhase.Declined; p.Outcome = "Declined"; return; }
        if (c.Action == LicenseAction.Accept)
        {
            if (Control == ControlMode.EmployedLead && !p.EmployerApproved)
            {
                if (p.EmployerRequestedAt is not null) throw new InvalidCommandException("The employer is already reviewing this proposal.");
                p.EmployerRequestedAt = Clock.Now;
                p.DueAt = p.DueAt.AddDays(7);
                p.Outcome = "Employer approval requested; decision in seven days";
                return;
            }
            if (Progression.Projects.Count(x => x.Partner == p.Partner && LiveLicense(x)) >= 3)
                throw new InvalidCommandException("This partner's production schedule is full. Wait or choose another offer.");
            if (Progression.Projects.Any(x => x.Id != p.Id && x.SeriesId == p.SeriesId && x.Kind == p.Kind && LiveLicense(x)))
                throw new InvalidCommandException("This category is already licensed for the active term.");
            p.Phase = LicensePhase.PreProduction; p.SignedAt = Clock.Now;
            p.DueAt = Clock.Now.AddDays(p.Kind == LicenseKind.Anime ? 56 : 28);
            p.Outcome = "Agreement signed; beneficiaries and terms recorded";
            SettleLicense(p, p.Payment / 5);
            return;
        }
        if (c.Action is not (LicenseAction.Payment or LicenseAction.Control or LicenseAction.Schedule) || p.Rounds >= 2 || p.EmployerRequestedAt is not null)
            throw new InvalidCommandException("Up to two counteroffers are available before approval is requested.");
        if (c.Action == LicenseAction.Control && (p.Kind != LicenseKind.Anime || p.Control >= 2))
            throw new InvalidCommandException("There are no additional creative approval rights to request.");
        p.Rounds++;
        var accepted = Progression.LicensingRng.NextDouble() < Math.Clamp(.4 + series.Fanbase / 1000000 + FindPerson(p.CreatorId)!.Reputation / 300, .4, .85);
        var result = accepted ? "Accepted with a concession" : "Partner declined this counteroffer";
        if (accepted)
        {
            if (c.Action == LicenseAction.Payment) { p.Payment = p.Payment * 11 / 10; p.Weeks += 4; }
            if (c.Action == LicenseAction.Control) { p.Control++; p.Payment = p.Payment * 9 / 10; }
            if (c.Action == LicenseAction.Schedule) { p.Weeks += 8; p.Reliability = Math.Min(.98, p.Reliability + .08); }
        }
        else if (p.Rounds == 2 && p.Roll < .2) { p.Phase = LicensePhase.Declined; result = "Partner ended negotiations"; }
        p.Negotiations.Add($"{Clock.Now:d}: {c.Action} — {result}"); p.Outcome = result;
    }
    private void CreateLicenseOffer(Series s, LicenseKind kind)
    {
        var rng = Progression.LicensingRng;
        var previous = Progression.Projects.Where(p => p.SeriesId == s.Id && p.Kind == kind && p.Phase == LicensePhase.Completed).ToArray();
        var profiles = (kind == LicenseKind.Anime ? ProgressionCatalog.AnimationPartners : ProgressionCatalog.GoodsPartners)
            .Where(profile => Progression.Projects.Count(p => p.Partner == profile.Name && LiveLicense(p)) < 3).ToArray();
        if (profiles.Length == 0) { Emit(EventType.IndustryNews,"All suitable partners currently have full production schedules.",s.Id); return; }
        var profile = profiles[rng.NextInt(profiles.Length)];
        var partner = profile.Name;
        if (Progression.Projects.Count(p => p.Partner == partner && LiveLicense(p)) >= 3) return;
        var p = new LicenseProject
        {
            Id = AllocateId(), SeriesId = s.Id, BusinessId = s.BusinessId, CreatorId = s.RightsLeadPersonId,
            Kind = kind, Partner = partner, CreatedAt = Clock.Now, DueAt = Clock.Now.AddDays(30),
            Payment = kind == LicenseKind.Anime ? 200000 + (long)Math.Min(2000000, s.Fanbase * 2) : 30000 + (long)Math.Min(300000, s.Fanbase * .3),
            CreatorPercent = 20 + rng.NextInt(3) * 10, Control = kind == LicenseKind.Anime ? rng.NextInt(3) : 0,
            Reliability = Math.Clamp(profile.Reliability + rng.NextDouble() * .16 - .08, .6, .98),
            Fit = Math.Min(100, 55 + rng.NextDouble() * 30 + (s.Genre == profile.Specialty ? 15 : 0)),
            Roll = rng.NextDouble(), Weeks = kind == LicenseKind.Anime ? 26 + rng.NextInt(3) * 13 : 12,
            SourceChapters = kind == LicenseKind.Anime ? 24 : 0,
            SourceStart = previous.Select(x => x.SourceStart + x.SourceChapters).DefaultIfEmpty(0).Max(),
            Season = previous.Select(x => x.Season).DefaultIfEmpty(0).Max() + 1
        };
        Progression.Projects.Add(p);
        Emit(EventType.LicenseOffered, $"{partner} offers {kind} licensing for {s.Title}: ¥{p.Payment:N0}, creator share {p.CreatorPercent}%. Compare terms before signing.", s.Id);
    }
    private void SettleLicense(LicenseProject p, long income)
    {
        if (income <= 0) return;
        var share = income * p.CreatorPercent / 100;
        AccountPost(BusinessOf(p.BusinessId).Account, income, $"{p.Kind} license receipts · {p.Partner}", AccountEntryKind.LicenseIncome, p.SeriesId);
        AddBill(p.BusinessId, share, $"{p.Kind} creator share", p.CreatorId);
        Progression.Receipts.Add(new(p.Id, Clock.Now, income, share));
    }
    private void LicensingConsultationStep()
    {
        foreach (var p in Progression.Projects.Where(p => p.Kind == LicenseKind.Anime && p.Phase is LicensePhase.PreProduction or LicensePhase.Production && p.Involvement > 0).OrderBy(p => p.Id))
        {
            var person = FindPerson(p.CreatorId)!;
            if (!person.Schedule.IsRegularHour(TickStart) || TickStart.Hour != person.Schedule.WorkStartHour ||
                person.BusyUntil > TickStart || Progression.ConsultationDays.GetValueOrDefault(person.Id).Date == TickStart.Date ||
                TickStart.DayOfWeek == DayOfWeek.Sunday ||
                p.Involvement == 1 && TickStart.DayOfWeek is not (DayOfWeek.Monday or DayOfWeek.Thursday)) continue;
            if (person.Employment?.BusinessId != p.BusinessId) continue;
            person.BusyUntil = Clock.Now;
            person.HoursWorkedToday++;
            p.ConsultationHours++;
            Progression.ConsultationDays[person.Id] = TickStart.Date;
        }
    }
    private void LicensingStep()
    {
        if (Clock.Now.Day == 1)
            foreach (var s in Series.Where(s => s.Fanbase >= 10000 && SourceCount(s) > 0).OrderBy(s => s.Id))
            {
                if (Progression.LicensingRng.NextDouble() >= .12) continue;
                var kind = (LicenseKind)Progression.LicensingRng.NextInt(4);
                if (HasSequelMaterial(s,kind) && !Progression.Projects.Any(p => p.SeriesId == s.Id && p.Kind == kind && (LiveLicense(p) || p.Phase == LicensePhase.Offer)) &&
                    Progression.PitchCooldowns.GetValueOrDefault($"{s.Id}:{kind}") <= Clock.Now)
                {
                    Progression.PitchCooldowns[$"{s.Id}:{kind}"] = Clock.Now.AddDays(90);
                    CreateLicenseOffer(s, kind);
                }
            }
        foreach (var p in Progression.Projects.OrderBy(p => p.Id))
        {
            var s = FindSeries(p.SeriesId)!;
            if (p.Phase == LicensePhase.Offer)
            {
                if (p.BusinessId != s.BusinessId || p.CreatorId != s.RightsLeadPersonId) { p.Phase = LicensePhase.Expired; p.Outcome = "Rights changed before agreement"; }
                else if (p.EmployerRequestedAt is {} asked && asked.AddDays(7) <= Clock.Now && !p.EmployerApproved)
                {
                    p.EmployerApproved = p.Roll < .85;
                    if (!p.EmployerApproved) p.Phase = LicensePhase.Declined;
                    p.Outcome = p.EmployerApproved ? "Employer approved; review and accept the agreement" : "Employer declined this proposal";
                    Emit(EventType.LicenseDecision, $"{s.Title}: {p.Outcome}.", s.Id);
                }
                if (p.DueAt <= Clock.Now) { p.Phase = LicensePhase.Expired; p.Outcome = "Offer expired"; }
                continue;
            }
            if (!LiveLicense(p)) continue;
            if (p.Decision.Length > 0)
            {
                if (p.DecisionAt!.Value.AddDays(14) > Clock.Now) continue;
                if (p.Decision == "Source material") { p.ShortSeason = true; p.SourceChapters = Math.Max(1, SourceCount(s) - p.SourceStart); }
                else { p.DueAt = p.DueAt.AddDays(56); p.Reliability = Math.Min(1, p.Reliability + .05); }
                p.Outcome = "Default response: retain approved rights and allow a safer schedule";
                p.Decision = "";
            }
            if (p.Kind == LicenseKind.Anime && p.Phase == LicensePhase.Production && !p.WarningChecked && p.DueAt.AddDays(-60) <= Clock.Now)
            {
                p.WarningChecked = true;
                if (p.Roll > p.Reliability || p.Roll < .05)
                {
                    p.Decision = "Schedule pressure"; p.DecisionAt = Clock.Now;
                    Emit(EventType.LicenseDecision, $"{s.Title}: the anime team reports schedule pressure and a creative dispute. Review the proposed delay within fourteen days.", s.Id);
                    continue;
                }
            }
            if (p.DueAt > Clock.Now)
            {
                if (p.Kind != LicenseKind.Anime && p.Phase == LicensePhase.Released && (p.LastReceipt is null || p.LastReceipt.Value.AddMonths(1) <= Clock.Now))
                { SettleLicense(p, (long)(p.Payment * .08 * p.Reception / 100)); p.LastReceipt = Clock.Now; }
                continue;
            }
            if (p.Phase == LicensePhase.PreProduction)
            {
                p.Phase = LicensePhase.Production;
                p.DueAt = Clock.Now.AddDays(p.Weeks * 7);
                p.Outcome = "In production";
                if (p.Kind == LicenseKind.Anime && SourceCount(s) < p.SourceStart + p.SourceChapters)
                {
                    p.Decision = "Source material"; p.DecisionAt = Clock.Now;
                    Emit(EventType.LicenseDecision, $"{s.Title}: the anime may catch up. Finish a shorter season, add side stories, or approve an original ending if your contract permits.", s.Id);
                }
                continue;
            }
            if (p.Phase == LicensePhase.Production)
            {
                if (p.WarningChecked && p.Roll < .02 && p.Reliability < .92)
                { p.Phase = LicensePhase.Cancelled; p.Outcome = "Production cancelled; signing payment is retained, release payment not due"; Emit(EventType.LicenseDecision, $"{s.Title}: {p.Outcome}.", s.Id); continue; }
                var quality = s.Chapters.Where(c => c.Status == ChapterStatus.Complete).Select(c => c.Quality ?? 0).DefaultIfEmpty(50).Average();
                p.Reception = Math.Clamp(.45 * quality + .3 * p.Fit + 25 * p.Reliability + Math.Min(5, p.ConsultationHours / 20d) + (p.Roll - .5) * 20, 0, 100);
                if (p.Original) p.Reception = Math.Clamp(p.Reception + (p.Fit - 70) * (p.OriginalEnding ? .3 : .15) +
                    (p.Roll - .5) * (p.OriginalEnding ? 16 : 8), 0, 100);
                p.Phase = LicensePhase.Released; p.ReleasedAt = Clock.Now;
                p.DueAt = Clock.Now.AddDays(p.Kind == LicenseKind.Anime ? 91 : 365);
                p.Outcome = p.Reception >= 75 ? "A well-received release" : p.Reception >= 55 ? "Mixed reception, with some new readers" : "Disappointing reception";
                if (!p.Settled) { SettleLicense(p, p.Payment - p.Payment / 5); p.Settled = true; }
                double reach = (p.Kind == LicenseKind.Anime ? 1 : .4) * (p.ShortSeason ? .65 : 1) / Math.Sqrt(p.Season);
                s.Fanbase += FanbaseRules.Saturated(s.Fanbase, Math.Min(50000, (200 + Math.Sqrt(s.Fanbase) * 10) * p.Reception / 100 * reach));
                Progression.Effects.Add(new(p.Id, s.Id, Clock.Now, Clock.Now.AddDays(182), Math.Max(.01, p.Reception / 500)));
                if (p.Reception >= 75) RecognitionImpact(s, 4d / p.Season);
                if (p.Reception < 45)
                {
                    var creator = FindPerson(p.CreatorId)!;
                    var outstanding = Progression.Projects.Where(x => x.CreatorId == p.CreatorId && x.ReputationRecoveryAt is not null).Sum(x => x.ReputationLoss);
                    p.ReputationLoss = Math.Min(creator.Reputation, Math.Min(2, Math.Max(0, 5 - outstanding)));
                    ChangeReputation(creator, -p.ReputationLoss); p.ReputationRecoveryAt = Clock.Now.AddDays(182);
                }
                MarkMilestone(p.Kind == LicenseKind.Anime ? p.Season == 1 ? "anime_release" : "anime_followup" : "merchandise_license", s.Id, $"{s.Title}: {p.Kind} released. {p.Outcome}.", p.Season == 1 || p.Reception >= 75);
                Emit(EventType.LicenseReleased, $"{s.Title}: {p.Outcome}. {p.Kind} reception {p.Reception:0}/100.", s.Id);
            }
            else if (p.Phase == LicensePhase.Released)
            { p.Phase = LicensePhase.Completed; p.Outcome = "Agreed season or license term completed; a follow-up needs a new offer"; }
        }
        foreach (var p in Progression.Projects.Where(p => p.ReputationRecoveryAt <= Clock.Now))
        { ChangeReputation(FindPerson(p.CreatorId)!, p.ReputationLoss); p.ReputationLoss = 0; p.ReputationRecoveryAt = null; }
    }
}
