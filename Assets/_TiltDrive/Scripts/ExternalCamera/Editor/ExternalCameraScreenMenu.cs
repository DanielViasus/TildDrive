#if UNITY_EDITOR
using TiltDrive.ExternalCameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TiltDrive.ExternalCameraSystem.Editor
{
    public static class ExternalCameraScreenMenu
    {
        [MenuItem("TiltDrive/External Camera/Create Vuforia Screen")]
        public static void CreateVuforiaScreen()
        {
            GameObject root = new GameObject("Vuforia External Camera Screen");
            Undo.RegisterCreatedObjectUndo(root, "Create Vuforia Screen");

            ExternalCameraScreen screen = root.AddComponent<ExternalCameraScreen>();
            Selection.activeGameObject = root;

            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(root.scene);
        }
    }
}
#endif
