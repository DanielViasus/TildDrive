#if UNITY_EDITOR
using TiltDrive.Platform;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TiltDrive.Platform.Editor
{
    public static class TiltDrivePlatformClientMenu
    {
        private const string LoginScenePath = "Assets/_TiltDrive/Scenes/Login.unity";

        [MenuItem("TiltDrive/Platform/Create Platform Client")]
        public static void CreatePlatformClient()
        {
            TiltDrivePlatformClient existing = Object.FindFirstObjectByType<TiltDrivePlatformClient>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject client = new GameObject("TiltDrive Platform Client");
            client.AddComponent<TiltDrivePlatformClient>();
            Selection.activeGameObject = client;
            Undo.RegisterCreatedObjectUndo(client, "Create TiltDrive Platform Client");
        }

        [MenuItem("TiltDrive/Platform/Create Login Scene")]
        public static void CreateLoginScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Login";

            GameObject login = new GameObject("TiltDrive Login Scene");
            login.AddComponent<TiltDriveLoginSceneController>();

            GameObject cameraObject = new GameObject("Login Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.05f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            EditorSceneManager.SaveScene(scene, LoginScenePath);
            AddSceneToBuildSettings(LoginScenePath, true);
            Selection.activeGameObject = login;
        }

        private static void AddSceneToBuildSettings(string scenePath, bool first)
        {
            EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < currentScenes.Length; i++)
            {
                if (currentScenes[i].path == scenePath)
                {
                    currentScenes[i].enabled = true;
                    EditorBuildSettings.scenes = currentScenes;
                    return;
                }
            }

            EditorBuildSettingsScene loginScene = new EditorBuildSettingsScene(scenePath, true);
            if (!first)
            {
                ArrayUtility.Add(ref currentScenes, loginScene);
                EditorBuildSettings.scenes = currentScenes;
                return;
            }

            EditorBuildSettingsScene[] nextScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
            nextScenes[0] = loginScene;
            for (int i = 0; i < currentScenes.Length; i++)
            {
                nextScenes[i + 1] = currentScenes[i];
            }

            EditorBuildSettings.scenes = nextScenes;
        }
    }
}
#endif
