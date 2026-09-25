using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private Action? _refreshOnlinePanel;
    private void OpenOnline(int seriesId,int bookId){_printingBookId=bookId;Navigate("Sell online",seriesId);}
    private void DoujinOnlinePage()
    {
        var series=ManagedSeries.FirstOrDefault(s=>s.Id==_detailId);
        var books=series?.Volumes.Where(v=>v.IsDoujin&&v.BusinessId==_state.ControlledBusinessId).ToArray()??[];
        Words(_sideContent,"Sell doujin online",26);
        if(series is null||books.Length==0){Words(_sideContent,"Finish a doujin chapter or one-shot first.");return;}
        Words(_sideContent,"Publish a downloadable edition without paying for printing, stock, delivery or setup. The shop takes its fee only when someone buys a copy.",14);
        var choice=new OptionButton();foreach(var book in books)choice.AddItem($"{series.Title} · {GameState.EditionName(book)}",book.Id);
        choice.Select(Math.Max(0,choice.GetItemIndex(_printingBookId)));_sideContent.AddChild(choice);
        var quote=Words(_sideContent,"");var sales=Words(_sideContent,"");Button? publish=null;
        void RefreshOnline()
        {
            var book=books.Single(v=>v.Id==choice.GetSelectedId());_printingBookId=book.Id;
            var listed=_state.DoujinOnlineListed(book.Id);
            var receipts=_state.World.Receipts.Where(r=>r.VolumeId==book.Id&&_state.World.Channels.Any(a=>a.Id==r.AgreementId&&a.Channel==ReleaseChannel.DomesticDigital)).ToArray();
            var legacy=listed&&!_state.World.Channels.Any(a=>a.DirectDoujin&&a.VolumeIds.Contains(book.Id));
            quote.Text=legacy?"This edition uses its existing digital distribution agreement and pricing. Its setup fee and receipts are unchanged.":$"Upfront cost: ¥0\nDownload price: ¥{GameState.DoujinDownloadPrice(book):N0} · printed cover price ¥{book.Price:N0}\nShop fee: 30% per sale · business receives ¥{GameState.DoujinDownloadNet(book):N0} per download, before creator share.";
            sales.Text=$"{(listed?"On sale online":"Not published online yet")}\n{receipts.Sum(r=>r.Units):N0} downloads sold · ¥{receipts.Sum(r=>r.NetYen):N0} earned after shop fees";
            if(publish is not null){publish.Disabled=listed;publish.Text=listed?"Already on sale online":"Publish online · ¥0 upfront";}
        }
        publish=ActionButton(_sideContent,"Publish online · ¥0 upfront",()=>{_state.Apply(new PublishDoujinOnlineCommand(choice.GetSelectedId()));_dirty=true;RefreshOnline();RefreshManagement();});
        publish.ThemeTypeVariation="PrimaryAction";
        Words(_sideContent,"Purchases settle on Mondays. Quality and current genre popularity determine demand; older editions retain a small backlist audience. No purchase means no income. This route does not require a studio internet upgrade.",14);
        ActionButton(_sideContent,"Print physical copies",()=>OpenPrinting(series.Id,choice.GetSelectedId()));
        choice.ItemSelected+=_=>RefreshOnline();_refreshOnlinePanel=RefreshOnline;RefreshOnline();
        ActionPageColumns("PUBLISH A DOWNLOAD",quote,sales);
    }
}
