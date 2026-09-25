using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;
using MangakaSim.Rules;

namespace MangakaGame;

public partial class DebugMain
{
    private OfficeView _officeView=null!;
    private OptionButton _officeLocation=null!,_officeCatalog=null!,_officeProperty=null!,_officePerson=null!,_officeEffect=null!;
    private Label _officeInfo=null!,_officeFeedback=null!,_officeSelection=null!;
    private ItemList _officeItems=null!;
    private Control _officeEditor=null!, _officeCommitBar=null!;
    private OfficePortrait _officePortrait=null!;
    private List<OptionButton> _appearanceOptions=new();
    private CheckBox _appearanceGlasses=null!;
    private AppearanceRecipe? _appearanceShown;
    private ApplyOfficeLayoutCommand? _officeDraft;
    private GameState? _officeMovePreview;
    private double _officePreviousSpeed;
    private int _officeMoveOffer,_officeMoveRevision,_officeItem,_officeTemporary=-1;
    private GameState OfficeState=>_officeMovePreview??_state;
    private int OfficeLocation=>_officeDraft?.LocationId??(_officeLocation.GetSelectedId()>0?_officeLocation.GetSelectedId():_state.Protagonist.Employment!.LocationId);
    private bool OfficeEditing=>_officeDraft is not null;
    private Control BuildOfficePanel()
    {
        var panel=new VBoxContainer{Name="Office"};
        var toolbar=new HFlowContainer();panel.AddChild(toolbar);
        _officeLocation=new OptionButton();toolbar.AddChild(_officeLocation);
        _officeLocation.ItemSelected+=_=>RefreshOffice();
        void Button(Control parent,string title,Action action){var b=new Button{Text=title};parent.AddChild(b);b.Pressed+=()=>{try{action();}catch(InvalidCommandException ex){_officeFeedback.Text=ex.Message;}};}
        Button(toolbar,"Focus mangaka",()=>{_officeLocation.Select(Math.Max(0,_officeLocation.GetItemIndex(_state.Protagonist.Employment!.LocationId)));_officeView.SelectedPerson=_state.ProtagonistPersonId;RefreshOffice();_officeView.FocusSelected();});
        Button(toolbar,"Furnish",()=>BeginOfficeEditor());
        Button(toolbar,"Reset camera",()=>_officeView.ResetCamera());
        Button(toolbar,"↶",()=>_officeView.RotateCamera(-90));Button(toolbar,"↷",()=>_officeView.RotateCamera(90));
        toolbar.AddChild(new Label{Text="Timelapse"});var effect=new OptionButton();_officeEffect=effect;effect.AddItem("Full");effect.AddItem("Reduced");effect.AddItem("Off");toolbar.AddChild(effect);effect.ItemSelected+=i=>{_officeView.Effect=2-(int)i;_officeView.ClearMotion();};
        if(!ManagementInterface)Button(toolbar,"Import previous save",ImportOfficeSave);
        var move=new HFlowContainer();panel.AddChild(move);_officeProperty=new OptionButton();move.AddChild(_officeProperty);
        foreach(var p in TokyoProperties.All)_officeProperty.AddItem($"{p.District} · {p.Seats} desks · ¥{p.Rent:N0}/month",p.Id);
        Button(move,"Preview studio move",()=>BeginOfficeEditor(_officeProperty.GetSelectedId()));
        Button(move,"Lease empty branch",()=>{if(OfficeEditing)throw new InvalidCommandException("Finish furnishing first.");var command=new StudioActionCommand(StudioAction.Lease,_officeProperty.GetSelectedId());void Lease(){try{_state.Apply(command);_dirty=true;RefreshOffice();}catch(InvalidCommandException ex){_officeFeedback.Text=ex.Message;}}if(ManagementInterface)ConfirmPlayerAction("Lease empty branch",OperationConfirmation(command),Lease);else Lease();});
        _officeCommitBar=new HFlowContainer{Visible=false};panel.AddChild(_officeCommitBar);
        Button(_officeCommitBar,"Apply layout and cost",ApplyOfficeEditor);
        Button(_officeCommitBar,"Discard changes",()=>EndOfficeEditor());
        _officeInfo=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart};panel.AddChild(_officeInfo);
        _officeFeedback=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart};_officeFeedback.AddThemeColorOverride("font_color",new(.95f,.77f,.36f));panel.AddChild(_officeFeedback);
        var body=new HBoxContainer{SizeFlagsVertical=SizeFlags.ExpandFill};panel.AddChild(body);
        _officeView=new OfficeView();body.AddChild(_officeView);
        var scroll=new ScrollContainer{CustomMinimumSize=new(310,0),HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};body.AddChild(scroll);
        var side=new VBoxContainer{CustomMinimumSize=new(295,0),SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(side);
        _officePortrait=new OfficePortrait{CustomMinimumSize=new(120,120)};side.AddChild(_officePortrait);
        _officeSelection=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new(290,0)};side.AddChild(_officeSelection);
        _officePerson=new OptionButton();side.AddChild(_officePerson);_officePerson.ItemSelected+=_=>{_officeView.SelectedPerson=_officePerson.GetSelectedId();RefreshOfficeSelection();};
        Button(side,"Staff and series details",()=>{if(OfficeEditing)return;_selectedPersonId=_officeView.SelectedPerson;if(ManagementInterface)Navigate("Person",_selectedPersonId);else _mainTabs.CurrentTab=2;_dirty=true;});
        var alerts=new Button{Text="Next staff needing attention"};side.AddChild(alerts);alerts.Pressed+=()=>
        {
            if(OfficeEditing)return;
            var p=_state.ControlledStaff.FirstOrDefault(p=>p.LowestNeed<35||p.Resigning||_state.WageObligations.Any(w=>w.PersonId==p.Id&&w.Remaining>0));
            if(p is null){_officeFeedback.Text="No staff need attention.";return;}
            _officeLocation.Select(_officeLocation.GetItemIndex(p.Employment!.LocationId));_officeView.SelectedPerson=p.Id;RefreshOffice();
        };
        _officeEditor=new VBoxContainer{Visible=false};side.AddChild(_officeEditor);
        _officeCatalog=new OptionButton();_officeEditor.AddChild(_officeCatalog);
        foreach(var f in OfficeCatalog.Furniture)_officeCatalog.AddItem($"{f.Name} · ¥{f.Price:N0}");
        Button(_officeEditor,"Add / replace selected chair",AddOfficeFurniture);
        Button(_officeEditor,"Auto-arrange owned furniture",()=>AutoOffice(false));
        Button(_officeEditor,"Fill all desks (purchase missing)",()=>AutoOffice(true));
        Button(_officeEditor,"Basic furnishing package",()=>{AutoOffice(true);AddOfficeKind("cabinet");});
        _officeItems=new ItemList{CustomMinimumSize=new(280,180)};_officeEditor.AddChild(_officeItems);
        _officeItems.ItemSelected+=i=>{_officeItem=(int)_officeItems.GetItemMetadata((int)i);_officeView.SelectedFurniture=_officeItem;RefreshOfficeSelection();};
        Button(_officeEditor,"Place selected stored item",()=>PlaceStoredOffice());
        Button(_officeEditor,"Rotate selected (R)",RotateOfficeFurniture);
        Button(_officeEditor,"Lock / unlock selected",()=>{if(_officeDraft is null)return;var p=_officeDraft.Placements.FirstOrDefault(p=>p.ItemId==_officeItem);if(p is not null)ReplaceOfficePlacement(p,p with{Locked=!p.Locked});});
        Button(_officeEditor,"Put selected in storage",()=>RemoveOfficeFurniture(false));
        Button(_officeEditor,"Sell selected",()=>RemoveOfficeFurniture(true));
        Button(_officeEditor,"Assign selected person's desk",AssignOfficeDesk);
        side.AddChild(new Label{Text="Furniture prices are game estimates.\nStored furniture has no ongoing charge.\nWalking adds no productivity penalty.",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        var customize=new VBoxContainer();side.AddChild(customize);customize.AddChild(new Label{Text="Starting mangaka appearance"});
        var options=new List<OptionButton>();_appearanceOptions=options;
        foreach(var (label,values) in new[]{("Build",new[]{"Slim","Medium","Broad"}),("Skin",new[]{"Light","Warm","Tan","Deep"}),("Hair",new[]{"Black","Brown","Light brown","Grey"}),("Style",new[]{"Short","Bob","Tied"}),("Clothes",new[]{"Blue","Burgundy","Green","Ochre","Purple","Grey"}),("Clothing",new[]{"Cardigan","Collared shirt","Sweater"})})
        {var option=new OptionButton();foreach(var v in values)option.AddItem($"{label}: {v}");customize.AddChild(option);options.Add(option);}
        var glasses=new CheckBox{Text="Glasses"};_appearanceGlasses=glasses;customize.AddChild(glasses);
        var appearance=_state.Protagonist.Appearance??new();
        var chosen=new[]{appearance.Build,appearance.Skin,appearance.Hair,appearance.Style,appearance.Outfit,appearance.Wardrobe};
        for(var i=0;i<options.Count;i++)options[i].Select(chosen[i]);glasses.ButtonPressed=appearance.Glasses;
        Button(customize,"Apply starting appearance",()=>{if(OfficeEditing)throw new InvalidCommandException("Finish furnishing first.");_state.Apply(new SetAppearanceCommand(new(options[1].Selected,options[2].Selected,options[4].Selected,options[3].Selected,glasses.ButtonPressed,options[0].Selected,options[5].Selected)));_dirty=true;RefreshOffice();});
        _officeView.PersonClicked+=id=>{_officeView.SelectedPerson=id;_selectedPersonId=id;RefreshOfficeSelection();};
        _officeView.FurnitureClicked+=id=>{_officeItem=id;_officeView.SelectedFurniture=id;RefreshOfficeSelection();};
        _officeView.GroundClicked+=cell=>MoveOfficeFurniture(cell);
        _officeView.GroundDragged+=cell=>MoveOfficeFurniture(cell);
        _officeView.RotateRequested+=RotateOfficeFurniture;
        return panel;
    }
    private void RefreshOffice()
    {
        if(_officeView is null)return;
        var appearance=_state.Protagonist.Appearance??new();
        if(_appearanceShown!=appearance)
        {
            var selected=new[]{appearance.Build,appearance.Skin,appearance.Hair,appearance.Style,appearance.Outfit,appearance.Wardrobe};
            for(var n=0;n<_appearanceOptions.Count;n++)_appearanceOptions[n].Select(selected[n]);
            _appearanceGlasses.ButtonPressed=appearance.Glasses;_appearanceShown=appearance;
        }
        var id=_officeLocation.GetSelectedId();
        if(!OfficeEditing)
        {
            SyncOptions(_officeLocation,_state.Locations.Where(l=>l.BusinessId==_state.ControlledBusinessId&&!l.Closed).Select(l=>(l.Id,$"{l.Name} · {l.District}")),id);
            var index=_officeLocation.GetItemIndex(id);if(index<0)index=_officeLocation.GetItemIndex(_state.Protagonist.Employment!.LocationId);_officeLocation.Select(Math.Max(0,index));
        }
        var state=OfficeState;var location=state.Locations.FirstOrDefault(l=>l.Id==OfficeLocation);if(location is null)return;
        for(var i=0;i<OfficeCatalog.Furniture.Length;i++)_officeCatalog.SetItemDisabled(i,!state.EquipmentAvailable(OfficeCatalog.Furniture[i].Id));
        var office=_officeDraft is null?state.OfficeAt(location.Id):new OfficeLayout{LocationId=location.Id,Placements=_officeDraft.Placements,Assignments=_officeDraft.Assignments??state.OfficeAt(location.Id).Assignments};
        var inventory=DraftInventory();
        _officeView.Editing=OfficeEditing;_officeView.Speed=_speed;
        _officeView.Bind(state,location.Id,office,inventory);
        _officeEditor.Visible=OfficeEditing;_officeCommitBar.Visible=OfficeEditing;_officeLocation.Disabled=OfficeEditing;
        _officeInfo.Text=$"{location.Name} · {state.ControlledBusiness.Name} · business ¥{state.Money:N0} · rent ¥{location.MonthlyRent:N0}/month\n"+
            $"{state.UsableWorkspaces(location.Id)}/{location.Seats} usable desks · {location.BreakSeats} break places · {(location.IsFamilyHome?"Shared family home":"Shared building")}";
        if(OfficeEditing)
        {
            _officeItems.Clear();foreach(var item in inventory.Values.OrderBy(i=>i.Id))
            {
                var p=_officeDraft!.Placements.FirstOrDefault(p=>p.ItemId==item.Id);var row=_officeItems.AddItem($"{OfficeCatalog.Get(item.Kind).Name} · {(p is null?"stored":p.Locked?"locked":"placed")}");_officeItems.SetItemMetadata(row,item.Id);
                if(item.Id==_officeItem)_officeItems.Select(row);
            }
            try
            {
                var quote=state.QuoteOffice(_officeDraft!);
                _officeFeedback.Text=_officeMovePreview is null?quote.Description:$"Move including lease, moving and furniture: ¥{_state.QuoteOfficeRelocation(new(_officeMoveOffer,_officeMoveRevision,_officeDraft!)):N0}\n{quote.Description}";
            }
            catch(InvalidCommandException ex){_officeFeedback.Text=$"Cannot apply: {ex.Message}";}
        }
        var person=_officeView.SelectedPerson;_officePerson.Clear();foreach(var p in state.People.Where(p=>p.Employment?.LocationId==location.Id))_officePerson.AddItem(p.Name,p.Id);
        if(_officePerson.ItemCount>0)_officePerson.Select(Math.Max(0,_officePerson.GetItemIndex(person)));
        RefreshOfficeSelection();
    }
    private IReadOnlyDictionary<int,OfficeFurniture> DraftInventory()
    {
        if(_officeDraft is null)return OfficeState.OfficeInventory;
        var items=OfficeState.AvailableFurniture(_officeDraft.LocationId).Where(i=>!_officeDraft.Sell.Contains(i.Id)).ToDictionary(i=>i.Id);
        foreach(var purchase in _officeDraft.Purchases)items[purchase.TemporaryId]=new(){Id=purchase.TemporaryId,Kind=purchase.Kind,BusinessId=OfficeState.ControlledBusinessId};
        return items;
    }
    private void RefreshOfficeSelection()
    {
        var p=OfficeState.FindPerson(_officeView.SelectedPerson)??OfficeState.Protagonist;_officePortrait.Recipe=p.Appearance??new();_officePortrait.QueueRedraw();
        var a=OfficeState.OfficeActivities.FirstOrDefault(a=>a.PersonId==p.Id);
        _officeSelection.Text=$"{p.Name}\n{a?.Kind.ToString()??"Ready"}{(a?.Stage is{} stage?$" · {stage}":"")}\nFood {p.Food:F0} · drink {p.Drink:F0}\nComfort {p.Comfort:F0} · happiness {p.Happiness:F0}\n{(p.Resigning?"Notice given":p.LowestNeed<35?"Needs a break":"")}";
        if(OfficeEditing&&DraftInventory().TryGetValue(_officeItem,out var item))_officeSelection.Text+=$"\nSelected: {OfficeCatalog.Get(item.Kind).Name}\nOwner: {item.Owner} · resale ¥{(item.Owner==FurnitureOwner.Business?item.Paid/2:0):N0}";
    }
    private void BeginOfficeEditor(int moveOffer=0)
    {
        if(OfficeEditing)throw new InvalidCommandException("Apply or discard this layout first.");
        GameState? preview=moveOffer==0?null:_state.PreviewOfficeMove(moveOffer);
        _officePreviousSpeed=_speed;Pause();_officeMovePreview=preview;_officeMoveOffer=moveOffer;_officeMoveRevision=_state.OfficeRevision;
        var id=preview?.Protagonist.Employment!.LocationId??OfficeLocation;var state=OfficeState;
        _officeDraft=new(id,state.OfficeRevision,state.OfficeAt(id).Placements.ToList(),[],[],state.OfficeAt(id).Assignments.ToList());
        _officeTemporary=-1;_officeItem=0;
        LockOfficeControls(true);RefreshOffice();
    }
    private void LockOfficeControls(bool locked)
    {
        var officeTab=Enumerable.Range(0,_mainTabs.GetTabCount()).Single(i=>_mainTabs.GetTabControl(i).Name=="Office");
        for(var i=0;i<_mainTabs.GetTabCount();i++)_mainTabs.SetTabDisabled(i,locked&&i!=officeTab);
        foreach(var button in _clockLabel.GetParent().FindChildren("*","BaseButton",true,false).OfType<BaseButton>())button.Disabled=locked;
        if(locked)_mainTabs.CurrentTab=officeTab;
    }
    private void EndOfficeEditor()
    {
        _officeDraft=null;_officeMovePreview=null;_officeItem=0;_officeView.SelectedFurniture=0;LockOfficeControls(false);
        _officeView.ClearMotion();if(!_recapDialog.Visible)SetSpeed(_officePreviousSpeed);_officeFeedback.Text="";_dirty=true;RefreshOffice();
    }
    private void ApplyOfficeEditor()
    {
        if(_officeDraft is null)return;
        if(_officeMovePreview is null)_state.Apply(_officeDraft);
        else _state.Apply(new RelocateOfficeCommand(_officeMoveOffer,_officeMoveRevision,_officeDraft));
        EndOfficeEditor();ScanEvents();
    }
    private void AutoOffice(bool fill)
    {
        if(_officeDraft is null)return;
        var location=OfficeState.Locations.Single(l=>l.Id==_officeDraft.LocationId);
        var a=OfficeAutoArrange.Arrange(OfficeCatalog.Plan(location),DraftInventory().Values,_officeDraft.Placements,
            OfficeState.People.Count(p=>p.Employment?.LocationId==location.Id),fill,fill,OfficeState.OfficeDeskOrder(location.Id));
        _officeDraft=_officeDraft with{Placements=a.Placements,Purchases=_officeDraft.Purchases.Concat(a.Purchases).ToList(),Assignments=null};RefreshOffice();
    }
    private void AddOfficeFurniture()=>AddOfficeKind(OfficeCatalog.Furniture[_officeCatalog.Selected].Id);
    private void AddOfficeKind(string kind)
    {
        if(_officeDraft is null)return;
        var before=_officeDraft;var inventory=DraftInventory();while(inventory.ContainsKey(_officeTemporary))_officeTemporary--;
        var id=_officeTemporary--;var purchases=_officeDraft.Purchases.ToList();purchases.Add(new(id,kind));
        var placements=_officeDraft.Placements.ToList();var definition=OfficeCatalog.Get(kind);
        if(definition.Chair)
        {
            var selected=placements.FirstOrDefault(p=>p.ItemId==_officeItem);
            var desk=selected?.DeskId is{} d?placements.Single(p=>p.ItemId==d):selected;
            if(desk is null||!OfficeCatalog.Get(inventory[desk.ItemId].Kind).Desk)throw new InvalidCommandException("Select a desk or its chair before replacing the chair.");
            placements.RemoveAll(p=>p.DeskId==desk.ItemId);placements.Add(OfficeLayoutRules.ChairAt(desk,id));
        }
        if(definition.Desk)purchases.Add(new(_officeTemporary--,"chair"));
        _officeDraft=_officeDraft with{Placements=placements,Purchases=purchases,Assignments=null};
        try{if(!definition.Chair)AutoOffice(false);else RefreshOffice();_officeItem=id;_officeView.SelectedFurniture=id;}
        catch{_officeDraft=before;throw;}
    }
    private void PlaceStoredOffice()
    {
        if(_officeDraft is null)return;var inventory=DraftInventory();
        if(!inventory.TryGetValue(_officeItem,out var item))return;
        if(_officeDraft.Placements.Any(p=>p.ItemId==item.Id))throw new InvalidCommandException("This item is already placed. Click an empty grid position to move it.");
        var selected=item.Id;AutoOffice(false);_officeItem=selected;_officeFeedback.Text="Auto-arranged available items. If this item remains stored, unlock furniture or make more room.";
    }
    private void ReplaceOfficePlacement(OfficePlacement old,OfficePlacement changed)
    {
        if(_officeDraft is null)return;
        var placements=_officeDraft.Placements.Select(p=>p.ItemId==old.ItemId?changed:p).ToList();
        var chair=placements.FirstOrDefault(p=>p.DeskId==old.ItemId);
        if(chair is not null)placements[placements.IndexOf(chair)]=OfficeLayoutRules.ChairAt(changed,chair.ItemId,chair.Locked);
        _officeDraft=_officeDraft with{Placements=placements};RefreshOffice();
    }
    private void MoveOfficeFurniture(OfficeCell cell)
    {
        if(_officeDraft is null)return;var p=_officeDraft.Placements.FirstOrDefault(p=>p.ItemId==_officeItem);if(p is null)return;
        if(p.DeskId is{} desk)p=_officeDraft.Placements.Single(x=>x.ItemId==desk);
        if(p.Locked){_officeFeedback.Text="Unlock this furniture before moving it.";return;}
        ReplaceOfficePlacement(p,p with{X=cell.X,Z=cell.Z});
    }
    private void RotateOfficeFurniture()
    {
        if(_officeDraft is null)return;var p=_officeDraft.Placements.FirstOrDefault(p=>p.ItemId==_officeItem);if(p is null)return;
        if(p.DeskId is{} desk)p=_officeDraft.Placements.Single(x=>x.ItemId==desk);if(p.Locked)return;
        ReplaceOfficePlacement(p,p with{Rotation=(p.Rotation+1)%4});
    }
    private void RemoveOfficeFurniture(bool sell)
    {
        if(_officeDraft is null)return;
        var inventory=DraftInventory();if(!inventory.TryGetValue(_officeItem,out var item))return;
        if(sell&&item.Owner!=FurnitureOwner.Business)throw new InvalidCommandException("Family and landlord furnishings stay with the property.");
        var ids=new HashSet<int>{_officeItem};foreach(var p in _officeDraft.Placements.Where(p=>p.DeskId==_officeItem))ids.Add(p.ItemId);
        _officeDraft=_officeDraft with{Placements=_officeDraft.Placements.Where(p=>!ids.Contains(p.ItemId)).ToList(),Assignments=null,
            Purchases=sell?_officeDraft.Purchases.Where(p=>!ids.Contains(p.TemporaryId)).ToList():_officeDraft.Purchases,
            Sell=sell?_officeDraft.Sell.Concat(ids.Where(id=>id>0)).Distinct().ToList():_officeDraft.Sell};RefreshOffice();
    }
    private void AssignOfficeDesk()
    {
        if(_officeDraft is null)return;
        var p=_officeDraft.Placements.FirstOrDefault(p=>p.ItemId==_officeItem);var id=p?.DeskId??p?.ItemId;
        if(id is null||!_officeDraft.Placements.Any(p=>p.DeskId==id))throw new InvalidCommandException("Select a complete workstation.");
        var person=_officePerson.GetSelectedId();var assignments=(_officeDraft.Assignments??OfficeState.OfficeAt(_officeDraft.LocationId).Assignments).ToList();
        var old=assignments.FirstOrDefault(a=>a.PersonId==person);var other=assignments.FirstOrDefault(a=>a.DeskId==id);
        assignments.RemoveAll(a=>a.PersonId==person||a.DeskId==id);assignments.Add(new(person,id.Value));
        if(old is not null&&other is not null&&other.PersonId!=person)assignments.Add(new(other.PersonId,old.DeskId));
        _officeDraft=_officeDraft with{Assignments=assignments};RefreshOffice();
    }
    private void SaveOfficePreferences()
    {
        using var file=FileAccess.Open(_savePath+".office-view.json",FileAccess.ModeFlags.Write);
        if(file is not null)file.StoreString(System.Text.Json.JsonSerializer.Serialize(_officeView.CapturePreferences()));
    }
    private void LoadOfficePreferences()
    {
        RefreshOffice();
        if(!FileAccess.FileExists(_savePath+".office-view.json"))return;
        using var file=FileAccess.Open(_savePath+".office-view.json",FileAccess.ModeFlags.Read);
        if(file is null)return;
        try
        {
            var view=System.Text.Json.JsonSerializer.Deserialize<OfficeViewPreferences>(file.GetAsText());if(view is null)return;
            var index=_officeLocation.GetItemIndex(view.Location);if(index>=0)_officeLocation.Select(index);
            RefreshOffice();_officeView.RestorePreferences(view);_officeEffect.Select(2-_officeView.Effect);RefreshOfficeSelection();
        }
        catch(System.Text.Json.JsonException){LogLine("Camera preferences could not be restored; using defaults.");}
    }
    private void ImportOfficeSave()
    {
        if(OfficeEditing)throw new InvalidCommandException("Finish furnishing before importing a save.");
        using var file=FileAccess.Open("user://debug-v3.json",FileAccess.ModeFlags.Read);
        if(file is null){_officeFeedback.Text="No previous debug-v3.json save found. The original is never overwritten.";return;}
        try
        {
            var imported=GameState.ImportStudioV3(file.GetAsText());CancelOvernight();_state=imported;Pause();_recapDialog.Hide();_log.Clear();_scanIndex=_state.Events.Count;
            _selectedPersonId=_state.ProtagonistPersonId;ResetPersonInputs();_dirty=true;_officeFeedback.Text="Imported into memory. Save writes debug-v5.json; the original remains intact.";
        }
        catch(Exception ex) when(ex is System.IO.InvalidDataException or System.Text.Json.JsonException){_officeFeedback.Text=ex.Message;}
    }
}

public partial class OfficePortrait : Control
{
    public AppearanceRecipe Recipe { get; set; }=new();
    public override void _Draw()
    {
        var centre=new Vector2(Size.X/2,44);var skin=new[]{new Color(.92f,.73f,.57f),new(.78f,.56f,.40f),new(.59f,.38f,.27f),new(.36f,.23f,.18f)}[Recipe.Skin];
        var hair=new[]{new Color(.12f,.10f,.09f),new(.25f,.16f,.10f),new(.44f,.32f,.22f),new(.48f,.49f,.48f)}[Recipe.Hair];
        var clothes=new[]{new Color(.30f,.45f,.51f),new(.51f,.32f,.35f),new(.36f,.45f,.32f),new(.64f,.52f,.34f),new(.43f,.36f,.53f),new(.6f,.63f,.61f)}[Recipe.Outfit];
        DrawRect(new Rect2(0,0,Size.X,120),new(.19f,.24f,.27f));DrawCircle(centre+new Vector2(0,45),29,clothes);DrawRect(new Rect2(centre+new Vector2(-8,18),new(16,23)),skin);
        DrawCircle(centre,28,hair);DrawCircle(centre+new Vector2(0,7),23,skin);DrawArc(centre-new Vector2(0,4),25,Mathf.Pi,Mathf.Tau,24,hair,12,true);
        if(Recipe.Style==1){DrawRect(new(centre+new Vector2(-29,-3),new(8,41)),hair);DrawRect(new(centre+new Vector2(21,-3),new(8,41)),hair);}
        if(Recipe.Style==2)DrawCircle(centre+new Vector2(26,-11),11,hair);
        foreach(var x in new[]{-9,9}){DrawCircle(centre+new Vector2(x,8),2,new(.12f,.11f,.1f));if(Recipe.Glasses)DrawArc(centre+new Vector2(x,8),8,0,Mathf.Tau,20,new(.2f,.22f,.24f),2,true);}
        DrawLine(centre+new Vector2(-6,21),centre+new Vector2(6,21),skin.Darkened(.3f),2,true);
    }
}
