using UnityEngine;

namespace StratusFieldLink.Config
{
    /// <summary>
    /// Where the field app points. In a real build this comes down from the
    /// tenant config at login; here it is a constant you can override in the
    /// editor before you press Play.
    /// </summary>
    public static class FieldLinkConfig
    {
        public const string DefaultApiBaseUrl = "http://localhost:5072";

        private static string _apiBaseUrl = DefaultApiBaseUrl;

        public static string ApiBaseUrl
        {
            get { return _apiBaseUrl; }
            set { _apiBaseUrl = string.IsNullOrEmpty(value) ? DefaultApiBaseUrl : value.TrimEnd('/'); }
        }

        public const string ClientVersion = "field-1.4.2";

        /// <summary>Crew the tablet is signed in as. The auth stub just echoes it back.</summary>
        public const string CrewId = "CREW-04";

        public const string DefaultPackageId = "PKG-CHW-L02";

        public static string Url(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return ApiBaseUrl;
            }

            return path.StartsWith("/") ? ApiBaseUrl + path : ApiBaseUrl + "/" + path;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyCommandLineOverride()
        {
            var fromEnv = System.Environment.GetEnvironmentVariable("STRATUS_API_BASE_URL");
            if (!string.IsNullOrEmpty(fromEnv))
            {
                ApiBaseUrl = fromEnv;
                Debug.Log("[FieldLink] API base url overridden to " + ApiBaseUrl);
            }
        }
    }
}
