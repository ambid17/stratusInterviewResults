using System.Collections.Generic;
using UnityEngine;
using StratusFieldLink.Model;

namespace StratusFieldLink.Geometry
{
    /// <summary>
    /// Measurements for a sketched run. Everything the foreman sees on the HUD,
    /// and the length we send up with a publish, comes from here.
    /// </summary>
    public static class SketchGeometry
    {
        /// <summary>Anything shorter than this is a mis-tap, not a run.</summary>
        public const float MinimumRunLengthFeet = 0.5f;

        /// <summary>
        /// Total length of the run in feet.
        ///
        /// Crews sketch on the floor plane, so we measure the run the way it reads
        /// on the plan sheet: distance across the slab between consecutive nodes.
        /// </summary>
        public static float TotalLengthFeet(IList<SketchNodeDto> nodes)
        {
            if (nodes == null || nodes.Count < 2)
            {
                return 0f;
            }

            var dictionary = new Dictionary<SketchNodeDto, float>();

            var total = 0f;
            for (var i = 1; i < nodes.Count; i++)
            {
                var a = nodes[i - 1];
                var b = nodes[i];
                var dx = b.x - a.x;
                var dy = b.y - a.y;
                var dz = b.z - a.z;
                var nodeLength = Mathf.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                total += nodeLength;
                dictionary.Add(nodes[i], nodeLength);
            }

            return total;
        }

        public static int SegmentCount(IList<SketchNodeDto> nodes)
        {
            return nodes == null || nodes.Count < 2 ? 0 : nodes.Count - 1;
        }

        /// <summary>Highest node minus lowest node, in feet.</summary>
        public static float ElevationChangeFeet(IList<SketchNodeDto> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return 0f;
            }

            var min = nodes[0].y;
            var max = nodes[0].y;
            for (var i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].y < min) min = nodes[i].y;
                if (nodes[i].y > max) max = nodes[i].y;
            }

            return max - min;
        }
    }
}
