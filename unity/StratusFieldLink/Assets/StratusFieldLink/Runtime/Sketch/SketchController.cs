using System.Collections.Generic;
using UnityEngine;
using StratusFieldLink.Config;
using StratusFieldLink.Geometry;
using StratusFieldLink.Model;

namespace StratusFieldLink.Sketch
{
    /// <summary>
    /// Turns taps on the slab into nodes on a run, and draws what the foreman has
    /// sketched so far.
    ///
    /// Controls:
    ///   Left click        place a node at the current working elevation
    ///   Backspace         remove the last node
    ///   R                 nudge the working elevation up   (start a riser)
    ///   F                 nudge the working elevation down (start a drop)
    ///   N                 start a new sketch
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class SketchController : MonoBehaviour
    {
        public const float ElevationStepFeet = 2f;

        [SerializeField] private Camera sketchCamera;
        [SerializeField] private float slabElevationFeet = 10f;
        [SerializeField] private float nodeMarkerDiameter = 0.9f;

        private readonly List<GameObject> _markers = new List<GameObject>();
        private LineRenderer _line;

        public FieldSketchDto Current { get; private set; }

        /// <summary>Elevation the next node lands at. Moving this is how a riser gets sketched.</summary>
        public float WorkingElevationFeet { get; private set; }

        public int NodeCount
        {
            get { return Current == null || Current.nodes == null ? 0 : Current.nodes.Count; }
        }

        public float CurrentLengthFeet
        {
            get { return Current == null ? 0f : SketchGeometry.TotalLengthFeet(Current.nodes); }
        }

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.widthMultiplier = 0.35f;
            _line.positionCount = 0;

            if (sketchCamera == null)
            {
                sketchCamera = Camera.main;
            }

            WorkingElevationFeet = slabElevationFeet;
            StartNewSketch();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                StartNewSketch();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                RemoveLastNode();
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                WorkingElevationFeet += ElevationStepFeet;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                WorkingElevationFeet -= ElevationStepFeet;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 point;
                if (TryPointUnderCursor(out point))
                {
                    AddNode(point);
                }
            }
        }

        public void StartNewSketch()
        {
            Current = FieldSketchDto.NewDraft(FieldLinkConfig.DefaultPackageId, FieldLinkConfig.CrewId);
            WorkingElevationFeet = slabElevationFeet;
            ClearMarkers();
            Redraw();
        }

        public void AddNode(Vector3 worldPoint)
        {
            if (Current == null)
            {
                StartNewSketch();
            }

            Current.nodes.Add(new SketchNodeDto(worldPoint));
            SpawnMarker(worldPoint);
            Redraw();
        }

        public void RemoveLastNode()
        {
            if (Current == null || Current.nodes.Count == 0)
            {
                return;
            }

            Current.nodes.RemoveAt(Current.nodes.Count - 1);

            if (_markers.Count > 0)
            {
                var last = _markers[_markers.Count - 1];
                _markers.RemoveAt(_markers.Count - 1);
                if (last != null)
                {
                    Destroy(last);
                }
            }

            Redraw();
        }

        private bool TryPointUnderCursor(out Vector3 point)
        {
            point = Vector3.zero;

            var cam = sketchCamera != null ? sketchCamera : Camera.main;
            if (cam == null)
            {
                return false;
            }

            var plane = new Plane(Vector3.up, new Vector3(0f, WorkingElevationFeet, 0f));
            var ray = cam.ScreenPointToRay(Input.mousePosition);

            float enter;
            if (!plane.Raycast(ray, out enter))
            {
                return false;
            }

            point = ray.GetPoint(enter);
            return true;
        }

        private void SpawnMarker(Vector3 worldPoint)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "SketchNode_" + _markers.Count;
            marker.transform.SetParent(transform, true);
            marker.transform.position = worldPoint;
            marker.transform.localScale = Vector3.one * nodeMarkerDiameter;

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            _markers.Add(marker);
        }

        private void ClearMarkers()
        {
            for (var i = 0; i < _markers.Count; i++)
            {
                if (_markers[i] != null)
                {
                    Destroy(_markers[i]);
                }
            }

            _markers.Clear();
        }

        private void Redraw()
        {
            if (_line == null)
            {
                return;
            }

            var nodes = Current != null ? Current.nodes : null;
            var count = nodes == null ? 0 : nodes.Count;

            _line.positionCount = count;
            for (var i = 0; i < count; i++)
            {
                _line.SetPosition(i, nodes[i].ToVector3());
            }
        }
    }
}
