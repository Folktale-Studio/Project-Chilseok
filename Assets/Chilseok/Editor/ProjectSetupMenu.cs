using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chilseok.EditorTools
{
    internal static class ProjectSetupMenu
    {
        private const string MenuPath = "Tools/Chilseok/0. Setup Project";
        private const string ZoneTestScenePath = "Assets/Scenes/ZoneTest.unity";

        private static Scene _pendingTestScene;

        [MenuItem(MenuPath)]
        private static void PrepareZoneTestScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == ZoneTestScenePath ||
                (activeScene == _pendingTestScene && string.IsNullOrEmpty(activeScene.path)))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ZoneTestScenePath) != null)
            {
                EditorSceneManager.OpenScene(ZoneTestScenePath, OpenSceneMode.Single);
                return;
            }

            _pendingTestScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(_pendingTestScene);
            Debug.Log($"[Chilseok] 빈 테스트 씬을 열었습니다. File > Save As에서 {ZoneTestScenePath}로 저장하세요.");
        }

        [MenuItem(MenuPath, true)]
        private static bool CanPrepareZoneTestScene()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
