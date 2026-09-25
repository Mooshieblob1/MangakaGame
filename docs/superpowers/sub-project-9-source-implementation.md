# Sub-project 9: GUI source implementation

Date: 2026-09-25.
Status: source implementation delivered, followed by authorized compilation and
Windows packaging. See [alpha.6 build verification](sub-project-9-build-verification.md)
for executed results; the source-phase evidence below is retained as history.

The user approved the GUI direction and both aspect-ratio mockups, then instructed
implementation. The earlier instruction to leave **all compilation** until asked
was honored during this source phase. This initial delivery did not create a
playable build; the subsequent explicit build request authorized alpha.6.

## Implemented in source

- Persistent labeled left navigation, a shared management workspace and explicit
  office-side Inbox/Series shortcuts. Player-facing workbench tabs are hidden;
  the developer scene retains its harness and all existing command handlers.
- Global account/date/speed/unread information and a selected-title HUD with
  publishing state, title fans, numeric progress, sales, stock and reservations.
  A no-series state explains how to begin. The unread shortcut follows the current
  context: sidebar from Office, full Inbox from management.
- Illustrated series overview and parallel production/release/readers cards;
  state-derived primary actions; one-shot continuation and all existing publishing,
  showcase, issue, collection and distribution routes remain available.
- Separate input and quote/status cards for printing, no-upfront-cost downloads
  and convention bookings. Existing prices, wages, stock protection and validations
  remain authoritative. Incoming simulation events do not destroy open forms.
- Mouthless chibi staff portraits from saved appearance recipes, staff and candidate
  comparisons, explicit hiring workplace and employee context. Hidden talent stays
  unknown. Recruitment, assignment, schedules, overtime and pay remain reachable.
- Business/personal financial metrics and existing ledgers/charts; actual studio
  floor-plan cards with rent, capacity and storage, plus View/Furnish shortcuts.
  Property, branch, career and furniture-preview flows retain their command guards.
- Current-era Industry overview with title art, news and magazine rankings, linked
  to scouting, negotiations, channels, contests, awards and adaptations.
- Four inbox views: Needs attention, Results & milestones, Routine and All.
  Current decisions remain independent of read status. Routine updates expand in
  groups; older/newer pagination keeps historical messages reachable.
- Existing transparent Helper-Chan artwork cropped non-destructively for compact
  guidance. Original expressive portraits and story illustrations remain available.
- Shared light/dark styling, independent text scale, saved compact spacing and
  reduced interface motion. Office hyperlapse settings remain separate.
- Bounded reading/form columns and wrapping groups for 16:9 and 21:9; the office
  camera explicitly retains vertical framing. Headers/navigation stay outside
  scrolling content. No image is stretched to fill an ultrawide viewport.
- Stable named action routes, explicit confirmations for consequential publishing,
  debt, employment and property decisions, and captured targets for confirmation.
  Back and snapshots retain valid selections, sidebar and scroll context. Career
  authority changes invalidate the old workspace and follow the mangaka.

## Source locations

- Shell/routing/components: `godot/DebugMain.Management.cs`, `DebugMain.Gui.cs`,
  `GuiReadingColumn.cs`, `DebugMain.CurrentProgress.cs`, `DebugMain.Usability.cs`.
- Overviews: `godot/DebugMain.GuiPages.cs`, `DebugMain.ManagementPanels.cs`,
  `StaffPortrait.cs`; existing studio preview/dashboard components are reused.
- Focused forms: existing Alpha, ProductionClarity, DoujinOnline, Publishing,
  Staff, Operations, Industry and Office partials.
- Presentation persistence: `src/MangakaSim/CareerStore.cs` and the management
  menus. Added fields have defaults for older snapshots; simulation save data,
  ownership, economy and Sandbox achievement provenance are unchanged.
- Current action coverage: [management UI inventory](management-ui-actions.md).

## Evidence and remaining acceptance

- Parsed all 162 non-generated C# source files with the installed Roslyn syntax
  parser: **zero syntax errors**. Parsing does not type-check, compile, load the
  game, or prove runtime behavior.
- Checked whitespace in tracked diffs and reviewed the new source/routes,
  existing Godot API definitions, authority filters and artwork references.
- Updated existing navigation, production, publishing-input and usability smoke
  sources for the new hierarchy. Extended management smoke coverage for 1920×1080,
  2560×1080, 3440×1440, persistent navigation, sidebar/camera Back behavior and saved
  GUI preferences. Extended portable-career coverage to retain those preferences.
  **These checks have not been executed.**
- No compiler, test runner, Godot launch, game screenshot capture, export or
  packaging was run for this implementation. Existing HTML previews are design
  evidence only, not evidence of native rendering.

Once compilation is authorized, run the focused regression and visual matrix in
the [implementation plan](plans/2026-09-25-gui-redesign.md), including both themes,
both densities, larger text, keyboard/furniture guards, live quotes, authority
changes and existing careers. Resolve rendering or integration defects before
marking the GUI milestone runtime-verified. Packaging needs a separate instruction.
