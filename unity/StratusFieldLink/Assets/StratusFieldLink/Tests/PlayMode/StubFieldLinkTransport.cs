using System;
using System.Collections;
using System.Collections.Generic;
using StratusFieldLink.Net;

namespace StratusFieldLink.Tests.PlayMode
{
    /// <summary>
    /// Records what the client tried to send and replays a canned answer.
    /// Use this instead of standing up the API in a PlayMode test.
    /// </summary>
    public sealed class StubFieldLinkTransport : IFieldLinkTransport
    {
        public readonly List<string> SentUrls = new List<string>();
        public readonly List<string> SentBodies = new List<string>();

        public TransportResponse NextResponse = new TransportResponse
        {
            StatusCode = 201,
            Body = "{\"id\":\"SK-TEST\"}",
            IsNetworkError = false
        };

        /// <summary>Queue responses to model a failure then a retry.</summary>
        public readonly Queue<TransportResponse> ScriptedResponses = new Queue<TransportResponse>();

        public int CallCount
        {
            get { return SentUrls.Count; }
        }

        public IEnumerator PostJson(string url, string json, Action<TransportResponse> onComplete)
        {
            SentUrls.Add(url);
            SentBodies.Add(json);

            // One frame, so the caller behaves the way it would against a real request.
            yield return null;

            var response = ScriptedResponses.Count > 0 ? ScriptedResponses.Dequeue() : NextResponse;

            if (onComplete != null)
            {
                onComplete(response);
            }
        }

        public static TransportResponse Offline()
        {
            return TransportResponse.NetworkFailure("Cannot connect to destination host");
        }

        public static TransportResponse Status(long code, string body)
        {
            return new TransportResponse { StatusCode = code, Body = body, IsNetworkError = false };
        }
    }
}
