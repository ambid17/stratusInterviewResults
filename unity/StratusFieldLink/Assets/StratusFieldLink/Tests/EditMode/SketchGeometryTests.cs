using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using StratusFieldLink.Geometry;
using StratusFieldLink.Model;

namespace StratusFieldLink.Tests.EditMode
{
    public class SketchGeometryTests
    {
        private static SketchNodeDto N(float x, float y, float z)
        {
            return new SketchNodeDto(new Vector3(x, y, z));
        }

        [Test]
        public void TotalLengthFeet_NullOrSingleNode_IsZero()
        {
            Assert.AreEqual(0f, SketchGeometry.TotalLengthFeet(null));
            Assert.AreEqual(0f, SketchGeometry.TotalLengthFeet(new List<SketchNodeDto>()));
            Assert.AreEqual(0f, SketchGeometry.TotalLengthFeet(new List<SketchNodeDto> { N(1f, 10f, 2f) }));
        }

        [Test]
        public void TotalLengthFeet_StraightRunAcrossTheSlab_IsTheDistance()
        {
            var nodes = new List<SketchNodeDto> { N(0f, 10f, 0f), N(30f, 10f, 0f) };

            Assert.AreEqual(30f, SketchGeometry.TotalLengthFeet(nodes), 0.001f);
        }

        [Test]
        public void TotalLengthFeet_LShapedRun_SumsBothLegs()
        {
            var nodes = new List<SketchNodeDto> { N(0f, 10f, 0f), N(30f, 10f, 0f), N(30f, 10f, 40f) };

            Assert.AreEqual(70f, SketchGeometry.TotalLengthFeet(nodes), 0.001f);
        }

        [Test]
        public void SegmentCount_IsNodesMinusOne()
        {
            var nodes = new List<SketchNodeDto> { N(0f, 10f, 0f), N(1f, 10f, 0f), N(1f, 10f, 1f) };

            Assert.AreEqual(2, SketchGeometry.SegmentCount(nodes));
        }

        [Test]
        public void ElevationChangeFeet_FlatRun_IsZero()
        {
            var nodes = new List<SketchNodeDto> { N(0f, 10f, 0f), N(30f, 10f, 0f) };

            Assert.AreEqual(0f, SketchGeometry.ElevationChangeFeet(nodes), 0.001f);
        }

        [Test]
        public void ElevationChangeFeet_RunWithADrop_IsHighestMinusLowest()
        {
            var nodes = new List<SketchNodeDto> { N(0f, 22f, 0f), N(20f, 22f, 0f), N(20f, 10f, 0f) };

            Assert.AreEqual(12f, SketchGeometry.ElevationChangeFeet(nodes), 0.001f);
        }
    }
}
