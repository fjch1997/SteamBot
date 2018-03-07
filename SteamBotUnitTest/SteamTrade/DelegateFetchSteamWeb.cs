using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamKit2;
using SteamTrade;

namespace SteamBotUnitTest.SteamTrade
{

    [JsonObject(MemberSerialization = MemberSerialization.Fields)]
    class DelegateFetchSteamWeb : ISteamWeb
    {
        [NonSerialized]
        private readonly Func<string, Task<string>> func;

        public DelegateFetchSteamWeb(Func<Task<string>> func)
        {
            this.func = s => (func ?? throw new ArgumentNullException(nameof(func)))();
        }
        public DelegateFetchSteamWeb(Func<string, Task<string>> func)
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
            return func(url).Result;
        }

        public Task<string> FetchAsync(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false)
        {
            return func(url);
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
