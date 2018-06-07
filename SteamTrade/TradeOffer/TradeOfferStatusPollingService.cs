using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamTrade.Exceptions;

namespace SteamTrade.TradeOffer
{
    public class TradeOfferStatusPollingService : ITradeOfferStatusPollingService
    {
        private static readonly TraceSource trace = new TraceSource(nameof(TradeOfferStatusPollingService));
        private readonly List<(ITradeOfferWebAPI tradeOfferWebAPI, string botUsername, string tradeOfferId, TradeOfferState originalState, TaskCompletionSource<TradeOfferState> tcs)> pollingRequests =
            new List<(ITradeOfferWebAPI tradeOfferWebAPI, string botUsername, string tradeOfferId, TradeOfferState originalState, TaskCompletionSource<TradeOfferState> tcs)>();
        private Task task;
        public virtual Task<TradeOfferState> WaitForStatusChangeAsync(ITradeOfferWebAPI tradeOfferWebApi, string botUsername, string tradeOfferId, TradeOfferState originalState,
            DateTime timeoutTime, CancellationToken cancellationToken)
        {
            if (botUsername == null) throw new ArgumentNullException(nameof(botUsername));
            if (tradeOfferId == null) throw new ArgumentNullException(nameof(tradeOfferId));

            trace.TraceEvent(TraceEventType.Information, 765, "开始刷新报价 " + tradeOfferId + " 的信息，机器人用户名是 " + botUsername);
            var tcs = new TaskCompletionSource<TradeOfferState>();
            var request = (tradeOfferWebApi, botUsername, tradeOfferId, originalState, tcs);
            lock (pollingRequests)
            {
                pollingRequests.Add(request);
            }
            if (task == null || task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Faulted)
            {
                task = Task.Run(PollStatusesAsync);
            }
            var timeoutInMiliseconds = (timeoutTime - DateTime.UtcNow).TotalMilliseconds;
            if (timeoutInMiliseconds < int.MaxValue)
            {
                var timeoutInMilisecondsInt = (int)timeoutInMiliseconds;
                Task.Delay(timeoutInMilisecondsInt, cancellationToken).ContinueWith(t => SetTaskCompletionSourceTimeout(), cancellationToken);
            }
            else
            {
                cancellationToken.Register(SetTaskCompletionSourceTimeout);
            }
            void SetTaskCompletionSourceTimeout()
            {
                lock (pollingRequests)
                {
                    trace.TraceEvent(TraceEventType.Information, 765, "报价 " + tradeOfferId + " 已超时。");
                    pollingRequests.Remove(request);
                }
                tcs.TrySetException(new TradeOfferTimeoutException());
            }
            return tcs.Task;
        }
        private async Task PollStatusesAsync()
        {
            while (true)
            {
                await Task.Delay(TradeOfferStatePollingInterval);
                (ITradeOfferWebAPI tradeOfferWebAPI, string botUsername, string tradeOfferId, TradeOfferState originalState, TaskCompletionSource<TradeOfferState> tcs)[] requests;
                lock (pollingRequests)
                {
                    requests = pollingRequests.ToArray();
                }
                if (requests.Length == 0)
                    return;
                foreach (var requestGroup in requests.GroupBy(r => r.botUsername))
                {
                    try
                    {
                        var firstRequest = requestGroup.First();
                        var api = firstRequest.tradeOfferWebAPI;
                        var offerResponse = api.GetTradeOffers(GetSentOffers, GetReceivedOffers, false, ActiveOnly, HistoricalOnly, "1389106496", "english");
                        foreach (var request in requestGroup)
                        {
                            var offer = offerResponse.AllOffers.FirstOrDefault(o => o.TradeOfferId == request.tradeOfferId);
                            if (offer == null)
                            {
                                if (!string.IsNullOrEmpty(request.tradeOfferId))
                                {
                                    request.tcs.SetException(new TradeException($"机器人账号上找不到 ID 为 {request.tradeOfferId} 的交易报价。"));
                                    lock (pollingRequests)
                                    {
                                        pollingRequests.Remove(request);
                                    }
                                }
                                continue;
                            }
                            if (offer.TradeOfferState == request.originalState) continue;
                            request.tcs.TrySetResult(offer.TradeOfferState);
                            lock (pollingRequests)
                            {
                                pollingRequests.Remove(request);
                            }
                        }
                        HandleLongPoll(offerResponse, api, firstRequest.botUsername);
                    }
                    catch (Exception ex)
                    {
                        trace.TraceEvent(TraceEventType.Error, 766, "处理报价时出错。\r\n" + ex);
                    }
                }
            }
        }
        public bool ActiveOnly { get; set; } = true;
        public bool GetReceivedOffers { get; set; }
        public bool GetSentOffers { get; set; } = true;
        public bool HistoricalOnly { get; set; }
        protected virtual void HandleLongPoll(OffersResponse offerResponse, ITradeOfferWebAPI api, string firstRequestItem2) { }
        public TimeSpan TradeOfferStatePollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    }
}