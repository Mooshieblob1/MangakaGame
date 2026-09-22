# Staff and Studio Implementation Plan

**Status: approved plan, implementation in progress.** Completion notes will be written to `docs/superpowers/sub-project-3-completion.md`.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Turn the one-person garage into a studio: a monthly candidate pool, hiring and firing, salaries and payroll, premises and amenities, needs that force breaks, fatigue that lowers quality, happiness that drives moonlighting and quitting, monthly costs, a planner that hands stages to the best available person with inks and backgrounds in parallel, and a debug scene that exposes all of it.

**Architecture:** As in sub-projects 1 and 2: partial classes on `GameState` (`GameState.Staff.cs`, `GameState.Hiring.cs`, `GameState.Costs.cs`) adding steps to `Tick()`, stateless rule modules under `src/MangakaSim/Rules/` with exact-number tests, catalog data as embedded JSON (`Data/staff.json`), all randomness through `GameState.Rng`, no Godot in `MangakaSim`.

**Tech Stack:** .NET 8 SDK, C# 12, System.Text.Json, xUnit 2.9, Godot 4.7.2 stable mono.

**Spec:** `docs/superpowers/specs/2026-09-22-staff-studio-design.md` (section numbers below refer to it).

## Global Constraints

- Everything from the sub-project 1 and 2 plans still holds.
- Tick order becomes: `Clock.Advance`, `NeedsStep`, `WorkStep`, `EditorStep`, `IssueCloseStep`, `SalesStep`, `PitchStep`, `CostsStep`, `RiskStep`, `DayEndStep`, midnight rollover, then `FatigueStep`, `HappinessStep`, `MoonlightStep` and the monthly quit rolls on the new day.
- Save format `Version` = 3. Version 2 saves are rejected as unsupported.
- New `EventType` values are appended after `WentOnline` in the spec's order.
- Every commit message ends with the attribution lines given by the session.

## Decisions locked in by this plan (not spelled out in the spec)

- **Break hour.** A break replaces the whole working hour: no hours accrue, `HoursWorkedToday` does not advance, and an overtime hour spent on a break still counts against the overtime cap.
- **Need 0 is unreachable.** A need below 25 forces a break before the next hour's depletion (at most 9) could reach 0, so the spec's half-speed rule can never apply and is not implemented; `NeedCritical` stays as an event for future balance changes. `Person.RegularHoursToday` counts regular hours actually worked so fatigue and the seven-day windows are not inflated by a break taken in an overtime hour.
- **Needs reset** happens in `NeedsStep` on the first regular working hour of the day, detected by `HoursWorkedToday == 0 && BreaksToday == 0` at a regular hour.
- **Rolling seven-day windows** for overtime share and breaks per day use two small per-person ring buffers (`RecentOvertime`, `RecentRegular`, `RecentBreaks` as `List<int>` of at most seven entries) appended at midnight. They are saved.
- **Moonlighting hours.** A moonlighting assistant's effective `WorkEndHour` is `max(WorkStartHour + 1, WorkEndHour − 2)` for both planning and work, and `IsOvertimeHour` returns false for them.
- **Quit rolls** run at midnight on the first day of the month, after `HappinessStep`, so the month's last happiness is what counts.
- **Former people.** `GameState.FormerPeople` keeps the `Person` record of anyone who quit or was fired (queues cleared) so `HoursByPerson`, ledger entries and events keep resolving names. `FindPerson` searches current people only; `FindAnyPerson` searches both.
- **Re-hire pool entry** is a normal candidate with the former person's name and skills, `AskingSalary = round(1.3 × old salary / 1000) × 1000`, added at the first pool refresh six or more months after the quit; it is not subject to the pool-size cap.
- **Scheduled candidates** ignore the pool-size cap and the level draw; their asking salary is `MarketSalary(skills) × multiplier` from the catalog (`AskingMultiplier`), rounded to 1,000.
- **Candidate ids** come from `AllocateId`. Hiring reuses the candidate's id for the new person.
- **Lead rule threshold.** "More than 40 hours of startable work" counts the lead's queued person-hours (remaining hours divided by their skill multiplier) over startable items only.
- **Manual assignments** live on `StageWork.ManualAssignee` (`int?`) and win until the stage is done or the person leaves.
- **Promotion** is stored as `Person.PromotionSeriesId` (`int?`, with a sentinel `PromotionAll = 0` for the whole studio) plus `Person.IsPromoting` (bool); it is applied in `WorkStep` when the person has no `CurrentTask`.
- **Payroll and rent at 09:00** run in `CostsStep` on the tick whose `TickStart` is 09:00 of the 25th and the 1st respectively, tracked by `LastPayrollMonth` and `LastCostsMonth` (month precision) so a save loaded mid-day never double-charges.
- **Doujin printing** is charged inside the same `"doujin sales"` ledger entry (net amount); the convention table fee is a separate `"convention table"` entry.
- **Atmosphere crowding** uses the current people count against capacity; the studio starts in the garage with capacity 2, so hiring a second assistant needs the apartment.
- **`ComputeAtRisk`** with several assignees: each assignee's remaining share (their stages' remaining person-hours) is compared with their own regular hours before the due date; unassigned stages fall to the lead.
- **Solo weekly guardrail** uses the real editor and the real pipeline; the staffed guardrail hires two skill-60 assistants directly through a test-only catalog candidate rather than searching seeds.

## File structure

| Path | Responsibility |
|---|---|
| `src/MangakaSim/Catalog/StaffCatalog.cs` | `Premises`, `Amenity`, `ScheduledCandidate`, name pools, load and validate `staff.json` |
| `src/MangakaSim/Data/staff.json` | embedded resource |
| `src/MangakaSim/Staff.cs` | `PersonRole`, `Needs`, `Candidate`, `StudioState`, `PersonMood` |
| `src/MangakaSim/Rules/NeedsRules.cs`, `FatigueRules.cs`, `HappinessRules.cs`, `MoonlightRules.cs`, `PayRules.cs`, `HiringRules.cs`, `AssignmentRules.cs`, `PremisesRules.cs`, `PromotionRules.cs` | pure rules |
| `src/MangakaSim/GameState.Staff.cs` | `NeedsStep`, `FatigueStep`, `HappinessStep`, `MoonlightStep`, quit rolls, shocks |
| `src/MangakaSim/GameState.Hiring.cs` | pool refresh, scheduled and returning candidates, hire, fire, salary, allowed stages, lead, manual assignment |
| `src/MangakaSim/GameState.Costs.cs` | `CostsStep`, payroll, rent, upkeep, provider, move, amenities, promotion |
| `tests/MangakaSim.Tests/Rules/StaffRulesTests.cs` | pure rule tests |
| `tests/MangakaSim.Tests/StaffCatalogTests.cs`, `NeedsTests.cs`, `HiringTests.cs`, `AssignmentTests.cs`, `CostTests.cs`, `MoodTests.cs`, `SaveV3Tests.cs`, `StudioScenarioTests.cs` | state and scenario tests |
| `docs/superpowers/sub-project-3-completion.md` | completion notes |

Modified: `Stage.cs`, `Model.cs`, `Market.cs` (`LedgerEntry.PersonId`), `GameEvent.cs`, `EventType.cs`, `Settings.cs`, `Commands.cs`, `GameState.cs`, `GameState.Planner.cs`, `GameState.Work.cs`, `GameState.Risk.cs`, `GameState.Recap.cs`, `GameState.Reputation.cs` (hour shares), `GameState.Sales.cs`, `GameState.Market.cs` (pool refresh trigger), `GameState.Serialization.cs`, `Rules/QualityRules.cs`, `Rules/SalesRules.cs`, `godot/DebugMain.cs`, `godot/DebugMain.SmokeTest.cs`, `README.md`, roadmap.

---

### Task 1: Dependency graph, hours by person, fatigue hook

**Files:** `Stage.cs`, `Model.cs`, `GameState.Planner.cs` (`IsStartable`), `GameState.Work.cs`, `GameState.Reputation.cs` (`HourShares`), `Rules/QualityRules.cs`; tests `StageTests.cs`, `WorkTests.cs`, `Rules/QualityRulesTests.cs`.

- `StageOrder.Prerequisites(stage)`: Name `{}`, Pencils `{Name}`, Inks and Backgrounds `{Pencils}`, Tones `{Inks, Backgrounds}`.
- `IsStartable` uses prerequisites. With one person nothing observable changes except that Backgrounds becomes startable while Inks is in progress; the single-person queue still takes them in stage order.
- `StageWork.HoursByPerson` (`Dictionary<int,double>`) filled by `WorkStep`; `HourShares` reads it.
- `QualityRules.SkillFactor(skill, redoCount, fatigue)` with `FatigueRules.EffectiveSkill`; `QualityRules.WeightedSkill(HoursByPerson, skillOf)` gives the hours-weighted mean skill used for a stage's contribution.

- [ ] Tests: prerequisites table; Backgrounds startable when Inks is in progress; hours by person sums to hours done; weighted skill of 10 h at 80 and 10 h at 60 → 70; fatigue 100 at skill 80 → 56 and a clean chapter → 65; all 283 existing tests stay green.
- [ ] Implement, run, commit `feat(sim): stage dependency graph, hours by person and fatigue hook`.

---

### Task 2: Staff catalog, model, new game, save version 3

**Files:** `Catalog/StaffCatalog.cs`, `Data/staff.json`, `Staff.cs`, `Model.cs`, `Market.cs`, `GameState.cs`, `GameState.Serialization.cs`; tests `StaffCatalogTests.cs`, extend `GameStateTickTests.cs`, `SerializationTests.cs`, `RegressionTests.cs`.

- Catalog per spec section 2 with the shipped premises, nine amenities, two scheduled candidates and pools of at least 60 names each; validation rules per section 2.
- `Person` gains the section 1 fields plus `RecentOvertime`, `RecentRegular`, `RecentBreaks`, `ManualAssignee` on `StageWork`, `Series.LeadId`, `GameState.Studio`, `Candidates`, `FormerPeople`, `ScheduledCandidatesShown`, `LastPoolRefreshMonth`, `LastPayrollMonth`, `LastCostsMonth`, `MissedPayrolls` on `StudioState`.
- `NewGame`: Aki as Mangaka, needs 100, happiness 70, all stages allowed, garage, empty pool.
- `CurrentVersion = 3`; `ValidateSave` drops the one-person check and adds the section 10 checks.

- [ ] Tests: catalog loads with three premises, nine amenities, two scheduled candidates, pools ≥ 60, `Require` and validation failures on mutated JSON; new game studio and person defaults; version 3 in JSON, version 2 rejected; round trip.
- [ ] Implement, run, commit `feat(sim): staff catalog, studio state and save version 3`.

---

### Task 3: Pure staff rules

**Files:** the nine rule files, `Rules/SalesRules.cs` (`DoujinIncome` with print cost); tests `Rules/StaffRulesTests.cs`, extend `Rules/MarketRulesTests.cs`.

- [ ] Tests with the exact numbers of spec section 12 (needs depletion and recovery, fatigue accrual and recovery, equilibrium values, moonlight and quit chances, market salary, asking salary range, starting happiness, assignment score, move cost and monthly charge, doujin income 180 per copy).
- [ ] Implement, run, commit `feat(sim): staff, pay, hiring, assignment and premises rules`.

---

### Task 4: Needs, breaks, fatigue

**Files:** `GameState.Staff.cs`, `GameState.Work.cs`, `GameState.cs`, `GameState.Recap.cs`; tests `NeedsTests.cs`.

- `NeedsStep` per spec section 3, `FatigueStep` per section 4 at the midnight rollover, `TookBreak` and `NeedCritical` events, `DailyRecapPayload.Moods`.

- [ ] Tests: no break on a 10-hour day; on a 08:00–20:00 schedule with overtime the second overtime hour is a break; needs reset at the first working hour; a fridge changes recovery; fatigue after a week of overtime lowers the next chapter's quality; a week off restores it; `TookBreak` once per person per day.
- [ ] Implement, run, commit `feat(sim): needs, breaks and fatigue`.

---

### Task 5: Hiring

**Files:** `GameState.Hiring.cs`, `Commands.cs`, `GameState.Commands.cs`, `GameState.Market.cs` (refresh trigger); tests `HiringTests.cs`.

- Pool refresh, scheduled candidates, returning candidates, `Hire`, `Fire`, `SetSalary`, `SetAllowedStages`, `SetSeriesLead`, `AssignStage` per spec section 7.

- [ ] Tests: first refresh in May 1996 fills four candidates with skills in 5..95 and names from the pools; Eiichido Oga appears in July 1997 once; hire at asking, below 80%, past capacity; fire pays severance; salary shocks; allowed stages and lead validation.
- [ ] Implement, run, commit `feat(sim): candidate pool, hiring and staff commands`.

---

### Task 6: Assignment planner v2

**Files:** `GameState.Planner.cs`, `GameState.Risk.cs`, `GameState.Work.cs` (moonlighting hours); tests `AssignmentTests.cs`.

- [ ] Tests: lead keeps Name; inks go to the inker while Aki pencils the next chapter; inks and backgrounds run in the same hour on two desks; manual assignment wins; a leaver's stage is reassigned with hours kept; at-risk per assignee.
- [ ] Implement, run, commit `feat(sim): skill-based assignment with parallel stages`.

---

### Task 7: Costs, premises, amenities, promotion

**Files:** `GameState.Costs.cs`, `GameState.Sales.cs`, `Commands.cs`, `GameState.Commands.cs`; tests `CostTests.cs`.

- [ ] Tests: payroll on the 25th per assistant; missed payroll event and shock; rent, upkeep and provider on the 1st; move charges one month's rent and validates capacity; amenity purchase; doujin income nets printing; convention table fee; promotion adds fans to a doujin series in idle hours when online.
- [ ] Implement, run, commit `feat(sim): payroll, rent, premises, amenities and promotion`.

---

### Task 8: Happiness, moonlighting, quitting

**Files:** `GameState.Staff.cs`; tests `MoodTests.cs`.

- [ ] Tests: equilibrium drift; shocks on raise, cut, cancellation, colleague quit, move; moonlighting starts under a chosen seed, shortens the day, stops after a raise; quit roll fires once under a chosen seed, the person moves to `FormerPeople`, their stage is reassigned, they return to the pool six months later at 1.3 × salary.
- [ ] Implement, run, commit `feat(sim): happiness, moonlighting and quitting`.

---

### Task 9: Save validation, recap, scenarios

**Files:** `GameState.Serialization.cs`; tests `SaveV3Tests.cs`, `StudioScenarioTests.cs`.

- [ ] Tests: a year with three people, one quit and one fired round-trips and replays; a mutated copy per new invariant; the three guardrails of spec section 12 (solo weekly fails, staffed weekly holds, overtime costs quality and triggers moonlighting, solvency by rank).
- [ ] Implement, run, commit `feat(sim): staff save validation and studio scenarios`.

---

### Task 10: Debug scene, smoke test, docs

**Files:** `godot/DebugMain.cs`, `godot/DebugMain.SmokeTest.cs`, `README.md`, roadmap, `docs/superpowers/sub-project-3-completion.md`.

- [ ] Staff panel, hiring panel, studio panel, assignee initials, recap moods; smoke test per spec section 11; strict build; headless and rendered runs; docs.
- [ ] Commit `feat(godot): staff, hiring and studio debug controls, smoke test and docs`.

---

## Self-review notes

- Spec coverage: section 1 (Tasks 1, 2), section 2 (Task 2), section 3 (Task 4), section 4 (Tasks 1, 4), section 5 (Task 8), section 6 (Task 7), section 7 (Task 5), section 8 (Task 6), section 9 (Tasks 2, 4, 7, 8), section 10 (Tasks 2, 9), section 11 (Task 10), section 12 (every task).
