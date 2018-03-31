using System;
using System.Threading.Tasks;

namespace SteamTrade.TradeOffer
{
    public interface ITradeOfferStatusPollingService
    {
        Task<TradeOfferState> WaitForStatusChangeAsync(ITradeOfferWebAPI tradeOfferWebApi, string botUsername, string tradeOfferId, TradeOfferState originalState, DateTime timeoutTime);
    }
}