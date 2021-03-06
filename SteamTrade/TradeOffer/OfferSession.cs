using HtmlAgilityPack;
using Newtonsoft.Json;
using SteamKit2;
using SteamTrade.Exceptions;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace SteamTrade.TradeOffer
{
    public class OfferSession
    {
        private readonly ITradeOfferWebAPI webApi;
        private readonly ISteamWeb steamWeb;

        internal JsonSerializerSettings JsonSerializerSettings { get; set; }

        internal const string SendUrl = "https://steamcommunity.com/tradeoffer/new/send";

        public OfferSession(ITradeOfferWebAPI webApi, ISteamWeb steamWeb)
        {
            this.webApi = webApi;
            this.steamWeb = steamWeb;

            JsonSerializerSettings = new JsonSerializerSettings();
            JsonSerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
            JsonSerializerSettings.Formatting = Formatting.None;
        }

        public TradeOfferAcceptResponse Accept(string tradeOfferId)
        {
            var data = new NameValueCollection();
            data.Add("sessionid", steamWeb.SessionId);
            data.Add("serverid", "1");
            data.Add("tradeofferid", tradeOfferId);

            string url = string.Format("https://steamcommunity.com/tradeoffer/{0}/accept", tradeOfferId);
            string referer = string.Format("https://steamcommunity.com/tradeoffer/{0}/", tradeOfferId);

            string resp = steamWeb.Fetch(url, "POST", data, false, referer, true);

            if (!String.IsNullOrEmpty(resp))
            {
                try
                {
                    var res = JsonConvert.DeserializeObject<TradeOfferAcceptResponse>(resp);
                    //steam can return 'null' response
                    if (res != null)
                    {
                        res.Accepted = string.IsNullOrEmpty(res.TradeError);
                        return res;
                    }
                }
                catch (JsonException)
                {
                    if (TryParseHtmlTradeError(resp, out var tradeError))
                        return new TradeOfferAcceptResponse { TradeError = tradeError };
                    else
                        return new TradeOfferAcceptResponse { TradeError = "Error parsing server response: " + resp };
                }
            }
            //if it didn't work as expected, check the state, maybe it was accepted after all
            var state = webApi.GetOfferState(tradeOfferId);
            return new TradeOfferAcceptResponse { Accepted = state == TradeOfferState.TradeOfferStateAccepted };
        }
        
        public bool Decline(string tradeOfferId)
        {
            var data = new NameValueCollection();
            data.Add("sessionid", steamWeb.SessionId);
            data.Add("serverid", "1");
            data.Add("tradeofferid", tradeOfferId);

            string url = string.Format("https://steamcommunity.com/tradeoffer/{0}/decline", tradeOfferId);
            //should be http://steamcommunity.com/{0}/{1}/tradeoffers - id/profile persona/id64 ideally
            string referer = string.Format("https://steamcommunity.com/tradeoffer/{0}/", tradeOfferId);

            var resp = steamWeb.Fetch(url, "POST", data, false, referer);

            if (!String.IsNullOrEmpty(resp))
            {
                try
                {
                    var json = JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                    if (json.TradeOfferId != null && json.TradeOfferId == tradeOfferId)
                    {
                        return true;
                    }
                }
                catch (JsonException jsex)
                {
                    Debug.WriteLine(jsex);
                }
            }
            else
            {
                var state = webApi.GetOfferState(tradeOfferId);
                if (state == TradeOfferState.TradeOfferStateDeclined)
                {
                    return true;
                }
            }
            return false;
        }

        public void Cancel(string tradeOfferId)
        {
            var data = new NameValueCollection();
            data.Add("sessionid", steamWeb.SessionId);
            data.Add("tradeofferid", tradeOfferId);
            data.Add("serverid", "1");
            string url = string.Format("https://steamcommunity.com/tradeoffer/{0}/cancel", tradeOfferId);
            //should be http://steamcommunity.com/{0}/{1}/tradeoffers/sent/ - id/profile persona/id64 ideally
            string referer = string.Format("https://steamcommunity.com/tradeoffer/{0}/", tradeOfferId);

            var resp = steamWeb.Fetch(url, "POST", data, false, referer);

            if (!String.IsNullOrEmpty(resp))
            {
                try
                {
                    var json = JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                    if (json.TradeOfferId == null || json.TradeOfferId != tradeOfferId)
                    {
                        var state = webApi.GetOfferState(tradeOfferId);
                        if (state != TradeOfferState.TradeOfferStateCanceled)
                            throw new TradeException("Trade offer could not be cancelled. Original server response: " + resp);
                    }
                }
                catch (JsonException)
                {
                    if (TryParseHtmlTradeError(resp, out var tradeError))
                        throw new TradeException(tradeError);
                    var state = webApi.GetOfferState(tradeOfferId);
                    if (state != TradeOfferState.TradeOfferStateCanceled)
                        throw new TradeException("Trade offer could not be cancelled. Original server response: " + resp);
                }
            }
            else
            {
                var state = webApi.GetOfferState(tradeOfferId);
                if (state != TradeOfferState.TradeOfferStateCanceled)
                    throw new TradeException("Trade offer could not be cancelled. Server returned empty response. ");
            }
        }

        [Obsolete("Use NewTradeOfferResponse CounterOffer(string, SteamID, TradeOffer.TradeStatus, string) instead.")]
        /// <summary>
        /// Creates a new counter offer
        /// </summary>
        /// <param name="message">A message to include with the trade offer</param>
        /// <param name="otherSteamId">The SteamID of the partner we are trading with</param>
        /// <param name="status">The list of items we and they are going to trade</param>
        /// <param name="newTradeOfferId">The trade offer Id that will be created if successful</param>
        /// <param name="tradeOfferId">The trade offer Id of the offer being countered</param>
        /// <returns></returns>
        public bool CounterOffer(string message, SteamID otherSteamId, TradeOffer.TradeStatus status, out string newTradeOfferId, string tradeOfferId)
        {
            if (String.IsNullOrEmpty(tradeOfferId))
            {
                throw new ArgumentNullException("tradeOfferId", "Trade Offer Id must be set for counter offers.");
            }

            var data = new NameValueCollection();
            data.Add("sessionid", steamWeb.SessionId);
            data.Add("serverid", "1");
            data.Add("partner", otherSteamId.ConvertToUInt64().ToString());
            data.Add("tradeoffermessage", message);
            data.Add("json_tradeoffer", JsonConvert.SerializeObject(status, JsonSerializerSettings));
            data.Add("tradeofferid_countered", tradeOfferId);
            data.Add("trade_offer_create_params", "{}");

            string referer = string.Format("https://steamcommunity.com/tradeoffer/{0}/", tradeOfferId);

            if (!Request(SendUrl, data, referer, tradeOfferId, out newTradeOfferId))
            {
                var state = webApi.GetOfferState(tradeOfferId);
                if (state == TradeOfferState.TradeOfferStateCountered)
                {
                    return true;
                }
                return false;
            }
            return true;
        }

        public NewTradeOfferResponse CounterOffer(string message, SteamID otherSteamId, TradeOffer.TradeStatus status, string tradeOfferId)
        {
            if (String.IsNullOrEmpty(tradeOfferId))
            {
                throw new ArgumentNullException("tradeOfferId", "Trade Offer Id must be set for counter offers.");
            }

            var data = new NameValueCollection();
            data.Add("sessionid", steamWeb.SessionId);
            data.Add("serverid", "1");
            data.Add("partner", otherSteamId.ConvertToUInt64().ToString());
            data.Add("tradeoffermessage", message);
            data.Add("json_tradeoffer", JsonConvert.SerializeObject(status, JsonSerializerSettings));
            data.Add("tradeofferid_countered", tradeOfferId);
            data.Add("trade_offer_create_params", "{}");

            string referer = string.Format("https://steamcommunity.com/tradeoffer/{0}/", tradeOfferId);

            return Request(SendUrl, data, referer);
        }

        [Obsolete("Use SendTradeOffer(string message, SteamID otherSteamId, TradeOffer.TradeStatus status) instead.")]
        /// <summary>
        /// Creates a new trade offer
        /// </summary>
        /// <param name="message">A message to include with the trade offer</param>
        /// <param name="otherSteamId">The SteamID of the partner we are trading with</param>
        /// <param name="status">The list of items we and they are going to trade</param>
        /// <param name="newTradeOfferId">The trade offer Id that will be created if successful</param>
        /// <returns>True if successfully returns a newTradeOfferId, else false</returns>
        public bool SendTradeOffer(string message, SteamID otherSteamId, TradeOffer.TradeStatus status, out string newTradeOfferId)
        {
            var data = new NameValueCollection();
            data.Add("sessionid", steamWeb.SessionId);
            data.Add("serverid", "1");
            data.Add("partner", otherSteamId.ConvertToUInt64().ToString());
            data.Add("tradeoffermessage", message);
            data.Add("json_tradeoffer", JsonConvert.SerializeObject(status, JsonSerializerSettings));
            data.Add("trade_offer_create_params", "{}");

            string referer = string.Format("https://steamcommunity.com/tradeoffer/new/?partner={0}",
                otherSteamId.AccountID);

            return Request(SendUrl, data, referer, null, out newTradeOfferId);
        }

        /// <summary>
        /// Creates a new trade offer.
        /// </summary>
        /// <param name="message">A message to include with the trade offer</param>
        /// <param name="otherSteamId">The SteamID of the partner we are trading with</param>
        /// <param name="status">The list of items we and they are going to trade</param>
        /// <returns>If Steam returns an error, <see cref="NewTradeOfferResponse.TradeOfferId"/> will be empty and <see cref="NewTradeOfferResponse.TradeError"/> contains the error</returns>
        /// <exception cref="JsonException">An error occurred while parsing server's response</exception>
        /// <exception cref="System.Net.WebException">Network error</exception>
        public NewTradeOfferResponse SendTradeOffer(string message, SteamID otherSteamId, TradeOffer.TradeStatus status)
        {
            var data = new NameValueCollection();
            data.Add("sessionid", steamWeb.SessionId);
            data.Add("serverid", "1");
            data.Add("partner", otherSteamId.ConvertToUInt64().ToString());
            data.Add("tradeoffermessage", message);
            data.Add("json_tradeoffer", JsonConvert.SerializeObject(status, JsonSerializerSettings));
            data.Add("trade_offer_create_params", "{}");

            string referer = string.Format("https://steamcommunity.com/tradeoffer/new/?partner={0}",
                otherSteamId.AccountID);

            return Request(SendUrl, data, referer);
        }

        [Obsolete("Use SendTradeOfferWithToken(string message, SteamID otherSteamId, TradeOffer.TradeStatus status, string token, out string newTradeOfferId) instead.")]
        /// <summary>
        /// Creates a new trade offer with a token
        /// </summary>
        /// <param name="message">A message to include with the trade offer</param>
        /// <param name="otherSteamId">The SteamID of the partner we are trading with</param>
        /// <param name="status">The list of items we and they are going to trade</param>
        /// <param name="token">The token of the partner we are trading with</param>
        /// <param name="newTradeOfferId">The trade offer Id that will be created if successful</param>
        /// <returns>True if successfully returns a newTradeOfferId, else false</returns>
        public bool SendTradeOfferWithToken(string message, SteamID otherSteamId, TradeOffer.TradeStatus status,
            string token, out string newTradeOfferId)
        {
            if (String.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException("token", "Partner trade offer token is missing");
            }
            var offerToken = new OfferAccessToken() { TradeOfferAccessToken = token };

            var data = new NameValueCollection();
            data.Add("sessionid", steamWeb.SessionId);
            data.Add("serverid", "1");
            data.Add("partner", otherSteamId.ConvertToUInt64().ToString());
            data.Add("tradeoffermessage", message);
            data.Add("json_tradeoffer", JsonConvert.SerializeObject(status, JsonSerializerSettings));
            data.Add("trade_offer_create_params", JsonConvert.SerializeObject(offerToken, JsonSerializerSettings));

            string referer = string.Format("https://steamcommunity.com/tradeoffer/new/?partner={0}&token={1}",
                        otherSteamId.AccountID, token);
            return Request(SendUrl, data, referer, null, out newTradeOfferId);
        }

        /// <summary>
        /// Creates a new trade offer with a token
        /// </summary>
        /// <param name="message">A message to include with the trade offer</param>
        /// <param name="otherSteamId">The SteamID of the partner we are trading with</param>
        /// <param name="status">The list of items we and they are going to trade</param>
        /// <param name="token">The token of the partner we are trading with</param>
        /// <param name="newTradeOfferId">The trade offer Id that will be created if successful</param>
        /// <returns>If Steam returns an error, <see cref="NewTradeOfferResponse.TradeOfferId"/> will be empty and <see cref="NewTradeOfferResponse.TradeError"/> contains the error</returns>
        /// <exception cref="JsonException">An error occurred while parsing server's response</exception>
        /// <exception cref="System.Net.WebException">Network error</exception>
        public NewTradeOfferResponse SendTradeOfferWithToken(string message, SteamID otherSteamId, TradeOffer.TradeStatus status,
            string token)
        {
            if (String.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException("token", "Partner trade offer token is missing");
            }
            var offerToken = new OfferAccessToken() { TradeOfferAccessToken = token };

            var data = new NameValueCollection();
            data.Add("sessionid", steamWeb.SessionId);
            data.Add("serverid", "1");
            data.Add("partner", otherSteamId.ConvertToUInt64().ToString());
            data.Add("tradeoffermessage", message);
            data.Add("json_tradeoffer", JsonConvert.SerializeObject(status, JsonSerializerSettings));
            data.Add("trade_offer_create_params", JsonConvert.SerializeObject(offerToken, JsonSerializerSettings));

            string referer = string.Format("https://steamcommunity.com/tradeoffer/new/?partner={0}&token={1}",
                        otherSteamId.AccountID, token);
            return Request(SendUrl, data, referer);
        }

        [Obsolete]
        internal bool Request(string url, NameValueCollection data, string referer, string tradeOfferId, out string newTradeOfferId)
        {
            newTradeOfferId = "";

            string resp = steamWeb.Fetch(url, "POST", data, false, referer);
            if (!String.IsNullOrEmpty(resp))
            {
                try
                {
                    var offerResponse = JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                    if (!String.IsNullOrEmpty(offerResponse.TradeOfferId))
                    {
                        newTradeOfferId = offerResponse.TradeOfferId;
                        return true;
                    }
                    else
                    {
                        //todo: log possible error
                        Debug.WriteLine(offerResponse.TradeError);
                    }
                }
                catch (JsonException jsex)
                {
                    Debug.WriteLine(jsex);
                }
            }
            return false;
        }

        internal NewTradeOfferResponse Request(string url, NameValueCollection data, string referer)
        {
            string resp = steamWeb.Fetch(url, "POST", data, false, referer, true);
            if (!String.IsNullOrEmpty(resp))
            {
                try
                {
                    return JsonConvert.DeserializeObject<NewTradeOfferResponse>(resp);
                }
                catch (JsonException)
                {
                    if (TryParseHtmlTradeError(resp, out var tradeError))
                        return new NewTradeOfferResponse { TradeError = tradeError };
                    else
                        return new NewTradeOfferResponse { TradeError = "Error parsing server response: " + resp };
                }
            }
            else
            {
                throw new WebException("The response has no content.");
            }
        }
        private static bool TryParseHtmlTradeError(string response, out string tradeError)
        {
            //Parse HTML error response
            try
            {
                var document = new HtmlDocument();
                document.LoadHtml(response);
                if (document.GetElementbyId("error_msg") is var error_msg)
                {
                    tradeError = error_msg.InnerText.Trim();
                    return true;
                }
            }
            catch { }
            tradeError = null;
            return false;
        }

    }

    public class NewTradeOfferResponse
    {
        [JsonProperty("tradeofferid")]
        public string TradeOfferId { get; set; }

        [JsonProperty("strError")]
        public string TradeError { get; set; }

        [JsonProperty("needs_email_confirmation")]
        public bool NeedsEmailConfirmation { get; set; }

        [JsonProperty("needs_mobile_confirmation")]
        public bool NeedsMobileConfirmation { get; set; }
    }

    public class OfferAccessToken
    {
        [JsonProperty("trade_offer_access_token")]
        public string TradeOfferAccessToken { get; set; }
    }

    public class TradeOfferAcceptResponse
    {
        public bool Accepted { get; set; }

        [JsonProperty("tradeid")]
        public string TradeId { get; set; }

        [JsonProperty("strError")]
        public string TradeError { get; set; }

        public TradeOfferAcceptResponse()
        {
            TradeId = String.Empty;
            TradeError = String.Empty;
        }
    }
}
