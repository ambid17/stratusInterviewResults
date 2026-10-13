using System.Collections.Generic;
using StratusFieldLink.Model;

namespace StratusFieldLink.Geometry
{
    public struct SketchValidationResult
    {
        public bool IsValid;
        public string Message;

        public static SketchValidationResult Ok()
        {
            return new SketchValidationResult { IsValid = true, Message = string.Empty };
        }

        public static SketchValidationResult Fail(string message)
        {
            return new SketchValidationResult { IsValid = false, Message = message };
        }
    }

    /// <summary>
    /// Client-side pre-flight. The server validates again — this exists so the
    /// foreman finds out before they walk out of Wi-Fi range.
    /// </summary>
    public static class SketchValidation
    {
        private static readonly HashSet<string> KnownDisciplines =
            new HashSet<string> { "Piping", "SheetMetal", "Electrical" };

        public static SketchValidationResult Validate(FieldSketchDto sketch)
        {
            if (sketch == null)
            {
                return SketchValidationResult.Fail("No sketch.");
            }

            if (string.IsNullOrEmpty(sketch.id))
            {
                return SketchValidationResult.Fail("Sketch has no id.");
            }

            if (string.IsNullOrEmpty(sketch.packageId))
            {
                return SketchValidationResult.Fail("Pick a package before publishing.");
            }

            if (string.IsNullOrEmpty(sketch.crewId))
            {
                return SketchValidationResult.Fail("Not signed in to a crew.");
            }

            if (!KnownDisciplines.Contains(sketch.discipline))
            {
                return SketchValidationResult.Fail("Unknown discipline '" + sketch.discipline + "'.");
            }

            if (sketch.diameterInches <= 0f)
            {
                return SketchValidationResult.Fail("Set a diameter.");
            }

            if (sketch.nodes == null || sketch.nodes.Count < 2)
            {
                return SketchValidationResult.Fail("A run needs at least two nodes.");
            }

            var length = SketchGeometry.TotalLengthFeet(sketch.nodes);
            if (length < SketchGeometry.MinimumRunLengthFeet)
            {
                return SketchValidationResult.Fail(
                    "Run is too short to publish (" + length.ToString("0.##") + " ft).");
            }

            return SketchValidationResult.Ok();
        }
    }
}
