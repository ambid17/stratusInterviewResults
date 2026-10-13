using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using StratusFieldLink.Model;
using StratusFieldLink.Net;

namespace StratusFieldLink.Tests.PlayMode
{
    public class PublishFlowTests
    {
        private GameObject _host;
        private PublishService _publisher;
        private StubFieldLinkTransport _transport;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("PublishServiceHost");
            _publisher = _host.AddComponent<PublishService>();
            _transport = new StubFieldLinkTransport();
            _publisher.Transport = _transport;
            _publisher.RetryDelaySeconds = 0f;
            _publisher.AutoDrain = false;
            _publisher.Queue.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // The service persists to the real key; do not leave test sketches in it.
            PlayerPrefs.DeleteKey(PublishQueue.DefaultStorageKey);

            if (_host != null)
            {
                Object.Destroy(_host);
            }
        }

        private static FieldSketchDto Sketch()
        {
            var sketch = FieldSketchDto.NewDraft("PKG-CHW-L02", "CREW-04");
            sketch.nodes.Add(new SketchNodeDto(new Vector3(0f, 10f, 0f)));
            sketch.nodes.Add(new SketchNodeDto(new Vector3(36f, 10f, 0f)));
            return sketch;
        }

        [UnityTest]
        public IEnumerator Publish_WhenServerAccepts_ReportsSuccess()
        {
            _transport.NextResponse = StubFieldLinkTransport.Status(201, "{\"id\":\"SK-1\"}");

            yield return _publisher.PublishRoutine(Sketch());

            Assert.IsTrue(_publisher.LastResult.IsSuccess, _publisher.LastResult.Message);
            Assert.AreEqual(1, _transport.CallCount);
        }

        [UnityTest]
        public IEnumerator Publish_SendsTheSketchAsJsonToThePublishEndpoint()
        {
            var sketch = Sketch();

            yield return _publisher.PublishRoutine(sketch);

            StringAssert.EndsWith(PublishService.PublishPath, _transport.SentUrls[0]);
            StringAssert.Contains(sketch.id, _transport.SentBodies[0]);
            StringAssert.Contains("\"packageId\"", _transport.SentBodies[0]);
            StringAssert.Contains("\"nodes\"", _transport.SentBodies[0]);
        }

        [UnityTest]
        public IEnumerator Publish_StampsTheClientMeasuredLength()
        {
            var sketch = Sketch();

            yield return _publisher.PublishRoutine(sketch);

            Assert.AreEqual(36f, sketch.clientLengthFeet, 0.001f);
        }

        [UnityTest]
        public IEnumerator Publish_SketchThatFailsLocalValidation_NeverHitsTheNetwork()
        {
            var sketch = FieldSketchDto.NewDraft("PKG-CHW-L02", "CREW-04");
            sketch.nodes.Add(new SketchNodeDto(new Vector3(0f, 10f, 0f)));

            yield return _publisher.PublishRoutine(sketch);

            Assert.AreEqual(PublishOutcome.FailedLocalValidation, _publisher.LastResult.Outcome);
            Assert.AreEqual(0, _transport.CallCount);
        }

        [UnityTest]
        public IEnumerator Publish_WhenOffline_QueuesTheSketchWithoutRetrying()
        {
            _transport.NextResponse = StubFieldLinkTransport.Offline();
            var sketch = Sketch();

            yield return _publisher.PublishRoutine(sketch);

            Assert.AreEqual(PublishOutcome.QueuedOffline, _publisher.LastResult.Outcome);
            Assert.AreEqual(1, _transport.CallCount);
            Assert.AreEqual(1, _publisher.Queue.Count);
            Assert.AreEqual(sketch.id, _publisher.Queue.Peek().id);
            Assert.IsFalse(_publisher.IsBusy);
        }

        [UnityTest]
        public IEnumerator Publish_WhenOffline_PersistsTheQueue()
        {
            _transport.NextResponse = StubFieldLinkTransport.Offline();

            yield return _publisher.PublishRoutine(Sketch());

            var reloaded = new PublishQueue();
            reloaded.Load();
            Assert.AreEqual(1, reloaded.Count);
        }

        [UnityTest]
        public IEnumerator Publish_WhenOffline_QueuesASnapshotNotTheLiveSketch()
        {
            _transport.NextResponse = StubFieldLinkTransport.Offline();
            var sketch = Sketch();

            yield return _publisher.PublishRoutine(sketch);
            sketch.nodes.Add(new SketchNodeDto(new Vector3(36f, 10f, 20f)));

            Assert.AreEqual(2, _publisher.Queue.Peek().nodes.Count);
        }

        [UnityTest]
        public IEnumerator Publish_WhenServerSays400_ReportsTheServersReasonAndDoesNotQueue()
        {
            _transport.NextResponse = StubFieldLinkTransport.Status(400, "{\"error\":\"Run is too short.\"}");

            yield return _publisher.PublishRoutine(Sketch());

            Assert.AreEqual(PublishOutcome.RejectedByServer, _publisher.LastResult.Outcome);
            Assert.AreEqual(400, _publisher.LastResult.StatusCode);
            StringAssert.Contains("Run is too short.", _publisher.LastResult.Message);
            Assert.AreEqual(1, _transport.CallCount);
            Assert.AreEqual(0, _publisher.Queue.Count);
        }

        [UnityTest]
        public IEnumerator Publish_When4xxBodyIsNotJson_StillReportsARejection()
        {
            _transport.NextResponse = StubFieldLinkTransport.Status(403, "<html>Forbidden</html>");

            yield return _publisher.PublishRoutine(Sketch());

            Assert.AreEqual(PublishOutcome.RejectedByServer, _publisher.LastResult.Outcome);
            StringAssert.Contains("403", _publisher.LastResult.Message);
            Assert.AreEqual(0, _publisher.Queue.Count);
        }

        [UnityTest]
        public IEnumerator Publish_WhenIdAlreadyHoldsDifferentContent_ReportsConflictAndDoesNotQueue()
        {
            _transport.NextResponse = StubFieldLinkTransport.Status(409, "{\"error\":\"already published\"}");

            yield return _publisher.PublishRoutine(Sketch());

            Assert.AreEqual(PublishOutcome.RejectedByServer, _publisher.LastResult.Outcome);
            Assert.AreEqual(409, _publisher.LastResult.StatusCode);
            Assert.AreEqual(1, _transport.CallCount);
            Assert.AreEqual(0, _publisher.Queue.Count);
        }

        [UnityTest]
        public IEnumerator Publish_SameSketchTwice_SecondIsStillASuccess()
        {
            var sketch = Sketch();
            _transport.ScriptedResponses.Enqueue(StubFieldLinkTransport.Status(201, "{}"));
            _transport.ScriptedResponses.Enqueue(StubFieldLinkTransport.Status(200, "{}"));

            yield return _publisher.PublishRoutine(sketch);
            yield return _publisher.PublishRoutine(sketch);

            Assert.IsTrue(_publisher.LastResult.IsSuccess, _publisher.LastResult.Message);
            StringAssert.Contains("already published", _publisher.LastResult.Message);
        }

        [UnityTest]
        public IEnumerator Publish_When5xxThenAccepted_RetriesAndSucceeds()
        {
            _transport.ScriptedResponses.Enqueue(StubFieldLinkTransport.Status(503, ""));
            _transport.ScriptedResponses.Enqueue(StubFieldLinkTransport.Status(201, "{}"));

            yield return _publisher.PublishRoutine(Sketch());

            Assert.IsTrue(_publisher.LastResult.IsSuccess, _publisher.LastResult.Message);
            Assert.AreEqual(2, _transport.CallCount);
            Assert.AreEqual(0, _publisher.Queue.Count);
        }

        [UnityTest]
        public IEnumerator Publish_When5xxPersists_GivesUpAfterMaxAttemptsAndQueues()
        {
            _transport.NextResponse = StubFieldLinkTransport.Status(503, "");

            yield return _publisher.PublishRoutine(Sketch());

            Assert.AreEqual(PublishOutcome.QueuedOffline, _publisher.LastResult.Outcome);
            Assert.AreEqual(_publisher.MaxAttempts, _transport.CallCount);
            Assert.AreEqual(1, _publisher.Queue.Count);
        }

        [UnityTest]
        public IEnumerator Drain_WhenBackOnline_SendsEverythingAndEmptiesTheQueue()
        {
            _transport.NextResponse = StubFieldLinkTransport.Offline();
            var first = Sketch();
            var second = Sketch();
            yield return _publisher.PublishRoutine(first);
            yield return _publisher.PublishRoutine(second);

            _transport.NextResponse = StubFieldLinkTransport.Status(201, "{}");
            yield return _publisher.DrainQueue();

            Assert.AreEqual(0, _publisher.Queue.Count);
            Assert.AreEqual(4, _transport.CallCount);
            StringAssert.Contains(first.id, _transport.SentBodies[2]);
            StringAssert.Contains(second.id, _transport.SentBodies[3]);
            Assert.IsTrue(_publisher.LastResult.IsSuccess, _publisher.LastResult.Message);
            Assert.IsFalse(_publisher.IsBusy);

            var reloaded = new PublishQueue();
            reloaded.Load();
            Assert.AreEqual(0, reloaded.Count);
        }

        [UnityTest]
        public IEnumerator Drain_WhenStillOffline_StopsAtTheFirstSketchAndKeepsThemAll()
        {
            _transport.NextResponse = StubFieldLinkTransport.Offline();
            yield return _publisher.PublishRoutine(Sketch());
            yield return _publisher.PublishRoutine(Sketch());

            yield return _publisher.DrainQueue();

            Assert.AreEqual(3, _transport.CallCount);
            Assert.AreEqual(2, _publisher.Queue.Count);
            Assert.AreEqual(PublishOutcome.QueuedOffline, _publisher.LastResult.Outcome);
        }

        [UnityTest]
        public IEnumerator Drain_WhenServerRejectsOne_DropsItTellsTheForemanAndCarriesOn()
        {
            _transport.NextResponse = StubFieldLinkTransport.Offline();
            var bad = Sketch();
            yield return _publisher.PublishRoutine(bad);
            yield return _publisher.PublishRoutine(Sketch());

            var rejections = new List<PublishResult>();
            _publisher.PublishCompleted += r =>
            {
                if (r.Outcome == PublishOutcome.RejectedByServer)
                {
                    rejections.Add(r);
                }
            };

            _transport.ScriptedResponses.Enqueue(StubFieldLinkTransport.Status(400, "{\"error\":\"Run is too short.\"}"));
            _transport.ScriptedResponses.Enqueue(StubFieldLinkTransport.Status(201, "{}"));
            yield return _publisher.DrainQueue();

            Assert.AreEqual(0, _publisher.Queue.Count);
            Assert.IsNotEmpty(rejections);
            StringAssert.Contains(bad.id, rejections[0].Message);
            StringAssert.Contains("Run is too short.", _publisher.LastResult.Message);
        }
    }
}
