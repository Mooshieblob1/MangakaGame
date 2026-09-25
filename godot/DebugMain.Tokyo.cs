using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private TokyoMap _tokyoMap=null!;
    private Label _travelSummary=null!;
    private Control BuildTokyoPanel()
    {
        var panel=new VBoxContainer{Name="Tokyo map"};
        panel.AddChild(new Label{Text="Tokyo's 23 wards and selected nearby cities. Schematic map; ward-centre distances and travel times are estimates.",AutowrapMode=TextServer.AutowrapMode.WordSmart});
        var places=new OptionButton();foreach(var district in TokyoProperties.Centres.Keys)places.AddItem(district);
        _tokyoMap=new TokyoMap{SizeFlagsVertical=SizeFlags.ExpandFill};
        places.ItemSelected+=i=>{_tokyoMap.SelectedDistrict=places.GetItemText((int)i);RefreshTokyo();};
        _tokyoMap.DistrictSelected=name=>{places.Select(Array.IndexOf(TokyoProperties.Centres.Keys.ToArray(),name));RefreshTokyo();};
        places.Select(Array.IndexOf(TokyoProperties.Centres.Keys.ToArray(),_tokyoMap.SelectedDistrict));
        panel.AddChild(places);panel.AddChild(_tokyoMap);
        _travelSummary=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart};panel.AddChild(_travelSummary);
        return panel;
    }
    private void RefreshTokyo()
    {
        _tokyoMap.Origin=_state.Locations.Single(l=>l.Id==_state.Protagonist.Employment!.LocationId).District;
        var route=TokyoProperties.Travel(_tokyoMap.Origin,_tokyoMap.SelectedDistrict);
        _travelSummary.Text=$"{_tokyoMap.Origin} → {_tokyoMap.SelectedDistrict}: {route.Km:F1} km estimated • ¥{route.Fare:N0} per person each way • {route.Hours}h each way\n"+
            (route.Fare==0?"Close enough to walk or cycle. Travel still takes time.":"Transit uses the researched 1995 Toei fare bands as a game-wide baseline.")+"\n"+
            string.Join("\n",TokyoProperties.All.Where(p=>p.District==_tokyoMap.SelectedDistrict).Select(p=>$"Available property: tier {p.Tier}, {p.Seats} desks, ¥{p.Rent:N0}/month. Choose it in Studio management."));
        _tokyoMap.QueueRedraw();
    }
}
