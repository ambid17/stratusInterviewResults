using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using StratusFieldLink.Sketch;

namespace StratusFieldLink.Tests.PlayMode
{
    /// <summary>
    /// Builds the real scene the way the editor menu item does, then drives it.
    /// </summary>
    public class SketchSceneTests
    {
        private SketchSceneFactory.SceneHandle _scene;

        [SetUp]
        public void SetUp()
        {
            _scene = SketchSceneFactory.Build();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene == null || _scene.Root == null)
            {
                return;
            }

            var line = _scene.Root.GetComponentInChildren<LineRenderer>();
            if (line != null && line.material != null)
            {
                Object.Destroy(line.material);
            }

            Object.Destroy(_scene.Root);
        }

        [UnityTest]
        public IEnumerator Scene_BuildsWithTheThingsTheFieldAppNeeds()
        {
            yield return null;

            Assert.IsNotNull(_scene.Camera, "no camera");
            Assert.IsNotNull(_scene.Controller, "no sketch controller");
            Assert.IsNotNull(_scene.Publisher, "no publish service");
            Assert.IsNotNull(_scene.Hud, "no HUD");
            Assert.IsNotNull(_scene.Controller.Current, "controller did not start a draft sketch");
        }

        [UnityTest]
        public IEnumerator AddingNodes_GrowsTheRunAndItsMeasuredLength()
        {
            yield return null;

            _scene.Controller.AddNode(new Vector3(0f, 10f, 0f));
            _scene.Controller.AddNode(new Vector3(45f, 10f, 0f));

            Assert.AreEqual(2, _scene.Controller.NodeCount);
            Assert.AreEqual(45f, _scene.Controller.CurrentLengthFeet, 0.01f);
        }

        [UnityTest]
        public IEnumerator Undo_RemovesTheLastNode()
        {
            yield return null;

            _scene.Controller.AddNode(new Vector3(0f, 10f, 0f));
            _scene.Controller.AddNode(new Vector3(20f, 10f, 0f));
            _scene.Controller.RemoveLastNode();

            Assert.AreEqual(1, _scene.Controller.NodeCount);
        }

        [UnityTest]
        public IEnumerator StartNewSketch_ResetsTheRun()
        {
            yield return null;

            _scene.Controller.AddNode(new Vector3(0f, 10f, 0f));
            _scene.Controller.AddNode(new Vector3(20f, 10f, 0f));

            var firstId = _scene.Controller.Current.id;
            _scene.Controller.StartNewSketch();

            Assert.AreEqual(0, _scene.Controller.NodeCount);
            Assert.AreNotEqual(firstId, _scene.Controller.Current.id);
        }
    }
}
