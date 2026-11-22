using Chilseok.Player;
using Chilseok.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chilseok.EditorTools
{
    internal static class PlayerMovementPreviewMenu
    {
        private const string MenuPath = "Tools/Chilseok/2. Setup Movement Preview";
        private const string UndoLabel = "Setup Chilseok Movement Preview";
        private const string ScenePath = "Assets/Scenes/ZoneTest.unity";
        private const string LayoutPath = "Assets/Chilseok/Editor/PlayerMovementPreviewLayout.txt";
        private const string SettingsPath = "Assets/Settings/ChilseokMovementSettings.asset";
        private const string GrayMaterialPath = "Assets/Chilseok/Editor/DitherPreviewGray.mat";
        private const string PreviewName = "Movement Preview";

        [MenuItem(MenuPath)]
        private static void CreateOrUpdate()
        {
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            var gray = AssetDatabase.LoadAssetAtPath<Material>(GrayMaterialPath);
            if (layout == null || gray == null)
            {
                Debug.LogError($"[Chilseok] 이동 미리보기에 필요한 {LayoutPath}, {GrayMaterialPath}를 확인하세요.");
                return;
            }

            var zone = ZoneParser.Parse(layout.text);
            var settings = AssetDatabase.LoadAssetAtPath<MovementSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MovementSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            var group = Undo.GetCurrentGroup();
            foreach (var existing in scene.GetRootGameObjects())
            {
                if (existing.name == PreviewName)
                    Undo.DestroyObjectImmediate(existing);
            }

            var root = CreateObject(PreviewName, null);
            AddCameraAndLight(root.transform, zone.Size);
            AddBlocks(root.transform, zone, gray);
            AddPlayer(root.transform, layout, settings, zone.Start, gray);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root;
        }

        [MenuItem(MenuPath, true)]
        private static bool CanCreateOrUpdate() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            SceneManager.GetActiveScene().path == ScenePath;

        private static void AddCameraAndLight(Transform root, Vector3Int size)
        {
            var cameraObject = CreateObject("Camera", root);
            cameraObject.transform.localPosition = new Vector3((size.x - 1) * 0.5f, 1.4f, -12f);
            var camera = Undo.AddComponent<Camera>(cameraObject);
            camera.orthographic = true;
            camera.orthographicSize = 4.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.08f);

            var lightObject = CreateObject("Directional Light", root);
            lightObject.transform.localRotation = Quaternion.Euler(25f, -28f, 0f);
            var light = Undo.AddComponent<Light>(lightObject);
            light.type = LightType.Directional;
            light.intensity = 1.8f;
        }

        private static void AddBlocks(Transform root, ZoneData zone, Material gray)
        {
            var blocks = CreateObject("Grid", root).transform;
            for (var y = 0; y < zone.Size.y; y++)
            for (var z = 0; z < zone.Size.z; z++)
            for (var x = 0; x < zone.Size.x; x++)
            {
                var cell = new Vector3Int(x, y, z);
                if (zone.IsSolid(cell))
                    AddCube(blocks, $"Block {x},{y},{z}", cell + Vector3.up * 0.5f, Vector3.one * 0.97f, gray);
            }
        }

        private static void AddPlayer(Transform root, TextAsset layout, MovementSettings settings,
            Vector3Int feet, Material gray)
        {
            var player = CreateObject("Gyeonu", root);
            player.transform.localPosition = feet;
            var controller = Undo.AddComponent<PlayerController2D>(player);
            var data = new SerializedObject(controller);
            data.FindProperty("_zoneData").objectReferenceValue = layout;
            data.FindProperty("_settings").objectReferenceValue = settings;
            data.ApplyModifiedProperties();
            AddCube(player.transform, "Body", Vector3.up * 0.5f, new Vector3(0.6f, 1f, 0.6f), gray);
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gameObject, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(gameObject.transform, parent, UndoLabel);
            return gameObject;
        }

        private static void AddCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            Undo.RegisterCreatedObjectUndo(cube, UndoLabel);
            Undo.SetTransformParent(cube.transform, parent, UndoLabel);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            // 충돌과 낙하는 투영 맵에서 판정한다.
            Undo.DestroyObjectImmediate(cube.GetComponent<BoxCollider>());
        }
    }
}
