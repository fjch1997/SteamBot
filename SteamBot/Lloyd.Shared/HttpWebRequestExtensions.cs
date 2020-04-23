using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Lloyd.Shared.Extensions
{
    internal static class HttpWebRequestExtensions
    {
        public static string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
        public static HttpWebResponse PostUrlEncoded(this HttpWebRequest request, CookieContainer cookieContainer, NameValueCollection data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            request.CookieContainer = cookieContainer;
            return PostUrlEncoded(request, data, ajax, referer, fetchError);
        }
        public static HttpWebResponse PostUrlEncoded(this HttpWebRequest request, string cookies, NameValueCollection data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            request.Headers[HttpRequestHeader.Cookie] = cookies;
            return PostUrlEncoded(request, data, ajax, referer, fetchError);
        }
        public static HttpWebResponse PostUrlEncoded(this HttpWebRequest request, CookieContainer cookieContainer, IEnumerable<KeyValuePair<string, string>> data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            request.CookieContainer = cookieContainer;
            return PostUrlEncoded(request, data, ajax, referer, fetchError);
        }
        public static HttpWebResponse PostUrlEncoded(this HttpWebRequest request, string cookies, IEnumerable<KeyValuePair<string, string>> data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            request.Headers[HttpRequestHeader.Cookie] = cookies;
            return PostUrlEncoded(request, data, ajax, referer, fetchError);
        }
        public static HttpWebResponse PostUrlEncoded(this HttpWebRequest request, NameValueCollection data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            var url = request.RequestUri.ToString();
            // Append the data to the URL for GET-requests.
            string dataString = (data == null ? null : String.Join("&", Array.ConvertAll(data.AllKeys, key =>
                // ReSharper disable once UseStringInterpolation
                string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(data[key]))
            )));
            return PostUrlEncodedInternal(request, dataString, ajax, referer, fetchError);
        }

        public static HttpWebResponse PostUrlEncoded(this HttpWebRequest request, IEnumerable<KeyValuePair<string, string>> data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            var url = request.RequestUri.ToString();
            // Append the data to the URL for GET-requests.
            string dataString = (data == null ? null : String.Join("&", data.Select(p => $"{HttpUtility.UrlEncode(p.Key)}={HttpUtility.UrlEncode(p.Value)}")));
            return PostUrlEncodedInternal(request, dataString, ajax, referer, fetchError);
        }
        public static Task<HttpWebResponse> PostUrlEncodedAsync(this HttpWebRequest request, CookieContainer cookieContainer, NameValueCollection data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            request.CookieContainer = cookieContainer;
            return PostUrlEncodedAsync(request, data, ajax, referer, fetchError);
        }
        public static Task<HttpWebResponse> PostUrlEncodedAsync(this HttpWebRequest request, string cookies, NameValueCollection data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            request.Headers[HttpRequestHeader.Cookie] = cookies;
            return PostUrlEncodedAsync(request, data, ajax, referer, fetchError);
        }
        public static Task<HttpWebResponse> PostUrlEncodedAsync(this HttpWebRequest request, CookieContainer cookieContainer, IEnumerable<KeyValuePair<string, string>> data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            request.CookieContainer = cookieContainer;
            return PostUrlEncodedAsync(request, data, ajax, referer, fetchError);
        }
        public static Task<HttpWebResponse> PostUrlEncodedAsync(this HttpWebRequest request, string cookies, IEnumerable<KeyValuePair<string, string>> data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            request.Headers[HttpRequestHeader.Cookie] = cookies;
            return PostUrlEncodedAsync(request, data, ajax, referer, fetchError);
        }
        public static Task<HttpWebResponse> PostUrlEncodedAsync(this HttpWebRequest request, NameValueCollection data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            var url = request.RequestUri.ToString();
            // Append the data to the URL for GET-requests.
            string dataString = (data == null ? null : String.Join("&", Array.ConvertAll(data.AllKeys, key =>
                // ReSharper disable once UseStringInterpolation
                string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(data[key]))
            )));
            return PostUrlEncodedInternalAsync(request, dataString, ajax, referer, fetchError);
        }

        public static Task<HttpWebResponse> PostUrlEncodedAsync(this HttpWebRequest request, IEnumerable<KeyValuePair<string, string>> data, bool ajax = true, string referer = "", bool fetchError = false)
        {
            var url = request.RequestUri.ToString();
            // Append the data to the URL for GET-requests.
            string dataString = (data == null ? null : String.Join("&", data.Select(p => $"{HttpUtility.UrlEncode(p.Key)}={HttpUtility.UrlEncode(p.Value)}")));
            return PostUrlEncodedInternalAsync(request, dataString, ajax, referer, fetchError);
        }

        private static HttpWebResponse PostUrlEncodedInternal(HttpWebRequest request, string dataString, bool ajax, string referer, bool fetchError)
        {
            byte[] dataBytes = PostUrlEncodedInternalPrepare(request, dataString, ajax, referer);

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(dataBytes, 0, dataBytes.Length);
            }

            // Get the response and return it.
            try
            {
                return request.GetResponse() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                //this is thrown if response code is not 200
                if (fetchError)
                {
                    var resp = ex.Response as HttpWebResponse;
                    if (resp != null)
                    {
                        return resp;
                    }
                }
                throw;
            }
        }

        private static async Task<HttpWebResponse> PostUrlEncodedInternalAsync(HttpWebRequest request, string dataString, bool ajax, string referer, bool fetchError)
        {
            byte[] dataBytes = PostUrlEncodedInternalPrepare(request, dataString, ajax, referer);

            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(dataBytes, 0, dataBytes.Length);
            }

            // Get the response and return it.
            try
            {
                return await request.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                //this is thrown if response code is not 200
                if (fetchError)
                {
                    var resp = ex.Response as HttpWebResponse;
                    if (resp != null)
                    {
                        return resp;
                    }
                }
                throw;
            }
        }

        private static byte[] PostUrlEncodedInternalPrepare(HttpWebRequest request, string dataString, bool ajax, string referer)
        {
            // Setup the request.
            request.Method = HttpMethod.Post.Method;
            //request.Accept = "application/json, text/javascript;q=0.9, */*;q=0.5";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            // request.Host is set automatically.
            if (!string.IsNullOrEmpty(UserAgent))
                request.UserAgent = UserAgent;
            if (!string.IsNullOrEmpty(referer))
                request.Referer = referer;
            request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Revalidate);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            // If the request is an ajax request we need to add various other Headers, defined below.
            if (ajax)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("X-Prototype-Version", "1.7");
            }

            // Write the data to the body for POST and other methods.
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);
            request.ContentLength = dataBytes.Length;
            return dataBytes;
        }

        public static string ReadToEnd(this WebResponse response)
        {
            if (!(response is HttpWebResponse))
                throw new NotSupportedException($"Only an {nameof(HttpWebResponse)} can be used with {nameof(HttpWebRequestExtensions)}.{nameof(ReadToEnd)}({nameof(WebResponse)}).");
            return ReadToEnd((HttpWebResponse)response);
        }

        public static string ReadToEnd(this HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, (string.IsNullOrEmpty(response.CharacterSet)) ? Encoding.UTF8 : Encoding.GetEncoding(response.CharacterSet)))
            {
                return reader.ReadToEnd();
            }
        }

        public static string ReadToEnd(this HttpWebRequest request)
        {
            using (var response = request.GetResponse() as HttpWebResponse)
            {
                return ReadToEnd(response);
            }
        }

        public static Task<string> ReadToEndAsync(this WebResponse response)
        {
            if (!(response is HttpWebResponse))
                throw new NotSupportedException($"Only an {nameof(HttpWebResponse)} can be used with {nameof(HttpWebRequestExtensions)}.{nameof(ReadToEnd)}({nameof(WebResponse)}).");
            return ReadToEndAsync((HttpWebResponse)response);
        }

        public static async Task<string> ReadToEndAsync(this HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, (string.IsNullOrEmpty(response.CharacterSet)) ? Encoding.UTF8 : Encoding.GetEncoding(response.CharacterSet)))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public static async Task<string> ReadToEndAsync(this HttpWebRequest request)
        {
            using (var response = request.GetResponse() as HttpWebResponse)
            {
                return await ReadToEndAsync(response);
            }
        }
    }
}
