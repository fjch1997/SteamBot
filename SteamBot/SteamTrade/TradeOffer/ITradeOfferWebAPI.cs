namespace SteamTrade.TradeOffer
{
    public interface ITradeOfferWebAPI
    {
        OffersResponse GetActiveTradeOffers(bool getSentOffers, bool getReceivedOffers, bool getDescriptions, string language = "en_us");
        OffersResponse GetAllTradeOffers(string timeHistoricalCutoff = "1389106496", string language = "en_us");
        TradeOfferState GetOfferState(string tradeofferid);
        OfferResponse GetTradeOffer(string tradeofferid);
        OffersResponse GetTradeOffers(bool getSentOffers, bool getReceivedOffers, bool getDescriptions, bool activeOnly, bool historicalOnly, string timeHistoricalCutoff = "1389106496", string language = "en_us");
        TradeOffersSummary GetTradeOffersSummary(uint timeLastVisit);
    }
}