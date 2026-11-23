using System.Collections.Generic;
using Chilseok.CameraSystem;
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
        private const string RotationMenuPath = "Tools/Chilseok/3. Setup Rotation Preview";
        private const string TrackingMenuPath = "Tools/Chilseok/4. Setup Zone Tracking Preview";
        private const string UndoLabel = "Setup Chilseok Movement Preview";
        private const string ScenePath = "Assets/Scenes/ZoneTest.unity";
        private const string LayoutPath = "Assets/Chilseok/Editor/PlayerMovementPreviewLayout.txt";
        private const string RotationLayoutPath = "Assets/Chilseok/Editor/PlayerRotationPreviewLayout.txt";
        private const string TrackingLayoutPath = "Assets/Chilseok/Editor/PlayerZoneTrackingPreviewLayout.txt";
        internal const string ConnectorLayoutPath = "Assets/Chilseok/Editor/PlayerZoneConnectorPreviewLayout.txt";
        internal const string SettingsPath = "Assets/Settings/ChilseokMovementSettings.asset";
        internal const string GrayMaterialPath = "Assets/Chilseok/Editor/DitherPreviewGray.mat";
        private const string PreviewName = "Movement Preview";

        [MenuItem(MenuPath)]
        private static void CreateOrUpdateMovement() => CreateOrUpdate(LayoutPath, withRotation: false);

        [MenuItem(RotationMenuPath)]
        private static void CreateOrUpdateRotation() => CreateOrUpdate(RotationLayoutPath, withRotation: true);

        [MenuItem(TrackingMenuPath)]
        private static void CreateOrUpdateTracking() => CreateOrUpdate(TrackingLayoutPath, withRotation: true,
            connectorPath: ConnectorLayoutPath);

        private static void CreateOrUpdate(string layoutPath, bool withRotation, string connectorPath = null)
        {
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(layoutPath);
            var gray = AssetDatabase.LoadAssetAtPath<Material>(GrayMaterialPath);
            if (layout == null || gray == null)
            {
                Debug.LogError($"[Chilseok] 미리보기에 필요한 {layoutPath}, {GrayMaterialPath}를 확인하세요.");
                return;
            }

            var connector = connectorPath == null ? null : AssetDatabase.LoadAssetAtPath<TextAsset>(connectorPath);
            if (connectorPath != null && connector == null)
            {
                Debug.LogError($"[Chilseok] 연결 길 미리보기에 필요한 {connectorPath}를 확인하세요.");
                return;
            }

            var zone = ZoneParser.Parse(layout.text);
            var connectorZone = connector == null ? null : ZoneParser.Parse(connector.text);
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
            var root = CreatePreviewRoot(scene);
            var pivot = AddCameraAndLight(root.transform, zone.Size);
            var player = AddPlayer(root.transform, layout, settings, zone.Start, gray);
            if (withRotation)
                AddRotationPreview(player, pivot, gray);
            if (connector == null)
                AddBlocks(root.transform, zone, gray, Vector3Int.zero);
            else
                AddTrackingPreview(root.transform, player, pivot, gray, layout, zone, connector, connectorZone);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root;
        }

        [MenuItem(MenuPath, true)]
        [MenuItem(RotationMenuPath, true)]
        [MenuItem(TrackingMenuPath, true)]
        private static bool CanCreateOrUpdate() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            SceneManager.GetActiveScene().path == ScenePath;

        internal static bool CanBuildInZoneTest() => CanCreateOrUpdate();

        internal static GameObject CreatePreviewRoot(Scene scene)
        {
            foreach (var existing in scene.GetRootGameObjects())
            {
                if (existing.name == PreviewName)
                    Undo.DestroyObjectImmediate(existing);
            }

            return CreateObject(PreviewName, null);
        }

        internal static Transform AddCameraAndLight(Transform root, Vector3Int size)
        {
            var pivot = CreateObject("View Pivot", root).transform;
            pivot.localPosition = new Vector3((size.x - 1) * 0.5f, 1.4f, (size.z - 1) * 0.5f);
            var cameraObject = CreateObject("Camera", pivot);
            cameraObject.transform.localPosition = Vector3.back * 12f;
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
            return pivot;
        }

        internal static void AddBlocks(Transform root, ZoneData zone, Material gray, Vector3Int origin,
            HashSet<Vector3Int> drawn = null)
        {
            var blocks = CreateObject("Grid", root).transform;
            for (var y = 0; y < zone.Size.y; y++)
            for (var z = 0; z < zone.Size.z; z++)
            for (var x = 0; x < zone.Size.x; x++)
            {
                var cell = new Vector3Int(x, y, z);
                var position = origin + cell;
                if (zone.IsSolid(cell) && (drawn == null || drawn.Add(position)))
                    AddCube(blocks, $"Block {x},{y},{z}", position + Vector3.up * 0.5f,
                        Vector3.one * PuzzleMovement.BlockWidth, gray);
            }
        }

        private static void AddTrackingPreview(Transform sceneRoot, GameObject player, Transform pivot,
            Material gray, TextAsset layout, ZoneData zone, TextAsset connector, ZoneData connectorZone)
        {
            var connectorOrigin = Vector3Int.right * (zone.Size.x - 1);
            var nextZoneOrigin = connectorOrigin + Vector3Int.right * (connectorZone.Size.x - 1);
            var drawn = new HashSet<Vector3Int>();
            AddBlocks(CreateObject("Zone 1", sceneRoot).transform, zone, gray, Vector3Int.zero, drawn);
            AddBlocks(CreateObject("Connector", sceneRoot).transform, connectorZone, gray, connectorOrigin, drawn);
            AddBlocks(CreateObject("Zone 2", sceneRoot).transform, zone, gray, nextZoneOrigin, drawn);

            var tracker = Undo.AddComponent<ZoneTracker>(player);
            var trackerData = new SerializedObject(tracker);
            var segments = trackerData.FindProperty("_segments");
            segments.arraySize = 3;
            BindSegment(segments.GetArrayElementAtIndex(0), layout, Vector3Int.zero, false);
            BindSegment(segments.GetArrayElementAtIndex(1), connector, connectorOrigin, true);
            BindSegment(segments.GetArrayElementAtIndex(2), layout, nextZoneOrigin, false);
            trackerData.ApplyModifiedProperties();

            AddCameraFollow(player, pivot);
        }

        internal static void AddCameraFollow(GameObject player, Transform pivot)
        {
            var follow = Undo.AddComponent<PlayerCameraFollow>(pivot.gameObject);
            var followData = new SerializedObject(follow);
            followData.FindProperty("_target").objectReferenceValue = player.transform;
            followData.ApplyModifiedProperties();
        }

        internal static void BindSegment(SerializedProperty segment, TextAsset layout, Vector3Int origin,
            bool isConnector)
        {
            segment.FindPropertyRelative("_layout").objectReferenceValue = layout;
            segment.FindPropertyRelative("_origin").vector3IntValue = origin;
            segment.FindPropertyRelative("_connector").boolValue = isConnector;
        }

        internal static GameObject AddPlayer(Transform root, TextAsset layout, MovementSettings settings,
            Vector3Int feet, Material gray)
        {
            var player = CreateObject("Gyeonu", root);
            player.transform.localPosition = feet;
            var controller = Undo.AddComponent<PlayerController2D>(player);
            var data = new SerializedObject(controller);
            data.FindProperty("_zoneData").objectReferenceValue = layout;
            data.FindProperty("_settings").objectReferenceValue = settings;
            data.ApplyModifiedProperties();
            AddCube(player.transform, "Body", Vector3.up * 0.5f,
                new Vector3(PlayerMovement.BodyWidth, 1f, PlayerMovement.BodyWidth), gray);
            return player;
        }

        internal static void AddRotationPreview(GameObject player, Transform pivot, Material gray)
        {
            var camera = pivot.GetComponentInChildren<Camera>();
            var fillObject = CreateObject("Rotation Fill Light", camera.transform);
            fillObject.transform.localRotation = Quaternion.Euler(22f, -15f, 0f);
            var fill = Undo.AddComponent<Light>(fillObject);
            fill.type = LightType.Directional;
            // 회전해도 정면에 빛을 줘 큐브와 배경이 같은 먹색으로 합쳐지는 것을 막는다.
            fill.intensity = 2.1f;
            fill.shadows = LightShadows.None;

            var marker = CreateObject("Rotation Rejected", camera.transform);
            marker.transform.localPosition = new Vector3(0f, 2.8f, 4f);
            var scale = new Vector3(0.75f, 0.12f, 0.12f);
            AddCube(marker.transform, "Stroke A", Vector3.zero, scale, gray).localRotation =
                Quaternion.Euler(0f, 0f, 45f);
            AddCube(marker.transform, "Stroke B", Vector3.zero, scale, gray).localRotation =
                Quaternion.Euler(0f, 0f, -45f);
            marker.SetActive(false);

            var rotator = Undo.AddComponent<ViewRotator>(player);
            var data = new SerializedObject(rotator);
            data.FindProperty("_viewPivot").objectReferenceValue = pivot;
            data.FindProperty("_rejectedIndicator").objectReferenceValue = marker;
            data.ApplyModifiedProperties();
        }

        internal static GameObject CreateObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gameObject, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(gameObject.transform, parent, UndoLabel);
            return gameObject;
        }

        internal static Transform AddCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
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
            return cube.transform;
        }
    }
}
