using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamTrade.Exceptions
{
    public class TradeJsonException : TradeException
    {
        public TradeJsonException(string originalServerResponse)
        {
            OriginalServerResponse = originalServerResponse;
        }

        public TradeJsonException(string message, string originalServerResponse) : base(message)
        {
            OriginalServerResponse = originalServerResponse;
        }

        public TradeJsonException(string message, Exception inner, string originalServerResponse) : base(message, inner)
        {
            OriginalServerResponse = originalServerResponse;
        }

        public string OriginalServerResponse { get; set; }
    }
}
