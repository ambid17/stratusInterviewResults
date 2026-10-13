using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StratusFieldLink.Sketch;

namespace StratusFieldLink.EditorTools
{
    /// <summary>
    /// Editor-side wrapper around <see cref="SketchSceneFactory"/>. Builds the
    /// scene, saves it to Assets/Scenes/FieldSketch.unity, and opens it.
    /// </summary>
    public static class FieldSketchSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/FieldSketch.unity";

        [MenuItem("Stratus/Build Field Sketch Scene")]
        public static void BuildAndSave()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SketchSceneFactory.Build();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[FieldLink] Built " + ScenePath + ". Press Play.");
        }
    }
}
