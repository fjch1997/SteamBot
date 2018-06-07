using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SteamTrade.TradeOffer
{
    public class LongRunningTradeOfferPollingService : TradeOfferStatusPollingService
    {
        private readonly HashSet<long> tradeOfferIds = new HashSet<long>();
        private readonly Dictionary<string, CancellationTokenSource> accounts = new Dictionary<string, CancellationTokenSource>();
        public LongRunningTradeOfferPollingService()
        {
            GetReceivedOffers = true;
        }
        public void AddSteamAccount(string username, TradeOfferWebAPI tradeOfferWebApi)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            WaitForStatusChangeAsync(tradeOfferWebApi, username, string.Empty, TradeOfferState.TradeOfferStateInvalid, DateTime.MaxValue, cancellationTokenSource.Token);
            accounts[username] = cancellationTokenSource;
        }
        public void RemoveSteamAccount(string username)
        {
            accounts[username].Cancel();
            accounts.Remove(username);
        }
        protected override void HandleLongPoll(OffersResponse offerResponse, ITradeOfferWebAPI api, string botUsername)
        {
            foreach (var offer in offerResponse.AllOffers.Where(o => !tradeOfferIds.Contains(long.Parse(o.TradeOfferId))))
            {
                NewOfferReceived?.Invoke(this, new NewOfferReceivedEventArgs { Offer = offer, OffersResponse = offerResponse, TradeOfferWebApi = api, BotUsername = botUsername });
                tradeOfferIds.Add(long.Parse(offer.TradeOfferId));
            }
            base.HandleLongPoll(offerResponse, api, botUsername);
        }
        public event EventHandler<NewOfferReceivedEventArgs> NewOfferReceived;
    }
    public class NewOfferReceivedEventArgs
    {
        public Offer Offer { get; set; }
        public OffersResponse OffersResponse { get; set; }
        public ITradeOfferWebAPI TradeOfferWebApi { get; set; }
        public string BotUsername { get; set; }
    }
}