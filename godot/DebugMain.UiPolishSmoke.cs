using Godot;

namespace MangakaGame;

public partial class DebugMain
{
    private void CheckUiPolishNavigation()
    {
        var before=_state.ToJson();var period=_chartDays;var personal=_personalAccount;
        ShowOffice();
        Check(!_currentStageBar.Visible&&!_dashboardTotals.Visible,"Empty career avoids meaningless progress and sales panels");
        Check(_dashboardConvention.Disabled,"Convention shortcut explains its missing selected title");
        Press("Books, printing & online sales");
        Check(_page=="Books"&&!_report.Visible,"Office sales shortcut opens self-publishing books, not magazine pitching");
        Navigate("Finances");Press("Personal savings");
        Check(_personalAccount&&ButtonNamed("Personal savings").ButtonPressed,"Finance account selection is visible");
        Press("Last 90 days");
        Check(_chartDays==90&&ButtonNamed("Last 90 days").ButtonPressed,"Selected reporting period is visible");
        ButtonNamed("▸  Part-time work & personal contributions").ButtonPressed=true;
        BuildManagementPage();
        Check(ButtonNamed("▾  Part-time work & personal contributions").ButtonPressed,"Expanded details survive page refresh");
        Check(_state.ToJson()==before,"UI filters, navigation and detail expansion leave the simulation untouched");
        _chartDays=period;_personalAccount=personal;_disclosureStates.Clear();ShowOffice();
    }
}
