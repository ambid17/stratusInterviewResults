using UnityEngine;
using StratusFieldLink.Geometry;
using StratusFieldLink.Net;
using StratusFieldLink.Sketch;

namespace StratusFieldLink.UI
{
    /// <summary>
    /// The whole field UI, such as it is. IMGUI on purpose — this starter keeps
    /// zero binary UI assets so every bit of the setup is readable in a diff.
    /// </summary>
    public sealed class FieldHud : MonoBehaviour
    {
        [SerializeField] private SketchController controller;
        [SerializeField] private PublishService publisher;

        private string _statusLine = "Tap the slab to place nodes.";
        private GUIStyle _panelStyle;

        public string StatusLine
        {
            get { return _statusLine; }
        }

        public void Bind(SketchController sketchController, PublishService publishService)
        {
            controller = sketchController;
            publisher = publishService;

            if (publisher == null)
            {
                return;
            }

            publisher.PublishCompleted -= OnPublishCompleted;
            publisher.PublishCompleted += OnPublishCompleted;
        }

        private void OnEnable()
        {
            if (publisher != null)
            {
                publisher.PublishCompleted -= OnPublishCompleted;
                publisher.PublishCompleted += OnPublishCompleted;
            }
        }

        private void OnDisable()
        {
            if (publisher != null)
            {
                publisher.PublishCompleted -= OnPublishCompleted;
            }
        }

        private void OnPublishCompleted(PublishResult result)
        {
            _statusLine = result.Message;
        }

        public void RequestPublish()
        {
            if (controller == null || publisher == null || publisher.IsBusy)
            {
                return;
            }

            _statusLine = "Publishing…";
            publisher.Publish(controller.Current);
        }

        private void OnGUI()
        {
            if (controller == null)
            {
                return;
            }

            if (_panelStyle == null)
            {
                _panelStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(12, 12, 10, 10) };
            }

            GUILayout.BeginArea(new Rect(14f, 14f, 340f, 360f), _panelStyle);

            GUILayout.Label("Stratus Field Link  " + StratusFieldLink.Config.FieldLinkConfig.ClientVersion);
            GUILayout.Space(6f);

            var sketch = controller.Current;
            GUILayout.Label("Sketch: " + (sketch != null ? sketch.id : "—"));
            GUILayout.Label("Package: " + (sketch != null ? sketch.packageId : "—"));
            GUILayout.Label("Nodes: " + controller.NodeCount);
            GUILayout.Label("Length: " + controller.CurrentLengthFeet.ToString("0.0") + " ft");
            GUILayout.Label("Working elevation: " + controller.WorkingElevationFeet.ToString("0.0") + " ft");

            if (sketch != null)
            {
                GUILayout.Label("Rise: " + SketchGeometry.ElevationChangeFeet(sketch.nodes).ToString("0.0") + " ft");
            }

            if (publisher != null)
            {
                GUILayout.Label("Pending offline: " + (publisher.Queue != null ? publisher.Queue.Count : 0));
            }

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Publish"))
            {
                RequestPublish();
            }

            if (GUILayout.Button("New"))
            {
                controller.StartNewSketch();
                _statusLine = "Started a new sketch.";
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label(_statusLine);
            GUILayout.Label("click place · backspace undo · R up · F down · N new", GUI.skin.label);

            GUILayout.EndArea();
        }
    }
}
