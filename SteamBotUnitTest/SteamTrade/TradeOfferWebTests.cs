using NUnit.Framework;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.Collections.Specialized;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SteamTrade.Fakes;
using Microsoft.QualityTools.Testing.Fakes;
using SteamBotUnitTest.Properties;

namespace SteamBotUnitTest.SteamTrade
{
    [TestFixture]
    public class TradeOfferWebTests
    {
        [Test]
        public void GetOfferStatusTest()
        {
            using (ShimsContext.Create())
            {
                ShimSteamWeb.AllInstances.FetchStringStringNameValueCollectionBooleanStringBoolean = (SteamWeb sw, string url, string method, NameValueCollection args, bool a, string b, bool c) =>
                {
                    return Resources.SentTradeOfferPage;
                };
                ShimSteamWeb.AllInstances.AuthenticateIEnumerableOfCookie = (SteamWeb sw, IEnumerable<Cookie> cookies) =>
                {

                };
                var api = new TradeOfferWeb(new SteamWeb());
                Assert.AreEqual(TradeOfferState.TradeOfferStateAccepted, api.GetOfferState("2260977534"));
            }
        }
    }
}
