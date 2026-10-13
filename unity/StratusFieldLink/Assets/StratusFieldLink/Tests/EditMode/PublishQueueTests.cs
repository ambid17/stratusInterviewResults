using NUnit.Framework;
using UnityEngine;
using StratusFieldLink.Model;
using StratusFieldLink.Net;

namespace StratusFieldLink.Tests.EditMode
{
    public class PublishQueueTests
    {
        private const string TestKey = "stratus.fieldlink.tests.pending";

        [SetUp]
        [TearDown]
        public void ClearStorage()
        {
            PlayerPrefs.DeleteKey(TestKey);
        }

        private static FieldSketchDto Sketch(string id)
        {
            var sketch = FieldSketchDto.NewDraft("PKG-CHW-L02", "CREW-04");
            sketch.id = id;
            sketch.nodes.Add(new SketchNodeDto(new Vector3(0f, 10f, 0f)));
            sketch.nodes.Add(new SketchNodeDto(new Vector3(12f, 10f, 0f)));
            return sketch;
        }

        [Test]
        public void Enqueue_AddsInOrder()
        {
            var queue = new PublishQueue(TestKey);
            queue.Enqueue(Sketch("SK-1"));
            queue.Enqueue(Sketch("SK-2"));

            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual("SK-1", queue.Peek().id);
        }

        [Test]
        public void Enqueue_SameIdTwice_ReplacesRatherThanDuplicates()
        {
            var queue = new PublishQueue(TestKey);
            queue.Enqueue(Sketch("SK-1"));
            queue.Enqueue(Sketch("SK-1"));

            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void Remove_DropsTheSketch()
        {
            var queue = new PublishQueue(TestKey);
            queue.Enqueue(Sketch("SK-1"));

            Assert.IsTrue(queue.Remove("SK-1"));
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void SaveThenLoad_SurvivesAnAppRestart()
        {
            var queue = new PublishQueue(TestKey);
            queue.Enqueue(Sketch("SK-1"));
            queue.Enqueue(Sketch("SK-2"));
            queue.Save();

            var reloaded = new PublishQueue(TestKey);
            reloaded.Load();

            Assert.AreEqual(2, reloaded.Count);
            Assert.AreEqual("SK-1", reloaded.Peek().id);
            Assert.AreEqual(2, reloaded.Peek().nodes.Count);
        }
    }
}
