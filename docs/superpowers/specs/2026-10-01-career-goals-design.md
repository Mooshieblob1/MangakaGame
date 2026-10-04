# Career goals board: design

Date: 2026-10-01. Status: design approved in conversation (Q48 to Q51 and three
design sections); written spec approved by the user on 2026-10-01.

Background: feedback pasted by the user on 2026-09-29 (source not stated):
"I'm confused as to what metrics or things I should be going for. There's a
bunch of options like go to con, print different stuff, things like that which
isn't explained" and "there's no visual goal specifically for me to achieve or
go for in order to play more." The user asked for things to strive for:
research points, unlocks, training, story milestones, and rewards at each
milestone.

An audit (2026-09-29) found most long-term goals already exist but are
invisible: ten career milestones appear only after they are earned, unlock
requirements (the studio desk at reputation 20, branches, property tiers) show
only as errors, Helper-Chan's guidance goes quiet after the first hire, skills
grow invisibly, and Aki starts at 95 in every stage.

## Goal

A player always knows what to aim for next, sees progress toward it and is
rewarded for reaching it, so there is a reason to play on. Each unexplained
option (conventions, print shops) is explained by the goal that needs it.

## Decisions

### Q48. What comes first (decided 2026-09-29)

A career goals board, built from the milestones and unlocks that already
exist. Research or know-how points, training and Aki's story milestones are
later sub-projects that plug into the board as more goals and rewards. Every
goal gives a reward (user addition).

### Q49. Rewards (decided 2026-09-29)

Unlocks and one-off opportunities: furniture or equipment, office trophies and
decorations, new options, Helper-Chan scenes and special events, plus a modest
one-off cash or fan boost. No permanent bonuses to skill, sales or speed. This
keeps awards decision 17 (milestones open opportunities, never automatic
permanent bonuses) and the economy fixed in Tier 1 fix 3. Rejected: permanent
bonuses (compound over a career), cash and fans only (flat late), cosmetic only
(weak pull).

### Q50. Structure (decided 2026-09-29)

Career chapters: five named chapters following the real journey, 4 to 6 goals
each. Only the current chapter is shown; its goals can be done in any order.
Finishing a chapter gives a bigger reward and a Helper-Chan scene, then opens
the next. Rejected: rolling goals (no arc), everything at once (too much at
once, tester finding A3).

### Q51. Helper-Chan's guidance (decided 2026-09-29)

One system, two roles: the board shows what (goals and progress), Helper-Chan
says how (short tips for the current goals). Her existing path steps become
goals of the early chapters, and later chapters get tips too, so guidance
continues after the first hire. Rejected: two separate lists.

## Chapters and goals

A goal counts when reached by any route. Numbers are starting values; the
playtest harness tunes them (see Testing). Rewards marked "goal" are small;
chapter rewards are the big moments.

### Chapter 1: Doujin Days

| Goal | Counts when | Goal reward |
|---|---|---|
| Finish your first doujin | a self-published work is print-ready | ¥5,000 |
| Sell your first copy | any copy of your work sells (print or download) | wall print decoration |
| Attend a convention | you take part in a booked convention | ¥10,000 |
| Reach 100 fans | your titles' fans total 100 | plant decoration |
| Earn ¥50,000 from your own sales | doujin sales income totals ¥50,000 | ¥10,000 |

Chapter reward: a trophy shelf with your first doujin on it, a Helper-Chan
scene, and a convention invitation (your next convention table is free).

### Chapter 2: Rookie

| Goal | Counts when | Goal reward |
|---|---|---|
| Pitch a magazine or enter a newcomer contest | a pitch or contest entry is submitted | ¥10,000 |
| Hear an editor's verdict | a pitch is accepted or rejected | framed editor's letter decoration (whatever the verdict) |
| Win a serialization or place in a contest | a serialization starts, or a contest placement | ¥30,000 |
| Reach 1,000 fans | your titles' fans total 1,000 | +200 fans for your newest series |

Chapter reward: the professional drawing desk unlocks (replacing its hidden
reputation 20 gate), a Helper-Chan scene, and an editor's visit (your next
pitch gets a better chance, once).

### Chapter 3: Serialized

| Goal | Counts when | Goal reward |
|---|---|---|
| Publish 10 magazine chapters | 10 chapters published in magazines | ¥30,000 |
| Reach the top 10 | a series ranks 10th or better in its magazine (top 5 at first; changed after the playtests, see the completion record) | ranking chart decoration |
| Release your first collected volume | a magazine series releases a collected volume | ¥50,000 |
| Hire your first assistant | an assistant joins your staff | supportive chair, free |
| Reach 10,000 readers | the existing milestone | +1,000 fans for your newest series |

Chapter reward: an award plaque for the office, a Helper-Chan scene and
¥100,000.

### Chapter 4: Studio Head

| Goal | Counts when | Goal reward |
|---|---|---|
| Move out of your parents' house | you work from a studio property | housewarming plant set |
| Employ 3 staff | three staff on your payroll | ¥50,000 |
| Incorporate | your business is incorporated | company sign decoration |
| Run two series at once | two of your series are active | ¥80,000 |
| Be shortlisted for the Manga Craft Award | a series is shortlisted for the annual award | gold frame decoration |

Chapter reward: a display cabinet for awards, a Helper-Chan scene and
¥300,000.

### Chapter 5: Legend

The existing top milestones: an anime adaptation, a merchandise deal, a million
readers, an iconic series and a sustainable studio year. Each goal reward is a
trophy for the cabinet. Chapter reward: a final Helper-Chan scene.

After Legend the game continues without end; a short list of optional mastery
goals (later the Steam achievement list, early-access Q3) keeps the board
useful. Mastery goals give trophies only.

## The board

- **Goals page:** a "Goals" item in the left rail with a badge such as "3/5".
  The page shows the chapter name in Lilita One, chapter progress, and one card
  per goal: the goal, one line on why it matters, a progress bar with real
  numbers ("¥32,000 of ¥50,000"), its reward, and a How? button. Finished goals
  get a mint tick; the chapter reward is previewed at the bottom.
- **Office dashboard:** a "This chapter" card with the two closest goals.
- **Helper-Chan's phone:** when a chapter opens she texts about its goals; How?
  shows her tip for that goal (where conventions, print shops and the other
  options are explained); when a goal is done she congratulates you and names
  the reward.
- **Celebrations:** a goal shows a short notice and her text, and the game keeps
  running, even at 32x. A chapter stops the game, plays the good-news music,
  shows her scene and grants the chapter reward; then the next chapter's goals
  appear.

## Rewards in practice

- Cash goes to the account you work from (the doujin budget, or the company
  once incorporated), recorded in the ledger as "Goal reward: ...".
- Fans go to your newest series.
- Decorations and trophies arrive in office storage to place. They are new
  catalogue items drawn from simple shapes in code, like existing furniture
  (no art credits).
- Unlocks show in the furniture catalogue as "Unlocked by: Rookie chapter"
  instead of a hidden number.
- Opportunity rewards are one-off: a free convention table, a better chance on
  the next pitch.

## Rules

- Existing careers: on load, goals already met count as done and the career
  starts in its first unfinished chapter. Their unlocks and decorations are
  granted; their cash and fans are not, so an old save gets no windfall.
- Goals follow Aki, like the career: moving employer, working for another
  studio or starting over keeps them.
- The same goals and rewards on every difficulty and in Sandbox (Sandbox still
  disables platform achievements only).
- No permanent bonuses (Q49, decision 17).
- Deterministic: goals are checked in the simulation, so replays and saves
  behave as before. The save gains one small section; older saves keep loading.

## Testing

- **Unit tests:** each goal's check and progress numbers, rewards granted
  exactly once, chapter order, old-save backfill without cash or fans, any
  route counting, determinism.
- **Playtest harness:** every chapter can be finished, with how long each takes
  in game time (Doujin Days within the first one to two in-game months is the
  starting target); flags goals that are too slow or never reached.
- **Godot checks:** a new `--goals-smoke` (page, dashboard card, phone tips,
  goal notice keeps 32x running, chapter stop, rewards in storage and ledger);
  the display sweep gains the Goals page at every size, text size and theme.
- **Rendered review** of the Goals page, dashboard card and celebrations in
  both themes at 1920 x 1080, 3440 x 1440 and 1280 x 720 with 150% text.
- **Not verifiable automatically:** whether the goals make players want to play
  on. That needs the user, then testers.

## Out of scope

Research or know-how points, staff and Aki training, Aki's story milestones
beyond the chapter scenes, and the Steam achievements themselves. Each is a
later sub-project that plugs into this board.

## Build approval

Building and running checks for this work needs the user's confirmation at the
plan handoff. Packaging a tester build needs its own go-ahead.
