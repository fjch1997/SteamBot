using Newtonsoft.Json;
using NUnit.Framework;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SteamBotUnitTest.SteamTrade
{
    [TestFixture]
    class SteamWebTests
    {
        private readonly ulong buyerSteamId = 76561198319214599UL;
        private readonly string buyerToken = "O4ZW_7D9";
        private readonly string buyerWinAuthBackup = "otpauth://totp/Steam:fjch1997?secret=WER5ITPMZQCRY24JKZETDODRTLMUQYVV&digits=5&issuer=Steam&deviceid=android%3a65af6cf3-492d-6cf3-6cf3-6cf3492d4d93&data=%7b%22shared_secret%22%3a%22sSPUTezMBRxriVZJMbhxmtlIYrU%3d%22%2c%22serial_number%22%3a%2212621599193736060956%22%2c%22revocation_code%22%3a%22R21525%22%2c%22uri%22%3a%22otpauth%3a%2f%2ftotp%2fSteam%3afjch1997%3fsecret%3dWER5ITPMZQCRY24JKZETDODRTLMUQYVV%26issuer%3dSteam%22%2c%22server_time%22%3a1469221491%2c%22account_name%22%3a%22fjch1997%22%2c%22token_gid%22%3a%22b055b5217b7d7ec%22%2c%22identity_secret%22%3a%22vetk8P7mmp41VZp7vri59g98Y9c%3d%22%2c%22secret_1%22%3a%22BGaqM5ENk1zJz92Rpa3Y59fhhjI%3d%22%2c%22status%22%3a1%2c%22device_id%22%3a%22android%3a65af6cf3-492d-6cf3-6cf3-6cf3492d4d93%22%2c%22fully_enrolled%22%3atrue%2c%22Session%22%3a%7b%22SessionID%22%3a%2222c7858da94870046a25b003%22%2c%22SteamLogin%22%3a%2276561198319214599%257C%257C974DB281FA7A3984D0A06EF91D7A08F32BB30823%22%2c%22SteamLoginSecure%22%3a%2276561198319214599%257C%257CA65E3AD622399D42AD96C72ECCF70CB5C995D528%22%2c%22WebCookie%22%3anull%2c%22OAuthToken%22%3a%220dd9bfb878ad5b516b2b27e31d625b5e%22%2c%22SteamID%22%3a76561198319214599%7d%7d";
        private readonly string buyerUsername = "fjch1997";
        private readonly string buyerPassword = "SteamTradeTest33";
        private readonly ulong sellerSteamId = 76561198040705264UL;
        private readonly string sellerWinAuthBackup = "otpauth://totp/Steam:993122511?secret=JBHS7DWRXQ5AWVQUS2URGYIG6K2VZKBS&digits=5&issuer=Steam&deviceid=android%3a6be70997-8c2a-4887-8c22-7c3cfc5c172a&data=%7b%22shared_secret%22%3a%22SE8vjtG8OgtWFJapE2EG8rVcqDI%3d%22%2c%22serial_number%22%3a%223925554272877039778%22%2c%22revocation_code%22%3a%22R22485%22%2c%22uri%22%3a%22otpauth%3a%2f%2ftotp%2fSteam%3aq993122511%3fsecret%3dJBHS7DWRXQ5AWVQUS2URGYIG6K2VZKBS%26issuer%3dSteam%22%2c%22server_time%22%3a1457236527%2c%22account_name%22%3a%22q993122511%22%2c%22token_gid%22%3a%228f14ffdcd760d1a%22%2c%22identity_secret%22%3a%22JI48hEeP8FNCZjT3sgzjXIOnj9Q%3d%22%2c%22secret_1%22%3a%22qJK%2fUffTmwZHMHf53A6xJ02ZO1c%3d%22%2c%22status%22%3a1%2c%22device_id%22%3a%22android%3a6be70997-8c2a-4887-8c22-7c3cfc5c172a%22%2c%22fully_enrolled%22%3afalse%2c%22Session%22%3a%7b%22SessionID%22%3a%2285fb69a09402e207598a5f98%22%2c%22SteamLogin%22%3a%2276561198040705264%257C%257C71090E19F5F0FCEAF1DCC22057EC4134F9A7F34C%22%2c%22SteamLoginSecure%22%3a%2276561198040705264%257C%257C3345E6131C0C0A0E5428F0784206B6AFF5C0A3FF%22%2c%22WebCookie%22%3a%2259A78F2FD073DC78D608F83B06E7DF8C2D2ADC67%22%2c%22OAuthToken%22%3a%22747a33e33c3cc6b5d7fc77a086278342%22%2c%22SteamID%22%3a76561198040705264%7d%7d";
        private readonly string sellerUsername = "q993122511";
        private readonly string sellerPassword = "000000";
        private SteamWeb buyerSteamWeb;
        private SteamWeb sellerSteamWeb;
        private SteamAuth.SteamGuardAccount buyerSteamGuardAccount;
        private SteamAuth.SteamGuardAccount sellerSteamGuardAccount;
        private GenericInventory sellerInventory;
        [OneTimeSetUp]
        public void LoginTest()
        {
            try
            {
                var serializer = new BinaryFormatter();
                var appDataDirectoryName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + Assembly.GetExecutingAssembly().GetName().Name;
                Directory.CreateDirectory(appDataDirectoryName);
                var buyerSteamGuardAccountFileName = appDataDirectoryName + "\\buyerSteamGuardAccount.bin";
                if (File.Exists(buyerSteamGuardAccountFileName))
                {
                    using (var stream = File.OpenRead(buyerSteamGuardAccountFileName))
                    {
                        var buyerSteamGuardAccount = (SteamAuth.SteamGuardAccount)serializer.Deserialize(stream);
                        if (buyerSteamGuardAccount.RefreshSession())
                            this.buyerSteamGuardAccount = buyerSteamGuardAccount;
                    }
                }
                if (this.buyerSteamGuardAccount == null)
                {
                    //Buyer
                    //Parse winAuthBackup
                    var uri = new Uri(buyerWinAuthBackup);
                    var queryString = HttpUtility.ParseQueryString(uri.Query);
                    buyerSteamGuardAccount = JsonConvert.DeserializeObject<SteamAuth.SteamGuardAccount>(queryString["data"]);
                    buyerSteamGuardAccount.DeviceID = queryString["deviceid"];
                    //SteamGuardAccount login
                    var userLogin = new SteamAuth.UserLogin(buyerUsername, buyerPassword);
                    userLogin.TwoFactorCode = buyerSteamGuardAccount.GenerateSteamGuardCode();
                    var loginResult = userLogin.DoLogin();
                    Assert.AreEqual(SteamAuth.LoginResult.LoginOkay, loginResult);
                    buyerSteamGuardAccount.Session = userLogin.Session;
                    using (var stream = File.Create(buyerSteamGuardAccountFileName))
                        serializer.Serialize(stream, buyerSteamGuardAccount);
                    Thread.Sleep(15000);
                }
                var buyerSteamWebFileName = appDataDirectoryName + "\\buyerSteamWeb.bin";
                if (File.Exists(buyerSteamWebFileName))
                {
                    using (var stream = File.OpenRead(buyerSteamWebFileName))
                    {
                        var buyerSteamWeb = (SteamWeb)serializer.Deserialize(stream);
                        if (buyerSteamWeb.VerifyCookies())
                            this.buyerSteamWeb = buyerSteamWeb;
                    }
                }
                if (this.buyerSteamWeb == null)
                {
                    buyerSteamWeb = new SteamWeb();
                    buyerSteamWeb.DoLogin(buyerUsername, buyerPassword, true, () => buyerSteamGuardAccount.GenerateSteamGuardCode(), null, null);
                    Assert.IsTrue(buyerSteamWeb.VerifyCookies());
                    Assert.NotNull(buyerSteamWeb.Token);
                    Assert.NotNull(buyerSteamWeb.TokenSecure);
                    Assert.NotNull(buyerSteamWeb.SessionId);
                    using (var stream = File.Create(buyerSteamWebFileName))
                        serializer.Serialize(stream, buyerSteamWeb);
                    Thread.Sleep(15000);
                }
                //Seller
                var sellerSteamGuardAccountFileName = appDataDirectoryName + "\\sellerSteamGuardAccount.bin";
                if (File.Exists(sellerSteamGuardAccountFileName))
                {
                    using (var stream = File.OpenRead(sellerSteamGuardAccountFileName))
                    {
                        var sellerSteamGuardAccount = (SteamAuth.SteamGuardAccount)serializer.Deserialize(stream);
                        if (sellerSteamGuardAccount.RefreshSession())
                            this.sellerSteamGuardAccount = sellerSteamGuardAccount;
                    }
                }
                if (this.sellerSteamGuardAccount == null)
                {
                    //Parse winAuthBackup
                    var uri = new Uri(sellerWinAuthBackup);
                    var queryString = HttpUtility.ParseQueryString(uri.Query);
                    sellerSteamGuardAccount = JsonConvert.DeserializeObject<SteamAuth.SteamGuardAccount>(queryString["data"]);
                    sellerSteamGuardAccount.DeviceID = queryString["deviceid"];
                    //SteamGuardAccount login
                    var userLogin = new SteamAuth.UserLogin(sellerUsername, sellerPassword);
                    userLogin.TwoFactorCode = sellerSteamGuardAccount.GenerateSteamGuardCode();
                    var loginResult = userLogin.DoLogin();
                    Assert.AreEqual(SteamAuth.LoginResult.LoginOkay, loginResult);
                    sellerSteamGuardAccount.Session = userLogin.Session;
                    Thread.Sleep(15000);
                }

                var sellerSteamWebFileName = appDataDirectoryName + "\\sellerSteamWeb.bin";
                if (File.Exists(sellerSteamWebFileName))
                {
                    using (var stream = File.OpenRead(sellerSteamWebFileName))
                    {
                        var sellerSteamWeb = (SteamWeb)serializer.Deserialize(stream);
                        if (sellerSteamWeb.VerifyCookies())
                            this.sellerSteamWeb = sellerSteamWeb;
                    }
                }
                if (this.sellerSteamWeb == null)
                {
                    sellerSteamWeb = new SteamWeb();
                    sellerSteamWeb.DoLogin(sellerUsername, sellerPassword, true, () => sellerSteamGuardAccount.GenerateSteamGuardCode(), null, null);
                    Assert.IsTrue(sellerSteamWeb.VerifyCookies());
                    Assert.NotNull(sellerSteamWeb.Token);
                    Assert.NotNull(sellerSteamWeb.TokenSecure);
                    Assert.NotNull(sellerSteamWeb.SessionId);
                    using (var stream = File.Create(sellerSteamWebFileName))
                        serializer.Serialize(stream, sellerSteamWeb);
                    Thread.Sleep(15000);
                }
                //Seller inventory
                sellerInventory = new GenericInventory(sellerSteamWeb);
                sellerInventory.LoadAsync(570, new long[] { 2 }, new SteamKit2.SteamID(sellerSteamId)).Wait();
                Assert.AreEqual(0, sellerInventory.Errors.Count);
            }
            catch (WebException ex)
            {
                var reason = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                Assert.Fail(ex.ToString() + "\r\n" + reason);
            }
        }

        [Test]
        public void TradeOfferWebCounterAndCancelTest()
        {
            try
            {
                var offer = new TradeOfferManager("", sellerSteamWeb).NewOffer(new SteamKit2.SteamID(buyerSteamId));
                AddSellerInventoryItems(offer);
                var response = offer.SendWithToken(buyerToken);
                var tradeOfferWeb = new TradeOfferWeb(sellerSteamWeb);
                void AssertOfferState(TradeOfferState state, bool isActive, string tradeOfferId)
                {
                    Assert.AreEqual(state, tradeOfferWeb.GetTradeOffer(tradeOfferId).Offer.TradeOfferState);
                    if (isActive)
                        Assert.AreEqual(state, tradeOfferWeb.GetActiveTradeOffers(true, true, false).AllOffers.FirstOrDefault(o => o.TradeOfferId == tradeOfferId)?.TradeOfferState);
                    Assert.AreEqual(state, tradeOfferWeb.GetAllTradeOffers().AllOffers.FirstOrDefault(o => o.TradeOfferId == tradeOfferId)?.TradeOfferState);
                    Assert.AreEqual(state, tradeOfferWeb.GetTradeOffers(true, true, false, false, false).AllOffers.FirstOrDefault(o => o.TradeOfferId == tradeOfferId)?.TradeOfferState);
                }
                AssertOfferState(TradeOfferState.TradeOfferStateNeedsConfirmation, true, response.TradeOfferId);
                foreach (var item in sellerSteamGuardAccount.FetchConfirmations())
                {
                    sellerSteamGuardAccount.AcceptConfirmation(item);
                }
                AssertOfferState(TradeOfferState.TradeOfferStateActive, true, response.TradeOfferId);
                var buyerOfferSession = new OfferSession(new TradeOfferWebAPI("", buyerSteamWeb), buyerSteamWeb);
                var counterOfferStatus = new TradeOffer.TradeStatus(new List<TradeOffer.TradeStatusUser.TradeAsset>(), offer.Items.GetMyItems());
                var counterResponse = buyerOfferSession.CounterOffer("", sellerSteamId, counterOfferStatus, response.TradeOfferId);
                //Assert.IsNull(counterResponse.TradeError);
                AssertOfferState(TradeOfferState.TradeOfferStateCountered, false, response.TradeOfferId);
                AssertOfferState(TradeOfferState.TradeOfferStateActive, true, counterResponse.TradeOfferId);
                buyerOfferSession.Cancel(counterResponse.TradeOfferId);
                AssertOfferState(TradeOfferState.TradeOfferStateCanceled, false, counterResponse.TradeOfferId);
            }
            catch (WebException ex)
            {
                var reason = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                Assert.Fail(ex.ToString() + "\r\n" + reason);
            }
        }
        [Test]
        public void TradeOfferWebDeclineTest()
        {
            try
            {
                var offer = new TradeOfferManager("", sellerSteamWeb).NewOffer(new SteamKit2.SteamID(buyerSteamId));
                AddSellerInventoryItems(offer);
                var response = offer.SendWithToken(buyerToken);
                var tradeOfferWeb = new TradeOfferWeb(sellerSteamWeb);
                void AssertOfferState(TradeOfferState state, bool isActive)
                {
                    Assert.AreEqual(state, tradeOfferWeb.GetTradeOffer(response.TradeOfferId).Offer.TradeOfferState);
                    if (isActive)
                        Assert.AreEqual(state, tradeOfferWeb.GetActiveTradeOffers(true, true, false).AllOffers.FirstOrDefault(o => o.TradeOfferId == response.TradeOfferId)?.TradeOfferState);
                    Assert.AreEqual(state, tradeOfferWeb.GetAllTradeOffers().AllOffers.FirstOrDefault(o => o.TradeOfferId == response.TradeOfferId)?.TradeOfferState);
                    Assert.AreEqual(state, tradeOfferWeb.GetTradeOffers(true, true, false, false, false).AllOffers.FirstOrDefault(o => o.TradeOfferId == response.TradeOfferId)?.TradeOfferState);

                }
                AssertOfferState(TradeOfferState.TradeOfferStateNeedsConfirmation, true);
                foreach (var item in sellerSteamGuardAccount.FetchConfirmations())
                {
                    sellerSteamGuardAccount.AcceptConfirmation(item);
                }
                AssertOfferState(TradeOfferState.TradeOfferStateActive, true);
                var buyerOfferSession = new OfferSession(new TradeOfferWebAPI("", buyerSteamWeb), buyerSteamWeb);
                Assert.True(buyerOfferSession.Decline(response.TradeOfferId));
                AssertOfferState(TradeOfferState.TradeOfferStateDeclined, false);
            }
            catch (WebException ex)
            {
                var reason = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                Assert.Fail(ex.ToString() + "\r\n" + reason);
            }
        }

        [Test]
        public void TradeOfferWebNormalTest()
        {
            try
            {
                var offer = new TradeOfferManager("", sellerSteamWeb).NewOffer(new SteamKit2.SteamID(buyerSteamId));
                AddSellerInventoryItems(offer);
                var response = offer.Send();
                var tradeOfferWeb = new TradeOfferWeb(sellerSteamWeb);
                void AssertOfferState(TradeOfferState state, bool isActive)
                {
                    Assert.AreEqual(state, tradeOfferWeb.GetTradeOffer(response.TradeOfferId).Offer.TradeOfferState);
                    if (isActive)
                        Assert.AreEqual(state, tradeOfferWeb.GetActiveTradeOffers(true, true, false).AllOffers.FirstOrDefault(o => o.TradeOfferId == response.TradeOfferId)?.TradeOfferState);
                    Assert.AreEqual(state, tradeOfferWeb.GetAllTradeOffers().AllOffers.FirstOrDefault(o => o.TradeOfferId == response.TradeOfferId)?.TradeOfferState);
                    Assert.AreEqual(state, tradeOfferWeb.GetTradeOffers(true, true, false, false, false).AllOffers.FirstOrDefault(o => o.TradeOfferId == response.TradeOfferId)?.TradeOfferState);

                }
                AssertOfferState(TradeOfferState.TradeOfferStateNeedsConfirmation, true);
                foreach (var item in sellerSteamGuardAccount.FetchConfirmations())
                {
                    sellerSteamGuardAccount.AcceptConfirmation(item);
                }
                AssertOfferState(TradeOfferState.TradeOfferStateActive, true);
                var buyerOfferSession = new OfferSession(new TradeOfferWebAPI("", buyerSteamWeb), buyerSteamWeb);
                var acceptResponse = buyerOfferSession.Accept(response.TradeOfferId);
                Assert.IsTrue(acceptResponse.Accepted, acceptResponse.TradeError);
                AssertOfferState(TradeOfferState.TradeOfferStateAccepted, false);
            }
            catch (WebException ex)
            {
                var reason = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                Assert.Fail(ex.ToString() + "\r\n" + reason);
            }
        }

        private void AddSellerInventoryItems(TradeOffer offer)
        {
            foreach (var item in sellerInventory.Items.Where(i => sellerInventory.Descriptions[i.Value.DescriptionId].Tradable).Take(5))
                offer.Items.AddMyItem(item.Value.appid, item.Value.contextid, (long)item.Value.assetid);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            var id = Guid.NewGuid();
            var offer = new TradeOfferManager("", buyerSteamWeb).NewOffer(new SteamKit2.SteamID(sellerSteamId));
            var buyerInventory = new GenericInventory(sellerSteamWeb);
            buyerInventory.LoadAsync(570, new long[] { 2 }, new SteamKit2.SteamID(sellerSteamId)).Wait();
            Assert.AreEqual(0, buyerInventory.Errors.Count);
            foreach (var item in buyerInventory.Items)
            {
                if (buyerInventory.Descriptions[item.Value.DescriptionId].Tradable)
                    offer.Items.AddMyItem(item.Value.appid, item.Value.contextid, (long)item.Value.assetid);
            }
            var response = offer.Send();
            foreach (var item in buyerSteamGuardAccount.FetchConfirmations())
            {
                buyerSteamGuardAccount.AcceptConfirmation(item);
            }
            var result = new OfferSession(new TradeOfferWebAPI("", sellerSteamWeb), sellerSteamWeb).Accept(response.TradeOfferId);
            Assert.IsTrue(result.Accepted, result.TradeError);
        }
    }
}
