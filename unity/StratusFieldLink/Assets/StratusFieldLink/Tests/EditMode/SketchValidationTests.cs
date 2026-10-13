using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using StratusFieldLink.Geometry;
using StratusFieldLink.Model;

namespace StratusFieldLink.Tests.EditMode
{
    public class SketchValidationTests
    {
        private static FieldSketchDto Valid()
        {
            var sketch = FieldSketchDto.NewDraft("PKG-CHW-L02", "CREW-04");
            sketch.nodes = new List<SketchNodeDto>
            {
                new SketchNodeDto(new Vector3(0f, 10f, 0f)),
                new SketchNodeDto(new Vector3(24f, 10f, 0f))
            };
            return sketch;
        }

        [Test]
        public void Validate_WellFormedSketch_Passes()
        {
            Assert.IsTrue(SketchValidation.Validate(Valid()).IsValid);
        }

        [Test]
        public void Validate_NoPackage_Fails()
        {
            var sketch = Valid();
            sketch.packageId = string.Empty;

            Assert.IsFalse(SketchValidation.Validate(sketch).IsValid);
        }

        [Test]
        public void Validate_SingleNode_Fails()
        {
            var sketch = Valid();
            sketch.nodes.RemoveAt(1);

            Assert.IsFalse(SketchValidation.Validate(sketch).IsValid);
        }

        [Test]
        public void Validate_TwoNodesInTheSameSpot_Fails()
        {
            var sketch = Valid();
            sketch.nodes[1] = new SketchNodeDto(new Vector3(0f, 10f, 0f));

            Assert.IsFalse(SketchValidation.Validate(sketch).IsValid);
        }

        [Test]
        public void Validate_UnknownDiscipline_Fails()
        {
            var sketch = Valid();
            sketch.discipline = "Plumbing";

            Assert.IsFalse(SketchValidation.Validate(sketch).IsValid);
        }
    }
}
