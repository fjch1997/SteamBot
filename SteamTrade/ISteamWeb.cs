using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using SteamKit2;

namespace SteamTrade
{
    /// <summary>
    /// SteamWeb class to create an API endpoint to the Steam Web.
    /// </summary>
    public interface ISteamWeb
    {
        /// <summary>
        /// The Accept-Language header when sending all HTTP requests. Default value is determined according to the constructor caller thread's culture.
        /// </summary>
        string AcceptLanguageHeader { get; set; }
        /// <summary>
        /// CookieContainer to save all cookies during the Login. 
        /// </summary>
        CookieContainer Cookies { get; }
        /// <summary>
        /// Session id of Steam after Login.
        /// </summary>  
        string SessionId { get; }
        /// <summary>
        /// Token of steam. Generated after login.
        /// </summary>
        string Token { get; }
        /// <summary>
        /// Token secure as string. It is generated after the Login.
        /// </summary>
        string TokenSecure { get; }

        /// <summary>
        /// Authenticate using an array of cookies from a browser or whatever source, without contacting the server.
        /// It is recommended that you call <see cref="VerifyCookies"/> after calling this method to ensure that the cookies are valid.
        /// </summary>
        /// <param name="cookies">An array of cookies from a browser or whatever source. Must contain sessionid, steamLogin, steamLoginSecure</param>
        /// <exception cref="ArgumentException">One of the required cookies(steamLogin, steamLoginSecure, sessionid) is missing.</exception>
        void Authenticate(IEnumerable<Cookie> cookies);
        ///<summary>
        /// Authenticate using SteamKit2 and ISteamUserAuth. 
        /// This does the same as SteamWeb.DoLogin(), but without contacting the Steam Website.
        /// </summary>
        /// <remarks>Should this one doesnt work anymore, use <see cref="ISteamWeb.DoLogin"/></remarks>
        /// <param name="myUniqueId">Id what you get to login.</param>
        /// <param name="client">An instance of a SteamClient.</param>
        /// <param name="myLoginKey">Login Key of your account.</param>
        /// <returns>A bool, which is true if the login was successful.</returns>
        bool Authenticate(string myUniqueId, SteamClient client, string myLoginKey);
        /// <summary>
        /// Executes the login by using the Steam Website.
        /// This Method is not used by Steambot repository, but it could be very helpful if you want to build a own Steambot or want to login into steam services like backpack.tf/csgolounge.com.
        /// </summary>
        /// <param name="username">Your Steam username.</param>
        /// <param name="password">Your Steam password.</param>
        /// <param name="twoFactorCodeCallback">A function that will return the current Steam Guard Mobile Authenticator/two factor code when needed. If this parameter is null when a code is required, an exception will be thrown.</param>
        /// <returns>A bool containing a value, if the login was successful.</returns>
        /// <exception cref="ArgumentNullException">One of the callback is null when it's needed.</exception>
        /// <exception cref="SteamWebLoginException">See <see cref="Exception.Message"/> and <see cref="SteamWebLoginException.SteamResult"/> for details.</exception>
        /// <exception cref="CryptographicException">An RSA key was not returned from steam.</exception>
        SteamResult DoLogin(string username, string password, bool rememberLogin, Func<string> twoFactorCodeCallback, Func<string, string> captchaCallback, Func<string> emailCodeCallback);
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
        /// <remarks>If you want to know how the request method works, use: <see cref="ISteamWeb.Request"/></remarks>
        string Fetch(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false);
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
        /// <remarks>If you want to know how the request method works, use: <see cref="ISteamWeb.Request"/></remarks>
        Task<string> FetchAsync(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false);
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
        HttpWebResponse Request(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false);
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
        Task<HttpWebResponse> RequestAsync(string url, string method, NameValueCollection data = null, bool ajax = true, string referer = "", bool fetchError = false);
        /// <summary>
        /// Method to allow all certificates.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="policyErrors">One or more errors associated with the remote certificate.</param>
        /// <returns>Always true to accept all certificates.</returns>
        bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors);
        /// <summary>
        /// Helper method to verify our precious cookies.
        /// </summary>
        /// <returns>true if cookies are correct; false otherwise</returns>
        bool VerifyCookies();
    }
}