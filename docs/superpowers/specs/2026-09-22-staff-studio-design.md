# Sub-project 3: Staff and Studio — Design

Date: 2026-09-22
Status: implemented on 2026-09-22 (plan `../plans/2026-09-22-staff-studio.md`, completion notes `../sub-project-3-completion.md`)
Roadmap: `2026-09-22-roadmap.md`
Builds on: `2026-09-22-publishing-market-design.md` and the sub-project 2 code
on branch `claude/sp2-plan-implementation-z2feyy`

## Goal

Turn the one-person garage into a studio. The owner hires assistants from a
monthly candidate pool, pays them, houses them, keeps them fed, watered and
comfortable, and steers who draws what. Assistants take pencils, inks,
backgrounds and tones off the mangaka's desk so a weekly flagship slot becomes
possible; overtime finally costs something (needs, happiness, fatigue and
therefore quality); unhappy staff moonlight and eventually quit. Money stops
being a score and becomes a constraint: rent, salaries, printing, convention
tables and the internet provider all come out of the ledger every month.

At the end of this sub-project the owner can open the debug scene, hire two
assistants, move out of the garage, buy a fridge, watch a weekly Tokiwa Jump
series ship on time with three people on it, see an assistant's happiness sink
under overtime, give a raise, and lose an assistant who was never paid enough.
Everything stays headless-testable and deterministic.

## Non-goals (deferred)

- Named rivals, era events, poaching offers from other studios (5). Hooks are
  left in the candidate pool (scheduled candidates) and the quit path.
- 3D office and walking characters (4). The premises model here is what the
  office scene will render.
- Real UI (6). Anime, awards, merchandise (7).
- Training and skill growth over time. Skills are fixed at hire in this
  sub-project; growth belongs with balance in 7.
- Personal life sim. Needs stay at hunger, thirst and comfort, as the roadmap
  fixes.
- Print runs as a player decision. Doujin printing is on demand at a per-copy
  cost; print-run sizing waits for 7.
- Bankruptcy. Money may go negative; the consequences are staff happiness and
  hiring refusals, not a game over.

## Architecture

Same pattern as sub-project 2: partial classes on `GameState` that each add
steps to `Tick()`, pure rule modules with exact-number tests, catalog data as
embedded JSON, all randomness through `GameState.Rng`, no Godot in
`MangakaSim`.

- `GameState.Staff.cs`: `NeedsStep` (breaks), `FatigueStep`, `HappinessStep`
  (daily), `MoonlightStep` (daily), quit rolls (monthly).
- `GameState.Hiring.cs`: candidate pool refresh, scheduled candidates, hire,
  fire, salary changes.
- `GameState.Costs.cs`: payroll, rent, provider fee, printing and convention
  costs, promotion.
- `GameState.Planner.cs`: assignment rewritten around skills, allowed stages,
  leads, manual assignments and the stage dependency graph.
- Rules: `NeedsRules`, `HappinessRules`, `FatigueRules`, `PayRules`,
  `HiringRules`, `AssignmentRules`, `PremisesRules`, `MoonlightRules`,
  `PromotionRules`.
- Catalog: `Catalog/StaffCatalog.cs` loading `Data/staff.json` (name pools,
  premises tiers, amenities, scheduled candidates).

### New files

| File | Responsibility |
|---|---|
| `src/MangakaSim/Catalog/StaffCatalog.cs` | `Premises`, `Amenity`, `ScheduledCandidate`, name pools, load and validate `staff.json` |
| `src/MangakaSim/Data/staff.json` | embedded resource |
| `src/MangakaSim/Staff.cs` | `Candidate`, `Needs`, `PersonRole`, `StageAssignment`, `StudioState` |
| `src/MangakaSim/Rules/*.cs` | one file per rule module above |
| `src/MangakaSim/GameState.Staff.cs`, `GameState.Hiring.cs`, `GameState.Costs.cs` | steps and commands |
| `tests/MangakaSim.Tests/Rules/*Tests.cs`, `StaffTests.cs`, `HiringTests.cs`, `CostTests.cs`, `AssignmentTests.cs`, `StudioScenarioTests.cs` | tests |

Modified: `Model.cs`, `Stage.cs` (dependency graph), `GameEvent.cs`,
`EventType.cs`, `Settings.cs`, `Commands.cs`, `GameState.cs`,
`GameState.Planner.cs`, `GameState.Work.cs`, `GameState.Risk.cs`,
`GameState.Recap.cs`, `GameState.Sales.cs` (doujin costs, promotion),
`GameState.Serialization.cs`, `Rules/QualityRules.cs` (fatigue in the skill
factor, hour-weighted skill), `Rules/SalesRules.cs` (printing cost),
`godot/DebugMain.cs`, `godot/DebugMain.SmokeTest.cs`.

## Section 1: Data model additions

### Static catalog (never saved)

```
Premises { string Id; string Name; int Capacity; int MonthlyRent; double Atmosphere; }
Amenity  { string Id; string Name; int Cost; int MonthlyUpkeep; string Need;  // hunger, thirst, comfort, or "none"
           double RecoveryPerBreak; double Atmosphere; }
ScheduledCandidate { string Name; DateTime AppearsAt; int MonthsAvailable;
                     Dictionary<Stage,int> Skills; int AskingSalary; string Note; }
NamePools { string[] Given; string[] Family; }
```

### GameState

```
StudioState Studio;               // premises id, owned amenity ids, moved-in date
List<Candidate> Candidates;       // current pool
List<string> ScheduledCandidatesShown;   // names already offered, so each appears once
DateTime LastPoolRefreshMonth;    // month precision
DateTime LastPayrollMonth;        // month precision
```

```
StudioState { string PremisesId; List<string> Amenities; DateTime MovedInAt; }
Candidate   { int Id; string Name; Dictionary<Stage,int> Skills; int AskingSalary;
              DateTime AvailableUntil; bool IsScheduled; string? Note; }
```

### Person

```
PersonRole Role;                  // Mangaka, Assistant
int Salary;                       // nominal yen per month; 0 for the mangaka
DateTime? HiredAt;
Needs Needs;                      // Hunger, Thirst, Comfort: 0..100, 100 is satisfied
double Happiness;                 // 0..100
double Fatigue;                   // 0..100
bool IsMoonlighting;
int MonthsEmployed;
HashSet<Stage> AllowedStages;     // stages the planner may hand this person
bool OnBreak;                     // this hour is a break, not work
int BreaksToday;
```

`Needs { double Hunger; double Thirst; double Comfort; }` with
`Min => min of the three`.

`NewGame` gives Aki `Role = Mangaka`, `Salary = 0`, needs 100, happiness 70,
fatigue 0, all stages allowed.

### Series

`int LeadId` (defaults to Aki). The lead is the person the Name stage always
goes to and the default for Pencils.

### StageWork

`Dictionary<int,double> HoursByPerson` replaces the single-owner reading of
`AssignedTo`. `AssignedTo` stays as the *current* assignee. `HoursDone` stays
the sum. Quality uses the hours-weighted mean skill of everyone in
`HoursByPerson`, which retires sub-project 2's "person who logged the final
hour" rule.

### Chapter

`Stage` dependencies replace the linear rule: Name → Pencils → {Inks,
Backgrounds} → Tones. `StageOrder.All` keeps its order for display;
`StageOrder.Prerequisites(stage)` returns `{}` for Name, `{Name}` for Pencils,
`{Pencils}` for Inks and Backgrounds, `{Inks, Backgrounds}` for Tones. A stage
is startable when every prerequisite is Complete or Skipped. Two people can
therefore work one chapter at once (inks and backgrounds), which is the whole
point of assistants.

## Section 2: Catalog data

### Premises

| Id | Name | Capacity | Rent (1996 yen/month) | Atmosphere |
|---|---|---|---|---|
| `garage` | The garage | 2 | 0 | −10 |
| `apartment` | Two-room apartment | 4 | 80,000 | 0 |
| `office` | Small office | 8 | 250,000 | +10 |

The game starts in the garage. Moving costs one month's rent of the new
premises up front (nothing for the garage). Amenities move with the studio.

### Amenities

| Id | Need | Cost | Upkeep/month | Recovery per break | Atmosphere |
|---|---|---|---|---|---|
| `kettle` | thirst | 8,000 | 500 | 40 | 0 |
| `water-cooler` | thirst | 40,000 | 3,000 | 70 | +2 |
| `snack-shelf` | hunger | 15,000 | 12,000 | 40 | 0 |
| `fridge` | hunger | 90,000 | 4,000 | 70 | +2 |
| `folding-chairs` | comfort | 12,000 | 0 | 30 | −2 |
| `office-chairs` | comfort | 120,000 | 0 | 60 | +4 |
| `sofa` | comfort | 60,000 | 0 | 50 | +3 |
| `radio` | none | 6,000 | 0 | 0 | +2 |
| `air-conditioner` | none | 150,000 | 6,000 | 0 | +5 |

Without any amenity for a need, a break restores 20 (the person steps out).
When several amenities serve one need the best `RecoveryPerBreak` applies.
All yen are 1996 prices multiplied by `Economy.PriceIndex` at purchase or
at each monthly charge. Upkeep is charged with rent.

### Name pools and scheduled candidates

`staff.json` ships at least 60 given names and 60 family names in the
near-real style the roadmap asks for. Generated candidates are
`Given Family` drawn with `Rng`; a name already in use by a person or a
candidate is re-rolled.

Scheduled candidates are the hook for sub-project 5. The shipped list holds
two placeholders so the mechanism is exercised:

| Name | Appears | Available | Skills (N/P/I/B/T) | Asking | Note |
|---|---|---|---|---|---|
| Eiichido Oga | 1997-07-01 | 3 months | 55/70/75/60/65 | market rate | future star, draws fast |
| Akiro Tokiyama | 1998-03-01 | 3 months | 40/85/80/70/50 | market × 1.2 | brilliant penciller, moody |

Sub-project 5 replaces this list; nothing else references the names.

### Validation on load

`InvalidDataException` for duplicate ids, capacity < 1, negative money
values, `RecoveryPerBreak` outside 0..100, an amenity need not in
`{hunger, thirst, comfort, none}`, name pools under 60 entries, scheduled
candidates with skills outside 0..100 or `MonthsAvailable < 1`.

## Section 3: Needs and breaks

Needs matter only during working hours. Every hour a person works, needs
deplete:

| Need | Regular hour | Overtime hour |
|---|---|---|
| Hunger | −5 | −8 |
| Thirst | −6 | −9 |
| Comfort | −3 | −6 |

Comfort depletion is reduced by the best comfort amenity: office chairs −1
per hour instead of −3, sofa −2, folding chairs −3 (it is the baseline).

At the start of each person's first working hour of the day all three needs
reset to 100 (they slept and ate at home). Days off do nothing.

### Breaks (`NeedsStep`, before `WorkStep`)

If any need is below 25 at the start of a working hour, the person takes a
break instead of working: `OnBreak = true` for that hour, no hours accrue,
`BreaksToday++`, and the lowest need recovers by the amenity's
`RecoveryPerBreak` (20 without one). The other two needs each recover 10.
One break per hour at most; a second need below 25 after the break triggers
another break next hour. Breaks happen in overtime hours too.

Emit `TookBreak` only for the first break of the day per person, so the log
does not drown. `NeedCritical` fires when a need reaches 0, which is only
possible when breaks keep being needed faster than they help (no amenities and
long overtime). A person at need 0 works at half speed for that hour and
takes a happiness hit (section 5).

Breaks are the cost of cheap premises: a person taking two breaks a day loses
20% of their output. Buying a fridge and chairs is the fix.

### `NeedsRules`

```
static Needs Deplete(Needs n, bool overtime, double comfortRate);
static bool NeedsBreak(Needs n);                              // any < 25
static Needs Recover(Needs n, string lowestNeed, double recovery);   // +recovery on that need, +10 on the others, cap 100
static double ComfortRate(IEnumerable<Amenity> owned);       // best comfort amenity: 1, 2, or 3
static double Recovery(string need, IEnumerable<Amenity> owned);   // best amenity for the need or 20
```

## Section 4: Fatigue and quality

Fatigue is the price of overtime and long weeks. Per person per day:

```
Fatigue += 1.5 × overtimeHours + 0.5 × max(0, regularHours − 8)
Fatigue −= 6 on a day off, −3 on any other day (applied at DayStarted)
clamp 0..100
```

Effective skill for quality: `skill × (1 − 0.3 × Fatigue / 100)` inside
`QualityRules.SkillFactor`, which gains an optional `fatigue` argument (the
sub-project 2 hook). Speed is unaffected: tired people draw as fast but worse.
At fatigue 100 Aki's skill 80 behaves like 56 for quality, so a chapter drops
from 84 to about 65. This is the deliberate reason overtime is no longer free.

`FatigueRules.Accrue(overtimeHours, regularHours)`,
`FatigueRules.Recover(bool dayOff)`, `FatigueRules.EffectiveSkill(skill, fatigue)`.

## Section 5: Happiness, moonlighting, quitting

### Happiness (0..100)

Each person has an **equilibrium** computed daily from their situation, and
happiness moves a tenth of the way toward it every day, plus shocks:

```
Equilibrium = 50
            + 20 × (PayFactor − 1)              // PayFactor = clamp(Salary / MarketSalary, 0.6, 1.4)
            + Atmosphere / 2                    // premises + amenities − crowding, see below
            − 15 × OvertimeShare                // overtime hours / regular hours over the last 7 days
            − 10 × BreaksPerDay                 // breaks over the last 7 days / working days
            + 10 × min(1, StudioTrackRecord / 50)
clamped 0..100
Happiness += 0.1 × (Equilibrium − Happiness)
```

Shocks, applied when they happen:

| Event | Delta |
|---|---|
| A need reaches 0 | −5 |
| Payroll missed | −20 |
| Raise (salary up by ≥ 10%) | +10 |
| Pay cut | −15 |
| Series the person worked on ranks top 3 | +2 |
| Series the person worked on is cancelled | −5 |
| A colleague quits | −3 |
| Moved to better premises | +5 |

The mangaka has no salary, so their `PayFactor` is fixed at 1.0; everything
else applies to them too (their happiness feeds the studio atmosphere).

Atmosphere: `Premises.Atmosphere + Σ Amenity.Atmosphere − 8 × max(0, People − Capacity)`.
Hiring past capacity is refused (section 7), so the crowding term only bites
after a move down or when a scheduled candidate is squeezed in; the command
`MovePremises` to a smaller place is allowed and unpleasant.

### Moonlighting (assistants only)

Daily, after the happiness update: an assistant with `Happiness < 40` and
`!IsMoonlighting` rolls `Rng.NextDouble() < 0.15` to start moonlighting; emit
`MoonlightingStarted`. While moonlighting the person leaves two hours early
every working day (their effective `WorkEndHour` is two lower for planning and
work) and never works overtime. It stops when `Happiness >= 55`
(`MoonlightingStopped`). The player can see it in the person panel and the
daily recap; the cure is pay, amenities or less overtime.

### Quitting (assistants only)

On the first day of each month an assistant with `Happiness < 30` rolls
`Rng.NextDouble() < (30 − Happiness) / 100 × LoyaltyFactor`, with
`LoyaltyFactor = max(0.3, 1 − 0.05 × MonthsEmployed)`. Quit: the person is
removed, their in-progress stages keep `HoursByPerson` and lose their assignee
(the planner reassigns), colleagues take the −3 shock, emit `StaffQuit`. A
quit assistant reappears in the candidate pool six months later at 1.3 × their
old salary if their skills still fit the pool's level, so poaching them back is
possible and expensive.

### `HappinessRules`, `MoonlightRules`

```
HappinessRules.Equilibrium(payFactor, atmosphere, overtimeShare, breaksPerDay, trackRecord)
HappinessRules.Step(happiness, equilibrium)                  // 0.1 toward
HappinessRules.Atmosphere(premises, amenities, people, capacity)
MoonlightRules.StartChance(happiness)                        // 0.15 below 40, else 0
MoonlightRules.Stops(happiness)                              // >= 55
MoonlightRules.QuitChance(happiness, monthsEmployed)
```

## Section 6: Pay and costs

### Market salary

```
MarketSalary(skills) = round((120,000 + 2,000 × mean(skills)) × PriceIndex(now)) to the nearest 1,000
```

A skill-50 assistant is worth 220,000 yen a month in 1996; a skill-80 one
280,000. Sub-project 2's price index inflates it over the years.

### Payroll (`PayrollStep`, 25th of each month at 09:00)

For each assistant: ledger entry `−Salary`, reason `"salary"`, `PersonId`
on the entry (`LedgerEntry` gains `int? PersonId`). `MonthsEmployed++`. If
`Money < Σ salaries` before paying, pay nobody, emit `PayrollMissed`, apply
the −20 shock to every assistant and set `Studio.MissedPayrolls++`; the
missed month is not paid later. Two consecutive missed payrolls double every
assistant's quit chance for that month.

### Rent and upkeep (1st of each month at 09:00)

Ledger entries `"rent"` and `"upkeep"` (one entry summing all amenities),
plus `"internet provider"` 3,000 × index when `HasInternet`. Rent is charged
even when money is short (the balance goes negative).

### Doujin printing and conventions

`SalesRules.DoujinIncome` becomes `copies × (cover × 0.6 − printCost)` with
`printCost = 120 × PriceIndex(release)`; the studio nets 180 yen per 500-yen
copy in 1996. On each convention recap (first Monday of a month with doujin
sales) a `"convention table"` entry of −20,000 × index is written. Both are
shown in the recap message.

### Promotion (staff-run marketing, needs internet)

A new pseudo-stage `Promotion` is not a chapter stage; it is a person-level
mode: `SetPromotion(personId, seriesId?)` puts the person on promotion duty
for one series (or the whole studio when null) during any working hour in
which their queue has no startable item. Each such hour adds
`2 × (1 + mean(skills) / 100) × InternetReach(now)` fans to the series (or
split evenly across Unpublished series). It exists so an idle assistant is
never worthless and so doujin studios have a marketing lever. It is only
available when `HasInternet`.

### `PayRules`, `PremisesRules`

```
PayRules.MarketSalary(IEnumerable<int> skills, double priceIndex)
PayRules.PayFactor(int salary, int marketSalary)               // clamp 0.6..1.4
PayRules.AskingSalary(marketSalary, Rng)                       // × NextDouble(0.9, 1.15), nearest 1,000
PremisesRules.MoveCost(Premises target, double priceIndex)     // one month's rent
PremisesRules.MonthlyCharge(premises, amenities, online, priceIndex)   // rent + upkeep + provider
```

## Section 7: Hiring

### Candidate pool

On the first issue close of each month the pool is refreshed (the same
`LastTrendUpdateMonth` trigger, extended with `LastPoolRefreshMonth`):
candidates whose `AvailableUntil` has passed leave, then new ones are drawn
until the pool holds 4 (pool size is a balance constant). Each generated
candidate:

- Name from the pools.
- A **level** drawn from the studio's standing: `NextInt(25, 45)` plus
  `round(StudioTrackRecord / 4)`, capped at 75. Each stage skill is
  `clamp(level + NextInt(−15, 15), 5, 95)`. Name skill is additionally −10
  (assistants rarely write).
- `AskingSalary = PayRules.AskingSalary(MarketSalary(skills))`.
- `AvailableUntil = now + NextInt(1, 2) months`.

Scheduled candidates whose `AppearsAt` has passed and whose name is not in
`ScheduledCandidatesShown` join the pool with `IsScheduled = true` and their
fixed skills; emit `CandidateAppeared` with the note. Every refresh emits one
`CandidatePoolRefreshed` event listing the pool.

### Commands

| Command | Valid when | Effect |
|---|---|---|
| `Hire(candidateId, salary)` | candidate in pool, `salary >= 0.8 × Asking`, `People.Count < Capacity` | new Person (Assistant, all stages but Name allowed), needs 100, happiness 60 + 20 × (salary/asking − 1), removed from pool, emit `StaffHired` |
| `Fire(personId)` | assistant | removed; one month's salary severance in the ledger `"severance"`; colleagues −3; emit `StaffFired`; cannot fire the mangaka |
| `SetSalary(personId, salary)` | assistant, `salary >= 0` | shock per section 5; takes effect next payroll |
| `SetAllowedStages(personId, stages)` | non-empty set; the mangaka must keep Name | planner input |
| `SetSeriesLead(seriesId, personId)` | person allowed Name | Name and default Pencils go to them |
| `AssignStage(chapterId, stage, personId?)` | stage not done; person allowed the stage | manual assignment until the stage completes; null clears |
| `SetPromotion(personId, seriesId?)` | online; series Unpublished or null | promotion duty when idle |
| `MovePremises(premisesId)` | different premises, `People.Count <= Capacity`, `Money >= MoveCost` | move cost in the ledger, atmosphere shock |
| `BuyAmenity(amenityId)` | not owned, `Money >= Cost` | ledger `"amenity"` |

A hire below the asking salary but above 80% of it succeeds with a lower
starting happiness; below 80% the candidate refuses (`InvalidCommandException`
"won't work for that"). This keeps negotiation to one knob.

`HiringRules.StartingHappiness(salary, asking)`, `HiringRules.Level(trackRecord, Rng)`,
`HiringRules.Skills(level, Rng)`.

## Section 8: Assignment (planner v2)

`AssignStages` is rewritten. Every hour the planner runs it is idempotent and
cheap:

1. **Manual assignments win.** A stage with a manual `AssignStage` keeps it.
2. **Lead rule.** Name goes to the series lead. Pencils goes to the lead
   unless the lead's queue already holds more than 40 hours of startable work,
   in which case it goes to the best available penciller.
3. **Best available.** Every other unfinished stage goes to the person, among
   those allowed the stage and not moonlighting away that hour, with the
   highest `score = SkillMultiplier(skill) / (1 + queueHours / 20)`, where
   `queueHours` are the person-hours already queued for them. Ties go to the
   lower person id. A stage in progress keeps its assignee unless that person
   left; hours already logged stay in `HoursByPerson`.
4. **Queues** per person are ordered as before (pins, manual order, then due
   date, stage order, chapter id). `CurrentTask` is the first startable item;
   the dependency graph makes inks and backgrounds startable together on
   different desks.

`AssignmentRules.Score(skillMultiplier, queueHours)` and
`AssignmentRules.Best(candidates)` are pure and tested. `ComputeAtRisk` sums
remaining hours per assignee and compares each with that person's regular
hours before the due date; a chapter is at risk when any assignee's share is.

Hour shares for reputation (sub-project 2, section 9) now come from
`HoursByPerson`, so an assistant who inked half a chapter earns half the
inker's share.

## Section 9: Events, tick order, recap

### New `EventType` values (appended after `WentOnline`)

```
TookBreak, NeedCritical, StaffHired, StaffFired, StaffQuit, CandidateAppeared,
CandidatePoolRefreshed, PayrollPaid, PayrollMissed, SalaryChanged,
MoonlightingStarted, MoonlightingStopped, PremisesMoved, AmenityBought,
MonthlyCostsPaid, PromotionAssigned
```

Auto-pause defaults on: `StaffQuit`, `PayrollMissed`, `CandidateAppeared`
(scheduled ones only carry the flag; generated refreshes do not pause),
`NeedCritical`.

`DailyRecapPayload` gains `List<PersonMood>` (`PersonId, Happiness, Fatigue,
Breaks, IsMoonlighting`) and `long YenSpent`.

### Tick order

```
Clock.Advance
NeedsStep             (breaks decided for this hour; needs reset on first working hour)
WorkStep              (existing; skips people on break; records HoursByPerson; half speed at need 0)
EditorStep, IssueCloseStep, SalesStep, PitchStep   (existing; pool refresh rides the monthly trigger)
CostsStep             (1st 09:00 rent and upkeep; 25th 09:00 payroll)
RiskStep, DayEndStep  (existing)
midnight rollover     (existing; then FatigueStep, HappinessStep, MoonlightStep, monthly quit rolls)
```

## Section 10: Commands, save format, validation

All commands in section 7 join the `ICommand` polymorphic set. Save
`CurrentVersion = 3`; version 2 saves are rejected as unsupported (the
project has no players yet, so no migration).

`ValidateSave` additions: exactly one `Mangaka`; `Salary >= 0` and 0 for the
mangaka; needs, happiness and fatigue in 0..100; `AllowedStages` non-empty and
the mangaka allowed Name; every `LeadId` is a person allowed Name; candidates
have unique ids and names, skills in 0..100, `AvailableUntil > now`; premises
and amenity ids exist; amenities unique; `People.Count <= Capacity + 2`
(scheduled squeezes); `HoursByPerson` sums to `HoursDone` within 1e-6 and
names existing or former people (former ids are kept in
`GameState.FormerPeople` for history); ledger `PersonId` points at a person or
a former person; `MissedPayrolls >= 0`.

The sub-project 1 "one person" check is removed. `Money` may be negative.

## Section 11: Debug scene

`godot/DebugMain.cs` grows:

- The person column becomes a **staff panel**: a dropdown over people, the
  existing schedule controls for the selected person, needs bars, happiness
  and fatigue, salary field with Set, allowed-stage checkboxes, Fire,
  Promotion dropdown, moonlighting flag.
- A **hiring panel** under it: the candidate pool with skills and asking
  salary, a salary field and Hire per row, the pool refresh date.
- A **studio panel** in the market column: premises name, capacity, rent,
  atmosphere, Move buttons, owned amenities and Buy buttons for the rest,
  next payroll amount.
- The chapter table shows the assignee initials on each stage bar.
- The recap dialog lists moods and yen spent.

Smoke test additions (headless): hire two candidates at asking, move to the
apartment, buy a fridge and office chairs, create a weekly action series,
pitch to Tokiwa Jump under a seed that succeeds, accept, run three months,
assert at least ten chapters published with no more than one miss, assert
payroll and rent entries, force an assistant's happiness to 20 and run a
month to see moonlighting start, save and reload. One rendered screenshot.

## Section 12: Testing

### Pure rule tests (exact numbers)

- `NeedsRules`: a regular hour without amenities takes hunger 100 → 95; an
  overtime hour 100 → 92; office chairs make comfort −1; a break with a
  fridge restores hunger by 70 and the others by 10; `NeedsBreak` at 24.9.
- `FatigueRules`: 2 overtime hours and 10 regular → +4; a day off −6;
  effective skill 80 at fatigue 100 → 56 and quality 84 → 65 through
  `QualityRules`.
- `HappinessRules`: equilibrium at pay factor 1, atmosphere 0, no overtime,
  no breaks, track record 0 → 50; pay factor 1.4 → 58; atmosphere +12 → +6;
  overtime share 0.2 → −3; one step from 20 toward 50 → 23.
- `MoonlightRules`: start chance 0.15 at 39, 0 at 40; quit chance at
  happiness 10 with 0 months → 0.2, with 20 months → 0.06.
- `PayRules`: market salary skill 50 → 220,000; 80 → 280,000; 2026 index
  → 330,400 for skill 80; pay factor clamps; asking salary within 0.9..1.15.
- `HiringRules`: level range at track record 0 and 100; skills within 5..95;
  starting happiness 60 at asking, 52 at 80%, 70 at 150%.
- `AssignmentRules`: two candidates, skill 80 with 30 queued hours against
  skill 60 with 0 → the skill-60 person wins (1.2 vs 1.6/2.5 = 0.64).
- `PremisesRules`: move cost to the office in 1996 → 250,000; monthly charge
  for the apartment with a fridge and a kettle online → 80,000 + 4,500 + 3,000.
- `SalesRules`: doujin income per copy in 1996 → 180.
- Catalog: `staff.json` loads, three premises, nine amenities, two scheduled
  candidates, pools ≥ 60; each validation rule fails on a mutated copy.

### State tests

- Breaks: without amenities a 10-hour day ends with thirst at 40 and no
  break. On a 08:00 to 20:00 schedule thirst is 28 after twelve regular hours
  and 19 after the first overtime hour, so the second overtime hour is Aki's
  first break of the day. The test pins these hours exactly.
- Needs reset at the first working hour; days off untouched.
- Fatigue accrues over a week of overtime and lowers the next chapter's
  quality below 84; a week off restores it.
- Hire at asking: person added with role Assistant, happiness 60, Name not
  allowed; hire past capacity throws; hire below 80% throws; the candidate
  leaves the pool.
- Assignment: with Aki and one inker, a chapter's inks go to the inker while
  Aki pencils the next chapter; backgrounds and inks run in the same hour on
  two desks; a manual assignment overrides; the lead keeps Name.
- Payroll on the 25th writes one entry per assistant; short money misses
  payroll, emits the event and drops happiness by 20; rent on the 1st.
- Happiness sinks under daily overtime and low pay, moonlighting starts under
  a chosen seed, the person leaves two hours early, a raise stops it.
- Quit: happiness forced to 10, monthly roll fires once under a chosen seed,
  the person is gone, their half-inked stage is reassigned with hours kept,
  they reappear in the pool six months later at 1.3 × salary.
- Scheduled candidate appears in July 1997 with the shipped skills, once.
- Promotion: an idle assistant online adds fans to a doujin series each hour.
- Move to the office charges 250,000 and raises atmosphere; buying an
  amenity charges its cost and changes the break recovery.
- Save round trip after a year with three people, one quit, one fired, and a
  mutated copy per new invariant.

### Scenario and balance guardrails

- **Solo weekly fails, staffed weekly holds.** Aki alone on a serialized
  weekly Tokiwa Jump series (chapters forced through the real editor) misses
  at least 20 of 52 issues; Aki plus two skill-60 assistants (inker and
  background/tones) with an apartment, a fridge and office chairs publishes at
  least 48 of 52. This is the sub-project 2 balance observation turned into a
  test.
- **Overtime is not free.** The same staffed studio with 2 hours of overtime
  every day ends the year with mean chapter quality at least 6 points below
  the no-overtime run and at least one moonlighting event.
- **Money.** The staffed weekly studio at rank ≤ 10 is solvent after a year
  (chapter fees 19 × ~14,500 × 52 ≈ 14.3 million against ~9 million in
  salaries, rent and upkeep); the same studio at rank 18 for the whole year is
  not. These are the guardrails for the salary and rent constants.

## Hooks into earlier code

| Existing member | Change |
|---|---|
| `GameState.Tick()` | insert `NeedsStep`, `CostsStep`; midnight adds `FatigueStep`, `HappinessStep`, `MoonlightStep` |
| `AssignStages` | replaced by section 8 |
| `IsStartable` | dependency graph instead of "all earlier stages" |
| `WorkStep` | skip people on break; `HoursByPerson`; half speed at need 0; moonlighting early leave |
| `QualityRules.SkillFactor` | fatigue argument; hours-weighted skill from `HoursByPerson` |
| `HourShares` | from `HoursByPerson` |
| `ComputeAtRisk` | per-assignee remaining hours |
| `SalesRules.DoujinIncome` | printing cost |
| `ConventionRecap` | convention table cost |
| `UpdateTrendsMonthly` trigger | also refreshes the candidate pool |
| `ValidateSave` | section 10; the one-person check goes |
| `Settings.Default()` | four new auto-pause keys |
| `NewGame` | studio in the garage, empty pool, Aki as Mangaka |

## Decisions (approved defaults)

Each was presented with a recommended default; the owner approved all of them on 2026-09-22.

1. **Does the mangaka draw a salary?** Default: no. Aki is the owner; living
   costs are abstracted away. Alternative: a fixed 150,000/month "owner draw"
   that makes the early game tighter.
2. **Can money go negative?** Default: yes, with happiness and hiring
   consequences only. Alternative: rent unpaid for three months forces a move
   back to the garage.
3. **Stage concurrency.** Default: inks and backgrounds in parallel after
   pencils, tones after both. Alternative: keep the linear pipeline and let
   assistants only take whole stages, which makes the staffed-weekly guardrail
   harder to hit.
4. **Break granularity.** Default: a whole hour, one need at a time, in the
   hourly tick. Alternative: fractional productivity loss instead of whole
   hours, which is smoother but invisible in the recap.
5. **Moonlighting visibility.** Default: the player sees the flag. Alternative:
   hidden until a colleague or the recap hints at it, which is more
   Football-Manager but harder to debug.
6. **Quit re-hire.** Default: a quit assistant returns to the pool after six
   months at 1.3 × salary. Alternative: gone for good until sub-project 5's
   rival studios exist.
7. **Promotion duty.** Default: included, online only, idle hours only.
   Alternative: defer to 7 with merchandise and keep this sub-project to the
   staff loop.
8. **Scheduled candidate placeholders.** Default: ship the two roadmap names
   so the mechanism is tested. Alternative: ship an empty list and test the
   mechanism with a test-only catalog.
9. **Pool size and refresh.** Default: four candidates, refreshed monthly on
   the first issue close. Alternative: a weekly trickle of one.
10. **Difficulty.** Default: none yet; the prodigy start stays. Sub-project 7
    scales starting skills and money.
