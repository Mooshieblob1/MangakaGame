using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;
using MangakaSim.Rules;

namespace MangakaGame;

public partial class OfficeView : SubViewportContainer
{
    private SubViewport _viewport=null!;
    private Node3D _world=null!, _room=null!, _actorsRoot=null!;
    private Camera3D _camera=null!;
    private readonly Dictionary<int,OfficeActor> _actors=new();
    private readonly List<OfficeActor> _ambient=new();
    private readonly Dictionary<int,AppearanceRecipe> _recipes=new();
    private readonly List<(Node3D Wall,int Side)> _walls=new();
    private GameState? _state;
    private int _location;
    private FloorPlanDefinition _plan=null!;
    private OfficeLayout _layout=null!;
    private IReadOnlyDictionary<int,OfficeFurniture> _inventory=null!;
    private string _signature="";
    private DateTime _lastAt;
    private bool _discontinuity;
    private readonly Dictionary<int,Vector3> _targets=new();
    private readonly Dictionary<int,(float Angle,float Zoom,Vector3 Pan)> _bookmarks=new();
    private readonly List<OfficeAmbientTraffic> _ambientTraffic=new();
    private readonly Dictionary<(int Location,int Parent),OfficeAmbientTraffic.Continuity> _familyContinuity=new();
    internal IReadOnlyList<OfficeAmbientTraffic> AmbientTraffic=>_ambientTraffic;
    private float _angle=.70f,_zoom=12;
    private Vector3 _pan;
    private bool _rotating,_panning;
    public bool Editing { get; set; }
    public int SelectedPerson { get; set; }
    public int SelectedFurniture { get; set; }
    public double Speed { get; set; }
    public int Effect { get; set; } = 2;
    public bool ShowCompanion { get; set; }
    public event Action? HelperClicked;
    private HelperChan? _helper;
    public bool CompanionVisible => _helper is not null && GodotObject.IsInstanceValid(_helper)&&_helper.Visible;
    public int ActorCount=>_actors.Count;
    public int AmbientCount=>_ambient.Count;
    public int VisibleTrails=>_actors.Values.Concat(_ambient).Sum(a=>a.TrailCount);
    public event Action<int>? PersonClicked;
    public event Action<int>? FurnitureClicked;
    public event Action<OfficeCell>? GroundClicked;
    public event Action<OfficeCell>? GroundDragged;
    public event Action? RotateRequested;
    private bool _draggingFurniture;
    private Label _hint=null!;
    public bool ShowNavigationHint { get=>_showNavigationHint; set{_showNavigationHint=value;if(_hint is not null)_hint.Visible=value;} }
    private bool _showNavigationHint=true;
    public double LabelTextScale { get; set; }=1;
    // The interface size (display settings, spec 2026-10-04) scales this control's logical size down; the 3D image
    // renders at the window's real pixels, and points convert between the two through this scale.
    private float _renderScale=1;
    public float RenderScale { get=>_renderScale; set{if(Mathf.IsEqualApprox(_renderScale,value))return;_renderScale=Math.Max(.5f,value);Callable.From(FitViewport).CallDeferred();} }
    private void FitViewport()
    {
        if(_viewport is null||!IsInsideTree())return;
        var size=new Vector2I(Math.Max(2,(int)Math.Round(Size.X*_renderScale)),Math.Max(2,(int)Math.Round(Size.Y*_renderScale)));
        if(_viewport.Size!=size)_viewport.Size=size;
    }
    internal SubViewport RenderViewport=>_viewport;
    private Vector2 ToViewport(Vector2 local)=>local*((Vector2)_viewport.Size/Size.Max(Vector2.One));
    private Vector2 ToLocal(Vector2 viewportPoint)=>viewportPoint/((Vector2)_viewport.Size/Size.Max(Vector2.One));
    public Rect2 PresentationArea { get; set; }

    public override void _Ready()
    {
        // Click focus only: a controller moves between panels and never lands on the whole office picture.
        // Not stretched: the 3D image is sized by RenderScale and shown through a picture that fills this control, so it
        // stays sharp at large interface sizes (display settings, spec 2026-10-04). Our own native draw is hidden.
        Stretch=false;SizeFlagsHorizontal=SizeFlags.ExpandFill;SizeFlagsVertical=SizeFlags.ExpandFill;
        CustomMinimumSize=new(640,400);FocusMode=FocusModeEnum.Click;MouseDefaultCursorShape=CursorShape.Arrow;
        // Held under a plain node so this container neither measures nor draws it; the picture below shows it.
        var holder=new Node{Name="ViewportHolder"};AddChild(holder);
        _viewport=new SubViewport{OwnWorld3D=true,TransparentBg=false,Size=new(1000,650),Msaa3D=Viewport.Msaa.Msaa2X,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};holder.AddChild(_viewport);
        var picture=new TextureRect{Name="OfficePicture",Texture=_viewport.GetTexture(),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode=TextureRect.StretchModeEnum.Scale,MouseFilter=MouseFilterEnum.Ignore};
        AddChild(picture);picture.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);Callable.From(FitViewport).CallDeferred();
        _world=new Node3D();_viewport.AddChild(_world);
        _room=new Node3D();_world.AddChild(_room);_actorsRoot=new Node3D();_world.AddChild(_actorsRoot);
        _camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,KeepAspect=Camera3D.KeepAspectEnum.Height,Current=true,Far=300};_world.AddChild(_camera);
        BuildDaylight();BuildToiletPrivacy();
        _hint=new Label{Text="Wheel: zoom · right drag: rotate · middle drag / WASD: pan",Visible=_showNavigationHint,Position=new(14,14),Size=new(400,52),AutowrapMode=TextServer.AutowrapMode.WordSmart,MouseFilter=MouseFilterEnum.Ignore};AddChild(_hint);
        Resized+=()=>{_hint.Size=new(Math.Max(100,Size.X-28),52);Callable.From(FitViewport).CallDeferred();UpdateCamera();};
        _hint.AddThemeColorOverride("font_color",new Color("f5f0e5"));_hint.AddThemeColorOverride("font_shadow_color",Colors.Black);_hint.AddThemeConstantOverride("shadow_offset_x",1);_hint.AddThemeConstantOverride("shadow_offset_y",1);
        VisibilityChanged+=()=>{if(_viewport is not null)_viewport.RenderTargetUpdateMode=IsVisibleInTree()?SubViewport.UpdateMode.Always:SubViewport.UpdateMode.Disabled;};
    }
    public void Bind(GameState state,int location,OfficeLayout? draft=null,IReadOnlyDictionary<int,OfficeFurniture>? inventory=null)
    {
        if(_world is null)return;
        var l=state.Locations.FirstOrDefault(l=>l.Id==location);if(l is null)return;
        var layout=draft??state.OfficeAt(location);var items=inventory??state.OfficeInventory;
        var signature=$"{location}:{l.FloorPlanId}:{Editing}:{SelectedFurniture}:{ShowCompanion}:{state.Protagonist.Employment?.LocationId}:"+string.Join(';',layout.Placements);
        var replaced=!ReferenceEquals(state,_state);
        if(replaced||state.Clock.Now<_lastAt)_familyContinuity.Clear();
        else if(_location!=location&&ViewingFamilyHome)
            for(var i=0;i<_ambientTraffic.Count;i++)_familyContinuity[(_location,i)]=_ambientTraffic[i].CaptureContinuity();
        _discontinuity=replaced||_location!=location||state.Clock.Now<_lastAt;
        _lastAt=state.Clock.Now;
        if(_discontinuity){ResetToiletVisits();_arriving.Clear();_visualBreaks.Clear();_nextVisualBreak.Clear();_visualClock=0;_parentMealAfter=0;_creatorBreakAfter=0;_targets.Clear();ClearMotion();foreach(var traffic in _ambientTraffic)traffic.Reset();}
        if(_location!=location||replaced)
        {
            if(_location!=0&&!replaced)_bookmarks[_location]=(_angle,_zoom,_pan);
            if(_bookmarks.TryGetValue(location,out var bookmark)&&!replaced){_angle=bookmark.Angle;_zoom=bookmark.Zoom;_pan=bookmark.Pan;}
            else{_angle=.70f;_zoom=Math.Max(10,Math.Max(OfficeCatalog.Plan(l).Width+16,OfficeCatalog.Plan(l).Depth)*.30f);_pan=Vector3.Zero;}
            foreach(var a in _actors.Values.Concat(_ambient))a.QueueFree();_actors.Clear();_ambient.Clear();_recipes.Clear();_targets.Clear();_ambientTraffic.Clear();_departing.Clear();
        }
        _state=state;_location=location;_layout=layout;_inventory=items;_plan=OfficeCatalog.Plan(l);
        if(signature!=_signature||replaced){_signature=signature;BuildRoom(l);_targets.Clear();foreach(var a in _actors.Values)a.ClearTrail();}
        SyncActors();_discontinuity=false;UpdateCamera();
    }
    private void BuildRoom(StudioLocation l)
    {
        foreach(var n in _room.GetChildren()){_room.RemoveChild(n);n.QueueFree();}_walls.Clear();_doors.Clear();
        _helper=null;_helperTarget=null;_windowSkies.Clear();_windowOrbs.Clear();_roomLamps.Clear();_lampMaterials.Clear();
        var w=_plan.Width*.25f;var d=_plan.Depth*.25f;
        var home=l.IsFamilyHome;var floor=home?new Color(.60f,.47f,.33f):new Color(.55f,.60f,.59f);
        BuildNeighborhood(home,w,d);
        OfficeArt.Box(_room,new(w/2,-.10f,d/2),new(w,.2f,d),floor);
        if(home)BuildFamilyHallFloor();
        else OfficeArt.Box(_room,new(w+1.9f,-.10f,d/2+.25f),new(3.8f,.2f,d+.5f),new(.43f,.49f,.49f));
        OfficeArt.Box(_room,new(w/2,-.10f,d+.35f),new(w,.2f,.7f),home?new(.70f,.61f,.46f):new(.43f,.49f,.49f));
        if(home)for(var x=.15f;x<w;x+=.5f)OfficeArt.Box(_room,new(x,.003f,d/2),new(.01f,.004f,d),floor.Darkened(.06f));
        Wall(new(w/2,1.25f,0),new(w,2.5f,.12f),0);
        var companion=ShowCompanion&&_state!.Protagonist.Employment?.LocationId==l.Id;
        if(companion)
        {
            // A dedicated non-economic alcove extends the same room, outside every saved placement cell.
            // No production desk, door route, rent or inventory changes are needed, even at full capacity.
            var depth=d;
            if(d>depth)Wall(new(0,1.25f,depth+(d-depth)/2),new(.12f,2.5f,d-depth),1);
            OfficeArt.Box(_room,new(-1.5f,-.10f,depth/2),new(3,.2f,depth),floor);
            Wall(new(-1.5f,1.25f,0),new(3,2.5f,.12f),0);
            Wall(new(-3,1.25f,depth/2),new(.12f,2.5f,depth),1);
            Wall(new(-1.875f,1.25f,depth),new(2.25f,2.5f,.12f),2);
            if(_plan.Island)
            {
                // Studio island (Q59): her desk joins the room facing the spare desk, in cells the layout rules keep clear.
                var place=FloorPlanDefinition.IslandHelperDesk;var seat=OfficeLayoutRules.ChairAt(place,0);
                var desk=OfficeArt.Furniture("desk",1);desk.Position=PlacementOrigin(place,OfficeCatalog.Get("desk-better"));desk.RotationDegrees=new(0,-90*place.Rotation,0);_room.AddChild(desk);
                var chair=OfficeArt.Furniture("chair");chair.Position=PlacementOrigin(seat,OfficeCatalog.Get("chair"));chair.RotationDegrees=new(0,-90*seat.Rotation,0);_room.AddChild(chair);
                BuildStudioCorner();
            }
            else
            {
                var desk=OfficeArt.Furniture("desk-better",1,frontWriting:true);desk.Position=new(-2.15f,0,1.55f);_room.AddChild(desk);
                var chair=OfficeArt.Furniture("chair");chair.Position=new(-1.125f,0,1.615f);chair.RotationDegrees=new(0,180,0);_room.AddChild(chair);
            }
            _helper=new HelperChan{Position=HelperDesk,DeskFacing=_plan.Island?Mathf.Pi/2:0};_room.AddChild(_helper);
            OfficeArt.WorldLabel(_helper,"Helper-Chan",new(0,1.78f,0),20).Name="NameTag";
        }
        else Wall(new(0,1.25f,d/2),new(.12f,2.5f,d),1);
        BuildSouthWalls(w,d,companion,home,floor);
        // Actual openings in the outer wall, shared by the door art and traffic routes.
        var doorA=.85f;var doorB=d-.85f;
        var wc=ToiletDoor.Z;
        var edges=home?new[]{(0f,wc-.44f),(wc+.44f,doorB-.65f),(doorB+.65f,d)}:
            new[]{(0f,doorA-.65f),(doorA+.65f,wc-.44f),(wc+.44f,doorB-.65f),(doorB+.65f,d)};
        foreach(var (start,end) in edges)
            if(end>start)Wall(new(w+3.8f,1.25f,(start+end)/2),new(.12f,2.5f,end-start),3);
        if(home)BuildFamilyStairs();
        else AmbientDoor(new(w+3.8f,0,doorA),"201");
        BuildToiletRoom();
        AmbientDoor(new(w+3.8f,home?GenkanFloorHeight:0,doorB),home?"ENTRY":"STAIRS");
        // Rear corridor opening leads upstairs, well away from the entrance.
        Wall(new(w+(home?1.275f:1.9f),1.25f,0),new(home?2.55f:3.8f,2.5f,.12f),0);
        // Tenant boundary; common circulation and shared fittings remain outside the editable room.
        Wall(new(w,1.25f,d/2-.5f),new(.12f,2.5f,d-1),3);
        for(var x=1f;x<w-.5f;x+=2.5f)
        {
            BuildSkyWindow(x);
            OfficeArt.Box(_room,new(x,1.65f,.13f),new(.04f,.9f,.035f),new(.36f,.38f,.35f));
            OfficeArt.Box(_room,new(x,1.20f,.13f),new(1.35f,.06f,.18f),new(.36f,.38f,.35f));
        }
        BuildSharedBreakRoom();
        BuildRoomLamps(w,d);
        for(var i=0;i<3;i++)OfficeArt.Box(_room,new(w-1f+i%2*.35f,.15f+i/2*.3f,d-1.0f),new(.32f,.3f,.42f),new(.65f,.53f,.35f));
        if(home)
        {
            BuildGenkan();
        }
        foreach(var p in _layout.Placements)
        {
            if(!_inventory.TryGetValue(p.ItemId,out var item))continue;
            var def=OfficeCatalog.Get(item.Kind);var node=OfficeArt.Furniture(def.Art,def.Variant);
            node.Position=PlacementOrigin(p,def);node.RotationDegrees=new(0,-90*p.Rotation,0);_room.AddChild(node);
            if(p.ItemId==SelectedFurniture)
            {var size=OfficeLayoutRules.Size(def,p.Rotation);OfficeArt.Box(_room,new((p.X+size.W/2f)*.25f,.012f,(p.Z+size.D/2f)*.25f),new(size.W*.25f,.012f,size.D*.25f),new(.91f,.71f,.25f,.32f));}
        }
        if(Editing)
        {
            for(var x=0;x<=_plan.Width;x++)OfficeArt.Box(_room,new(x*.25f,.01f,d/2),new(.006f,.008f,d),new(.8f,.85f,.84f,.25f));
            for(var z=0;z<=_plan.Depth;z++)OfficeArt.Box(_room,new(w/2,.01f,z*.25f),new(w,.008f,.006f),new(.8f,.85f,.84f,.25f));
        }
        if(_ambient.Count==0)
        {
            for(var i=0;i<(home?2:Math.Min(6,2+l.PropertyTier));i++)
            {
                var actor=new OfficeActor{Ambient=true,Visible=false};_actorsRoot.AddChild(actor);
                if(home)actor.BuildParent(i==1);
                else{var recipe=AppearanceRecipe.Generate(_state!.RngSeed,10000+l.Id*10+i);actor.Build(recipe);}
                actor.Destination=actor.Position;actor.ClearTrail();_ambient.Add(actor);
                _ambientTraffic.Add(home
                    ?new(actor,FamilyStairTop,StaffExit,w+3.2f,i,true)
                    :new(actor,new(w+3.92f,0,doorA),new(w+3.92f,0,doorB),w+3.2f,i));
                if(home)
                {
                    var seat=2+i;
                    var parent=_ambientTraffic[^1];
                    parent.ConfigureFamily(ToiletDoor,ToiletSeat,()=>ToiletAvailable(parent));
                    if(_familyContinuity.TryGetValue((_location,i),out var saved))parent.RestoreContinuity(saved);
                    _ambientTraffic[^1].ConfigureMeals(seat,SharedBreakSeat(seat),seat%2==0?-Mathf.Pi/2:Mathf.Pi/2,
                        FamilyWalkingRoute,()=>ParentSeatAvailable(seat),ParentMealsMayStart);
                }
            }
        }
    }
    private void AmbientDoor(Vector3 threshold,string label,float width=OfficeDoor.LeafWidth,bool recess=true)
    {
        // Recess, open leaf and a persistent frame: cutaway walls cannot erase the exit.
        var frame=new Color(.33f,.29f,.23f);var dark=new Color(.12f,.14f,.14f);
        if(recess)OfficeArt.Box(_room,threshold+new Vector3(.60f,.015f,0),new(1.45f,.03f,width),new(.66f,.65f,.58f));
        // Recess backing faces the interior only; the dollhouse camera sees the open
        // frame from outside, rather than an opaque box hiding the doorway.
        if(recess)_room.AddChild(new MeshInstance3D{Mesh=new QuadMesh{Size=new(width,2.1f)},
            Position=threshold+new Vector3(1.32f,1.05f,0),RotationDegrees=new(0,-90,0),MaterialOverride=OfficeArt.Material(dark)});
        foreach(var side in new[]{-1f,1f})
        {
            OfficeArt.Box(_room,threshold+new Vector3(.1f,1.05f,side*width/2),new(.3f,2.1f,.04f),dark);
            OfficeArt.Box(_room,threshold+new Vector3(-.02f,1.1f,side*(width/2+.06f)),new(.18f,2.2f,.12f),frame);
        }
        OfficeArt.Box(_room,threshold+new Vector3(-.02f,2.2f,0),new(.18f,.14f,width+.24f),frame);
        var door=new OfficeDoor{Position=threshold,Width=width};_room.AddChild(door);_doors.Add(door);
        OfficeArt.WorldLabel(_room,label,threshold+new Vector3(-.15f,2.42f,0),20,new(.92f,.9f,.78f));
    }
    private void Wall(Vector3 position,Vector3 size,int side)
    {
        // Anchor cutaway scaling at the wall's floor, including the lowered genkan.
        var floor=Math.Min(0,position.Y-size.Y/2);
        var n=new Node3D{Position=new(position.X,floor,position.Z)};_room.AddChild(n);
        OfficeArt.Box(n,new(0,position.Y-floor,0),size,new(.78f,.79f,.71f));_walls.Add((n,side));
    }
    public static Vector3 PlacementOrigin(OfficePlacement p,FurnitureDefinition f)
    {
        var offset=p.Rotation switch{1=>new Vector3(f.Depth*.25f,0,0),2=>new Vector3(f.Width*.25f,0,f.Depth*.25f),3=>new Vector3(0,0,f.Width*.25f),_=>Vector3.Zero};
        return new Vector3(p.X*.25f,0,p.Z*.25f)+offset;
    }
    private void SyncActors()
    {
        if(_state is null)return;
        var staff=_state.People.Where(p=>p.Employment?.LocationId==_location).ToArray();
        foreach(var id in _actors.Keys.Where(id=>!staff.Any(p=>p.Id==id)).ToArray())
        {
            if(_staffToiletId==id){_toiletLeaveRequested=true;continue;}
            var departed=_actors[id];
            if(!departed.Visible){RemoveStaffActor(id);continue;}
            if(_departing.Add(id)){RouteTo(departed,StaffExit);_targets[id]=StaffExit;departed.Activity="Travel";}
        }
        foreach(var p in staff)
        {
            if(!_actors.TryGetValue(p.Id,out var actor))
            {actor=new OfficeActor{PersonId=p.Id,Position=StaffExit,Visible=_discontinuity||Editing};_actorsRoot.AddChild(actor);_actors[p.Id]=actor;}
            var recipe=p.Appearance??AppearanceRecipe.Generate(_state.RngSeed,p.Id);
            if(!_recipes.TryGetValue(p.Id,out var previous)||previous!=recipe){actor.Build(recipe);actor.ClearTrail();_recipes[p.Id]=recipe;}
            actor.SetDisplayName(p.Name);
            var activity=_state.OfficeActivities.FirstOrDefault(a=>a.PersonId==p.Id&&a.LocationId==_location);
            actor.Activity=(activity?.Kind??OfficeActivityKind.Idle).ToString();actor.Selected=SelectedPerson==p.Id;
            actor.NeedsAttention=p.LowestNeed<35||p.Resigning||_state.WageObligations.Any(w=>w.PersonId==p.Id&&w.Remaining>0);
            var present=!EndingDay&&(p.BusyUntil<=_state.Clock.Now||p.BusyUntil is null)&&activity?.Kind is not
                (OfficeActivityKind.OffDuty or OfficeActivityKind.Travel or OfficeActivityKind.Convention or OfficeActivityKind.Relocating or OfficeActivityKind.OutsideJob)&&p.Employment!.StartsAt<=_state.Clock.Now;
            if(_staffToiletId==p.Id)
            {
                _toiletLeaveRequested|=!present;
                actor.Activity="Toilet";
                if(!_toiletInside){actor.SeatHeight=0;actor.SeatedFacing=null;}
                if(!_toiletInside&&!_targets.ContainsKey(p.Id)){RouteTo(actor,ToiletDoor);_targets[p.Id]=ToiletDoor;}
                continue;
            }
            if(!present)
            {
                if(_discontinuity||!actor.Visible){HideStaffActor(actor);_departing.Add(p.Id);}
                else if(_departing.Add(p.Id)){RouteTo(actor,StaffExit);_targets[p.Id]=StaffExit;}
                continue;
            }
            if(_departing.Remove(p.Id))_targets.Remove(p.Id);
            if(!actor.Visible){actor.Position=StaffExit;_arriving.Add(p.Id);actor.ClearTrail();_targets.Remove(p.Id);}
            var desk=_layout.Assignments.FirstOrDefault(a=>a.PersonId==p.Id)?.DeskId;
            var chair=_layout.Placements.FirstOrDefault(x=>x.DeskId==desk&&x.DeskId is not null);
            var target=chair is null?new Vector3(_plan.Entrance.X*.25f,0,_plan.Entrance.Z*.25f):new((chair.X+1.5f)*.25f,0,(chair.Z+1.5f)*.25f);
            if(activity?.Kind is OfficeActivityKind.Break or OfficeActivityKind.Waiting)target=new(_plan.Width*.25f+.7f+(p.Id%2)*1.1f,0,Math.Max(1.6f,_plan.Depth*.25f*.48f)+.75f);
            if(activity?.Kind==OfficeActivityKind.Break)
            {
                var facility=_layout.Placements.FirstOrDefault(x=>x.ItemId==activity.FacilityId);
                if(facility is not null)
                {
                    var anchor=OfficeLayoutRules.Transform(facility,OfficeCatalog.Get(_inventory[facility.ItemId].Kind),activity.SeatIndex%2==0?0:5,3);
                    target=new((anchor.X+.5f)*.25f,0,(anchor.Z+.5f)*.25f);
                }
                else target=SharedBreakSeat((activity.SeatIndex??0)%4);
            }
            if(activity?.Kind==OfficeActivityKind.Mentoring&&activity.PartnerId is{} partner)
            {
                var partnerDesk=_layout.Assignments.FirstOrDefault(a=>a.PersonId==partner)?.DeskId;
                var partnerChair=_layout.Placements.FirstOrDefault(x=>x.DeskId==partnerDesk&&x.DeskId is not null);
                if(partnerChair is not null)target=new(partnerChair.X*.25f-.25f,0,(partnerChair.Z+3)*.25f);
            }
            if(_visualBreaks.TryGetValue(p.Id,out var visit)){target=SharedBreakSeat(visit.Seat);actor.Activity="Break";}
            actor.SeatHeight=actor.Activity=="Break"?OfficeArt.BreakSeatHeight:chair is not null?OfficeArt.DeskSeatHeight:0;
            actor.SeatedFacing=chair is not null&&actor.Activity is "Work" or "Idle" or "Promotion" or "Recovery"
                ?-chair.Rotation*Mathf.Pi/2:null;
            if(!_targets.TryGetValue(p.Id,out var old)||old.DistanceTo(target)>.01f)
            {
                RouteTo(actor,target);_targets[p.Id]=target;
                if(Editing||_discontinuity){actor.Visible=true;_arriving.Remove(p.Id);actor.Position=target;actor.Route.Clear();actor.Animate(0,1,0);actor.ClearTrail();}
            }
            if(!actor.Moving&&chair is not null&&actor.Activity is not ("Break" or "Waiting"))actor.Rotation=new(0,-chair.Rotation*Mathf.Pi/2,0);
        }
        SyncCompanion();
    }
    private void RouteTo(OfficeActor actor,Vector3 target)
    {
        actor.Route.Clear();foreach(var point in WalkingRoute(actor.Position,target))actor.Route.Enqueue(point);actor.Destination=target;
    }

    public override void _Process(double delta)
    {
        if(!IsVisibleInTree()||_state is null)return;
        _hint.Visible=ShowNavigationHint;
        UpdateDaylight(delta);
        UpdateDoors(delta);
        UpdateVisualBreaks(delta);
        UpdateToiletVisits(delta);
        foreach(var actor in _actors.Values.ToArray())
        {
            actor.Selected=actor.PersonId==SelectedPerson;
            if(_staffToiletVisit?.Actor==actor)continue;
            var elapsed=delta;
            if(_staffToiletId==actor.PersonId&&!_toiletInside&&Speed>0&&!DoorReady(ToiletDoor))
            {
                var length=0f;var previous=actor.Position;
                foreach(var point in actor.Route){length+=previous.DistanceTo(point);previous=point;}
                elapsed=Math.Min(delta,Math.Max(0,length-.65f)/(Speed*7));
            }
            if(actor.Visible)actor.Animate(elapsed,Editing?0:Speed,Effect);
            if(!Editing&&Speed>0)FinishToiletApproach(actor);
            if(!actor.Moving&&actor.Activity=="Break"&&actor.Position.X>=_plan.Width*.25f)
                actor.Rotation=new(0,actor.Position.X<_plan.Width*.25f+1.2f?-Mathf.Pi/2:Mathf.Pi/2,0);
            if(_departing.Contains(actor.PersonId)&&actor.Visible&&!actor.Moving)
            {
                HideStaffActor(actor);
                if(!_state.People.Any(p=>p.Id==actor.PersonId&&p.Employment?.LocationId==_location))RemoveStaffActor(actor.PersonId);
            }
        }
        AnimateCompanion(delta);
        foreach(var traffic in _ambientTraffic)
            traffic.Update(delta,Editing?0:Speed,Effect,!EndingDay&&!ClosedForNight&&_state.Clock.Hour is >=7 and <22,DoorReady);
        UpdateToiletPrivacy();
    }
    public OfficeViewPreferences CapturePreferences()
    {
        if(_location!=0)_bookmarks[_location]=(_angle,_zoom,_pan);
        return new(_location,SelectedPerson,Effect,_bookmarks.ToDictionary(p=>p.Key,p=>new[]{p.Value.Angle,p.Value.Zoom,p.Value.Pan.X,p.Value.Pan.Z,p.Value.Pan.Y}));
    }
    public void RestorePreferences(OfficeViewPreferences preferences)
    {
        _bookmarks.Clear();
        foreach(var (id,v) in preferences.Cameras??new())
            if(v is {Length:4 or 5}&&v.All(float.IsFinite)&&_state?.Locations.Any(l=>l.Id==id)==true)
                _bookmarks[id]=(v[0],Math.Clamp(v[1],4,40),new(Math.Clamp(v[2],-30,30),v.Length==5?Math.Clamp(v[4],-30,30):0,Math.Clamp(v[3],-30,30)));
        Effect=Math.Clamp(preferences.Effect,0,2);
        if(_state?.People.Any(p=>p.Id==preferences.SelectedPerson)==true)SelectedPerson=preferences.SelectedPerson;
        if(_bookmarks.TryGetValue(_location,out var current)){_angle=current.Angle;_zoom=current.Zoom;_pan=current.Pan;}
        ClearMotion();UpdateCamera();
    }
    public void ClearMotion(){foreach(var a in _actors.Values.Concat(_ambient))a.ClearTrail();}
    public void FocusSelected()
    {
        if(_actors.TryGetValue(SelectedPerson,out var actor))
        {_pan=actor.Position-new Vector3((_plan.Width+12)*.125f,0,_plan.Depth*.125f);UpdateCamera();}
    }
    public void ResetCamera(){_pan=Vector3.Zero;_angle=.7f;_zoom=Math.Max(10,Math.Max(_plan.Width+16,_plan.Depth)*.30f);_zoomStep=0;UpdateCamera();}
    // R3 on a controller steps through three zoom levels (controller support, 2026-10-06).
    private int _zoomStep;
    public void CycleZoom()
    {
        if(_plan is null)return;
        _zoomStep=(_zoomStep+1)%3;
        _zoom=Math.Clamp(Math.Max(10,Math.Max(_plan.Width+16,_plan.Depth)*.30f)*(_zoomStep switch{0=>1f,1=>.62f,_=>.38f}),4,40);UpdateCamera();
    }
    /// <summary>The grid step that best matches a direction on screen, whichever way the camera faces (furniture move buttons).</summary>
    public (int X,int Z) GridStep(Vector2 screen)
    {
        var right=_camera.GlobalBasis.X;var forward=-_camera.GlobalBasis.Z;
        var world=new Vector2(right.X,right.Z).Normalized()*screen.X-new Vector2(forward.X,forward.Z).Normalized()*screen.Y;
        return Math.Abs(world.X)>=Math.Abs(world.Y)?(Math.Sign(world.X),0):(0,Math.Sign(world.Y));
    }
    public void FocusCompanion()
    {
        if(_helper is null)return;
        _zoom=4;_pan=_helper.GlobalPosition-new Vector3((_plan.Width+12)*.125f,0,_plan.Depth*.125f);UpdateCamera();
    }
    public void RotateCamera(float degrees){_angle+=Mathf.DegToRad(degrees);UpdateCamera();}
    // Translate along the camera's screen axes, like dragging a flat image.
    public void PanScreenPixels(Vector2 drag)
    {
        if(_state is null)return;
        var shift=(-_camera.GlobalBasis.X*drag.X+_camera.GlobalBasis.Y*drag.Y)*(_zoom/Math.Max(100,Size.Y));
        // The equivalent ground-plane translation preserves screen-aligned dragging
        // without letting repeated vertical drags lower the camera below the ground.
        var direction=-_camera.GlobalBasis.Z;
        shift-=direction*(shift.Y/direction.Y);
        _pan+=shift;UpdateCamera();
    }
    internal Vector2 ProjectPoint(Vector3 point)=>ToLocal(_camera.UnprojectPosition(point));
    /// <summary>Seated at their own desk and working, not walking, on a break-room visit or on a toilet trip (work sparkles, 2026-10-03).</summary>
    public bool AtDeskWorking(int personId)=>_actors.TryGetValue(personId,out var a)&&a.Visible&&!a.Moving&&a.Activity=="Work"&&
        _staffToiletId!=personId&&!_visualBreaks.ContainsKey(personId)&&Mathf.IsEqualApprox(a.SeatHeight,OfficeArt.DeskSeatHeight);
    /// <summary>A shown person's head in canvas coordinates, or null when they are not on screen here (work sparkles).</summary>
    public Vector2? PersonScreenPoint(int personId)
    {
        if(!_actors.TryGetValue(personId,out var actor)||!actor.Visible||!actor.IsInsideTree())return null;
        var head=actor.GlobalPosition+new Vector3(0,1,0);
        if(_camera.IsPositionBehind(head))return null;
        var point=ToLocal(_camera.UnprojectPosition(head));
        if(!new Rect2(Vector2.Zero,Size).HasPoint(point))return null;
        return GetGlobalTransformWithCanvas()*point;
    }
    private void UpdateCamera()
    {
        if(_plan is null)return;
        var centre=new Vector3((_plan.Width+12)*.125f,0,_plan.Depth*.125f)+_pan;
        _camera.Size=_zoom;_camera.Position=centre+new Vector3(Mathf.Cos(_angle)*20,22,Mathf.Sin(_angle)*20);_camera.LookAt(centre);
        ConstrainNeighborhoodCamera();
        foreach(var (wall,side) in _walls)
        {
            var near=side switch{0=>Mathf.Sin(_angle)<0,1=>Mathf.Cos(_angle)<0,2=>Mathf.Sin(_angle)>0,_=>Mathf.Cos(_angle)>0};wall.Scale=new(1,near?.13f:1,1);
        }
        // Cut away the overhead beam rather than lowering it across the open doorway.
        if(_southDoorLintel is not null)_southDoorLintel.Visible=Mathf.Sin(_angle)<=0;
        UpdateToiletPrivacy();
    }
    private OfficeCell? MouseCell(Vector2 position)
    {
        position=ToViewport(position);var origin=_camera.ProjectRayOrigin(position);var direction=_camera.ProjectRayNormal(position);
        if(Math.Abs(direction.Y)<.001)return null;var point=origin+direction*(-origin.Y/direction.Y);
        return new((int)Math.Floor(point.X*4),(int)Math.Floor(point.Z*4));
    }
    public override void _GuiInput(InputEvent ev)
    {
        if(_state is null)return;
        if(ev is InputEventMouseButton b)
        {
            if(b.ButtonIndex==MouseButton.Right)_rotating=b.Pressed;
            if(b.ButtonIndex==MouseButton.Middle){_panning=b.Pressed;if(b.Pressed)GrabFocus();AcceptEvent();}
            if(b.Pressed&&b.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown){_zoom=Math.Clamp(_zoom*(b.ButtonIndex==MouseButton.WheelUp?.9f:1.1f),4,40);UpdateCamera();AcceptEvent();}
            if(b.ButtonIndex==MouseButton.Left)
            {
                _draggingFurniture=false;
                if(!b.Pressed)return;GrabFocus();
                if(!Editing)
                {
                    if(_helper is not null&&GodotObject.IsInstanceValid(_helper)&&ProjectPoint(_helper.GlobalPosition+new Vector3(0,1,0)).DistanceTo(b.Position)<38/_renderScale)
                    {HelperClicked?.Invoke();AcceptEvent();return;}
                    var hit=_actors.Values.Where(a=>a.Visible).OrderBy(a=>ProjectPoint(a.GlobalPosition+new Vector3(0,1,0)).DistanceTo(b.Position)).FirstOrDefault();
                    if(hit is not null&&ProjectPoint(hit.GlobalPosition+new Vector3(0,1,0)).DistanceTo(b.Position)<28/_renderScale){SelectedPerson=hit.PersonId;PersonClicked?.Invoke(hit.PersonId);AcceptEvent();return;}
                }
                if(MouseCell(b.Position) is{} cell)
                {
                    var hit=_layout.Placements.LastOrDefault(p=>{var f=OfficeCatalog.Get(_inventory[p.ItemId].Kind);var (w,d)=OfficeLayoutRules.Size(f,p.Rotation);return cell.X>=p.X&&cell.Z>=p.Z&&cell.X<p.X+w&&cell.Z<p.Z+d;});
                    if(hit is not null){SelectedFurniture=hit.ItemId;FurnitureClicked?.Invoke(hit.ItemId);_draggingFurniture=Editing;}
                    else GroundClicked?.Invoke(cell);
                }
            }
        }
        if(ev is InputEventMouseMotion m)
        {
            // Characters keep their physical size at any interface size, so clicks and turns are measured in screen pixels.
            if(_rotating){_angle-=m.Relative.X*.009f*_renderScale;UpdateCamera();AcceptEvent();}
            if(_panning){PanScreenPixels(m.Relative);AcceptEvent();}
            if(_draggingFurniture&&Editing&&MouseCell(m.Position)is{} cell)GroundDragged?.Invoke(cell);
        }
        if(ev is InputEventKey{Pressed:true,Keycode:Key.R}&&Editing)RotateRequested?.Invoke();
    }
}

public sealed record OfficeViewPreferences(int Location,int SelectedPerson,int Effect,Dictionary<int,float[]> Cameras);

