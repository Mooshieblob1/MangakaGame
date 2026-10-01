# Mangaka Days private alpha

Build: 0.8.0-private-alpha.12. Windows x64, standalone, no Steam required.

This build adds a shorter top bar, clear screen-sized nametags, total chapter progress with coloured stage sections, continuous south walls and visible WC occupants behind a privacy blur. Helper-Chan stays at her desk during the creator's WC visits. It retains the modular chibi employees, animated Helper-Chan and parents, staggered family routines, rear stairs, genkan and Tokyo-style neighborhood. The default 08:00-18:00 workday lasts about 45 seconds at 8x without slowing character movement. Pausing the brief overnight transition preserves its progress; resuming continues at 32x, then restores your previous daytime speed at morning.

## Play

Extract the whole archive to a folder and run `MangakaDays.exe`. Keep the
executable, `.pck` and data directory together. Choose New Career or load an
existing career. Your previous saves remain in the application user-data folder,
normally `%APPDATA%/Godot/app_userdata/MangakaGame/careers`.

Helper-Chan's optional next-step card suggests a 16-page one-shot doujin.
“Show me” opens the relevant controls; it never places an order for you. Work
progresses using the normal speed controls. Printing spends the doujin budget (business funds after incorporation),
and local sales settle weekly on Mondays. Follow-up directions cover doujin
growth, contests and studio employment. Help → Objectives and direction restores
hidden guidance. Existing careers skip opening steps supported by their records.

Choose **Series → Create a one-shot doujin** for one complete story that stops
when finished, or **Create an ongoing series** for numbered chapter issues.
One completed chapter unlocks an issue; five completed chapters also unlock an
optional collected book. Self-published dates are personal targets, with no
missed-deadline or overtime pressure. Publisher contracts still have deadlines.
Pitching prepares a separate 31-page sample and keeps started chapters on hold;
no collected book is required first.

Use **Books → Print physical copies** for printing. Choose a printer and quantity,
check the cost and printing break-even estimate, then order the run. Delivery
starts local distribution automatically. The screen shows stock, physical sales
and the next Monday sales check. Reprinting does not reopen a closed local sales
window; conventions can still sell stock. Digital and overseas agreements are
separate.

**Books → Sell online** lists each completed doujin edition with no upfront cost.
The download price is half the printed cover price, with a 30% storefront fee.
Monday purchases depend on quality, genre popularity and the backlist audience;
no purchase means no income. This does not require the studio internet upgrade.

**Send to convention**, available from series details, Books and printing, opens
the title, attendee and event together. Check the booth/travel total and stock,
then reserve copies and confirm beside the quote. Reserved stock cannot sell
locally before the event; timely paid print deliveries can also be reserved.
Unsold copies return to ordinary stock afterward. Attendance is automatic, uses work time and promotes the selected
title. Only that title's printed stock is taken. Free nearby events have no booth
or travel charge; ordinary staff wages still apply. An event may attract readers
even without stock, but sales depend on actual demand.

**Finances → Part-time job** offers afternoon or evening shifts on Monday,
Wednesday and Friday. Pay goes to personal savings; no manga work happens during
these shifts. Use **Contribute personal savings** to fund the business. Choose
**No outside job** to remain available for studio work. Hired assistants keep
studio wages; the protagonist's shifts suspend after joining an employer.

The starting prodigy now has 95 in every production skill. Loading a previous
alpha.3 career applies this floor without lowering higher skills or discarding
work. Original saved snapshots remain available.

The persistent left rail opens full overview pages for Series, Books, Staff,
Finances, Studios, Industry and Inbox. Cards lead to focused actions without a
second set of workbench tabs. **Office** returns to the room; its local Inbox
and Series buttons open sidebars. The unread button opens Inbox in your current
context. Back restores your previous view and selections. Both 16:9 and 21:9
layouts keep navigation and the HUD visible while page content scrolls.

The second HUD row follows the selected series. Its progress bar shows total
chapter completion, with coloured sections for storyboard, pencils, inks,
backgrounds and tones. Completed work stays filled when the next stage begins.
Choose another series in the strip or open its details to follow it.

The Series sidebar shows whether a title is self-published, being pitched, in
editor review, offered serialization, or under a signed contract. An accepted
pitch says **your decision needed** until you accept the offer; **Review publishing
offer** opens the correct title. Declined, expired and rejected outcomes remain
visible. Current titles appear above creation actions.

Genre fields are dropdowns containing the twelve supported genres. Escape leaves
a focused text or number entry without clearing it or closing the panel. Press
Escape again to close the current layer normally.

Middle-mouse drag pans along the screen axes, like dragging an image. WASD pans
the view; the wheel zooms and right drag rotates. Space pauses or resumes the
previous speed. **1** decreases speed and **2** increases it through paused,
1×, 2×, 4× and 8×. Shortcuts are disabled while typing or in a menu or dialog.
During the overnight transition, Space or the pause button pauses/resumes 32×;
1 pauses and 2 resumes. Daytime speed choices return in the morning.

Dark mode is the default. Settings → Dark mode switches to light mode;
this preference is saved for the application independently of career saves.
Text scaling, compact spacing and reduced interface motion are also available.
Interface motion is separate from office timelapse trails. Helper-Chan guidance
uses the existing transparent character artwork. Overview Refresh highlights new
activity; live counters and quotes update without rebuilding your open form.

Menu → Settings controls ambience and effects independently; zero mutes a layer.
Sound stays at natural speed. There is no music in this build.

Any Sandbox mode or assist permanently disables new platform achievement unlocks
for that save and its descendants. In-game milestones and stories remain available.
Live Steam achievement delivery is deferred.

## Send useful feedback manually

Open Menu → Report a problem, describe what happened and what you expected, then
export a local ZIP. Screenshot and career attachments are optional and initially
off. The contents preview explains what will be included. Review the archive
before sharing it with the developer through your usual channel. Nothing uploads
automatically. A save attachment includes full history, entered text and artwork.

## Suggested playtest

- Begin a Standard career. Follow the opening through its first actual sale.
  Note elapsed real time, reading time, chosen speeds and confusing controls.
- Try another guidance direction and hide/resume the card. Confirm exploration
  is unrestricted and old accomplishments do not require repetition.
- Compare printing quotes with charged expenses. Try insufficient funds and
  cancellation before confirming a career move.
- Save, restart, load and export/import the career. Keep original older saves.
- Try a populated studio, high speed, both muted layers and a resized window.
- Export a report with no attachments, then one with a screenshot or career.

The opening aims for 15–20 minutes including reading and interaction. This is a
playtest target, not a guaranteed sale or profit. Higher speeds shorten waits.
Please report actual experience; scripted checks cannot establish human pacing.

## Build and qualification notes

This is a private test candidate, not a public release. Current qualification
details are in `docs/superpowers/alpha-12-build-verification.md` in the development
repository. Clean-machine and wider hardware testing remain separate from local
development-machine checks. Music, Linux/macOS and native Steam support are later.

For developers: use Godot 4.7.2 .NET and .NET 8. Obtain the matching .NET export
templates and SHA512 sums from the official release:
https://github.com/godotengine/godot-builds/releases/tag/4.7.2-stable
Verify the archive checksum and extract `windows_release_x86_64.exe` and
`windows_debug_x86_64.exe` into `TestResults/export-tools`. Run
`scripts/package-alpha.ps1 -Godot <path-to-Godot.exe>` with an unused repository
output directory. It exports, runs the packaged checks, includes licenses and
creates the ZIP. It does not publish or send the build.
