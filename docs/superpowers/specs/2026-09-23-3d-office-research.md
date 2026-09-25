# 3D office research ledger

Researched: 2026-09-23. Game setting starts 1 April 1996.
Companion: [3D office design](2026-09-23-3d-office-design.md).
Earlier evidence: [Tokyo/Japan ledger](2026-09-23-tokyo-research.md).

## Evidence rules

- **H: historical evidence** explicitly describes a period before or around
  the start date. A retrospective source is not a contemporaneous price list.
- **P: proxy/context** informs appearance or spatial relationships, but does
  not establish the typical 1996 Tokyo home, office or price.
- **G: game choice** is an authored layout, price, capacity or presentation
  rule. The user's rent-free family home is a G requirement, not a rent claim.

Only brief extracted facts are recorded here. Reference photography and real
products are research material, not permission to copy images, branding or
model geometry into the game.

## O1. Tokyo housing: separate working and shared domestic spaces

Source: UR Urban Renaissance Agency, **URまちとくらしのミュージアム:
ミュージアムガイド**, undated current museum page, read 2026-09-23.
[Official museum guide](https://akabanemuseum.ur-net.go.jp/museumguide/).

The guide describes a reconstructed Hasune dwelling with a dining-kitchen
and two bedrooms (2DK), including a fitted dining table. It also describes
Tamadaira terrace housing built mainly in the Showa 30s and communal access
arrangements in the Harumi apartments. These are named historical housing
examples in Tokyo, not a statistical sample of 1996 homes.

**H/P:** evidence for older residential layouts and distinct private/shared
areas that can inform a lived-in family home. **G:** the game's two-desk
workroom, shared kitchen, entrance, bathroom and closed private doors. Do not
copy the museum's visitor circulation as a dwelling plan, infer commercial
office layouts from housing exhibits, or describe the fictional home as a
measured reconstruction. Exact game dimensions remain authored.

## O2. Manga households: useful atmosphere, wrong era for a direct replica

Source: Toshima Ward, **トキワ荘マンガミュージアムの妖精**, current municipal
article describing a reconstructed earlier building, read 2026-09-23.
[Toshima museum account](https://www.city.toshima.lg.jp/132/miryoku/meguru/spot/jimotoshima063.html).

The article describes the museum's wooden corridors, shared cooking area and
everyday kitchen objects. It identifies the depicted manga creators' lives
with the Showa 20s–30s, decades before our 1996 start. The original building
was demolished in 1982; the museum opened in 2020.

**H/P:** an earlier Tokyo manga living/work environment, useful for the idea
of shared circulation and personal traces. **G:** background residents,
footsteps, dishes, coats and household routines. Do not make every 1996 studio
look like 1950s Tokiwa-so or put this later museum into the 1996 world. The
starting location remains the same fictional parents' house in a random outer
ward, not a historical mangaka boarding house.

## O3. Desks and office chairs existed well before the start date

Source: KOKUYO, **Office furniture-inspired KuruKuruMeka**, corporate history,
undated retrospective page, read 2026-09-23.
[Manufacturer's desk and chair history](https://www.kokuyo.com/en/corporateprofile/history/episode_13/).

The history dates an early steel desk to 1965, a swivel office chair to 1966,
and the adjustable study desk discussed in the article to 1981.

**H:** these categories predate 1996. **P:** visual cues for modest older
study furniture and steel office furniture, not proof of a particular Tokyo
studio's purchases. **G:** our unbranded basic and improved desk/chair models,
prices, comfort effects and footprints. An old study desk at the family home
and plain office desks after expansion are plausible art directions, not
verified universal practice. No exact KOKUYO replica or logo is required.

## O4. Paper-era manga materials and anachronism checks

Source: DELETER, **会社概要**, manufacturer history, undated current page,
read 2026-09-23.
[Official company history](https://deleter.jp/company/).

The company dates its foundation to June 1984 and the start of DELETER Screen
production to May 1987. Its timeline puts Neopiko markers in May 1998,
Comic Art CG Illustration in December 2001, and ComicWorks 600 in July 2002.

**H:** physical screen-tone sheets existed before the game starts; these
specifically named later products must not be backdated. **G:** fictional
paper stacks, tone sheets, rulers and drawing-tool props distinguish manga
workstations. This source does not date every pen, lamp or computer model or
prove when all mangaka adopted digital work. Digital drawing progression is
left to the later historical-timeline milestone. Generic older office
equipment must not silently grant digital production or internet access.

## O5. Shared office buildings

The player explicitly wants other tenants and visitors visible in common
areas of properties occupying only part of a building. That is a **G** design
requirement. O1–O2 establish residential examples only; they are not evidence
for a specific 1996 commercial office's corridor, lift or tenancy plan.

Author fictional shared-building layouts with common halls, neighbouring
closed doors and an entrance/lift boundary. Give each property a context
label. Do not claim exact addresses, surveyed dimensions, average corridor
widths or historical lease inclusions. District, rent, seat and book-stock
capacity continue to come from the implemented Sub-project 3 catalog, with
its existing evidence limitations. A full building is not implied by a
high property tier.

## O6. Prices and placement dimensions

No contemporaneous 1996 Tokyo furniture price list or commercial interior
survey was verified in this pass. All catalog prices, resale percentages,
furnishing packages, grid dimensions, circulation clearances and office areas
in the companion design are **G**. They are explicit starting balance and
readability defaults, not historical retail quotes or building-code advice.

The older rent benchmark and salary evidence in the earlier ledger remain
comparisons only. Do not derive an exact furniture price from a rent or wage
figure. Recheck dated primary sources before adding a branded appliance,
technology unlock or claim of historically exact dimensions.

## Technical references, separate from historical evidence

- Godot's [Camera3D reference](https://docs.godotengine.org/en/stable/classes/class_camera3d.html)
  describes orthographic projection, where distance does not change apparent
  object size. This supports the proposed overhead camera; angle and controls
  are authored defaults.
- Godot's [MultiMesh performance guide](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html)
  describes batching repeated instances and the absence of per-instance
  visibility culling. Use this as a potential optimisation for repeated props,
  not proof that transparent character trails will be inexpensive.

These are rolling stable docs read 2026-09-23. Check APIs against the project's
installed Godot 4.7.2 .NET build when implementing. No built-in motion-blur
feature is assumed. The character-only timelapse effect needs a rendered
prototype and measurements, including depth occlusion and transparency.
