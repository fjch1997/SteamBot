using NUnit.Framework;
using SteamTrade;
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
using System.IO;
using SteamBotUnitTest.Properties;

namespace SteamBotUnitTest.SteamTrade
{
    [TestFixture]
    public class GenericInventory2Tests
    {
        [Test]
        public void LoadTest()
        {
            var genericInventory2 = new GenericInventory2(new DelegateFetchSteamWeb(() =>
            {
                using (var reader = new StreamReader(new MemoryStream(Resources.SampleInventory)))
                {
                    return reader.ReadToEndAsync();
                }
            }), 76561198058183411UL, 570U, 2U);
            genericInventory2.Wait();
            Assert.AreEqual(36, genericInventory2.DescriptionsRaw.Count);
            Assert.AreEqual(48, genericInventory2.Items.Count);
            var item = genericInventory2.GetDescription(1454279429U, 1454279430U);
            Assert.AreEqual("封装的礼物", item.MarketName);
            Assert.AreEqual("Wrapped Gift", item.MarketHashName);
        }

        class DelegateFetchSteamWeb : ISteamWeb
        {
            private Func<Task<string>> func;

            public DelegateFetchSteamWeb(Func<Task<string>> func)
            {
                this.func = func ?? throw new ArgumentNullException(nameof(func));
            }

            public string AcceptLanguageHeader { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public CookieContainer Cookies => throw new NotImplementedException();

            public string SessionId => throw new NotImplementedException();

            public string Token => throw new NotImplementedException();

            public string TokenSecure => throw new NotImplementedException();

            public void Authenticate(IEnumerable<Cookie> cookies)
            {
                throw new NotImplementedException();
            }

            public bool Authenticate(string myUniqueId, SteamClient client, string myLoginKey)
            {
                throw new NotImplementedException();
            }

            public SteamResult DoLogin(string username, string password, bool rememberLogin, Func<string> twoFactorCodeCallback, Func<string, string> captchaCallback, Func<string> emailCodeCallback)
            {
                throw new NotImplementedException();
            }

            public string Fetch(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false)
            {
                return func().Result;
            }

            public Task<string> FetchAsync(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false)
            {
                return func();
            }

            public HttpWebResponse Request(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false)
            {
                throw new NotImplementedException();
            }

            public Task<HttpWebResponse> RequestAsync(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false)
            {
                throw new NotImplementedException();
            }

            public bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
            {
                throw new NotImplementedException();
            }

            public bool VerifyCookies()
            {
                throw new NotImplementedException();
            }
        }
    }
}
