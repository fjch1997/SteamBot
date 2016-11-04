using Newtonsoft.Json;
using NUnit.Framework;
using SteamTrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SteamBotUnitTest.SteamTrade
{
    [TestFixture]
    class SteamWebTests
    {
        [Test]
        public void LoginTest()
        {
            var username = "fjch1997";
            var password = "SteamTradeTest33";
            var winAuthBackup = "otpauth://totp/Steam:fjch1997?secret=WER5ITPMZQCRY24JKZETDODRTLMUQYVV&digits=5&issuer=Steam&deviceid=android%3a65af6cf3-492d-6cf3-6cf3-6cf3492d4d93&data=%7b%22shared_secret%22%3a%22sSPUTezMBRxriVZJMbhxmtlIYrU%3d%22%2c%22serial_number%22%3a%2212621599193736060956%22%2c%22revocation_code%22%3a%22R21525%22%2c%22uri%22%3a%22otpauth%3a%2f%2ftotp%2fSteam%3afjch1997%3fsecret%3dWER5ITPMZQCRY24JKZETDODRTLMUQYVV%26issuer%3dSteam%22%2c%22server_time%22%3a1469221491%2c%22account_name%22%3a%22fjch1997%22%2c%22token_gid%22%3a%22b055b5217b7d7ec%22%2c%22identity_secret%22%3a%22vetk8P7mmp41VZp7vri59g98Y9c%3d%22%2c%22secret_1%22%3a%22BGaqM5ENk1zJz92Rpa3Y59fhhjI%3d%22%2c%22status%22%3a1%2c%22device_id%22%3a%22android%3a65af6cf3-492d-6cf3-6cf3-6cf3492d4d93%22%2c%22fully_enrolled%22%3atrue%2c%22Session%22%3a%7b%22SessionID%22%3a%2222c7858da94870046a25b003%22%2c%22SteamLogin%22%3a%2276561198319214599%257C%257C974DB281FA7A3984D0A06EF91D7A08F32BB30823%22%2c%22SteamLoginSecure%22%3a%2276561198319214599%257C%257CA65E3AD622399D42AD96C72ECCF70CB5C995D528%22%2c%22WebCookie%22%3anull%2c%22OAuthToken%22%3a%220dd9bfb878ad5b516b2b27e31d625b5e%22%2c%22SteamID%22%3a76561198319214599%7d%7d";
            //Parse winAuthBackup
            SteamAuth.SteamGuardAccount steamGuardAccount;
            var uri = new Uri(winAuthBackup);
            var queryString = HttpUtility.ParseQueryString(uri.Query);
            steamGuardAccount = JsonConvert.DeserializeObject<SteamAuth.SteamGuardAccount>(queryString["data"]);
            steamGuardAccount.DeviceID = queryString["deviceid"];
            //SteamGuardAccount login
            var userLogin = new SteamAuth.UserLogin(username, password);
            userLogin.TwoFactorCode = steamGuardAccount.GenerateSteamGuardCode();
            var loginResult = userLogin.DoLogin();
            Assert.AreEqual(loginResult, SteamAuth.LoginResult.LoginOkay);
            steamGuardAccount.Session = userLogin.Session;
            new SteamWeb().DoLogin(username, password, true, () => steamGuardAccount.GenerateSteamGuardCode(), null, null);
        }
    }
}
