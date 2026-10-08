using System;
using Godot;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private sealed class CashFeedback
    {
        private CashAccount? _account;
        private int _entries;
        private double _gainAge=4,_lossAge=4;
        public decimal Gains { get; private set; }
        public decimal Losses { get; private set; }
        public string GainReason { get; private set; }="";
        public string LossReason { get; private set; }="";
        public float GainOpacity=>(float)Math.Clamp(4-_gainAge,0,1);
        public float LossOpacity=>(float)Math.Clamp(4-_lossAge,0,1);
        private long _balance;
        public void Reset(){_account=null;Gains=Losses=0;_gainAge=_lossAge=4;GainReason=LossReason="";_balance=0;}
        public void Observe(CashAccount account)
        {
            // Loading a career, changing accounts or rewinding never produces fake income.
            if(!ReferenceEquals(_account,account)||account.Entries.Count<_entries)
            {Reset();_account=account;_entries=account.Entries.Count;_balance=account.Balance;return;}
            long added=0;
            for(;_entries<account.Entries.Count;_entries++)
            {
                var entry=account.Entries[_entries];added+=entry.Amount;
                if(entry.Amount>0){Gains+=entry.Amount;_gainAge=0;GainReason=entry.Reason;}
                else if(entry.Amount<0){Losses-=(decimal)entry.Amount;_lossAge=0;LossReason=entry.Reason;}
            }
            // Streaming sales add to today's line instead of adding one (spec 2026-10-03).
            var merged=account.Balance-_balance-added;
            if(merged>0){Gains+=merged;_gainAge=0;GainReason="sales";}
            else if(merged<0){Losses-=merged;_lossAge=0;LossReason="adjustment";}
            _balance=account.Balance;
        }
        public void Advance(double seconds)
        {
            _gainAge+=Math.Max(0,seconds);_lossAge+=Math.Max(0,seconds);
            if(_gainAge>=4)Gains=0;
            if(_lossAge>=4)Losses=0;
        }
    }
    private sealed record MoneyBadge(Label Caption,Label Total,Label Gain,Label Loss,CashFeedback Feedback);
    private MoneyBadge? _personalMoneyBadge,_productionMoneyBadge;
    private GameState? _moneyState;
    private int _moneyBusiness,_moneyPerson;
    private DateTime _moneyAt;
    private bool? _moneyDark;

    private void BuildMoneyHeader(Control parent)
    {
        var row=new HBoxContainer();row.AddThemeConstantOverride("separation",12);parent.AddChild(row);
        MoneyBadge Badge(string name)
        {
            var box=new VBoxContainer{Name=name,CustomMinimumSize=new(180,0)};row.AddChild(box);
            box.AddThemeConstantOverride("separation",0);
            var balance=new HBoxContainer();balance.AddThemeConstantOverride("separation",8);box.AddChild(balance);
            var caption=Words(balance,"",12);var total=Words(balance,"",18);
            caption.AutowrapMode=total.AutowrapMode=TextServer.AutowrapMode.Off;
            caption.SizeFlagsVertical=total.SizeFlagsVertical=SizeFlags.ShrinkCenter;
            caption.ThemeTypeVariation="QuietLabel";total.HorizontalAlignment=HorizontalAlignment.Right;
            var changes=new HBoxContainer{Alignment=BoxContainer.AlignmentMode.End};changes.AddThemeConstantOverride("separation",6);box.AddChild(changes);
            var gain=Words(changes,"",12);var loss=Words(changes,"",12);
            gain.AutowrapMode=loss.AutowrapMode=TextServer.AutowrapMode.Off;
            gain.SizeFlagsHorizontal=loss.SizeFlagsHorizontal=SizeFlags.ShrinkEnd;
            // Keep one slim line below the balance so live feedback never changes bar height.
            gain.CustomMinimumSize=loss.CustomMinimumSize=new(64,16);
            return new(caption,total,gain,loss,new CashFeedback());
        }
        _personalMoneyBadge=Badge("PersonalMoney");_productionMoneyBadge=Badge("ProductionMoney");
        void LinkAccount(Label total,bool personal)
        {
            total.FocusMode=FocusModeEnum.All;total.MouseFilter=MouseFilterEnum.Stop;total.MouseDefaultCursorShape=CursorShape.PointingHand;
            total.FocusEntered+=()=>total.AddThemeColorOverride("font_color",Accent);
            total.FocusExited+=()=>total.RemoveThemeColorOverride("font_color");
            total.GuiInput+=input=>
            {
                if(input is InputEventMouseButton{Pressed:true,ButtonIndex:MouseButton.Left}||input is InputEventKey{Pressed:true,Echo:false,Keycode:Key.Enter or Key.KpEnter}||input is InputEventJoypadButton{Pressed:true,ButtonIndex:JoyButton.A})
                {
                    if(OfficeEditing){Notify("Apply or discard furniture changes before opening finances.");return;}
                    _personalAccount=personal;Navigate("Finances");total.AcceptEvent();
                }
            };
        }
        LinkAccount(_personalMoneyBadge.Total,true);LinkAccount(_productionMoneyBadge.Total,false);
    }

    private string ProductionFundsCaption=>_state.Control==ControlMode.EmployedLead?"Employer funds":
        _state.ControlledBusiness.Incorporated?"Business funds":"Doujin budget";

    private void RefreshMoneyHeader()
    {
        if(_personalMoneyBadge is null||_productionMoneyBadge is null)return;
        if(!ReferenceEquals(_moneyState,_state)||_moneyBusiness!=_state.ControlledBusinessId||
            _moneyPerson!=_state.ProtagonistPersonId||_state.Clock.Now<_moneyAt)
        {_personalMoneyBadge.Feedback.Reset();_productionMoneyBadge.Feedback.Reset();}
        _moneyState=_state;_moneyBusiness=_state.ControlledBusinessId;_moneyPerson=_state.ProtagonistPersonId;_moneyAt=_state.Clock.Now;
        _personalMoneyBadge.Caption.Text="Personal savings";
        _personalMoneyBadge.Total.Text=$"¥{_state.PersonalMoney:N0}";
        _personalMoneyBadge.Total.TooltipText="Open personal finances. Your mangaka's savings are separate from the production budget.";
        _productionMoneyBadge.Caption.Text=ProductionFundsCaption;
        _productionMoneyBadge.Total.Text=$"¥{_state.Money:N0}";
        _productionMoneyBadge.Total.TooltipText=$"Open finances · {ProductionFundsCaption} available to spend: ¥{_state.AvailableBusinessCash:N0}\n"+
            (_state.Control==ControlMode.EmployedLead?"Your employer's account, not your personal savings. Your role may limit spending.":
             _state.ControlledBusiness.Incorporated?"The incorporated business's account.":"Money set aside for printing, equipment and other production costs; separate from personal savings.");
        _personalMoneyBadge.Feedback.Observe(_state.Protagonist.PersonalAccount);
        _productionMoneyBadge.Feedback.Observe(_state.ControlledBusiness.Account);
        PaintMoneyFeedback();
    }

    private void TickMoneyFeedback(double delta)
    {
        // UI seconds, not simulation hours: a transaction stays readable at 8x and while paused.
        _personalMoneyBadge?.Feedback.Advance(delta);_productionMoneyBadge?.Feedback.Advance(delta);
        PaintMoneyFeedback();
    }
    private void PaintMoneyFeedback()
    {
        var updateColors=_moneyDark!=_darkMode;_moneyDark=_darkMode;
        void Paint(MoneyBadge? badge)
        {
            if(badge is null)return;
            var feedback=badge.Feedback;
            badge.Gain.Text=feedback.Gains>0?$"+¥{feedback.Gains:N0}":"";
            badge.Loss.Text=feedback.Losses>0?$"−¥{feedback.Losses:N0}":"";
            badge.Gain.TooltipText=feedback.Gains>0?$"Recent money received. Latest: {feedback.GainReason}":"";
            badge.Loss.TooltipText=feedback.Losses>0?$"Recent money spent. Latest: {feedback.LossReason}":"";
            if(updateColors)
            {
                badge.Gain.AddThemeColorOverride("font_color",GainColour);
                badge.Loss.AddThemeColorOverride("font_color",LossColour);
            }
            badge.Gain.Modulate=new(1,1,1,_presentation.ReducedUiMotion?1:feedback.GainOpacity);
            badge.Loss.Modulate=new(1,1,1,_presentation.ReducedUiMotion?1:feedback.LossOpacity);
        }
        Paint(_personalMoneyBadge);Paint(_productionMoneyBadge);
    }
}
