using System;

namespace SteamTrade.TradeOffer
{
    public class TradeOfferTimeoutException : TimeoutException
    {
        public TradeOfferTimeoutException() : base("报价已超时，请重新发送。") { }
    }
}
