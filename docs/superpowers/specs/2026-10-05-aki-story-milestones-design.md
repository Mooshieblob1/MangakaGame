# Aki's story milestones: design

Date: 2026-10-05. Status: draft for review. Not implemented, not approved.

Background: the ten-year balance pass
(`docs/superpowers/ten-year-balance-findings.md`) found the middle game empty
for careers with a hit. From their fifth year those careers make 0 to 5
decisions a year, almost all accepting licence offers, with quiet stretches of
90 to 285 days and 0 or 1 Helper-Chan texts a year. The coordinator (with
discretion from Blob) chose to pull Aki's story milestones forward from Tier 3,
starting with this spec only.

What exists: Helper-Chan's conversations (`HelperStories` in `Career.cs`) are a
six-scene arc, everyday scenes and goal chapter scenes. Each offers two answers,
but the answers only change later wording. Nothing a player picks costs or
changes anything in the career.

## Goal

Every quiet year of an established career brings one or two moments that need
the player: a choice about Aki's own life or work, where each answer has a
visible cost and a visible effect, told through Helper-Chan in her usual
voice. The milestones should make a settled studio feel like a life still
unfolding, not add busywork.

## Principles

- **A real choice.** Each milestone has two answers, and both are reasonable.
  Neither is "the right one" in every career.
- **The cost is on the card.** The card states in plain words what each answer
  costs and gives (yen, weeks of Aki's time, readers, staff), before the player
  picks. No hidden penalties.
- **Built from existing systems.** Effects use money, Aki's working hours,
  series status and contracts, staff, fans, track record, licences and
  overseas interest, which all exist. No new screens beyond the existing
  conversation card.
- **One-off, never permanent bonuses**, matching the goals board rule (Q49).
- **Quiet moments only.** A milestone never interrupts a deadline crunch, a
  cancellation warning or a debut. It waits for a calm stretch.
- **Optional to defer, not to dodge.** "Later" is allowed, as today, but each
  milestone has a deadline after which a default answer applies and is told.

## When they trigger

A milestone becomes due when all of these hold:

1. The career has finished the Serialized goal chapter (a team, a collected
   volume, the top 10), so the studio is settled.
2. Nothing has stopped 32x for 45 days and no Helper-Chan text is unread.
3. No other milestone or arc scene was offered in the last 120 days.
4. The milestone's own condition (below) is met.

This gives a settled career about two or three milestones a year, which fits
the measured quiet stretches of 90 to 285 days. Struggling careers, which stay
busy with pitches and cancellations, rarely meet rule 2 and are not
interrupted. They see milestones once they settle.

Each milestone happens at most once per career, in the order its condition is
first met.

## The milestones

Six, each with its condition, its two answers and what they cost. Numbers are
gameplay estimates to tune in a re-run of the ten-year playtest.

### 1. The final arc

Condition: a series has run for 60 months, around when readers start to tire
(fatigue starts at 70 months).

Helper-Chan: the editor asks whether the story is heading for its ending.

- **Plan the ending.** The series ends on a high note in about 12 chapters,
  with the existing ending bonus, and its readers follow Aki to the next title
  (the 30% following). Cost: the hit's page fees and new volumes stop.
- **Keep it running.** The contract continues and the page fee rises 10%.
  Cost: readers tire as now, and the next ask comes in two years.

### 2. A bigger magazine calls

Condition: a series has ranked in its magazine's top 3 for 12 issues, and Aki
has fewer than two series running.

Helper-Chan: an editor from a tier 1 magazine invites Aki to launch a new series
there.

- **Accept.** The next pitch to that magazine is guaranteed an offer within a
  year. Cost: a second series needs a desk and an assistant, and Aki's hours
  split between two titles.
- **Stay loyal.** The current editor raises the page fee 10% and adds a cover
  feature (a one-off reader boost). Cost: the tier 1 door stays closed for two
  years.

### 3. The assistant who wants to debut

Condition: an assistant has worked at the studio for two years with a high
skill.

Helper-Chan: the assistant has been drawing their own pages at night.

- **Back their debut.** The assistant leads a new series under the studio (the
  existing staff proposal), and stays. Cost: that assistant's hours go to their
  own title, so the studio may need another hire.
- **Ask them to wait.** Nothing changes now. Cost: their wellbeing drops, and if
  it falls far they leave with their idea within the year.

### 4. Teaching at a manga school

Condition: Aki's reputation is high enough for the industry to notice (an award
shortlist or a top 10 series).

Helper-Chan: a vocational school asks Aki to teach one evening a week for a
school year.

- **Teach.** Aki loses about four working hours a week for a year. Gains: a
  one-off boost to the studio's track record, and a promising graduate appears
  among the next recruitment's candidates.
- **Decline.** No cost, no gain. Helper-Chan notes that the offer may not come
  again.

### 5. An overseas convention

Condition: from 2000 (period detail to research, for example a North American
anime convention inviting Japanese guests), and a series with a collected
volume.

Helper-Chan: Aki is invited as a guest of honour abroad for a week.

- **Go.** Aki is away for seven days (no drawing; chapter buffers matter), and
  travel costs come from the business. Gains: international interest for the
  overseas channel and a reader boost.
- **Send a signed art board instead.** A small cost in Aki's time (one day).
  A smaller interest boost.

### 6. Family at the old house

Condition: Aki has moved out, and personal savings exceed ¥5 million.

Helper-Chan: Aki's parents mention that the house needs repairs and they are
thinking about it carefully.

- **Help pay for it.** ¥2 million to ¥3 million from personal savings (scaled to
  the period), and Aki's parents visit the studio. Gain: Aki's wellbeing is
  restored, and a warm scene. This is the one money sink in the set.
- **Leave it to them.** No cost. Helper-Chan mentions it gently once more a
  year later.

## What the player sees

- Helper-Chan sends one text that names the milestone and says a decision is
  waiting. It stops 32x like other path texts.
- The existing conversation card shows the scene, then the two answers. Under
  each answer, one line states its cost and gain in plain words, for example
  "Ends in about 12 chapters. Readers follow you to your next series."
- The answer appears in the story journal and, where it has a lasting effect
  (teaching, a pending tier 1 offer), as a line in the relevant page until it
  ends.

## Saves and determinism

- New saved state: milestones offered, answers given, and any running effect
  with its end date (teaching until, guaranteed offer until). Older saves start
  with none offered and are otherwise unchanged.
- Triggers are checked on the daily narrative tick that already exists, with no
  new randomness except where a milestone needs it, drawn from the existing
  narrative random stream so replays stay identical.

## How we will know it works

- Re-run the ten-year playtest with the bot answering milestones (first answer
  on even seeds, second on odd). Target: careers with a hit make at least four
  decisions a year after their fifth year, and no quiet stretch runs past about
  150 days.
- Struggling careers see no milestone before they settle.
- Unit tests for each condition, each answer's effect, deferral and the default
  answer, save round trip and replay.
- Rendered check of the card with cost lines at 1280 x 720 and 150% text.

## Open questions, for one at a time

1. Whether to implement this now, or after the other Tier 2 items.
2. Whether six milestones is the right size for the first pass, or three
   (recommended order: the final arc, a bigger magazine calls, the assistant
   who wants to debut, because they change the career most).
3. Whether a deferred milestone's default answer should be the safer one
   (recommended) or the one Helper-Chan favours.

## Out of scope

Research or know-how points and training stay in Tier 3. The ten-year
playtest showed training adds little while page quality already sits at 90 to
99, and research points would need something worth buying.
