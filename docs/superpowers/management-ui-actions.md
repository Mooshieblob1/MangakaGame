# Management UI action inventory

Original inventory: 24 September 2026. The same command controller and simulation
remain authoritative. Sub-project 9 replaces player-facing workbench tabs with
a persistent rail, full overview pages and named action workspaces.
The older tables below record previous deliveries; the current route map follows.

## Current GUI routes (source implementation, 25 September 2026)

| Entry | Destination and retained actions |
| --- | --- |
| Office | Office camera, local Inbox and Series shortcuts; management rail stays visible. |
| Series | Illustrated title overview; production/release/readers, live progress, publishing state, physical and online sales; focused one-shot/ongoing creation. |
| Series details → Production / Publishing | Schedule, queue, cadence, pages, pitch, offers, withdrawal, ending, rankings, trends and books; current target is visible. |
| Books / title → Print, Sell online, Conventions | Bounded forms and separate quote/status cards; current edition, printer, stock, delivery, actual costs and reserved copies. |
| Staff | Chibi portraits and comparison table; known skills only; Person details; Recruitment; Team settings; Business actions for pay. |
| Recruitment | Targeted search, candidate pool, known-skill comparison, salary, explicit workplace, team/stage assignment and lead selection. |
| Finances → Business actions | Business/personal metrics and ledgers; outside work, contributions, salaries, borrowing, repayment, incorporation. Consequential actions show confirmation. |
| Studios | Real floor plans, rent/capacity/storage, View/Furnish; Properties, Tokyo map, Career moves. |
| Properties / Furniture / Career moves | Existing branch, move, closure, staff-transfer, staged quote and Apply/Discard flows; all draft guards retained. |
| Industry | Current-era news, title art, magazine rankings; Industry contacts, awards, adaptations and milestones. |
| Inbox | Needs attention, Results & milestones, Routine, All; grouped routine updates, older/newer pages, current decisions independent of read status. |
| Help / Settings | Cropped existing transparent Helper portrait for guidance; original expressive/story panels; light/dark, text scale, density and reduced UI motion, separate office trails. |

Normal simulation updates change live metrics and quotes without reconstructing
the open page. The Refresh action highlights new activity on overview pages.
Back and saves retain valid scope, selections, sidebar and scroll context; career
authority changes return to the mangaka’s current office. No old tab navigation
is exposed in the player shell. The debug scene keeps its harness.

Follow-up validation and Windows alpha.6 packaging were explicitly authorized
and completed. See [executed checks](sub-project-9-build-verification.md).

## Original management delivery

| Player entry | Actions and data | Authority / failure handling |
| --- | --- | --- |
| Series → Create or manage production | Create, pause/resume, cadence, pages, chapter stages; person schedule, overtime, pins, reorder and skip | Existing series/person command validation. Rejections appear in the workbench footer. |
| Series details → Publishing / Industry → Magazine rankings | Pitch, accept/decline, withdraw, end, editor review, rankings, volumes, trends, internet | Selected title is forwarded to the publishing selector. Live offers remain subject to expiry and ownership checks. |
| Staff → Recruit, pay and organize | Candidate pool, targeted recruitment, hire, dismiss, wages, contributions, teams, stage assignments, lead selection | Existing owner/team restrictions and funds/workstation checks. Rival private stats are not exposed by the new staff cards. |
| Finances → Credit, contributions and business actions | Personal loans/cards, company credit, repayments, incorporation, commissions, salary and policy settings | Separate personal/business ledgers and existing authority rules. Charts distinguish operating income/spending from financing/transfers. |
| Series details → Printing & promotion | Print quantity/tier, auto-print, conventions, cancellation, promotion, campaigns | Same quotes, stock, reservations, fees and time constraints as existing operations controls. |
| Studios → Moves, branches and business operations | Lease, move, additional office, closure, staff relocation, career quotes, return home, form studio, join employer, follower invitation, title release | Existing staged quote and confirmation flow. Protagonist perspective and prospective rights remain authoritative. |
| Studios → Tokyo map and properties | Ward/property map, travel estimates, location inspection | Existing researched data and game estimates; no new Tokyo factual or economic claims. |
| Studios → Arrange furniture | Camera, staff selection, catalog, placement/rotation, storage/sale, locked items, Auto-arrange, assignments, relocation preview, Apply/Discard | Unsaved furniture draft blocks navigation, saves and menus. Original validation preserves routes and workspaces. |
| Industry → Scouting, recruitment and distribution | News, public rivals, paid scouting, approaches, cancellation, retention, career offers, temporary assistants, digital and overseas proposals | Existing date, funds, skill visibility, negotiation and edition-rights checks. |
| Inbox | Current pending serialization/staff decisions, important notices, routine results, acknowledgments | Decision cards derive from current state independently of read status. Duplicate queued notices coalesce by subject/type. Resolved publishing offers are labeled when opened. |
| Help / Helper-Chan | Contextual guides, optional conversations, settings, conversation journal | Narrative commands validate pending scene and answer. No employee, wages, work output or gameplay RNG effect. |
| Series details → Showcase | Bundled cover/panels, PNG/JPEG import, fit or center crop, reset, synopsis, release history | Images are bounded before decoding and copied to immutable career storage. No gameplay stat changes. |
| Finances / Series details | Cash, operating income/spending, physical/digital/overseas copies, ranks; month/quarter/year/all time | Real ledger/sample dates and identities; exact-figure tables and full ledger pagination. No invented pre-import sales/ranks. |
| Menu / Save | New career, Continue, Load, settings, named snapshots, three rotating daily autosaves, portable export/import | Immutable atomic snapshots, original files retained, malformed states rejected, missing custom art visibly falls back. Loads pause. |

Office selection and report location filtering are independent. Camera/effect,
page/detail/filter, finance period/account, scroll, tutorial flags and read
acknowledgments persist with each snapshot. Back retains in-session detail and
filter context. Escape closes the current layer, respecting furniture/story
drafts. Buttons retain normal keyboard focus and the text setting affects the
new labels as well as controls.

The debug scene remains available for development and regression tests. Raw
command/event text is hidden in the player-facing production workbench.

## Sub-project 7 additions

| Player controls | Simulation owner | Validation and presentation |
| --- | --- | --- |
| Industry / Series → Awards | `RecognitionCommand`, `AdoptManuscriptCommand` | Completed unpublished work, exclusive pending entry, immutable revision snapshot, later-edition retries; automatic annual judging. |
| Industry / Series → Licenses | `LicenseCommand` | Rights and employer approval, source material, capacity, cooldown, two counteroffers, involvement, catch-up and delay decisions. |
| Inbox → Review license decision | Current `Progression.Projects` | Pending cards survive read acknowledgments; historical deals retain named beneficiaries. |
| Industry → Career journal | `Progression.Milestones` | Persistent honors and platform eligibility; reading adds no bonuses or RNG rolls. |
| Setup / Settings → difficulty and assists | `DifficultyCommand` | Bounded options; one-time cushion; prospective prices/grace; irreversible Sandbox platform provenance. |
| Finance charts / deal receipts | Account ledger and `Progression.Receipts` | Prize, licensing and creator-share income; exact subsidies excluded from operating profit. |
| Helper-Chan illustrated everyday event | `StoryCommand("desk_moment", ...)` | Natural offers only, unchanged full image, two answers, defer/skip, immutable journal replay. |

## Sub-project 8 additions

| Player controls | Owner | Validation and presentation |
| --- | --- | --- |
| Series → Create a one-shot doujin | `CreateDoujinCommand` | 8–64 story pages in multiples of four; ordinary work creates one book, without ongoing chapters or a fake contest entry. |
| Helper-Chan card → Show me / Direction | `CareerGuidance`, `CareerPresentation.Guidance` | Read-only evidence queries; persistent optional routes, selected project and completion; existing careers skip proven steps; navigation respects dialogs/drafts. |
| Show me → Print doujin | Existing `StudioAction.Print` | Focused printer, quantity, exact business quote, available funds, stock and delivery; no purchase merely from opening guidance. |
| Help → Objectives and direction | Presentation preferences | Hide/resume and change route without affecting production, money or achievements. |
| Settings → ambience/effects | `OfficeAudio`, presentation preferences | Original procedural PCM, independent volume/mute, real-time budgets, no historical cue replay or simulation RNG use. |
| Menu → Report a problem | `ProblemReport`, `CareerStore.ExportSnapshot` | Allowlisted local report, preview, opt-in attachments, atomic file replacement; no upload or saved-career side effects. |
| Save / Load / portable careers | `CareerStore`, `ImportSupported` | Lossless format-2 compressed envelope, metadata-only listing, legacy readers, complete history and Sandbox provenance; simulation version 9 is separate. |


## Alpha.4 production feedback

| Player controls | Simulation owner | Behavior |
| --- | --- | --- |
| Series → Create an ongoing series | `CreateSeriesCommand.ShortIssues` | One printable issue per chapter; optional collected edition every five chapters. |
| Series → Enable short numbered issues | `SetDoujinIssuesCommand` | Retains existing books and enables chapter releases for an older ongoing title. |
| Finances → Part-time job | `SetOutsideJobCommand` | Personal hourly pay; no simultaneous production; suspended under a studio employer. |
| Series / Books / Printing → Send to convention | `StudioAction.BookConvention` with title ID | Exact existing booth/travel quote; selected title stock, automatic attendee, recorded sales and promotion. |
| Staff / Studios / Industry tabs | Navigation only | Open full workbench controls immediately; no simulation change merely from navigation. |
| Series progress bars | Read-only chapter work | Visible percentage inside bar; current chapter above detail actions. |

## Alpha.5 sidebar and input clarity

| Player controls | Owner | Behavior |
| --- | --- | --- |
| Series cards and details | `PublishingStatusText`, current offer/contract and outcome events | Pitch preparation, editor review/revision, decision waiting, accepted offer, signed serialization and declined/expired/rejected/cancelled/withdrawn outcomes. Existing series appear before creation actions. |
| Review publishing offer | Publishing workbench | Selects the relevant title before opening the accept/decline controls. |
| Genre in one-shot, ongoing and workbench creation | `TrendCatalog.Genres` | Dropdown with all twelve supported values; saved genre text and simulation rules remain compatible. |
| Escape in text entry | Input focus | First Escape releases text/number-entry focus without deleting text or closing the containing layer; the next Escape keeps normal navigation behavior. |

Validation: warning-free build, 21 rendered publishing/input checks (including
a real pitch, accepted contract, declined/expired branches, all creation routes,
and single/multiline focus), plus 50 existing rendered usability checks.

## Next alpha source changes (not packaged)

| Control | Owner | Behavior |
| --- | --- | --- |
| Top series strip / Series cards | Read-only series, print runs and receipts | Fans, all copies sold, physical stock and reservations; downloads distinguished in details. |
| Unread count | Inbox navigation | Opens Inbox without acknowledging messages. |
| Continue as ongoing series | `ContinueOneShotCommand` | Preserves title identity and readers; subsequent chapters produce numbered issues. |
| Books → Sell online | `PublishDoujinOnlineCommand` | Per-edition listing with no upfront cost; lower download price and fees only on actual sales. |
| Convention → Reserve copies | `StudioActionCommand.ReservedCopies` | Protects stock and timely paid deliveries from other sales/bookings. |
| Update reserved copies | `ReserveConventionStockCommand` | Adjusts the reservation until departure; cancellation releases it. |
| Speed buttons / Space / 1 / 2 | Presentation and simulation speed | Persistent selection highlight and brief translucent icon. |
| Office travel, companion and decorative breaks | Office renderer only | Doorway arrivals/departures, companion following and idle gestures; decorative breaks do not change simulation work. |

Earlier sales/UI checks passed. Latest office and follow-up changes await explicit
authorization to compile and run checks; no new package exists. Full scope and
verification details: `private-alpha-feedback-3.md`.
