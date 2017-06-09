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
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            Assert.AreEqual(48, genericInventory2.GetItemCount());
            var item = genericInventory2.GetDescription<ItemDescription>(1454279429U, 1454279430U);
            Assert.AreEqual("封装的礼物", item.MarketName);
            Assert.AreEqual("Wrapped Gift", item.MarketHashName);
        }

        [Test]
        public void SerializationTest()
        {
            var genericInventory2 = new GenericInventory2(new DelegateFetchSteamWeb(() =>
            {
                using (var reader = new StreamReader(new MemoryStream(Resources.SampleInventory)))
                {
                    return reader.ReadToEndAsync();
                }
            }), 76561198058183411UL, 570U, 2U);
            genericInventory2.Wait();

            var serializer = new JsonSerializer() { ContractResolver = new DefaultContractResolver() { IgnoreSerializableAttribute = false } };
            var tempFileName = Path.GetTempFileName();
            using (var stream = File.Create(tempFileName))
            using (var writer = new StreamWriter(stream))
            {
                serializer.Serialize(writer, genericInventory2);
            }
            using (var stream = File.OpenRead(tempFileName))
            using (var streamReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(streamReader))
            {
                genericInventory2 = serializer.Deserialize<GenericInventory2>(reader);
                var item = genericInventory2.GetDescription<ItemDescription>(genericInventory2.GetItem(10918549176UL));
                Assert.AreEqual("冷血刺客副手匕首", item.Name);
                Assert.AreEqual("冷血刺客副手匕首", item.MarketName);
                Assert.AreEqual("Dagger of the Frozen Blood Off-Hand", item.MarketHashName);
            }
            File.Delete(tempFileName);
        }
        
        [JsonObject(MemberSerialization = MemberSerialization.Fields)]
        class DelegateFetchSteamWeb : ISteamWeb
        {
            [NonSerialized]
            private Func<Task<string>> func;

            public DelegateFetchSteamWeb(Func<Task<string>> func)
            {
                this.func = func ?? throw new ArgumentNullException(nameof(func));
            }

            public string AcceptLanguageHeader { get; set; } = "en-US,en;q=0.5";

            public CookieContainer Cookies { get; set; } = new CookieContainer();

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
