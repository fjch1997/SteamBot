using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public Task<TradeOfferState> WaitForStatusChangeAsync(ITradeOfferWebAPI tradeOfferWebApi, string botUsername, string tradeOfferId, TradeOfferState originalState,
            DateTime timeoutTime)
        {
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
            Task.Delay(timeoutTime - DateTime.UtcNow).ContinueWith(t =>
            {
                lock (pollingRequests)
                {
                    trace.TraceEvent(TraceEventType.Information, 765, "报价 " + tradeOfferId + " 已超时。");
                    pollingRequests.Remove(request);
                }
                return tcs.TrySetException(new TradeOfferTimeoutException());
            });
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
                        var api = requestGroup.First().tradeOfferWebAPI;
                        var offerResponse = api.GetTradeOffers(true, false, true, false, false, "1389106496", "english");
                        foreach (var request in requestGroup)
                        {
                            Offer offer = (offerResponse.TradeOffersSent.FirstOrDefault(o => o.TradeOfferId == request.tradeOfferId) ??
                                           throw new TradeException($"机器人账号上找不到 ID 为 {request.tradeOfferId} 的交易报价。"));
                            if (offer.TradeOfferState != request.originalState)
                            {
                                request.tcs.TrySetResult(offer.TradeOfferState);
                                lock (pollingRequests)
                                {
                                    pollingRequests.Remove(request);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        trace.TraceEvent(TraceEventType.Error, 766, "处理报价时出错。\r\n" + ex);
                    }
                }
            }
        }
        public TimeSpan TradeOfferStatePollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    }
    public class TradeOfferTimeoutException : TimeoutException
    {
        public TradeOfferTimeoutException() : base("报价已超时，请重新发送。")
        {
        }
    }
}