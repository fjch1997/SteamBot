using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Lloyd.Shared.Extensions;
using Newtonsoft.Json;
using SteamKit2;
using SteamTrade.Exceptions;
using SteamBot.Properties;

namespace SteamTrade
{
    /// <summary>
    /// SteamWeb class to create an API endpoint to the Steam Web.
    /// </summary>
    [Serializable]
    public class SteamWeb : ISteamWeb
    {
        private static Uri steamCommunityUri = new Uri("https://" + SteamCommunityDomain);
        private static Uri storeUri = new Uri("https://store.steampowered.com");
        /// <summary>
        /// Base steam community domain.
        /// </summary>
        public const string SteamCommunityDomain = "steamcommunity.com";
        /// <summary>
        /// Session id of Steam after Login.
        /// </summary>
        [Obsolete("Session ID can be different between store.steampowered.com and steamcommunity.com. Use Cookies.GetCookies(new Uri(\"https://steamcommunity.com/\")).Cast<Cookie>().FirstOrDefault(c => c.Name == \"sessionid\")?.Value to get Session ID for the domain you are working with.")]
        public string SessionId => _cookies?.GetCookies(storeUri).Cast<Cookie>().FirstOrDefault(c => c.Name == "sessionid")?.Value ?? throw new SteamWebNotLoggedInException();

        /// <summary>
        /// Token secure as string. It is generated after the Login.
        /// </summary>
        public string TokenSecure => _cookies?.GetCookies(storeUri).Cast<Cookie>().FirstOrDefault(c => c.Name == "steamLoginSecure")?.Value ?? throw new SteamWebNotLoggedInException();

        /// <summary>
        /// The Accept-Language header when sending all HTTP requests. Default value is determined according to the constructor caller thread's culture.
        /// </summary>
        public string AcceptLanguageHeader { get; set; } = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName == "en" ? $"{Thread.CurrentThread.CurrentCulture},en;q=0.8" : $"{Thread.CurrentThread.CurrentCulture},{Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName};q=0.8,en;q=0.6";

        /// <summary>
        /// CookieContainer to save all cookies during the Login. 
        /// </summary>
        public CookieContainer Cookies
        {
            get { return _cookies ?? (_cookies = new CookieContainer()); }
            set => _cookies = value;
        }
        // ReSharper disable once InconsistentNaming
        private CookieContainer _cookies;

        /// <summary>
        /// This method is using the Request method to return the full http stream from a web request as string.
        /// </summary>
        /// <param name="url">URL of the http request.</param>
        /// <param name="method">Gets the HTTP data transfer method (such as GET, POST, or HEAD) used by the client.</param>
        /// <param name="data">A NameValueCollection including Headers added to the request.</param>
        /// <param name="ajax">A bool to define if the http request is an ajax request.</param>
        /// <param name="referer">Gets information about the URL of the client's previous request that linked to the current URL.</param>
        /// <param name="fetchError">If true, response codes other than HTTP 200 will still be returned, rather than throwing exceptions</param>
        /// <returns>The string of the http return stream.</returns>
        /// <remarks>If you want to know how the request method works, use: <see cref="SteamWeb.Request"/></remarks>
        public string Fetch(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false)
        {
            // Reading the response as stream and read it to the end. After that happened return the result as string.
            using (HttpWebResponse response = Request(url, method, data, ajax, referer, fetchError))
            {
                return response.ReadToEnd();
            }
        }
        /// <summary>
        /// This method is using the Request method to return the full http stream from a web request as string.
        /// </summary>
        /// <param name="url">URL of the http request.</param>
        /// <param name="method">Gets the HTTP data transfer method (such as GET, POST, or HEAD) used by the client.</param>
        /// <param name="data">A NameValueCollection including Headers added to the request.</param>
        /// <param name="ajax">A bool to define if the http request is an ajax request.</param>
        /// <param name="referer">Gets information about the URL of the client's previous request that linked to the current URL.</param>
        /// <param name="fetchError">If true, response codes other than HTTP 200 will still be returned, rather than throwing exceptions</param>
        /// <returns>The string of the http return stream.</returns>
        /// <remarks>If you want to know how the request method works, use: <see cref="SteamWeb.Request"/></remarks>
        public async Task<string> FetchAsync(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false)
        {
            // Reading the response as stream and read it to the end. After that happened return the result as string.
            using (HttpWebResponse response = await RequestAsync(url, method, data, ajax, referer, fetchError))
            {
                return await response.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Custom wrapper for creating a HttpWebRequest, edited for Steam.
        /// </summary>
        /// <param name="url">Gets information about the URL of the current request.</param>
        /// <param name="method">Gets the HTTP data transfer method (such as GET, POST, or HEAD) used by the client.</param>
        /// <param name="data">A NameValueCollection including Headers added to the request.</param>
        /// <param name="ajax">A bool to define if the http request is an ajax request.</param>
        /// <param name="referer">Gets information about the URL of the client's previous request that linked to the current URL.</param>
        /// <param name="fetchError">Return response even if its status code is not 200</param>
        /// <returns>An instance of a HttpWebResponse object.</returns>
        public HttpWebResponse Request(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false)
        {
            PrepareRequest(url, method, data, ajax, referer, out var isGetMethod, out var dataString, out var request);

            // If the request is a GET request return now the response. If not go on. Because then we need to apply data to the request.
            if (isGetMethod || string.IsNullOrEmpty(dataString))
            {
                var httpWebResponse = request.GetResponse() as HttpWebResponse;
                ThrowSteamWebNotLoggedInException(httpWebResponse);
                return httpWebResponse;
            }

            // Write the data to the body for POST and other methods.
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);
            request.ContentLength = dataBytes.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(dataBytes, 0, dataBytes.Length);
            }

            // Get the response and return it.
            try
            {
                var httpWebResponse = request.GetResponse() as HttpWebResponse;
                ThrowSteamWebNotLoggedInException(httpWebResponse);
                return httpWebResponse;
            }
            catch (WebException ex) when (fetchError && ex.Response != null)
            {
                //this is thrown if response code is not 200
                var resp = ex.Response as HttpWebResponse;
                return resp;
            }
        }
        /// <summary>
        /// Custom wrapper for creating a HttpWebRequest, edited for Steam.
        /// </summary>
        /// <param name="url">Gets information about the URL of the current request.</param>
        /// <param name="method">Gets the HTTP data transfer method (such as GET, POST, or HEAD) used by the client.</param>
        /// <param name="data">A NameValueCollection including Headers added to the request.</param>
        /// <param name="ajax">A bool to define if the http request is an ajax request.</param>
        /// <param name="referer">Gets information about the URL of the client's previous request that linked to the current URL.</param>
        /// <param name="fetchError">Return response even if its status code is not 200</param>
        /// <returns>An instance of a HttpWebResponse object.</returns>
        public async Task<HttpWebResponse> RequestAsync(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false)
        {
            PrepareRequest(url, method, data, ajax, referer, out var isGetMethod, out var dataString, out var request);

            // If the request is a GET request return now the response. If not go on. Because then we need to apply data to the request.
            if (isGetMethod || string.IsNullOrEmpty(dataString))
            {
                var httpWebResponse = await request.GetResponseAsync() as HttpWebResponse;
                ThrowSteamWebNotLoggedInException(httpWebResponse);
                return httpWebResponse;
            }

            // Write the data to the body for POST and other methods.
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);
            request.ContentLength = dataBytes.Length;

            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(dataBytes, 0, dataBytes.Length);
            }

            // Get the response and return it.
            try
            {
                var httpWebResponse = await request.GetResponseAsync() as HttpWebResponse;
                ThrowSteamWebNotLoggedInException(httpWebResponse);
                return httpWebResponse;
            }
            catch (WebException ex) when (fetchError && ex.Response != null)
            {
                //this is thrown if response code is not 200
                var resp = ex.Response as HttpWebResponse;
                return resp;
            }
        }
        private static void ThrowSteamWebNotLoggedInException(HttpWebResponse httpWebResponse)
        {
            if (httpWebResponse.StatusCode == HttpStatusCode.Redirect && Uri.TryCreate(httpWebResponse.Headers[HttpResponseHeader.Location], UriKind.Absolute, out var uri) && uri.AbsolutePath.StartsWith("/login/"))
                throw new SteamWebNotLoggedInException();
        }

        private void PrepareRequest(string url, string method, NameValueCollection data, bool ajax, string referer, out bool isGetMethod, out string dataString, out HttpWebRequest request)
        {
            // Append the data to the URL for GET-requests.
            isGetMethod = (method.ToLower() == "get");
            dataString = (data == null ? null : String.Join("&", Array.ConvertAll(data.AllKeys, key =>
// ReSharper disable once UseStringInterpolation
string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(data[key]))
)));

            // Example working with C# 6
            // string dataString = (data == null ? null : String.Join("&", Array.ConvertAll(data.AllKeys, key => $"{HttpUtility.UrlEncode(key)}={HttpUtility.UrlEncode(data[key])}" )));

            // Append the dataString to the url if it is a GET request.
            if (isGetMethod && !string.IsNullOrEmpty(dataString))
            {
                url += (url.Contains("?") ? "&" : "?") + dataString;
            }

            // Setup the request.
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Accept = "application/json, text/javascript;q=0.9, */*;q=0.5";
            request.Headers[HttpRequestHeader.AcceptLanguage] = AcceptLanguageHeader;
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            // request.Host is set automatically.
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
            request.Referer = string.IsNullOrEmpty(referer) ? "https://steamcommunity.com/trade/1" : referer;
            request.Timeout = 50000; // Timeout after 50 seconds.
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Revalidate);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            // If the request is an ajax request we need to add various other Headers, defined below.
            if (ajax)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("X-Prototype-Version", "1.7");
            }

            // Cookies
            request.CookieContainer = _cookies;
        }

        /// <summary>
        /// Executes the login by using the Steam Website.
        /// This Method is not used by Steambot repository, but it could be very helpful if you want to build a own Steambot or want to login into steam services like backpack.tf/csgolounge.com.
        /// </summary>
        /// <param name="username">Your Steam username.</param>
        /// <param name="password">Your Steam password.</param>
        /// <param name="rememberLogin"></param>
        /// <param name="twoFactorCodeCallback">A function that will return the current Steam Guard Mobile Authenticator/two factor code when needed. If this parameter is null when a code is required, an exception will be thrown.</param>
        /// <param name="captchaCallback"></param>
        /// <param name="emailCodeCallback"></param>
        /// <returns>A bool containing a value, if the login was successful.</returns>
        /// <exception cref="ArgumentNullException">One of the callback is null when it's needed.</exception>
        /// <exception cref="SteamWebLoginException">See <see cref="Exception.Message"/> and <see cref="SteamWebLoginException.SteamResult"/> for details.</exception>
        /// <exception cref="CryptographicException">An RSA key was not returned from steam.</exception>
        public SteamResult DoLogin(string username, string password, bool rememberLogin, Func<string> twoFactorCodeCallback, Func<string, string> captchaCallback, Func<string> emailCodeCallback)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            var data = new NameValueCollection { { "username", username } };
            // First get the RSA key with which we will encrypt our password.
            string response = Fetch("https://store.steampowered.com/login/getrsakey/", "POST", data, false);
            GetRsaKey rsaJson = JsonConvert.DeserializeObject<GetRsaKey>(response);

            // Validate, if we could get the rsa key.
            if (!rsaJson.success)
            {
                throw new CryptographicException("Missing RSA key.");
            }

            byte[] encryptedPasswordBytes;
            using (var rsaEncryptor = new RSACryptoServiceProvider())
            {
                var passwordBytes = Encoding.ASCII.GetBytes(password);
                var rsaParameters = rsaEncryptor.ExportParameters(false);
                rsaParameters.Exponent = HexStringToByteArray(rsaJson.publickey_exp);
                rsaParameters.Modulus = HexStringToByteArray(rsaJson.publickey_mod);
                rsaEncryptor.ImportParameters(rsaParameters);
                encryptedPasswordBytes = rsaEncryptor.Encrypt(passwordBytes, false);
            }
            string encryptedBase64Password = Convert.ToBase64String(encryptedPasswordBytes);

            SteamResult loginJson = null;
            string cookieHeader;
            string steamGuardText = "";
            string steamGuardId = "";

            // Do this while we need a captcha or need email authentification. Probably you have misstyped the captcha or the SteamGaurd code if this comes multiple times.
            do
            {
                bool captcha = loginJson != null && loginJson.captcha_needed;
                bool steamGuard = loginJson != null && loginJson.emailauth_needed;
                bool twoFactor = loginJson != null && loginJson.requires_twofactor;

                string time = Uri.EscapeDataString(rsaJson.timestamp);

                string capGid = string.Empty;
                // Response does not need to send if captcha is needed or not.
                // ReSharper disable once MergeSequentialChecks
                if (loginJson != null && loginJson.captcha_gid != null)
                {
                    capGid = Uri.EscapeDataString(loginJson.captcha_gid);
                }

                data = new NameValueCollection { { "password", encryptedBase64Password }, { "username", username } };

                // Captcha Check.
                string capText = "";
                if (captcha && captchaCallback != null)
                    capText = captchaCallback("https://store.steampowered.com/public/captcha.php?gid=" + loginJson.captcha_gid);
                else if (captcha)
                    throw new ArgumentNullException(nameof(captchaCallback));

                data.Add("captchagid", captcha ? capGid : "");
                data.Add("captcha_text", captcha ? capText : "");
                // Captcha end.
                // Added Header for two factor code.
                if (twoFactor && twoFactorCodeCallback != null)
                {
                    var twoFactorCode = twoFactorCodeCallback();
                    if (string.IsNullOrWhiteSpace(twoFactorCode))
                        throw new ArgumentException(Resources.TwoFactorCodeIsInvalid);
                    data.Add("twofactorcode", twoFactorCode);
                }
                else if (twoFactor)
                    throw new ArgumentNullException(nameof(twoFactorCodeCallback));

                // Added Header for remember login. It can also set to true.
                data.Add("remember_login", rememberLogin.ToString());

                // SteamGuard check. If SteamGuard is enabled you need to enter it. Care probably you need to wait 7 days to trade.
                // For further information about SteamGuard see: https://support.steampowered.com/kb_article.php?ref=4020-ALZM-5519&l=english.
                if (steamGuard && emailCodeCallback != null)
                {
                    steamGuardText = emailCodeCallback();
                    steamGuardId = loginJson.emailsteamid;
                    //loginfriendlyname is no longer required.
                    data.Add("loginfriendlyname", "");
                }
                else if (steamGuard)
                {
                    throw new ArgumentNullException(nameof(emailCodeCallback));
                }

                data.Add("emailauth", steamGuardText);
                data.Add("emailsteamid", steamGuardId);
                // SteamGuard end.

                // Added unixTimestamp. It is included in the request normally.
                var unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                // Added three "0"'s because Steam has a weird unix timestamp interpretation.
                data.Add("donotcache", unixTimestamp + "000");

                data.Add("rsatimestamp", time);

                // Sending the actual login.
                using (HttpWebResponse webResponse = Request("https://store.steampowered.com/login/dologin/", "POST", data, false))
                {
                    var stream = webResponse.GetResponseStream();
                    using (StreamReader reader = new StreamReader(stream ?? throw new InvalidOperationException()))
                    {
                        string json = reader.ReadToEnd();
                        loginJson = JsonConvert.DeserializeObject<SteamResult>(json);
                        cookieHeader = webResponse.Headers[HttpResponseHeader.SetCookie];
                    }
                }
            } while (loginJson.captcha_needed || loginJson.emailauth_needed || loginJson.requires_twofactor);

            // If the login was successful, we need to enter the cookies to steam.
            if (loginJson.success)
            {
                _cookies = new CookieContainer();
                _cookies.SetCookies(steamCommunityUri, cookieHeader);
                _cookies.SetCookies(storeUri, cookieHeader);
                _cookies.SetCookies(new Uri("https://help.steampowered.com/"), cookieHeader);
                SubmitCookies(_cookies);
                return loginJson;
            }
            throw new SteamWebLoginException(loginJson);
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            int hexLen = hex.Length;
            byte[] ret = new byte[hexLen / 2];
            for (int i = 0; i < hexLen; i += 2)
            {
                ret[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return ret;
        }

        ///<summary>
        /// Authenticate using SteamKit2 and ISteamUserAuth. 
        /// This does the same as SteamWeb.DoLogin(), but without contacting the Steam Website.
        /// </summary>
        /// <remarks>Should this one doesnt work anymore, use <see cref="SteamWeb.DoLogin"/></remarks>
        /// <param name="myUniqueId">Id what you get to login.</param>
        /// <param name="client">An instance of a SteamClient.</param>
        /// <param name="myLoginKey">Login Key of your account.</param>
        /// <returns>A bool, which is true if the login was successful.</returns>
        public bool Authenticate(string myUniqueId, SteamClient client, string myLoginKey)
        {
            _cookies = new CookieContainer();

            using (dynamic userAuth = WebAPI.GetInterface("ISteamUserAuth"))
            {
                // Generate an AES session key.
                var sessionKey = CryptoHelper.GenerateRandomBlock(32);

                // rsa encrypt it with the public key for the universe we're on
                byte[] cryptedSessionKey;
                using (RSACrypto rsa = new RSACrypto(KeyDictionary.GetPublicKey(client.Universe)))
                {
                    cryptedSessionKey = rsa.Encrypt(sessionKey);
                }

                byte[] loginKey = new byte[20];
                Array.Copy(Encoding.ASCII.GetBytes(myLoginKey), loginKey, myLoginKey.Length);

                // AES encrypt the loginkey with our session key.
                byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);

                KeyValue authResult;

                // Get the Authentification Result.
                try
                {
                    authResult = userAuth.AuthenticateUser(
                        steamid: client.SteamID.ConvertToUInt64(),
                        sessionkey: cryptedSessionKey,
                        encrypted_loginkey: cryptedLoginKey,
                        method: "POST",
                        secure: true
                        );
                }
                catch (Exception)
                {
                    return false;
                }

                // Adding cookies to the cookie container.
                foreach (var uri in new[] { steamCommunityUri, storeUri, new Uri("https://help.steampowered.com/") })
                {
                    _cookies.SetCookies(uri, $"steamLogin={authResult["token"].AsString()}; path=/; HttpOnly");
                    _cookies.SetCookies(uri, $"steamLoginSecure={authResult["tokensecure"].AsString()}; path=/; secure; HttpOnly");
                    _cookies.SetCookies(uri, $"sessionid={Convert.ToBase64String(Encoding.UTF8.GetBytes(myUniqueId))}; path=/");
                }
                SubmitCookies(_cookies);
                return true;
            }
        }

        /// <summary>
        /// Authenticate using an array of cookies from a browser or whatever source, without contacting the server.
        /// It is recommended that you call <see cref="VerifyCookies"/> after calling this method to ensure that the cookies are valid.
        /// </summary>
        /// <param name="cookies">An array of cookies from a browser or whatever source. Must contain sessionid, steamLogin, steamLoginSecure</param>
        /// <exception cref="ArgumentException">One of the required cookies(steamLogin, steamLoginSecure, sessionid) is missing.</exception>
        public void Authenticate(IEnumerable<Cookie> cookies)
        {
            var cookieContainer = new CookieContainer();
            string tokenSecure = null;
            string sessionId = null;
            foreach (var cookie in cookies)
            {
                if (cookie.Name == "sessionid")
                    sessionId = cookie.Value;
                else if (cookie.Name == "steamLoginSecure")
                    tokenSecure = cookie.Value;
                cookieContainer.Add(cookie);
            }
            if (tokenSecure == null)
                throw new ArgumentException("Cookie with name \"steamLoginSecure\" is not found.");
            if (sessionId == null)
                throw new ArgumentException("Cookie with name \"sessionid\" is not found.");
            _cookies = cookieContainer;
        }

        /// <summary>
        /// Helper method to verify our precious cookies.
        /// </summary>
        /// <returns>true if cookies are correct; false otherwise</returns>
        public bool VerifyCookies()
        {
            using (HttpWebResponse response = Request("https://store.steampowered.com/", "HEAD"))
            {
                return response.Cookies["steamLogin"] == null || !response.Cookies["steamLogin"].Value.Equals("deleted");
            }
        }

        /// <summary>
        /// Method to submit cookies to Steam after Login.
        /// </summary>
        /// <param name="cookies">Cookiecontainer which contains cookies after the login to Steam.</param>
        static void SubmitCookies(CookieContainer cookies)
        {
            HttpWebRequest w = WebRequest.Create("https://store.steampowered.com/") as HttpWebRequest;

            // Check, if the request is null.
            if (w == null)
            {
                return;
            }
            w.Method = "POST";
            w.ContentType = "application/x-www-form-urlencoded";
            w.CookieContainer = cookies;
            // Added content-length because it is required.
            w.ContentLength = 0;
            var response = w.GetResponse();
            try
            {
                var cookieHeader = response.Headers[HttpResponseHeader.SetCookie];
                if (cookieHeader != null)
                {
                    cookies.SetCookies(steamCommunityUri, cookieHeader);
                    cookies.SetCookies(storeUri, cookieHeader);
                    cookies.SetCookies(new Uri("https://help.steampowered.com/"), cookieHeader);
                }
            }
            finally
            {
                response.Close();
            }
        }

        /// <summary>
        /// Method to allow all certificates.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="policyErrors">One or more errors associated with the remote certificate.</param>
        /// <returns>Always true to accept all certificates.</returns>
        public bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }
        /// <summary>
        /// Gets Steam ID from Cookies collection.
        /// </summary>
        /// <returns>64 bit Steam ID.</returns>
        public long GetSteamId64()
        {
            var steamLoginCookie = Cookies.GetCookies(storeUri).Cast<Cookie>().FirstOrDefault(c => c.Name == "steamLogin") ?? Cookies.GetCookies(storeUri).Cast<Cookie>().FirstOrDefault(c => c.Name == "steamLoginSecure");
            string value;
            int index;
            long steamId64;
            if (steamLoginCookie == null)
            {
                var machineAuthCookie = Cookies.GetCookies(storeUri).Cast<Cookie>().FirstOrDefault(c => c.Name == "steamMachineAuth");
                if (machineAuthCookie == null) return default;
                value = machineAuthCookie.Value;
                index = value.IndexOf('=');
                return long.TryParse(value.Substring(0, index), out steamId64) ? steamId64 : throw new SteamWebNotLoggedInException();
            }
            value = steamLoginCookie.Value;
            index = value.IndexOf('%');
            return long.TryParse(value.Substring(0, index), out steamId64) ? steamId64 : throw new SteamWebNotLoggedInException();
        }
    }

    // JSON Classes
    // These classes are used to deserialize response strings from the login:
    // Example of a return string: {"success":true,"publickey_mod":"XXXX87144BF5B2CABFEC24E35655FDC5E438D6064E47D33A3531F3AAB195813E316A5D8AAB1D8A71CB7F031F801200377E8399C475C99CBAFAEFF5B24AE3CF64BXXXXB2FDBA3BC3974D6DCF1E760F8030AB5AB40FA8B9D193A8BEB43AA7260482EAD5CE429F718ED06B0C1F7E063FE81D4234188657DB40EEA4FAF8615111CD3E14CAF536CXXXX3C104BE060A342BF0C9F53BAAA2A4747E43349FF0518F8920664F6E6F09FE41D8D79C884F8FD037276DED0D1D1D540A2C2B6639CF97FF5180E3E75224EXXXX56AAA864EEBF9E8B35B80E25B405597219BFD90F3AD9765D81D148B9500F12519F1F96828C12AEF77D948D0DC9FDAF8C7CC73527ADE7C7F0FF33","publickey_exp":"010001","timestamp":"241590850000","steamid":"7656119824534XXXX","token_gid":"c35434c0c07XXXX"}

    /// <summary>
    /// Class to Deserialize the json response strings of the getResKey request. See: <see cref="SteamWeb.DoLogin"/>
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class GetRsaKey
    {
        public bool success { get; set; }

        public string publickey_mod { get; set; }

        public string publickey_exp { get; set; }

        public string timestamp { get; set; }
    }

    // Examples:
    // For not accepted SteamResult:
    // {"success":false,"requires_twofactor":false,"message":"","emailauth_needed":true,"emaildomain":"gmail.com","emailsteamid":"7656119824534XXXX"}
    // For accepted SteamResult:
    // {"success":true,"requires_twofactor":false,"login_complete":true,"transfer_url":"https:\/\/store.steampowered.com\/login\/transfer","transfer_parameters":{"steamid":"7656119824534XXXX","token":"XXXXC39589A9XXXXCB60D651EFXXXX85578AXXXX","auth":"XXXXf1d9683eXXXXc76bdc1888XXXX29","remember_login":false,"webcookie":"XXXX4C33779A4265EXXXXC039D3512DA6B889D2F","token_secure":"XXXX63F43AA2CXXXXC703441A312E1B14AC2XXXX"}}

    /// <summary>
    /// Class to Deserialize the json response strings after the login. See: <see cref="SteamWeb.DoLogin"/>
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SteamResult
    {
        public bool success { get; set; }

        public string message { get; set; }

        public bool captcha_needed { get; set; }

        public string captcha_gid { get; set; }

        public bool emailauth_needed { get; set; }

        public string emailsteamid { get; set; }

        public bool requires_twofactor { get; set; }

        [JsonProperty("login_complete")]
        public bool LoginComplete { get; set; }

        [JsonProperty("transfer_urls")]
        public string[] TransferUrls { get; set; }

        [JsonProperty("transfer_parameters")]
        public TransferParameters TransferParameters { get; set; }
    }

    public class TransferParameters
    {

        [JsonProperty("steamid")]
        public string Steamid { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("auth")]
        public string Auth { get; set; }

        [JsonProperty("remember_login")]
        public bool RememberLogin { get; set; }

        [JsonProperty("webcookie")]
        public string Webcookie { get; set; }

        [JsonProperty("token_secure")]
        public string TokenSecure { get; set; }
    }


    [Serializable]
    public class SteamWebLoginException : Exception
    {
        public SteamWebLoginException(SteamResult steamResult) : base(steamResult.message) { SteamResult = steamResult; }
        public SteamResult SteamResult { get; set; }
        protected SteamWebLoginException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
