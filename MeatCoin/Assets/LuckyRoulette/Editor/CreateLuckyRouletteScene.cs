using LuckyRoulette;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LuckyRoulette.Editor
{
    public static class CreateLuckyRouletteScene
    {
        private const string ScenePath = "Assets/Scenes/LuckyRoulette.unity";

        [MenuItem("Tools/Lucky Roulette/Create Scene")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LuckyRoulette";

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.042f, 0.075f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0, 0, -10);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.7f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var gameObject = new GameObject("Lucky Roulette Game");
            gameObject.AddComponent<LuckyRouletteGame>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Lucky Roulette scene created at " + ScenePath);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                if (scene.path == scenePath)
                    return;
            }

            var next = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
                next[i] = scenes[i];
            next[next.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = next;
        }
    }
}
