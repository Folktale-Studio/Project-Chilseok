using System;
using System.Collections.Generic;
using Chilseok.CameraSystem;
using Chilseok.Player;
using Chilseok.Puzzle;
using Chilseok.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Chilseok.EditorTools.PlayerMovementPreviewMenu;

namespace Chilseok.EditorTools
{
    public static class ZoneSetupMenu
    {
        private const string MenuPath = "Tools/Chilseok/5. Setup Zone From Text";
        private const string LevelDirectory = "Assets/Chilseok/Levels/";
        private const string DefaultLayoutPath = LevelDirectory + "zone-02.txt";
        private const string UndoLabel = "Setup Chilseok Zone From Text";

        [MenuItem(MenuPath)]
        private static void BuildSelected()
        {
            var selected = Selection.activeObject as TextAsset;
            var path = selected == null ? "" : AssetDatabase.GetAssetPath(selected);
            var layout = path.StartsWith(LevelDirectory, StringComparison.Ordinal) &&
                path.EndsWith(".txt", StringComparison.Ordinal)
                ? selected
                : AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultLayoutPath);
            Build(layout);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanBuild() => CanBuildInZoneTest();

        public static GameObject Build(TextAsset layout)
        {
            var gray = AssetDatabase.LoadAssetAtPath<Material>(GrayMaterialPath);
            var settings = AssetDatabase.LoadAssetAtPath<MovementSettings>(SettingsPath);
            var connector = AssetDatabase.LoadAssetAtPath<TextAsset>(ConnectorLayoutPath);
            if (layout == null || gray == null || settings == null || connector == null)
            {
                Debug.LogError($"[Chilseok] {DefaultLayoutPath}, {GrayMaterialPath}, {SettingsPath}, " +
                    $"{ConnectorLayoutPath} 경로를 확인하세요.");
                return null;
            }

            ZoneData zone;
            ZoneData path;
            try
            {
                zone = ZoneParser.Parse(layout.text);
                path = ZoneParser.Parse(connector.text);
            }
            catch (FormatException error)
            {
                Debug.LogError($"[Chilseok] 구간 배치 실패. {error.Message}");
                return null;
            }

            var startView = new ProjectionMap(zone, zone.StartView);
            Reprojector.TryUpdateDepth(startView, zone.Start, out var startFeet);
            var connectExit = TryGetConnectorOrigin(zone, path, out var connectorOrigin);
            if (!connectExit)
            {
                Debug.LogWarning($"[Chilseok] {layout.name}의 까치 발 칸이 0도에서 " +
                    "오른쪽 끝 발판과 맞아야 연결 길을 배치할 수 있습니다.");
            }

            var scene = SceneManager.GetActiveScene();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            var group = Undo.GetCurrentGroup();
            var root = CreatePreviewRoot(scene);
            var pivot = AddCameraAndLight(root.transform, zone.Size);
            pivot.localRotation = Quaternion.Euler(0f, -(int)zone.StartView, 0f);
            var player = AddPlayer(root.transform, layout, settings, startFeet, gray);
            AddRotationPreview(player, pivot, gray);
            AddCameraFollow(player, pivot);

            var drawn = new HashSet<Vector3Int>();
            AddBlocks(root.transform, zone, gray, Vector3Int.zero, drawn);
            if (connectExit)
                AddExit(root.transform, player, gray, path, connectorOrigin, drawn, layout, connector);
            AddMarkers(root.transform, player, gray, zone, layout);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = root;
            return root;
        }

        private static bool TryGetConnectorOrigin(ZoneData zone, ZoneData path, out Vector3Int origin)
        {
            origin = zone.Magpie - path.Start;
            if (zone.Magpie.x != zone.Size.x - 1 || path.Start.x != 0 || path.StartView != ViewDirection.Degrees0)
                return false;

            var forward = new ProjectionMap(zone, ViewDirection.Degrees0);
            return !forward.IsSolid(forward.Project(zone.Magpie)) &&
                Reprojector.TryUpdateDepth(forward, zone.Magpie, out var feet) && feet == zone.Magpie;
        }

        private static void AddExit(Transform root, GameObject player, Material gray, ZoneData path,
            Vector3Int origin, HashSet<Vector3Int> drawn, TextAsset layout, TextAsset connector)
        {
            AddBlocks(CreateObject("Connector", root).transform, path, gray, origin, drawn);
            var tracker = Undo.AddComponent<ZoneTracker>(player);
            var data = new SerializedObject(tracker);
            var segments = data.FindProperty("_segments");
            segments.arraySize = 2;
            BindSegment(segments.GetArrayElementAtIndex(0), layout, Vector3Int.zero, false);
            BindSegment(segments.GetArrayElementAtIndex(1), connector, origin, true);
            data.ApplyModifiedProperties();
        }

        private static void AddMarkers(Transform root, GameObject player, Material gray, ZoneData zone,
            TextAsset layout)
        {
            var markers = CreateObject("Markers", root);
            AddCube(markers.transform, "Start", zone.Start + Vector3.up * 0.035f,
                new Vector3(0.8f, 0.07f, 0.8f), gray);

            var magpie = CreateObject("Magpie", markers.transform).transform;
            magpie.localPosition = zone.Magpie;
            AddCube(magpie, "Post", Vector3.up * 0.45f, new Vector3(0.14f, 0.9f, 0.14f), gray);
            AddCube(magpie, "Flag", Vector3.up * 1.04f, new Vector3(0.56f, 0.26f, 0.56f), gray)
                .localRotation = Quaternion.Euler(0f, 0f, 25f);

            var feathers = CreateObject("Feathers", markers.transform).transform;
            var visibility = Undo.AddComponent<ZonePreviewMarkers>(markers);
            var data = new SerializedObject(visibility);
            data.FindProperty("_player").objectReferenceValue = player.GetComponent<PlayerController2D>();
            data.FindProperty("_tracker").objectReferenceValue = player.GetComponent<ZoneTracker>();
            data.FindProperty("_layout").objectReferenceValue = layout;
            var instances = data.FindProperty("_feathers");
            instances.arraySize = zone.Feathers.Count;
            for (var i = 0; i < zone.Feathers.Count; i++)
            {
                var feather = zone.Feathers[i];
                var marker = AddCube(feathers, $"Feather {i + 1}", feather.Position + Vector3.up * 0.5f,
                    new Vector3(0.2f, 0.42f, 0.2f), gray);
                marker.localRotation = Quaternion.Euler(0f, 0f, 38f);
                marker.gameObject.SetActive(feather.IsActive(zone.StartView));
                instances.GetArrayElementAtIndex(i).objectReferenceValue = marker.gameObject;
            }
            data.ApplyModifiedProperties();
        }
    }
}
