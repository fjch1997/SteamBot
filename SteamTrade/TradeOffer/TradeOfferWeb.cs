using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;
using System.Collections;
using System.Web;

namespace SteamTrade.TradeOffer
{
    /// <summary>
    /// Countepart to <see cref="TradeOfferWebAPI"/>. Only except this download and parse HTML response instead of using Steam Web API. It returns trade offers that belongs to the account <see cref="ISteamWeb"/> is logged on to. Limitations: Only recent offers are availale. Descriptions are not implemented yet but are possible to implement given some time. GetTradeOffersSummary is not implemented. Some trade offer states are not able to get. See comments for details.
    /// </summary>
    public class TradeOfferWeb : ITradeOfferWebAPI
    {
        private const string STEAM_COMMUNITY_BASE_URL = "http://steamcommunity.com/";
        private readonly ISteamWeb steamWeb;
        private string receivedTradeOfferUrl;
        private string receivedHistoricTradeOfferUrl;
        private string sentTradeOfferUrl;
        private string sentHistoricTradeOfferUrl;

        public TradeOfferWeb(ISteamWeb steamWeb)
        {
            this.steamWeb = steamWeb;
        }

        private void GetTradeUrlsIfNull()
        {
            if (receivedTradeOfferUrl == null)
            {
                var html = steamWeb.Fetch(STEAM_COMMUNITY_BASE_URL, "GET");
                var document = new HtmlDocument();
                document.LoadHtml(html);
                var user_avatar = document.DocumentNode.GetElementsByClassName("user_avatar");
                var href = document.DocumentNode.GetElementsByClassName("user_avatar").Select(a => a.Attributes["href"].Value).FirstOrDefault();
                if (href == null || string.IsNullOrWhiteSpace(href) || !(href.StartsWith("http://steamcommunity.com/id/") || href.StartsWith("http://steamcommunity.com/profiles/")))
                    throw new UnexpectedHtmlException($"Unexpected html when parsing {STEAM_COMMUNITY_BASE_URL} for user's profile page base URL. See {nameof(UnexpectedHtmlException.OriginalResponse)} for details.", html);
                receivedTradeOfferUrl = href + "tradeoffers";
                receivedHistoricTradeOfferUrl = receivedTradeOfferUrl + "/?history=1";
                sentTradeOfferUrl = receivedTradeOfferUrl + "/sent";
                sentHistoricTradeOfferUrl = sentTradeOfferUrl + "/?history=1";
            }
        }

        /// <summary>
        /// Only gets AccountIdOther, Message, TradeOfferId and tradeOfferState(Incomplete) of the <see cref="Offer"/>. All other properties are not implemented yet. TradeOfferStateInEscrow, TradeOfferStateInvalid, TradeOfferStateCanceledBySecondFactor not supported.
        /// </summary>
        public OffersResponse GetActiveTradeOffers(bool getSentOffers, bool getReceivedOffers, bool getDescriptions, string language = "en_us")
        {
            GetTradeUrlsIfNull();
            if (language != "en_us")
                throw new NotImplementedException($"Change of {nameof(language)} is not supported with {nameof(TradeOfferWeb)}. Language depends on {nameof(ISteamWeb)}.{nameof(ISteamWeb.AcceptLanguageHeader)}.");
            if (getDescriptions)
                throw new NotImplementedException($"Getting descriptions is not implemented in {nameof(TradeOfferWeb)}.");
            var response = new OffersResponse();
            var steamWeb = GetEnglishSteamWeb();
            if (getSentOffers)
                response.TradeOffersSent = new List<Offer>(ParseTradeOffer(steamWeb.Fetch(sentTradeOfferUrl, "GET", null, false)));
            if (getReceivedOffers)
                response.TradeOffersReceived = new List<Offer>(ParseTradeOffer(steamWeb.Fetch(receivedTradeOfferUrl, "GET", null, false)));
            return response;
        }

        private IEnumerable<Offer> ParseTradeOffer(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);
            foreach (var item in document.DocumentNode.GetElementsByClassName("tradeoffer"))
            {
                var offer = new Offer();
                //AccountIdOther
                var playerAvatars = item.GetElementsByClassName("playerAvatar").ToArray();
                if (playerAvatars.Length > 0)
                {
                    var data_miniprofile = playerAvatars[0].Attributes["data-miniprofile"];
                    int accountIdOther;
                    if (data_miniprofile != null && int.TryParse(data_miniprofile.Value, out accountIdOther))
                        offer.AccountIdOther = accountIdOther;
                }
                offer.IsOurOffer = true;
                //Message
                var tradeoffer_message = item.GetElementsByClassName("tradeoffer_message").FirstOrDefault();
                if (tradeoffer_message != null)
                {
                    var quote = tradeoffer_message.GetElementsByClassName("quote").FirstOrDefault();
                    if (quote != null)
                    {
                        var innerText = HttpUtility.HtmlDecode(quote.InnerText);
                        var indexOfTheQuestionmark = innerText.LastIndexOf("(?)");
                        if (indexOfTheQuestionmark == -1)
                            offer.Message = innerText.Trim();
                        else
                            offer.Message = innerText.Substring(0, indexOfTheQuestionmark).Trim();
                    }
                }
                //TradeOfferId
                offer.TradeOfferId = item.Id.Substring(13, item.Id.Length - 13);
                //TradeOfferState
                var banner = item.GetElementsByClassName("tradeoffer_items_banner").FirstOrDefault();
                if (banner != null)
                {
                    DateTime timeUpdated;
                    if (DateTime.TryParse(banner.InnerText, out timeUpdated))
                        offer.TimeUpdated = GetUnixTimestamp(timeUpdated);

                    if (banner.InnerText.Contains("Trade Offer Canceled"))
                        offer.TradeOfferState = TradeOfferState.TradeOfferStateCanceled;
                    else if (banner.InnerText.Contains("Trade Declined"))
                        offer.TradeOfferState = TradeOfferState.TradeOfferStateDeclined;
                    else if (banner.InnerText.Contains("Items Now Unavailable For Trade"))
                        offer.TradeOfferState = TradeOfferState.TradeOfferStateInvalidItems;
                    else if (banner.InnerText.Contains("Awaiting Mobile Confirmation"))
                        offer.TradeOfferState = TradeOfferState.TradeOfferStateNeedsConfirmation;
                    else if (banner.InnerText.Contains("Trade Accepted"))
                        offer.TradeOfferState = TradeOfferState.TradeOfferStateAccepted;
                    else if (banner.InnerText.Contains("Expired"))//TODO: Not sure about the actual text when it's expired
                        offer.TradeOfferState = TradeOfferState.TradeOfferStateExpired;
                    else if (banner.InnerText.Contains("Counter Offer Made"))
                        offer.TradeOfferState = TradeOfferState.TradeOfferStateCountered;
                }
                else
                    offer.TradeOfferState = TradeOfferState.TradeOfferStateActive;
                var footer = item.GetElementsByClassName("tradeoffer_footer_actions").FirstOrDefault();
                if (footer != null)
                {
                    var i = footer.InnerText.IndexOf("expires on ");
                    if (i >= 0)
                    {
                        var dateString = footer.InnerText.Substring(i, footer.InnerText.Length - i);
                        DateTime result;
                        if (DateTime.TryParse(dateString, out result))
                            offer.ExpirationTime = GetUnixTimestamp(result);
                    }
                }
                yield return offer;
            }
        }

        private int GetUnixTimestamp(DateTime timeUpdated)
        {
            return (Int32)(timeUpdated.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        private ISteamWeb GetEnglishSteamWeb()
        {
            //Create a copy of SteamWeb because the preservation of old language cookie is not threadsafe
            Uri steamCommunityUri = new Uri("https://" + ISteamWeb.SteamCommunityDomain);
            var steamWeb = new ISteamWeb();
            steamWeb.Authenticate(this.steamWeb.Cookies.GetCookies(steamCommunityUri).Cast<Cookie>());
            steamWeb.Cookies.SetCookies(steamCommunityUri, "Steam_Language=english; expires=Fri, 24-Dec-2050 16:37:28 GMT; path=/");
            return steamWeb;
        }

        public OffersResponse GetAllTradeOffers(string timeHistoricalCutoff = "1389106496", string language = "en_us")
        {
            GetTradeUrlsIfNull();
            if (timeHistoricalCutoff != "1389106496" || language != "en_us")
                throw new NotImplementedException($"Change of {nameof(timeHistoricalCutoff)} or {nameof(language)} is not supported with {nameof(TradeOfferWeb)}. Language depends on {nameof(ISteamWeb)}.{nameof(ISteamWeb.AcceptLanguageHeader)}.");
            var steamWeb = GetEnglishSteamWeb();
            var response = new OffersResponse();
            response.TradeOffersSent = new List<Offer>(ParseTradeOffer(steamWeb.Fetch(sentTradeOfferUrl, "GET", null, false)));
            response.TradeOffersSent.AddRange(ParseTradeOffer(steamWeb.Fetch(sentHistoricTradeOfferUrl, "GET", null, false)));
            response.TradeOffersReceived = new List<Offer>(ParseTradeOffer(steamWeb.Fetch(receivedTradeOfferUrl, "GET", null, false)));
            response.TradeOffersReceived.AddRange(ParseTradeOffer(steamWeb.Fetch(receivedHistoricTradeOfferUrl, "GET", null, false)));
            return response;
        }

        public TradeOfferState GetOfferState(string tradeofferid)
        {
            GetTradeUrlsIfNull();
            return GetTradeOffer(tradeofferid).Offer.TradeOfferState;
        }

        public OfferResponse GetTradeOffer(string tradeofferid)
        {
            GetTradeUrlsIfNull();
            var steamWeb = GetEnglishSteamWeb();
            var offer = ParseTradeOffer(steamWeb.Fetch(sentTradeOfferUrl, "GET", null, false)).FirstOrDefault(o => o.TradeOfferId == tradeofferid);
            if (offer != null)
                return new OfferResponse() { Offer = offer }; 
            offer = ParseTradeOffer(steamWeb.Fetch(sentHistoricTradeOfferUrl, "GET", null, false)).FirstOrDefault(o => o.TradeOfferId == tradeofferid);
            if (offer != null)
                return new OfferResponse() { Offer = offer }; 
            offer = ParseTradeOffer(steamWeb.Fetch(receivedTradeOfferUrl, "GET", null, false)).FirstOrDefault(o => o.TradeOfferId == tradeofferid);
            if (offer != null)
                return new OfferResponse() { Offer = offer }; 
            offer = ParseTradeOffer(steamWeb.Fetch(receivedHistoricTradeOfferUrl, "GET", null, false)).FirstOrDefault(o => o.TradeOfferId == tradeofferid);
            if (offer != null)
                return new OfferResponse() { Offer = offer };
            throw new TradeOfferNotFoundException(tradeofferid);
        }

        public OffersResponse GetTradeOffers(bool getSentOffers, bool getReceivedOffers, bool getDescriptions, bool activeOnly, bool historicalOnly, string timeHistoricalCutoff = "1389106496", string language = "en_us")
        {
            GetTradeUrlsIfNull();
            if (timeHistoricalCutoff != "1389106496" || language != "en_us")
                throw new NotImplementedException($"Change of {nameof(timeHistoricalCutoff)} or {nameof(language)} is not supported with {nameof(TradeOfferWeb)}. Language depends on {nameof(ISteamWeb)}.{nameof(ISteamWeb.AcceptLanguageHeader)}.");
            var response = new OffersResponse() { TradeOffersReceived = new List<Offer>(), TradeOffersSent = new List<Offer>() };
            var steamWeb = GetEnglishSteamWeb();
            if (getSentOffers && !historicalOnly)
                response.TradeOffersSent.AddRange(ParseTradeOffer(steamWeb.Fetch(sentTradeOfferUrl, "GET", null, false)));
            if (getSentOffers && !activeOnly)
                response.TradeOffersSent.AddRange(ParseTradeOffer(steamWeb.Fetch(sentHistoricTradeOfferUrl, "GET", null, false)));
            if (getReceivedOffers && !historicalOnly)
                response.TradeOffersReceived.AddRange(ParseTradeOffer(steamWeb.Fetch(receivedTradeOfferUrl, "GET", null, false)));
            if (getReceivedOffers && !activeOnly)
                response.TradeOffersReceived.AddRange(ParseTradeOffer(steamWeb.Fetch(receivedHistoricTradeOfferUrl, "GET", null, false)));
            return response;
        }

        public TradeOffersSummary GetTradeOffersSummary(uint timeLastVisit)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    internal class TradeOfferNotFoundException : Exception
    {
        public TradeOfferNotFoundException(string id):base($"Trade offer {id} was not found.")
        {
        }
        
        protected TradeOfferNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class UnexpectedHtmlException : Exception
    {
        public string OriginalResponse { get; set; }
        public UnexpectedHtmlException(string originalResponse)
        {
            OriginalResponse = originalResponse;
        }

        public UnexpectedHtmlException(string message, string originalResponse) : base(message)
        {
            OriginalResponse = originalResponse;
        }

        public UnexpectedHtmlException(string message, string originalResponse, Exception innerException) : base(message, innerException)
        {
            OriginalResponse = originalResponse;
        }

        protected UnexpectedHtmlException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string ToString()
        {
            return base.ToString() + "\r\n\r\nOriginal server response:\r\n" + OriginalResponse;
        }
    }

    public static class HtmlAgilityPackExtensions
    {
        public static IEnumerable<HtmlNode> GetElementsByClassName(this HtmlNode htmlNode, string className)
        {
            var spaceCharArray = new char[] { ' ' };
            var classNames = className.Split(spaceCharArray, StringSplitOptions.RemoveEmptyEntries);
            return htmlNode.Descendants().Where(a =>
            {
                var c = a.Attributes["class"];
                if (c == null)
                    return false;
                var cClassNames = c.Value.ToLower().Split(spaceCharArray, StringSplitOptions.RemoveEmptyEntries);
                if (classNames.All(n => cClassNames.Contains(n.ToLower())))
                    return true;
                else
                    return false;
            });
        }
    }
}
