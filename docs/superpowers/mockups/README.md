# GUI review prototype

`gui-redesign.html` is the inline, self-contained review fragment for Sub-project 9.
It is not a Godot screen or a standalone production website. The conversation
renderer supplies the surrounding runtime and optional icon/design controls.

Assets are embedded to avoid remote dependencies. Helper-Chan uses the original
transparent neutral portrait, losslessly encoded as WebP; CSS changes its display
crop without modifying the source. The office uses the existing development
screenshot `TestResults/office-life-kitchen.avif`, WebP-encoded for preview size.
Original game assets remain unchanged. Sample cover and floor plan are illustrative.

The fragment is below the conversation renderer's 1 MB limit. It makes no network
requests and cannot touch saves or call game commands. Only local prototype state
changes. Appearance and navigation choices can be remembered by the host.

## Browser verification, 25 September 2026

Checked the wrapped fragment in headless Edge:

- Office rendering, transparent portrait loading, Series sidebar and navigation.
- Unaffordable print-order rejection; affordable orders debit sample business
  cash and add pending delivery, keeping existing stock unchanged.
- Convention reservation editing updates the selected-series HUD.
- Property comparisons, series switching, offer-received status, large Helper
  message and deferral. Marking updates read retains the unresolved decision.
- Office, Series, Studios, Print and Inbox fit at 1024, 736 and 320 browser pixels
  with larger text and compact density in both light and dark themes.
- No JavaScript runtime errors in the checked interactions.

Reviewed desktop screenshots of Series, Studios and the large Helper panel.
The full approved game interface is broader than this prototype: Staff, Finances
and Industry are visibly disabled; furnishing and exact offer terms have preview
explanations. No Godot compilation, game runtime checks or package was created
for this review. Review the implementation plan for game acceptance coverage.

## Full-monitor review: 16:9 and 21:9

The user approved the initial appearance and requested support for both aspect
ratios. `gui-desktop-preview.css` supplies full-monitor composition for review:
fixed-size navigation, bounded management/form columns, a wider office viewport,
and a 340-pixel office sidebar. Apply it after the fragment's own styles and set
the root's `data-screen` to the active page when using the capture harness.
This companion is separate from the fluid in-conversation preview.

`gui-*-16-9.png` captures are 1920×1080; `gui-*-21-9.png` captures are 2560×1080.
Office, Print, Series, Studios and Inbox were checked in both sizes. The complete
Print and Office views fit without vertical clipping. Longer management content
can scroll within its page while the navigation and HUD stay visible. Light-mode
print quotes were also exercised with larger text/compact density. No runtime
JavaScript errors were observed. These remain browser mockups, not Godot builds.

The office is a proportional display of an existing development image; it does
not demonstrate a live ultrawide camera field of view. The implementation design
requires preserving vertical framing and exposing additional horizontal office
space in the actual game. Verification at 3440×1440 remains in the game plan.
