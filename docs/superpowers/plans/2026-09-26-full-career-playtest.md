# Sub-project 10: full-career playtest plan

Date: 2026-09-26. Specification:
[full-career playtest design](../specs/2026-09-26-full-career-playtest-design.md).

## Steps

1. Done. Add `tests/MangakaSim.Tests/CareerPlaytest.cs`, an xUnit theory tagged
   `[Trait("Category", "Playtest")]` with the five seed and difficulty cases.
2. Done. Implement the guided player: doujin creation and printing, business
   funding from savings, online listing and reprints, pitching to the magazine
   with the best estimated chance, accepting offers, recruiting and hiring,
   furnishing or moving when there is no free desk.
3. Done. Record milestones, guidance timeline, monthly snapshots, rejected
   actions and event counts, and write one report per run.
4. Done. Assert JSON round trip and replay equality at the end of each career.
5. Done. Run all five careers and write the
   [findings](../full-career-playtest-findings.md).
6. Next. Agree the Tier 1 fixes that follow from the findings, then re-run the
   playtest after each fix to confirm the change.

## Running it

The five careers take about two minutes. Run them on their own:

```powershell
dotnet test tests/MangakaSim.Tests --filter Category=Playtest
```

Leave them out of the everyday test run:

```powershell
dotnet test tests/MangakaSim.Tests --filter "Category!=Playtest"
```

Reports are written to `TestResults/career-playtest/seed{seed}-{difficulty}.md`.
