using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private void ContinueOneShotFromSidebar(int seriesId)
    {
        _state.Apply(new ContinueOneShotCommand(seriesId));_dirty=true;
        Navigate("Series details",seriesId);RefreshManagement();
    }
    private OptionButton MakeGenreOption(string selected="adventure")
    {
        var choice=new OptionButton{Name="GenreChoice",TooltipText="Genre",CustomMinimumSize=new(120,0)};
        foreach(var genre in _state.TrendCatalog.Genres)choice.AddItem(genre);
        SelectGenre(choice,selected);
        return choice;
    }
    private static void SelectGenre(OptionButton choice,string genre)
    {
        for(var index=0;index<choice.ItemCount;index++)
            if(choice.GetItemText(index)==genre){choice.Select(index);return;}
    }
    private static Chapter? SidebarChapter(Series series)=>series.Publishing==PublishingStatus.Pitching
        ?series.Chapters.LastOrDefault(c=>c.IsOneShot&&!c.PitchResolved)
        :series.Chapters.FirstOrDefault(c=>!c.IsFinished)??series.Chapters.LastOrDefault();

    private string PublishingStatusText(Series series)
    {
        string Magazine(string id)=>_state.PublisherCatalog.Get(id).Name;
        if(series.PendingOffer is {} offer)
            return $"Pitch accepted · your decision needed\n{Magazine(offer.MagazineId)} · ¥{offer.FeePerPage:N0}/page\nAccept or decline by {offer.ExpiresAt:d MMM yyyy · HH:mm}.";
        if(series.Publishing==PublishingStatus.Pitching)
        {
            var sample=series.Chapters.LastOrDefault(c=>c.IsOneShot&&!c.PitchResolved);
            var stage=sample?.Status==ChapterStatus.Complete?"Awaiting magazine decision":sample?.Editor switch
            {
                EditorStatus.AwaitingReview=>"Editor reviewing storyboard",
                EditorStatus.RedoRequested=>"Storyboard revision requested",
                _=>"Preparing 31-page pitch sample",
            };
            return $"Being pitched · {stage}"+(sample?.PitchMagazineId is {} magazine?$"\n{Magazine(magazine)}":"")+
                "\nExisting doujin chapters are on hold until the pitch is resolved.";
        }
        if(series.Contract is {} contract)
            return $"Serialization accepted · {Magazine(contract.MagazineId)}\n"+
                (series.ChaptersPublished==0?$"First issue: {contract.FirstIssueClose:d MMM yyyy}":$"{series.ChaptersPublished} chapters published")+
                $" · ¥{contract.FeePerPage:N0}/page"+(_state.ChaptersReadyAhead(series) is var ahead?$"\nChapters ready ahead: {ahead.Ready} of {ahead.Target}":"");
        if(series.StandaloneDoujin)return "Self-published one-shot · no magazine contract";
        var outcome=_state.Events.LastOrDefault(e=>e.SeriesId==series.Id&&e.Type is
            EventType.PitchRejected or EventType.OfferDeclined or EventType.OfferExpired or EventType.SeriesCancelled or EventType.SeriesWithdrawn);
        var previous=outcome?.Type switch
        {
            EventType.PitchRejected=>"Pitch rejected",
            EventType.OfferDeclined=>"Offer declined",
            EventType.OfferExpired=>"Offer expired",
            EventType.SeriesCancelled=>"Serialization cancelled",
            EventType.SeriesWithdrawn=>"Withdrawn from magazine",
            _=>null,
        };
        return previous is null?"Self-published · no active pitch":$"{previous} · {outcome!.Time:d MMM}\nSelf-published · no active pitch";
    }
    private void PublishingStatusCard(Control parent,Series series)
    {
        var card=StudioCard(parent,"PUBLICATION STATUS");
        LiveWords(card,()=>PublishingStatusText(series),16);
    }
}
