using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private bool _darkMode=true;
    private ColorRect _backdrop=null!;
    private string _uiPreferencesPath="";
    private Action? _refreshPrintPanel;
    private int _printingBookId;
    private readonly Dictionary<string,Button> _navigation=new();
    private Color Paper=>new(_darkMode?"1b2938":"f5f0e5");
    private Color Ink=>new(_darkMode?"e3ecee":"283b3c");
    private Color Accent=>new(_darkMode?"85d8ca":"397f82");
    private Color Wash=>new(_darkMode?"151c23":"e6e1d5");
    private Color Hover=>new(_darkMode?"2c4057":"e5ece5");
    private Color SelectedSurface=>new(_darkMode?"34568a":"cbded7");
    private Color CardSurface=>new(_darkMode?"223344":"fffdf6");

    private void LoadUiPreferences()
    {
        if(_uiPreferencesPath.Length==0)_uiPreferencesPath=OS.GetCmdlineUserArgs().Any(a=>a.Contains("smoke"))
            ?Path.Combine(SmokeOutput,"ui-preferences-"+Guid.NewGuid().ToString("N")+".json")
            :ProjectSettings.GlobalizePath("user://ui-preferences.json");
        try{if(File.Exists(_uiPreferencesPath)){using var doc=JsonDocument.Parse(File.ReadAllText(_uiPreferencesPath));_darkMode=doc.RootElement.GetProperty("DarkMode").GetBoolean();}}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException){_darkMode=true;}
    }
    private void SetDarkMode(bool dark)
    {
        _darkMode=dark;ApplyUiTheme();
        var temp=_uiPreferencesPath+".tmp";
        try{Directory.CreateDirectory(Path.GetDirectoryName(_uiPreferencesPath)!);File.WriteAllText(temp,JsonSerializer.Serialize(new{DarkMode=dark}));File.Move(temp,_uiPreferencesPath,true);}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Notify("Theme changed, but the preference could not be saved.");}
        finally{if(File.Exists(temp))try{File.Delete(temp);}catch(IOException){}}
    }
    private void ApplyUiTheme()
    {
        Theme=EditorialTheme();_backdrop.Color=Wash;
        RefreshGuidance();
        foreach(var card in FindChildren("*","PanelContainer",true,false).OfType<PanelContainer>().Where(c=>c.HasMeta("card_surface")))
        {card.RemoveThemeStyleboxOverride("panel");card.ThemeTypeVariation="StudioCard";}
        foreach(var amount in FindChildren("*","Label",true,false).OfType<Label>().Where(l=>l.HasMeta("cash_sign")))
            amount.AddThemeColorOverride("font_color",new Color((int)amount.GetMeta("cash_sign")<0?(_darkMode?"ff929b":"b52035"):(_darkMode?"78e6a2":"16703a")));
        foreach(var plot in FindChildren("*","Control",true,false).OfType<ReportPlot>()){plot.DarkMode=_darkMode;plot.QueueRedraw();}
        foreach(var plan in FindChildren("*","Control",true,false).OfType<StudioPlanPreview>()){plan.DarkMode=_darkMode;plan.QueueRedraw();}
        ApplyTextScale();
    }
    private bool GameKeysAvailable(bool allowOfficeEditor=false)
    {
        if(!_managementReady||_inMenu||_helperPopup.Visible||_storyOpen||!allowOfficeEditor&&OfficeEditing)return false;
        if(GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit)return false;
        return !GetChildren().OfType<Window>().Any(w=>w.Visible);
    }
    public override void _Input(InputEvent ev)
    {
        if(ev is not InputEventKey key||!key.Pressed||key.Echo||key.CtrlPressed||key.AltPressed||key.MetaPressed)return;
        // Consume the first Escape before GUI handling so it only leaves text entry.
        // A second Escape can then close the current panel or menu normally.
        if(key.Keycode==Key.Escape&&GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit)
        {
            GetViewport().GuiGetFocusOwner().ReleaseFocus();
            GetViewport().SetInputAsHandled();return;
        }
        if(HandleTimeShortcut(key.Keycode))GetViewport().SetInputAsHandled();
    }
    private bool HandleTimeShortcut(Key key)
    {
        if(!GameKeysAvailable())return false;
        if(key==Key.Space){TogglePause();return true;}
        if(key is not (Key.Key1 or Key.Key2 or Key.Kp1 or Key.Kp2))return false;
        if(_overnightTarget is not null){SetSpeed(key is Key.Key1 or Key.Kp1?0:OvernightSpeed);return true;}
        var speeds=new double[]{0,1,2,4,8};int index=Array.IndexOf(speeds,_speed);
        SetSpeed(speeds[Math.Clamp(index+(key is Key.Key1 or Key.Kp1?-1:1),0,speeds.Length-1)]);return true;
    }
    private void PanWithKeys(double delta)
    {
        if(!GetWindow().HasFocus()||!GameKeysAvailable(true)||Input.IsKeyPressed(Key.Ctrl)||Input.IsKeyPressed(Key.Alt)||Input.IsKeyPressed(Key.Meta))return;
        var direction=new Vector2((Input.IsPhysicalKeyPressed(Key.D)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)?1:0),
            (Input.IsPhysicalKeyPressed(Key.S)?1:0)-(Input.IsPhysicalKeyPressed(Key.W)?1:0));
        PanOffice(direction,delta);
    }
    private void PanOffice(Vector2 direction,double delta)
    {
        if(direction==Vector2.Zero)return;
        var view=_report.Visible?_officeView:_homeOffice;
        if(!view.IsVisibleInTree())return;
        view.PanScreenPixels(-direction.Normalized()*(float)Math.Min(delta,.1)*view.Size.Y*.65f);
    }
    private void OpenPrinting(int series,int book=0){_printingBookId=book;Navigate("Print doujin",series);}
    private void RefreshNavigation()
    {
        var active=!_side.Visible&&!_report.Visible?"Office":_page switch
        {"Sell online" or "Print doujin" or "Conventions" or "Distribution settings"=>"Books","Series details" or "New doujin" or "New series" or "Showcase" or "Production" or "Publishing"=>"Series","Person" or "Recruitment" or "Team settings"=>"Staff","Awards" or "Licenses" or "Legacy" or "Industry contacts"=>"Industry","Business actions"=>"Finances","Studio actions" or "Properties" or "Tokyo map" or "Furniture" or "Career moves"=>"Studios","Guidance"=>"Help",_=>_page};
        foreach(var (name,button) in _navigation)button.SetPressedNoSignal(name==active);
    }
    private void BuildBooks()
    {
        PageActions(("+ Create a one-shot",()=>Navigate("New doujin")),("+ Start an ongoing series",()=>Navigate("New series")));
        QuietWords(_sideContent,"Physical copies need printing and delivery. Downloads need no stock or upfront payment. Sales settle on Mondays.");
        if(!ManagedSeries.Any())Words(Card("Your first release","Finish a one-shot or one short issue to unlock printing and online sales."),"A collected book is optional; it takes five finished chapters.");
        foreach(var series in ManagedSeries)
        {
            var books=series.Volumes.Where(v=>v.IsDoujin&&v.BusinessId==_state.ControlledBusinessId).ToArray();
            if(books.Length==0)
            {
                if(!series.StandaloneDoujin&&series.Publishing!=PublishingStatus.Unpublished)continue;
                var card=Card(series.Title,"In production · not available to print yet");
                Words(card,series.StandaloneDoujin?"Finish the story's five drawing stages to unlock printing.":series.ReleaseShortIssues?"Finish one chapter to print Issue 1. Five completed chapters also make a collected book.":"Five complete chapters make a collected book. Enable short issues in series details to print earlier.",14);
                ActionButton(card,"View production",()=>Navigate("Series details",series.Id));
            }
            foreach(var book in books.Reverse())
            {
                var card=Card($"{series.Title} · {GameState.EditionName(book)}",$"{book.PrintedPages} pages · ¥{book.Price:N0} printed cover price");
                LiveWords(card,()=>_state.DescribeDoujin(book.Id).Status,18);
                var figures=new HFlowContainer();card.AddChild(figures);
                Metric(figures,"IN STOCK",()=>_state.Stock(book.Id).ToString("N0"),"Delivered physical copies");
                Metric(figures,"AVAILABLE",()=>(_state.Stock(book.Id)-_state.ConventionReserved(book.Id,true)).ToString("N0"),"Stock not held for conventions");
                Metric(figures,"PHYSICAL SOLD",()=>_state.DescribeDoujin(book.Id).Sold.ToString("N0"));
                Metric(figures,"DOWNLOADS SOLD",()=>_state.DoujinDownloadsSold(book.Id).ToString("N0"));
                LiveWords(card,()=>
                {
                    var pending=_state.PrintRuns.Where(r=>r.VolumeId==book.Id&&!r.Delivered).OrderBy(r=>r.DueAt).FirstOrDefault();
                    return pending is null?"No print delivery pending":$"Awaiting delivery · {pending.Quantity:N0} copies due {pending.DueAt:ddd d MMM · HH:mm}";
                },14);
                LiveWords(card,()=>$"Convention reserve: {_state.ConventionReserved(book.Id):N0} copies (may include pending deliveries)\n"+
                    (_state.DoujinOnlineListed(book.Id)?"Online shop: on sale":"Online shop: not listed"),14);
                var actions=new HFlowContainer();card.AddChild(actions);
                ActionButton(actions,"Print physical copies",()=>OpenPrinting(series.Id,book.Id)).ThemeTypeVariation="PrimaryAction";
                var online=ActionButton(actions,"",()=>OpenOnline(series.Id,book.Id));
                void OnlineLabel()=>online.Text=_state.DoujinOnlineListed(book.Id)?"View online sales":"Sell online · ¥0 upfront";
                OnlineLabel();_pageLiveValues.Add(OnlineLabel);
                ActionButton(actions,"Reserve for a convention",()=>Navigate("Conventions",series.Id));
                var detail=Disclosure(card,"Distribution details",key:$"distribution-{book.Id}");
                LiveWords(detail,()=>_state.DescribeDoujin(book.Id).LocalSales,14);
            }
        }
        QuietWords(_sideContent,"Commercial books are handled by their publisher. Overseas and other digital agreements are managed through Industry.");
    }
}
