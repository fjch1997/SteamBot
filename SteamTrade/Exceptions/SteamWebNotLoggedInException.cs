using SteamTrade.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamTrade.Exceptions
{

    [Serializable]
    public class SteamWebNotLoggedInException : Exception
    {
        public SteamWebNotLoggedInException() : base(Resources.SteamWebNotLoggedInExceptionMessage) { }
        public SteamWebNotLoggedInException(string message) : base(message) { }
        public SteamWebNotLoggedInException(string message, Exception inner) : base(message, inner) { }
        protected SteamWebNotLoggedInException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
