using System;
using System.Linq;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private Action? _refreshConvention;
    private void NewOngoingPage()
    {
        Words(_sideContent,"Ongoing self-published series",26);
        Words(_sideContent,"Each finished chapter becomes a small numbered issue you can print immediately. Every five chapters also form an optional collected book. The story keeps going until you pause or end it.");
        var title=new LineEdit{PlaceholderText="Series title",MaxLength=120,Name="OngoingTitle"};_sideContent.AddChild(title);
        Words(_sideContent,"Genre",14);var genre=MakeGenreOption();_sideContent.AddChild(genre);
        Words(_sideContent,"Pages per issue");var pages=new SpinBox{MinValue=8,MaxValue=64,Step=4,Value=16};_sideContent.AddChild(pages);
        Words(_sideContent,"Personal production target · no missed-deadline penalties");var cadence=MakeCadenceOption();cadence.Select(cadence.GetItemIndex((int)Cadence.Monthly));_sideContent.AddChild(cadence);
        ActionButton(_sideContent,"Create ongoing series",()=>
        {
            _state.Apply(new CreateSeriesCommand(title.Text,genre.GetItemText(genre.Selected),(Cadence)cadence.GetSelectedId(),(int)pages.Value,true));
            var series=_state.Series.Last();_presentation.Guidance.Project=series.Id;_dirty=true;ScanEvents();Navigate("Series details",series.Id);
        }).ThemeTypeVariation="PrimaryAction";
        Words(_sideContent,"A magazine pitch is a separate 31-page sample. You do not need a collected book first. Unfinished issues are kept on hold while the sample is prepared.",14);
    }
    private static double ChapterPercent(Chapter chapter)=>ChapterProgressBar.PercentComplete(chapter);
    private void ChapterProgress(Control parent,Chapter chapter)
    {
        var bar=new ChapterProgressBar{CustomMinimumSize=new(0,26),SizeFlagsHorizontal=SizeFlags.ExpandFill};parent.AddChild(bar);
        bar.SetChapter(chapter);_pageLiveValues.Add(()=>bar.SetChapter(chapter));
        ChapterStageLegend(parent);
    }
    private HFlowContainer ChapterStageLegend(Control parent)
    {
        var legend=new HFlowContainer{Name="ChapterStageLegend"};parent.AddChild(legend);
        foreach(var stage in ChapterProgressBar.Stages)
        {
            var item=new HBoxContainer();item.AddThemeConstantOverride("separation",4);legend.AddChild(item);
            item.AddChild(new ColorRect{Color=stage.Color,CustomMinimumSize=new(8,8),SizeFlagsVertical=SizeFlags.ShrinkCenter,MouseFilter=MouseFilterEnum.Ignore});
            var caption=QuietWords(item,stage.Name,11);
            caption.AutowrapMode=TextServer.AutowrapMode.Off;
            caption.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;
        }
        return legend;
    }
    private string ProductionTarget(Chapter chapter)=>_state.HasPublisherDeadline(chapter)?$"Publisher deadline: {chapter.DueDate:d MMM}":$"Personal target: {chapter.DueDate:d MMM} · no penalty";
    private void OutsideWorkCard(Control? parent=null)
    {
        var card=StudioCard(parent??_sideContent,"Part-time job","Personal income while away from the studio");
        if(_state.Control!=ControlMode.OwnerDirector){Words(card,"Your studio employer provides your salary. Outside shifts are suspended while employed as a lead.");return;}
        Words(card,$"¥{_state.OutsideHourlyPay:N0}/hour · 12 hours/week. Paid into personal savings as shifts are worked. Studio production stops during those hours.",14);
        var choice=new OptionButton();choice.AddItem("No outside job · available for studio work",0);choice.AddItem("Mon / Wed / Fri · 12:00–16:00",1);choice.AddItem("Mon / Wed / Fri · 18:00–22:00",2);choice.Select((int)_state.Protagonist.OutsideJob);card.AddChild(choice);
        ActionButton(card,"Apply part-time schedule",()=>ProgressionAction(new SetOutsideJobCommand((OutsideJob)choice.Selected)));
        Words(card,"Hired assistants earn their studio wages. Your outside earnings are personal money; contribute savings below when the business needs funding.",14);
        var amount=new SpinBox{MinValue=0,MaxValue=Math.Max(0,_state.PersonalMoney),Step=1000,Value=Math.Min(10000,_state.PersonalMoney)};card.AddChild(amount);
        Words(card,$"Contribution to {ProductionFundsCaption.ToLowerInvariant()} · yen",14);card.MoveChild(card.GetChild(card.GetChildCount()-1),amount.GetIndex());
        ActionButton(card,"Contribute personal savings",()=>ProgressionAction(new ContributeFundsCommand((long)amount.Value)));
    }
    private void ConventionPage()
    {
        var selectors=new HFlowContainer();_sideContent.AddChild(selectors);
        VBoxContainer Field(string label){var box=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new(190,0)};selectors.AddChild(box);Words(box,label,14);return box;}
        var titleField=Field("Title to promote");var personField=Field("Attendee");var eventField=Field("Event");
        var titles=new OptionButton{FitToLongestItem=false,ClipText=true};foreach(var series in ManagedSeries)titles.AddItem(series.Title,series.Id);titleField.AddChild(titles);
        titles.Select(Math.Max(0,titles.GetItemIndex(_detailId)));
        var people=new OptionButton{FitToLongestItem=false,ClipText=true};foreach(var person in ManagedPeople.Where(p=>p.Employment!.StartsAt<=_state.Clock.Now&&p.Employment.NoticeEndsAt is null))people.AddItem(person.Name,person.Id);personField.AddChild(people);
        var events=new OptionButton();foreach(var name in new[]{"Free neighbourhood event","Regional event","Summer / winter convention"})events.AddItem(name);eventField.AddChild(events);
        var reserve=new SpinBox{Name="ConventionReservedCopies",MinValue=0,MaxValue=100000,Step=1};Field("Reserve copies").AddChild(reserve);
        var details=Words(_sideContent,"",14);Button? book=null;
        void Quote()
        {
            if(titles.ItemCount==0||people.ItemCount==0){details.Text="Create a title and have an available attendee first.";if(book is not null)book.Disabled=true;return;}
            var person=_state.FindPerson(people.GetSelectedId())!;var location=_state.Locations.Single(l=>l.Id==person.Employment!.LocationId);
            var scale=events.Selected;var date=_state.NextConvention(scale);var district=scale==0?location.District:scale==1?"Toshima":"Ariake";
            var route=TokyoProperties.Travel(location.District,district);var fee=_state.ConventionBoothFee(scale);var travel=route.Fare*2*(scale==2?2:1);
            var series=_state.FindSeries(titles.GetSelectedId())!;var stock=series.Volumes.Where(v=>v.IsDoujin&&v.BusinessId==_state.ControlledBusinessId).Sum(v=>_state.Stock(v.Id));
            var available=_state.ConventionReservable(series.Id,date);
            var reason=reserve.Value>available?$"Only {available:N0} copies can be reserved for this date.":date>_state.Clock.Now.Date.AddDays(28)?$"Booking opens {date.AddDays(-28):d MMM}.":_state.Bookings.Any(b=>!b.Cancelled&&!b.Settled&&b.BusinessId==_state.ControlledBusinessId&&b.Date==date)?"You already have a booth at this event.":fee+travel>_state.AvailableBusinessCash?$"Not enough available {ProductionFundsCaption.ToLowerInvariant()}.":"";
            details.Text=$"{date:ddd d MMM yyyy} · {district}\n{(fee==0&&scale>0?"Booth free (Doujin Days reward)":$"Booth ¥{fee:N0}")} + travel ¥{travel:N0} = ¥{fee+travel:N0}\n{stock:N0} copies in stock · {available:N0} can be reserved\nReserve for this booking: {reserve.Value:N0}"+(reason.Length>0?"\n"+reason:"");
            if(book is not null){book.Disabled=reason.Length>0;book.TooltipText=reason;}
        }
        reserve.ValueChanged+=_=>Quote();titles.ItemSelected+=_=>Quote();people.ItemSelected+=_=>Quote();events.ItemSelected+=_=>Quote();
        book=ActionButton(_sideContent,"Confirm convention booking",()=>ProgressionAction(new StudioActionCommand(StudioAction.BookConvention,people.GetSelectedId(),Amount:titles.GetSelectedId(),Value:events.Selected,ReservedCopies:(int)reserve.Value)));book.ThemeTypeVariation="PrimaryAction";
        _refreshConvention=Quote;Quote();
        ActionPageColumns("BOOK A CONVENTION",details,book);
        var help=Disclosure(_sideContent,"How convention sales work");
        QuietWords(help,"Attendance is automatic; the attendee cannot draw while away. Reservation availability includes print deliveries due before the event. Reserved copies cannot sell locally; unsold copies return after the event. Nearby events have free walking/cycling travel. Normal wages apply.");
        foreach(var booking in _state.Bookings.Where(b=>b.BusinessId==_state.ControlledBusinessId).OrderByDescending(b=>b.Date).Take(12))
        {
            var card=Card($"{booking.Date:d MMM} · {booking.District}",booking.Cancelled?"Cancelled":booking.Settled?$"Completed · {booking.CopiesSold:N0} copies sold":"Booked · attendance is automatic");
            Words(card,$"{_state.FindPerson(booking.PersonId)?.Name} · {(booking.SeriesId is {} id?_state.FindSeries(id)?.Title:"All doujin stock")}",14);
            if(!booking.Settled&&!booking.Cancelled)
            {
                Words(card,$"{booking.ReservedStock.Values.Sum():N0} copies reserved. Unsold copies return to regular stock after the event.",14);
                if(_state.Clock.Now<booking.Date.AddHours(11-booking.TravelHours))
                {
                    var quantity=new SpinBox{MinValue=0,MaxValue=100000,Step=1,Value=booking.ReservedStock.Values.Sum()};card.AddChild(quantity);
                    ActionButton(card,"Update reserved copies",()=>ProgressionAction(new ReserveConventionStockCommand(booking.Id,(int)quantity.Value)));
                }
                ActionButton(card,"Cancel booking",()=>ProgressionAction(new StudioActionCommand(StudioAction.CancelConvention,booking.Id)));
            }
        }
    }
}
