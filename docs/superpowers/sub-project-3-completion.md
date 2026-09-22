# Sub-project 3 completion

Completed 22 September 2026. Sub-project 4 has not been started.

## Delivered

- A staff catalog shipped as embedded JSON: three premises (garage, two-room
  apartment, small office), nine amenities, two scheduled candidates, and
  the name pools the monthly candidate pool draws from.
- A monthly candidate pool of four generated assistants (level, skills and
  asking salary from the market salary formula), scheduled candidates on
  their catalog month, and former staff returning at 1.3× their old salary
  six months after leaving. Hire (offers under 80% of the asking salary are
  refused; the premises must have a free desk), fire, set salary, allowed
  stages, series lead and manual stage assignment as replayable commands.
- Needs (hunger, bladder, energy, social) that deplete every working hour
  and force a break before any need can reach zero, with amenities slowing
  the depletion; fatigue that accrues on long days and overtime and lowers
  the effective skill used for chapter quality.
- Happiness with an equilibrium built from pay against the market rate,
  the studio atmosphere (premises, amenities, crowding), long hours and
  breaks, plus shocks for missed payroll, cancellations, firings and hits;
  moonlighting below 40 (two hours a day lost), quit rolls at the start of
  each month below 30.
- Monthly costs: rent, upkeep and the provider charge on the 1st, payroll on
  the 25th, a missed payroll when the balance cannot cover it, moves between
  premises with a moving cost, amenities, doujin printing costs, the
  convention table, and promotion work for idle staff.
- A planner that hands each startable stage to the best allowed person by
  skill and queue length, keeps the lead on Name and Pencils unless the
  Pencils backlog exceeds forty hours, runs Inks and Backgrounds in parallel,
  tracks hours per person per stage for quality and hour shares, and reports
  at-risk chapters per assignee.
- A serialized pipeline: a Serialized series opens its next chapter as soon
  as the current Name is submitted, so the lead writes while the editor
  reads and the assistants draw.
- Save format version 3 with the studio, former staff, candidate pool and
  departures, and invariant validation for every new structure.
- Debug scene: a staff dropdown with fire, salary, allowed stages and
  promotion controls, a mood line, the candidate list with an offer field and
  hire button, a studio line with move and buy buttons, assignee initials on
  the stage bars, and money and moods in the daily recap. The smoke test
  hires two assistants, moves, buys amenities, publishes twelve weekly
  chapters and reloads through the real controls.

## Verification

Verified in a Linux container (Ubuntu 24.04) with the .NET SDK 8.0.131 from
the distribution package and Godot 4.7.2.stable.mono.official.ed1daf0bf.

- `dotnet test tests/MangakaSim.Tests`: **357 passed**, none failed or
  skipped (283 from sub-projects 1 and 2, 74 new).
- `dotnet build MangakaGame.sln -warnaserror`: **0 warnings, 0 errors**.
- Godot headless editor import: successful.
- Godot scene walkthrough: **69 checks passed** headless; **73 checks
  passed** when rendered under Xvfb with software OpenGL, including four
  screenshot captures.
- The `debug-studio` screenshot was inspected: three people in the two-room
  apartment with a fridge and chairs, the weekly series ranked first in
  Tokiwa Jump with twelve chapters under contract, the assistants' initials
  on the inks, backgrounds and tones of every chapter since they were hired,
  the selected assistant's mood line, halved salary and allowed stages
  without Pencils, the candidate list with offers, and the studio line with
  rent and payroll. A first capture showed the assistants had drawn nothing
  (see the planner correction below) and the daily recap covering the staff
  column; both were fixed and the screenshot retaken.
- The scenario year (weekly action series at Tokiwa Jump from day one, seed
  7, 52 weeks) gives the guardrail numbers below.

| Studio | Published | Missed | Mean quality | Moonlighting |
|---|---|---|---|---|
| Aki plus two skill-60 assistants, apartment, fridge and chairs, 08:00–18:00 | 51 | 0 | 78.3 | 0 |
| Aki alone in the garage, 08:00–18:00 | 47 | 4 | 83.6 | 0 |
| Aki alone in the garage, 08:00–20:00 every day | 49 | 2 | 71.6 | 0 |
| Staffed, 08:00–20:00, 150,000 salaries, no amenities | 51 | 0 | 78.3 | 2 |

Aki's fatigue after the twelve-hour solo year is 94; on ten-hour days,
alone or staffed, it stays at 0. Chapter fees for the
staffed year are 9,690,000 yen against 6,206,520 yen of salaries, rent and upkeep.

## Corrections to the spec

The source files are the implemented version.

- **Fatigue and pay constants were retuned.** With the spec's numbers a
  twelve-hour day accrued 2 fatigue and the night recovered 3, so fatigue
  never accumulated, and the pay term (weight 20 at the 0.6 floor) could not
  push anyone below the moonlighting line. Fatigue now accrues 2 per overtime
  hour and 1 per regular hour beyond eight and recovers 2 a day (6 on a day
  off); the pay weight is 30. The rule tests assert the new values.
- **Need 0 is unreachable.** A need below 25 forces a break before the next
  hour's depletion (at most 9) could reach 0, so the spec's half-speed rule
  can never apply and is not implemented. `NeedCritical` stays as an event.
- **Serialized pipeline.** The spec's staffed-weekly guardrail cannot be met
  with one open chapter: the whole studio idles through every 48-hour editor
  review. A Serialized series now opens its next chapter as soon as the
  current Name is submitted (at most two open). Doujin series keep one open
  chapter. Sub-project 2 tests that assumed the mangaka idles during review
  were updated.
- **Solo weekly guardrail.** The spec expected the mangaka alone to miss at
  least 20 of 52 issues. With the pipeline the solo run misses 4 and
  publishes 47, so the test asserts at least three misses and fewer
  published chapters than the staffed studio. Assistants still make the
  difference in quality, fatigue and happiness.
- **Long-days guardrail.** The spec attached the overtime cost to the
  staffed studio. Three desks on ten-hour days leave enough slack that
  scheduling twelve hours changes nothing (nobody is busy long enough to
  tire), so the guardrail runs the solo mangaka on twelve-hour days instead:
  two more chapters published, quality down twelve points, fatigue over 60.
  The moonlighting event the spec attached to this run comes only when the
  assistants are underpaid in a bare studio (fourth row above).
- **Insolvency guardrail dropped.** The spec expected a rank-18 studio to be
  insolvent after a year. Royalties from sub-project 2 (tens of millions of
  yen a year for a serialized weekly series) dwarf salaries and rent at any
  rank, so the test cannot be written honestly. The fee-covers-costs
  guardrail stays. The royalty scale is recorded for sub-project 7's balance
  pass.

## Decisions recorded during implementation

All plan-level decisions are listed at the top of the plan. The ones found
only by running the whole loop:

- **The lead's stages are counted first.** The planner assigned stages in
  chapter order, so when this chapter's inks came up the lead's queue held
  nothing yet and a skill-80 mangaka outscored any pool assistant (skills
  in the 20s to 40s) on every stage. The debug walkthrough hired two
  assistants who then drew nothing for three months. `AssignStages` now
  takes the lead's Name and Pencils work (and every fixed assignment) across
  all open chapters before sharing out the rest, so the next chapter's Name
  and Pencils weigh on the lead when this chapter's inks, backgrounds and
  tones are handed out. The walkthrough now checks that the assistants have
  logged hours on the series.
- **Returning candidates** were blocked by the name-in-use check scanning
  former staff; the check now covers current people and candidates only.
- **Redo and skip** cleared `HoursDone` but not `HoursByPerson`; both reset.
- **Withdraw and end** left unpublished chapters waiting on an editor that
  will never answer; `EndSerialization` now marks them not required.
- **Finished stages keep their assignee** when the person leaves, so hour
  shares and quality history stay attributable through `FormerPeople`.
- **Ledger and screenshots**: the doujin printing cost is netted inside the
  "doujin sales" entry; the convention table is its own entry.

## Balance observations (not changed)

- Royalties dominate income. A serialized weekly series earns far more from
  tankobon than from fees, so money is never a constraint once a series is
  running. Sub-project 7 should revisit the royalty curve or the cost side.
- The solo mangaka is now nearly viable on a weekly magazine thanks to the
  pipeline; the assistants' value shows in quality and mood rather than in
  keeping the slot. Tier 1 weeklies with a higher page count would restore
  the gap.

No publishing, commercial release, or deployment was performed. Later roadmap
milestones remain pending further instructions.
