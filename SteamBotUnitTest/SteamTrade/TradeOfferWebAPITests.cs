using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SteamBotUnitTest.Properties;
using SteamTrade.TradeOffer;

namespace SteamBotUnitTest.SteamTrade
{
    [TestFixture]
    public class TradeOfferWebAPITests
    {
        [Test]
        public void GetTradeHoldDurations()
        {
            var apiKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
            var targetSteamId = 76561198008632750L;
            var token = "ABCDE";
            var api = new TradeOfferWebAPI(apiKey, new DelegateFetchSteamWeb(url =>
            {
                using (var reader = new StreamReader(new MemoryStream(Resources.GetTradeHoldDurations)))
                {
                    return reader.ReadToEndAsync();
                }
            }));
            var response = api.GetTradeHoldDurations(targetSteamId, token);
            Assert.AreEqual(10000, response.Response.MyEscrow.EscrowEndDurationSeconds);
            Assert.AreEqual(10000, response.Response.TheirEscrow.EscrowEndDurationSeconds);
            Assert.AreEqual(10000, response.Response.BothEscrow.EscrowEndDurationSeconds);
        }
    }
}
