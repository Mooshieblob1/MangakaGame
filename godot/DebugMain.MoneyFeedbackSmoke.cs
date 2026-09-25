using System;
using MangakaSim;

namespace MangakaGame;

public partial class DebugMain
{
    private void CheckMoneyFeedback()
    {
        var account=new CashAccount{OpeningBalance=100,Balance=100};var feedback=new CashFeedback();
        feedback.Observe(account);
        Check(feedback.Gains==0&&feedback.Losses==0,"Opening money is not shown as new income");
        account.Entries.Add(new(_state.Clock.Now,50,"sale",null));
        account.Entries.Add(new(_state.Clock.Now,-50,"printing",null));
        feedback.Observe(account);
        Check(feedback.Gains==50&&feedback.Losses==50,"Income and spending remain visible even when their net change is zero");
        feedback.Observe(account);
        Check(feedback.Gains==50&&feedback.Losses==50,"Refreshing does not repeat transactions");
        feedback.Advance(3.5);
        Check(Math.Abs(feedback.GainOpacity-.5f)<.001,"Money feedback fades on real elapsed time");
        account.Balance+=20;account.Entries.Add(new(_state.Clock.Now,20,"another sale",null));feedback.Observe(account);
        feedback.Advance(.6);
        Check(feedback.Gains==70&&feedback.Losses==0,"Fresh income extends its own display without retaining old expenses");
        feedback.Observe(new CashAccount{OpeningBalance=900,Balance=900});
        Check(feedback.Gains==0&&feedback.Losses==0,"Changing accounts clears old feedback without inventing gains");

        var personal=_personalMoneyBadge??throw new InvalidOperationException("Personal money badge is missing.");
        var production=_productionMoneyBadge??throw new InvalidOperationException("Production money badge is missing.");
        var original=_state;
        try
        {
            _state=GameState.NewGame(91);RefreshMoneyHeader();
            Check(production.Caption.Text=="Doujin budget"&&personal.Caption.Text=="Personal savings",
                "Unincorporated HUD distinguishes personal savings from the doujin budget");
            _state.Apply(new ContributeFundsCommand(10000));RefreshMoneyHeader();
            Check(personal.Loss.Text=="−¥10,000"&&production.Gain.Text=="+¥10,000",
                "A contribution shows a personal expense and production gain next to the correct balances");
            Check(personal.Total.Text==$"¥{_state.PersonalMoney:N0}"&&production.Total.Text==$"¥{_state.Money:N0}",
                "Money totals refresh with the transaction feedback");
            _state.ControlledBusiness.Incorporated=true;RefreshMoneyHeader();
            Check(production.Caption.Text=="Business funds","Incorporation changes the account caption");
            _state.Control=ControlMode.EmployedLead;RefreshMoneyHeader();
            Check(production.Caption.Text=="Employer funds","An employed mangaka does not mistake the employer account for personal money");
            TickMoneyFeedback(4.1);
            Check(personal.Loss.Text==""&&production.Gain.Text=="","Feedback clears without needing another simulation tick");
            _state=GameState.FromJson(original.ToJson());RefreshMoneyHeader();
            Check(personal.Feedback.Gains==0&&production.Feedback.Gains==0,"Loading a save establishes a fresh money baseline");
        }
        finally{_state=original;RefreshMoneyHeader();}
    }
}

