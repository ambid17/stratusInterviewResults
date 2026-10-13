using UnityEngine;
using StratusFieldLink.Net;
using StratusFieldLink.UI;

namespace StratusFieldLink.Sketch
{
    /// <summary>
    /// Builds the field-sketch scene in code.
    ///
    /// No .unity file is checked in on purpose: a scene asset is a merge-hostile
    /// blob and this starter would rather you could read the setup. The editor
    /// menu item (Stratus > Build Field Sketch Scene) calls this and saves the
    /// result; PlayMode tests call the same method so what they exercise is what
    /// ships.
    /// </summary>
    public static class SketchSceneFactory
    {
        public sealed class SceneHandle
        {
            public GameObject Root;
            public Camera Camera;
            public SketchController Controller;
            public PublishService Publisher;
            public FieldHud Hud;
        }

        public static SceneHandle Build()
        {
            var root = new GameObject("FieldSketch");

            var cameraObject = new GameObject("Sketch Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.position = new Vector3(0f, 70f, -55f);
            cameraObject.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.11f);
            camera.tag = "MainCamera";

            var lightObject = new GameObject("Sun");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, 140f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;

            var slab = GameObject.CreatePrimitive(PrimitiveType.Plane);
            slab.name = "Slab";
            slab.transform.SetParent(root.transform, false);
            slab.transform.localScale = new Vector3(12f, 1f, 12f);

            var sketchObject = new GameObject("Sketch");
            sketchObject.transform.SetParent(root.transform, false);
            var line = sketchObject.AddComponent<LineRenderer>();
            var lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null)
            {
                line.material = new Material(lineShader);
            }

            line.startColor = new Color(0.31f, 0.64f, 1f);
            line.endColor = new Color(0.31f, 0.64f, 1f);
            line.widthMultiplier = 0.35f;
            line.positionCount = 0;
            var controller = sketchObject.AddComponent<SketchController>();

            var publisher = sketchObject.AddComponent<PublishService>();

            var hudObject = new GameObject("Field HUD");
            hudObject.transform.SetParent(root.transform, false);
            var hud = hudObject.AddComponent<FieldHud>();
            hud.Bind(controller, publisher);

            return new SceneHandle
            {
                Root = root,
                Camera = camera,
                Controller = controller,
                Publisher = publisher,
                Hud = hud
            };
        }
    }
}
