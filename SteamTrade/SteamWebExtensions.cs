using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamTrade
{
    public static class SteamWebExtensions
    {
        public static long GetSteamId64(this ISteamWeb steamWeb)
        {
            var steamLoginCookie = steamWeb.Cookies.GetCookies(new Uri("https://steamcommunity.com")).Cast<Cookie>().FirstOrDefault(c => c.Name == "steamLogin");
            if (steamLoginCookie == null)
                return default(long);
            var value = steamLoginCookie.Value;
            var index = value.IndexOf('%');
            return long.TryParse(value.Substring(0, index), out var steamId64) ? steamId64 : default(long);
        }
    }
}
