# Tier 1 fix 1: career guidance after the first sale, considerations

Date: 2026-09-26.
Status: design complete and implemented 2026-09-26 (see the
[completion record](../career-guidance-completion.md)). Source finding: guidance stops after the
first sale ([full career playtest findings](../full-career-playtest-findings.md)).

## Problem

`CareerGuidance.Evaluate` covers the opening up to the first sale, then offers a
choice of routes. On the doujin route it repeats "Plan your next book or print
run" forever. Nothing tells the player that pitching needs an ongoing series,
that a pitch takes about two weeks to answer, why it was rejected or when the
same magazine will look again, or what changes once a serialization starts.
The playtest bot only found the magazine path because it was scripted to.

A second finding shapes the timing: doujin sales barely move reputation (the
track record rises by 0.5 only for books of quality 75 or more), so waiting to
"build up" before pitching gains the player almost nothing.

## Decisions

### Q12. Shape: one continuous career path (decided 2026-09-26)

Guidance becomes one path from the first doujin to a working serialized studio,
instead of ending at a route choice. Steps after the first sale:

1. Turn the doujin into an ongoing series (required before pitching).
2. Pitch to a magazine.
3. Wait for the editor's answer, with the answer date shown.
4. After a rejection, recover and try again.
5. Accept a serialization offer before it expires.
6. Deliver the first chapter by its deadline.
7. Make a first hire once it is affordable.

The contest and employment routes stay available as optional side routes.

### Q13. Timing: suggest pitching soon after the first sale (decided 2026-09-26)

Supersedes the readership threshold mentioned while asking Q12.

- Soon after the first sale, Helper-Chan suggests continuing the doujin as an
  ongoing series and pitching to the magazine with the best chance shown.
- She says upfront that rejection is normal for a first pitch.
- After a rejection she explains the weakest factor, the date that magazine
  will accept a new pitch, and what to do meanwhile (keep selling doujin, try
  another magazine, improve quality).

### Q14. Message presentation: period phone screen until 2010 (decided 2026-09-26)

Helper-Chan's tutorial messages and notifications appear as text messages with
her portrait, replacing the current next-step card.

- Until the end of 2009 in game time, messages appear on a 1996-style PHS
  handset screen: green-tinted display, her small portrait beside each message,
  in-game date and time on each one, and the "Show me" action as a reply
  button under the latest message.
- From 2010 the same thread switches to modern chat bubbles (option 1), in
  line with smartphones spreading in Japan after the iPhone's 2008 launch
  there. The thread and its history carry over unchanged.
- Accepted tradeoff: the handset screen is smaller than a card. To keep it
  readable, long tutorial text is split into several short messages, the way
  real texts arrive, and the handset grows with the text size setting instead
  of shrinking the text. It must pass the 150% text check at 1280x720 and fit
  the 21:9 layout.
- Past messages stay in a scrollable thread, so players can reread what she
  said.
- Period detail to confirm during implementation: which PHS carriers offered
  short text messages in 1996 and what the handsets looked like, recorded in
  the research ledger.

### Q15. Phone visibility: pops up for new messages (decided 2026-09-26)

When Helper-Chan sends a message, the handset slides up from the bottom corner
with a soft buzz. It stays until the player dismisses it or completes the step
she asked for, then shrinks to a small phone icon with an unread count. The
icon opens the full thread at any time. Hiding guidance in settings still
works as today and keeps the icon.

Design complete; implementation plan:
[2026-09-26-career-guidance.md](../plans/2026-09-26-career-guidance.md).

## Period research: PHS text messaging (2026-09-26)

Classification as in the [Tokyo research ledger](2026-09-23-tokyo-research.md):
H is historical evidence, P is proxy or context, G is a game choice.

- **H:** DDI Pocket launched Pmail (Pメール), Japan's first short message
  service between voice handsets, on 20 November 1996. Messages were limited
  to 20 half-width kana, numerals and pictograms, addressed by phone number.
  PHS ("pitch") handsets and Pmail became very popular with high school
  students. [Pメール, Wikipedia](https://ja.wikipedia.org/wiki/P%E3%83%A1%E3%83%BC%E3%83%AB)
- **H:** NTT Personal started its text service, later known as きゃらメール,
  in April 1997. Messages were held at a centre (up to 70 characters, 20
  messages, for 3 days) and could be sent from PHS or push-button phones. The
  service ended in February 2005.
  [NTT Docomo notice](https://www.docomo.ne.jp/info/notice/page/041008_00.html),
  [Weblio: きゃらメール](https://www.weblio.jp/content/%E3%81%8D%E3%82%83%E3%82%89%E3%83%A1%E3%83%BC%E3%83%AB)
- **H:** Astel also routed texts through a centre so they arrived after the
  handset came back into coverage; its internet service dot-i followed in 2000.
  [アステル, Wikipedia](https://ja.wikipedia.org/wiki/%E3%82%A2%E3%82%B9%E3%83%86%E3%83%AB),
  [ITmedia, Dec 2000](https://www.itmedia.co.jp/news/0012/01/astel.html)
- **H:** By 1998 each carrier offered larger text services (DDI's PmailDX
  allowed up to 1,000 characters).
  [INTERNET Watch, Mar 1998](https://internet.watch.impress.co.jp/www/article/980316/ddi.htm),
  [INTERNET Watch, Aug 1998](https://internet.watch.impress.co.jp/www/article/980818/special.htm)
- **P:** period handsets were small candy-bar phones with a stub antenna, a
  numeric keypad and a monochrome, often green or amber backlit, dot-matrix
  display of a few lines.
  [PHS 25-year history, Y!mobile](https://www.ymobile.jp/sp/goodbyephs/),
  [GetNavi 1999 handset review](https://getnavi.jp/gadgets/67365/)
- **G:** Helper-Chan's phone shows up to 140 characters per message, a
  portrait beside each text and a scrollable thread. A real 1996 handset could
  show only 20 kana, which would make tutorial text unreadable; the green
  screen, antenna, keypad and short split messages carry the period feel
  instead. Messages from April 1996, before Pmail existed, are an accepted
  anachronism of a few months.
