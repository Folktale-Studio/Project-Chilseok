using System.Linq;
using Chilseok.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Chilseok.EditorTools
{
    internal static class DitherPreviewMenu
    {
        private const string MenuPath = "Tools/Chilseok/1. Setup Dither Preview";
        private const string UndoLabel = "Setup Chilseok Dither Preview";
        private const string ScenePath = "Assets/Scenes/DitherPreview.unity";
        private const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string SettingsPath = "Assets/Settings/ChilseokDitherSettings.asset";
        private const string PreviewMaterialPath = "Assets/Chilseok/Editor/DitherPreviewGray.mat";
        private const string DitherShaderPath = "Assets/Chilseok/Shaders/Dither.shader";
        private const string PreviewRootName = "Dither Preview";

        [MenuItem(MenuPath)]
        private static void CreateOrUpdate()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(DitherShaderPath);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (renderer == null || shader == null || lit == null)
            {
                Debug.LogError($"[Chilseok] 디더 미리보기에 필요한 {RendererPath}, {DitherShaderPath}, URP/Lit을 확인하세요.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            var group = Undo.GetCurrentGroup();

            var settings = AssetDatabase.LoadAssetAtPath<DitherSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<DitherSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            if (!ConnectRenderer(renderer, shader, settings))
            {
                return;
            }

            var gray = AssetDatabase.LoadAssetAtPath<Material>(PreviewMaterialPath);
            if (gray == null)
            {
                gray = new Material(lit);
                gray.SetColor("_BaseColor", new Color(0.54f, 0.54f, 0.54f));
                gray.SetFloat("_Smoothness", 0.16f);
                AssetDatabase.CreateAsset(gray, PreviewMaterialPath);
            }

            var scene = SceneManager.GetActiveScene();
            var root = scene.GetRootGameObjects().FirstOrDefault(obj => obj.name == PreviewRootName);
            if (root == null)
            {
                root = new GameObject(PreviewRootName);
                Undo.RegisterCreatedObjectUndo(root, UndoLabel);
            }

            ConfigureCamera(root.transform);
            ConfigureLight(root.transform);
            PlaceCube(root.transform, gray, "Platform", new Vector3(0f, -0.22f, 0f), new Vector3(9f, 0.4f, 5.4f));
            PlaceCube(root.transform, gray, "Cube A", new Vector3(-2.6f, 0.42f, 0.6f), new Vector3(0.85f, 0.88f, 0.85f));
            PlaceCube(root.transform, gray, "Cube B", new Vector3(-1.05f, 0.62f, -0.15f), new Vector3(1.05f, 1.28f, 1.05f));
            PlaceCube(root.transform, gray, "Cube C", new Vector3(0.85f, 0.85f, 0.55f), new Vector3(1.16f, 1.74f, 1.16f));
            PlaceCube(root.transform, gray, "Cube D", new Vector3(2.62f, 1.05f, -0.35f), new Vector3(1.3f, 2.14f, 1.3f));

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanCreateOrUpdate() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            SceneManager.GetActiveScene().path == ScenePath;

        private static bool ConnectRenderer(UniversalRendererData renderer, Shader shader, DitherSettings settings)
        {
            var feature = renderer.rendererFeatures.OfType<DitherRenderFeature>().FirstOrDefault();
            if (feature == null)
            {
                var data = new SerializedObject(renderer);
                var features = data.FindProperty("m_RendererFeatures");
                var featureIds = data.FindProperty("m_RendererFeatureMap");
                if (features == null || featureIds == null || features.arraySize != featureIds.arraySize)
                {
                    Debug.LogError($"[Chilseok] {RendererPath}의 Renderer Feature 목록과 식별자 목록을 확인하세요.");
                    return false;
                }

                feature = ScriptableObject.CreateInstance<DitherRenderFeature>();
                feature.name = "Chilseok Dither";
                Undo.RegisterCreatedObjectUndo(feature, UndoLabel);
                AssetDatabase.AddObjectToAsset(feature, renderer);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long identifier);

                // URP 인스펙터와 같은 방식으로 기능과 로컬 식별자를 함께 등록한다.
                var index = features.arraySize;
                features.arraySize++;
                features.GetArrayElementAtIndex(index).objectReferenceValue = feature;
                featureIds.arraySize++;
                featureIds.GetArrayElementAtIndex(index).longValue = identifier;
                data.ApplyModifiedProperties();
            }

            var featureData = new SerializedObject(feature);
            featureData.FindProperty("_shader").objectReferenceValue = shader;
            featureData.FindProperty("_settings").objectReferenceValue = settings;
            featureData.ApplyModifiedProperties();
            Undo.RecordObject(feature, UndoLabel);
            feature.SetActive(true);
            renderer.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssetIfDirty(renderer);
            return true;
        }

        private static void ConfigureCamera(Transform root)
        {
            var obj = GetPart(root, "Dither Preview Camera");
            var camera = GetOrAdd<Camera>(obj);
            var extra = GetOrAdd<UniversalAdditionalCameraData>(obj);
            Undo.RecordObjects(new Object[] { obj.transform, camera, extra }, UndoLabel);
            obj.transform.position = new Vector3(5.5f, 6f, -10f);
            obj.transform.LookAt(new Vector3(0f, 0.55f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 4.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.018f, 0.018f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            extra.antialiasing = AntialiasingMode.None;
            extra.renderPostProcessing = false;
        }

        private static void ConfigureLight(Transform root)
        {
            var obj = GetPart(root, "Dither Preview Light");
            var light = GetOrAdd<Light>(obj);
            Undo.RecordObjects(new Object[] { obj.transform, light }, UndoLabel);
            obj.transform.rotation = Quaternion.Euler(38f, 40f, 0f);
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.Hard;
        }

        private static void PlaceCube(Transform root, Material material, string name, Vector3 position, Vector3 scale)
        {
            var obj = GetPart(root, name, true);
            var renderer = obj.GetComponent<MeshRenderer>();
            Undo.RecordObjects(new Object[] { obj.transform, renderer }, UndoLabel);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            renderer.sharedMaterial = material;
        }

        private static GameObject GetPart(Transform root, string name, bool cube = false)
        {
            var existing = root.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var part = cube ? GameObject.CreatePrimitive(PrimitiveType.Cube) : new GameObject();
            part.name = name;
            Undo.RegisterCreatedObjectUndo(part, UndoLabel);
            Undo.SetTransformParent(part.transform, root, UndoLabel);
            return part;
        }

        private static T GetOrAdd<T>(GameObject obj) where T : Component =>
            obj.TryGetComponent<T>(out var component) ? component : Undo.AddComponent<T>(obj);
    }
}
