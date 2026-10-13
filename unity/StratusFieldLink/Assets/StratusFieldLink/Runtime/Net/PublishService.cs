using System;
using System.Collections;
using UnityEngine;
using StratusFieldLink.Config;
using StratusFieldLink.Geometry;
using StratusFieldLink.Model;

namespace StratusFieldLink.Net
{
    public enum PublishOutcome
    {
        None = 0,
        Succeeded = 1,
        RejectedByServer = 2,
        QueuedOffline = 3,
        FailedLocalValidation = 4
    }

    public struct PublishResult
    {
        public PublishOutcome Outcome;
        public string Message;
        public long StatusCode;

        public bool IsSuccess
        {
            get { return Outcome == PublishOutcome.Succeeded; }
        }
    }

    /// <summary>
    /// Owns the client-to-cloud path for a sketch: validate, send, and decide what
    /// happens when the send does not land.
    ///
    /// The sketch id is client-generated and the server treats a repeat of the same
    /// id + content as a no-op (200), so resending is always safe. That gives:
    ///   2xx                  done; drop any queued copy
    ///   no signal / timeout  queue it, the drain sends it later
    ///   5xx, 408, 429        retry a few times with backoff, then queue it
    ///   other 4xx            the sketch is wrong; tell the foreman, never resend
    /// </summary>
    public sealed class PublishService : MonoBehaviour
    {
        public const string PublishPath = "/api/sketches";

        public const float MaxDrainIntervalSeconds = 300f;

        /// <summary>Swap this in tests. Defaults to a real UnityWebRequest transport.</summary>
        public IFieldLinkTransport Transport { get; set; }

        public PublishQueue Queue { get; private set; }

        public PublishResult LastResult { get; private set; }

        public bool IsBusy { get; private set; }

        /// <summary>Sends per sketch before a transient server failure is handed to the queue.</summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>Wait before the first retry; doubles on each one after.</summary>
        public float RetryDelaySeconds { get; set; } = 1f;

        /// <summary>When false nothing drains the queue unless DrainQueue is called.</summary>
        public bool AutoDrain { get; set; } = true;

        /// <summary>Gap between drain attempts; doubles while they keep failing, up to MaxDrainIntervalSeconds.</summary>
        public float DrainIntervalSeconds { get; set; } = 15f;

        public event Action<PublishResult> PublishCompleted;

        private float _nextDrainAt;
        private float _drainInterval;

        private void Awake()
        {
            if (Transport == null)
            {
                Transport = new UnityWebRequestTransport();
            }

            if (Queue == null)
            {
                Queue = new PublishQueue();
                Queue.Load();
            }
        }

        private void Update()
        {
            if (!AutoDrain || IsBusy || Queue == null || Queue.Count == 0)
            {
                return;
            }

            if (Time.realtimeSinceStartup < _nextDrainAt)
            {
                return;
            }

            StartCoroutine(DrainQueue());
        }

        public Coroutine Publish(FieldSketchDto sketch)
        {
            return StartCoroutine(PublishRoutine(sketch));
        }

        public IEnumerator PublishRoutine(FieldSketchDto sketch)
        {
            IsBusy = true;

            var validation = SketchValidation.Validate(sketch);
            if (!validation.IsValid)
            {
                Complete(new PublishResult
                {
                    Outcome = PublishOutcome.FailedLocalValidation,
                    Message = validation.Message
                });
                yield break;
            }

            var result = default(PublishResult);
            yield return SendRoutine(sketch, r => result = r);

            if (result.Outcome == PublishOutcome.QueuedOffline)
            {
                // Queue a copy: the foreman can keep editing this sketch, and resending
                // an edited run under an id that may already have landed is a 409.
                Queue.Enqueue(JsonUtility.FromJson<FieldSketchDto>(JsonUtility.ToJson(sketch)));
                Queue.Save();
                ScheduleDrain(DrainIntervalSeconds);
            }
            else if (result.IsSuccess)
            {
                if (Queue.Remove(sketch.id))
                {
                    Queue.Save();
                }

                // Signal is evidently back; send whatever else is waiting.
                ScheduleDrain(0f);
            }

            Complete(result);
        }

        /// <summary>
        /// Sends queued sketches oldest first. Stops at the first one that still
        /// cannot reach the server; drops any the server refuses outright so one bad
        /// sketch cannot block the rest.
        /// </summary>
        public IEnumerator DrainQueue()
        {
            if (IsBusy || Queue == null || Queue.Count == 0)
            {
                yield break;
            }

            IsBusy = true;

            var sent = 0;
            var rejected = 0;
            var lastRejection = default(PublishResult);
            var stalled = default(PublishResult);

            while (Queue.Count > 0)
            {
                var sketch = Queue.Peek();

                var result = default(PublishResult);
                yield return SendRoutine(sketch, r => result = r);

                if (result.Outcome == PublishOutcome.QueuedOffline)
                {
                    stalled = result;
                    break;
                }

                Queue.Remove(sketch.id);
                Queue.Save();

                if (result.IsSuccess)
                {
                    sent++;
                }
                else
                {
                    rejected++;
                    lastRejection = result;
                    Notify(result);
                }
            }

            var isStalled = stalled.Outcome == PublishOutcome.QueuedOffline;
            _drainInterval = isStalled
                ? Mathf.Min(Mathf.Max(_drainInterval, DrainIntervalSeconds) * 2f, MaxDrainIntervalSeconds)
                : DrainIntervalSeconds;
            _nextDrainAt = Time.realtimeSinceStartup + _drainInterval;

            var message = sent == 0 && isStalled
                ? "Still can't reach the server."
                : "Sent " + sent + " queued sketch" + (sent == 1 ? "" : "es") + ".";
            if (rejected > 0)
            {
                message += " " + lastRejection.Message;
            }

            if (isStalled)
            {
                message += " " + Queue.Count + " still pending.";
            }

            Complete(new PublishResult
            {
                Outcome = rejected > 0
                    ? PublishOutcome.RejectedByServer
                    : isStalled ? PublishOutcome.QueuedOffline : PublishOutcome.Succeeded,
                Message = message,
                StatusCode = rejected > 0 ? lastRejection.StatusCode : stalled.StatusCode
            });
        }

        /// <summary>
        /// One sketch, up to MaxAttempts sends. Reports QueuedOffline when the sketch
        /// should be kept and tried again later; the caller owns the queue.
        /// </summary>
        private IEnumerator SendRoutine(FieldSketchDto sketch, Action<PublishResult> onDone)
        {
            sketch.clientLengthFeet = SketchGeometry.TotalLengthFeet(sketch.nodes);
            sketch.clientVersion = FieldLinkConfig.ClientVersion;

            var json = JsonUtility.ToJson(sketch);
            var url = FieldLinkConfig.Url(PublishPath);

            var response = default(TransportResponse);
            var delay = RetryDelaySeconds;

            for (var attempt = 1; ; attempt++)
            {
                yield return Transport.PostJson(url, json, r => response = r);

                if (!IsTransient(response) || attempt >= MaxAttempts)
                {
                    break;
                }

                Debug.LogWarning("[FieldLink] publish " + sketch.id + " got " + response.StatusCode +
                                 ", retrying (" + attempt + "/" + MaxAttempts + ")");

                if (delay > 0f)
                {
                    yield return new WaitForSecondsRealtime(delay);
                    delay *= 2f;
                }
            }

            onDone(Classify(sketch, response));
        }

        // A server that answered but may answer differently next time. No signal is
        // not in here on purpose: retrying a 10s timeout in a basement just freezes
        // the HUD, so that goes straight to the queue.
        private static bool IsTransient(TransportResponse response)
        {
            if (response.IsNetworkError || response.IsSuccess)
            {
                return false;
            }

            var code = response.StatusCode;
            return code < 400 || code >= 500 || code == 408 || code == 429;
        }

        private static PublishResult Classify(FieldSketchDto sketch, TransportResponse response)
        {
            var result = new PublishResult { StatusCode = response.StatusCode };

            if (response.IsSuccess)
            {
                result.Outcome = PublishOutcome.Succeeded;
                result.Message = response.StatusCode == 200
                    ? sketch.id + " was already published."
                    : "Published " + sketch.id;
                return result;
            }

            if (response.IsNetworkError)
            {
                result.Outcome = PublishOutcome.QueuedOffline;
                result.Message = "No signal. Saved " + sketch.id + "; will send when back online.";
                return result;
            }

            if (IsTransient(response))
            {
                result.Outcome = PublishOutcome.QueuedOffline;
                result.Message = "Server unavailable (" + response.StatusCode + "). Saved " + sketch.id + "; will retry.";
                return result;
            }

            Debug.LogWarning("[FieldLink] publish " + sketch.id + " rejected: " + response.StatusCode + " " + response.Body);

            result.Outcome = PublishOutcome.RejectedByServer;
            if (response.StatusCode == 409)
            {
                result.Message = sketch.id + " was already published with different content. Start a new sketch.";
            }
            else
            {
                var error = ServerError(response.Body);
                result.Message = string.IsNullOrEmpty(error)
                    ? "Server refused " + sketch.id + " (" + response.StatusCode + ")."
                    : "Rejected " + sketch.id + ": " + error;
            }

            return result;
        }

        private static string ServerError(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            try
            {
                var parsed = JsonUtility.FromJson<ApiErrorDto>(body);
                return parsed != null ? parsed.error : null;
            }
            catch (ArgumentException)
            {
                // A proxy or captive portal answered with something that is not our JSON.
                return null;
            }
        }

        private void ScheduleDrain(float delaySeconds)
        {
            _drainInterval = DrainIntervalSeconds;
            _nextDrainAt = Time.realtimeSinceStartup + delaySeconds;
        }

        private void Complete(PublishResult result)
        {
            LastResult = result;
            IsBusy = false;
            Notify(result);
        }

        private void Notify(PublishResult result)
        {
            var handler = PublishCompleted;
            if (handler != null)
            {
                handler(result);
            }
        }
    }
}
