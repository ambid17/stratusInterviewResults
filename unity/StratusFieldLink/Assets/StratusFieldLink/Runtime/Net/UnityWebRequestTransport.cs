using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace StratusFieldLink.Net
{
    public sealed class UnityWebRequestTransport : IFieldLinkTransport
    {
        private readonly int _timeoutSeconds;

        public UnityWebRequestTransport(int timeoutSeconds = 10)
        {
            _timeoutSeconds = timeoutSeconds;
        }

        public IEnumerator PostJson(string url, string json, Action<TransportResponse> onComplete)
        {
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                var payload = Encoding.UTF8.GetBytes(json ?? string.Empty);
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                request.timeout = _timeoutSeconds;

                yield return request.SendWebRequest();

                TransportResponse response;
                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    response = TransportResponse.NetworkFailure(request.error);
                }
                else
                {
                    response = new TransportResponse
                    {
                        IsNetworkError = false,
                        StatusCode = request.responseCode,
                        Body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty,
                        Error = request.error
                    };
                }

                if (onComplete != null)
                {
                    onComplete(response);
                }
            }
        }
    }
}
