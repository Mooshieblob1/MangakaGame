# Tier 1 fix 1: career guidance and Helper-Chan's phone, implementation plan

Date: 2026-09-26. Design: [career guidance considerations](../specs/2026-09-26-career-guidance-considerations.md)
(Q12 to Q15). Finding addressed: guidance stops after the first sale.
Status: implemented 2026-09-26. See the
[completion record](../career-guidance-completion.md).

## Part A. Simulation guidance (src/MangakaSim/Guidance.cs)

Guidance stays read-only: it never changes game state, so saves, replays and
determinism are unaffected.

1. Merge the "opening" and "doujin" routes into one career path. Old saves
   with either route load onto the career path. Contest and employment stay as
   side routes; the "Where shall we go next?" choice is removed from the main
   path and moves to the Help page.
2. New steps after the first sale, in order:
   - `continue-series`: the project is a one-shot. Explain that pitching needs
     an ongoing series; target Series details, where the "Continue as ongoing
     series" button lives.
   - `pitch`: name the magazine with the best chance shown (skipping magazines
     on cooldown), say a first rejection is normal, target Publishing.
   - `pitch-sample`: the 31-page sample is being drawn; show its due date.
   - `pitch-name-review` and `pitch-name-redo`: the editor is reviewing the
     Name, or asked for a revision, with the answer date.
   - `pitch-waiting`: the sample is finished; the answer comes at the
     magazine's next issue close, with its date.
   - `pitch-rejected`: after a rejection, explain the weakest factor in plain
     words, the date that magazine accepts a new pitch (26 weeks), which other
     magazines are open now, and what to do meanwhile.
   - `offer`: accept or decline before the expiry date, with the page fee and
     what deadlines mean.
   - `first-deadline`: serialized with nothing published yet; show the first
     close date and how to watch progress.
   - `first-hire`: serialized and published with no assistants. Uses the
     affordability rule from Tier 1 fix 2 (first-hire safety); until fix 2
     lands, a placeholder rule: business funds cover three months of the
     cheapest candidate's wage.
   - `career-settled`: final message; guidance goes quiet and points to Help
     for side routes.
3. Every message is written as a few short texts (about 140 characters at
   most each) rather than one paragraph, per Q14.
4. Guidance keeps a message thread in `GuidancePreferences` (presentation
   data, saved with the career): each message holds its step, in-game time,
   texts and read state. `Observe` appends a message when the step changes.
   Helper-Chan's existing notices (for example from "Show me") also go into
   the thread. Older saves start with an empty thread.

## Part B. Helper-Chan's phone (godot)

1. Replace the next-step card in `DebugMain.Alpha.cs` with a phone control:
   - Before 1 Jan 2010 in game time: a PHS handset frame with a green-tinted
     screen. From 2010: modern chat bubbles. Same thread in both.
   - Her portrait beside each message, in-game date and time, and a "Show me"
     reply button under the latest message.
   - Slides up with a soft buzz on a new message; dismiss or completing the
     step shrinks it to a phone icon with an unread count; the icon reopens the
     scrollable thread.
2. The handset scales with the text size setting instead of shrinking text;
   compact window handling in `DebugMain.FloatingOffice.cs` is updated.
3. Draw the handset frame in code (no generated assets, no service credits).
4. Research note: confirm which PHS services offered short text messages in
   1996 and typical handset looks; record in a research ledger entry.

## Part C. Tests and checks

- Simulation tests: each new step is reached from a scripted career; old
  "doujin" and "opening" routes migrate; the thread saves and loads; guidance
  never changes `GameState` (state JSON identical before and after).
- The career playtest bot follows guidance targets after the first sale and
  reports any point where guidance is stale or missing.
- Godot smoke checks updated for the phone ("Show me", reopen from the icon,
  hide and resume, era switch at 2010).
- Rendered captures at 1920x1080, 2560x1080, 3440x1440 and 1280x720 with 150%
  text, both eras, retained as AVIF.

Running tests, builds and captures needs the user's go-ahead each time.

## Completion criteria

- From a new career, guidance names a correct, actionable next step at every
  point up to a serialized series with a first hire, verified by the playtest.
- The phone is readable at every supported layout and text size in captures.
- No change to simulation results for the same seed and commands.
