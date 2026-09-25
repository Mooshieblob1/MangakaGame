using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private Texture2D? ShowcaseTexture(int series,string genre,int slot)
    {
        if(_presentation.Artwork.TryGetValue($"{series}:{slot}",out var hash))
        {
            var path=_careers.AssetPath(_careerId,hash);
            if(System.IO.File.Exists(path)){var image=Image.LoadFromFile(path);if(image is not null&&!image.IsEmpty())return ImageTexture.CreateFromImage(image);}
        }
        var slug=genre.ToLowerInvariant().Replace(' ','-');var asset=$"res://Assets/Manga/{slug}.png";
        if(!ResourceLoader.Exists(asset))asset="res://Assets/Manga/other.png";
        if(!ResourceLoader.Exists(asset))return null;
        var atlas=GD.Load<Texture2D>(asset);var cell=slot==0?Math.Abs(series%2):slot+1;
        return new AtlasTexture{Atlas=atlas,Region=new Rect2(cell%2*atlas.GetWidth()/2f,cell/2*atlas.GetHeight()/3f,atlas.GetWidth()/2f,atlas.GetHeight()/3f)};
    }
    private void BuildShowcase()
    {
        var series=ManagedSeries.FirstOrDefault(s=>s.Id==_detailId);if(series is null){Words(_sideContent,"This series is outside your current management scope.");return;}
        Words(_sideContent,series.Title,30);Words(_sideContent,$"{series.Genre} · {_state.FindPerson(series.LeadPersonId)?.Name}");
        for(var slot=0;slot<4;slot++)
        {
            var n=slot;var texture=ShowcaseTexture(series.Id,series.Genre,slot);
            var image=new TextureRect{Texture=texture,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,CustomMinimumSize=new(0,slot==0?350:260),SizeFlagsHorizontal=SizeFlags.ExpandFill};_sideContent.AddChild(image);
            Words(_sideContent,slot==0?series.Title:$"Representative panel {slot}",slot==0?24:14);
            var row=new HBoxContainer();_sideContent.AddChild(row);
            ActionButton(row,slot==0?"Import cover":"Import panel",()=>ChooseFile("Choose original artwork",FileDialog.FileModeEnum.OpenFile,["*.png, *.jpg, *.jpeg ; Images"],path=>PreviewArtwork(path,series.Id,n)));
            ActionButton(row,"Use bundled art",()=>{_presentation.Artwork.Remove($"{series.Id}:{n}");BuildManagementPage();});
        }
        Words(_sideContent,"Synopsis",22);
        var synopsis=new TextEdit{Text=_presentation.Synopses.GetValueOrDefault(series.Id,$"An original {series.Genre} story about finding a path forward, one unexpected encounter at a time."),CustomMinimumSize=new(0,140),WrapMode=TextEdit.LineWrappingMode.Boundary};_sideContent.AddChild(synopsis);
        ActionButton(_sideContent,"Keep synopsis",()=>{_presentation.Synopses[series.Id]=synopsis.Text.Length>3000?synopsis.Text[..3000]:synopsis.Text;Notify("Synopsis kept for this career.");});
        Words(_sideContent,"Showcase artwork illustrates a premise. It does not reconstruct every simulated chapter.",13);
        Words(_sideContent,"Release history",22);
        foreach(var v in series.Volumes)Words(_sideContent,$"Volume {v.Number} · chapters {v.FirstChapter}–{v.LastChapter}\n{(v.ReleasedAt is {} at?at.ToString("d MMM yyyy"):"Prepared, not released")} · {v.CopiesSold:N0} physical copies");
    }
    private void PreviewArtwork(string path,int series,int slot)
    {
        if(new System.IO.FileInfo(path).Length>20*1024*1024)throw new System.IO.InvalidDataException("Choose an image smaller than 20 MiB.");
        var bytes=System.IO.File.ReadAllBytes(path);
        // Read image dimensions before asking Godot to allocate its decoded pixel buffer.
        var dimensions=ImageDimensions.Read(bytes);
        if(dimensions.Width>4096||dimensions.Height>4096)throw new System.IO.InvalidDataException("Choose an image at most 4096 pixels on either side.");
        var image=Image.LoadFromFile(path);if(image is null||image.IsEmpty())throw new System.IO.InvalidDataException("The image could not be read.");
        var window=new ConfirmationDialog{Title="Artwork preview",Size=new(680,640),OkButtonText="Use artwork"};AddChild(window);
        var column=new VBoxContainer();window.AddChild(column);var crop=new CheckBox{Text=slot==0?"Center-crop to portrait cover (2:3)":"Center-crop to square panel"};column.AddChild(crop);
        var prepared=image;
        var picture=new TextureRect{Texture=ImageTexture.CreateFromImage(image),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,CustomMinimumSize=new(620,480)};column.AddChild(picture);
        crop.Toggled+=enabled=>{var ratio=slot==0?2f/3:1;var width=Math.Min(image.GetWidth(),(int)(image.GetHeight()*ratio));var height=Math.Min(image.GetHeight(),(int)(image.GetWidth()/ratio));prepared=enabled?image.GetRegion(new Rect2I((image.GetWidth()-width)/2,(image.GetHeight()-height)/2,width,height)):image;picture.Texture=ImageTexture.CreateFromImage(prepared);};
        window.Confirmed+=()=>{try{_presentation.Artwork[$"{series}:{slot}"]=_careers.ImportAsset(_careerId,prepared.SavePngToBuffer());BuildManagementPage();}catch(System.IO.IOException ex){Notify(ex.Message);}finally{window.QueueFree();}};
        window.Canceled+=()=>window.QueueFree();window.PopupCentered();
    }
}
