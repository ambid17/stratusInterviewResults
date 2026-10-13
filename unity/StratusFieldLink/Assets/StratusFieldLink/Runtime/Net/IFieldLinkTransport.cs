using System;
using System.Collections;

namespace StratusFieldLink.Net
{
    public struct TransportResponse
    {
        public long StatusCode;
        public string Body;
        public string Error;

        /// <summary>True when the request never reached the server (no signal, DNS, timeout).</summary>
        public bool IsNetworkError;

        public bool IsSuccess
        {
            get { return !IsNetworkError && StatusCode >= 200 && StatusCode < 300; }
        }

        public static TransportResponse NetworkFailure(string error)
        {
            return new TransportResponse { IsNetworkError = true, Error = error, StatusCode = 0, Body = string.Empty };
        }
    }

    /// <summary>
    /// Everything that talks to Stratus goes through this. It exists so PlayMode
    /// tests can run the publish path without a live server.
    /// </summary>
    public interface IFieldLinkTransport
    {
        IEnumerator PostJson(string url, string json, Action<TransportResponse> onComplete);
    }
}
