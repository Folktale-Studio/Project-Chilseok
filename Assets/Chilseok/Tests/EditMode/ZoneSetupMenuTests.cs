using System.Collections.Generic;
using Chilseok.CameraSystem;
using Chilseok.EditorTools;
using Chilseok.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chilseok.Tests.EditMode
{
    public sealed class ZoneSetupMenuTests
    {
        private const string LayoutPath = "Assets/Chilseok/Levels/zone-02.txt";

        private Scene _previousScene;
        private Scene _testScene;
        private Object _previousSelection;
        private bool _openedScene;
        private readonly HashSet<GameObject> _initialRoots = new();

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousSelection = Selection.activeObject;
            _testScene = default;
            _openedScene = false;
            _initialRoots.Clear();
            if (string.IsNullOrEmpty(_previousScene.path))
            {
                // 테스트 러너의 임시 씬을 사용하되 러너가 만든 루트는 보존한다.
                foreach (var root in _previousScene.GetRootGameObjects())
                {
                    Assert.That(root.name, Is.Not.EqualTo("Movement Preview"));
                    _initialRoots.Add(root);
                }
                _testScene = _previousScene;
                return;
            }

            _testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _openedScene = true;
            SceneManager.SetActiveScene(_testScene);
        }

        [TearDown]
        public void TearDown()
        {
            if (_openedScene)
            {
                SceneManager.SetActiveScene(_previousScene);
                EditorSceneManager.CloseScene(_testScene, removeScene: true);
            }
            else if (_testScene.IsValid())
            {
                foreach (var root in _testScene.GetRootGameObjects())
                {
                    if (!_initialRoots.Contains(root))
                        Object.DestroyImmediate(root);
                }
            }
            Selection.activeObject = _previousSelection;
        }

        [Test]
        public void Build_Zone02_PlacesBlocksMarkersAndSharedExit()
        {
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);

            var preview = ZoneSetupMenu.Build(layout).transform;

            Assert.That(preview.Find("Grid").childCount, Is.EqualTo(10));
            var player = preview.GetComponentInChildren<PlayerController2D>();
            Assert.That(player.transform.localPosition, Is.EqualTo(new Vector3(2f, 1f, 0f)));
            Assert.That(preview.Find("Markers/Start").localPosition.x, Is.EqualTo(1f));
            Assert.That(preview.Find("Markers/Magpie").localPosition, Is.EqualTo(new Vector3(7f, 2f, 3f)));
            var feathers = preview.Find("Markers/Feathers");
            Assert.That(feathers.childCount, Is.EqualTo(3));
            Assert.That(feathers.Find("Feather 2").gameObject.activeSelf, Is.False);
            Assert.That(feathers.Find("Feather 1").gameObject.activeSelf, Is.True);

            var path = preview.Find("Connector/Grid");
            Assert.That(path.childCount, Is.EqualTo(5));
            Assert.That(path.Find("Block 1,0,0").localPosition, Is.EqualTo(new Vector3(8f, 1.5f, 3f)));
            Assert.That(player.GetComponent<ZoneTracker>(), Is.Not.Null);
        }

        [Test]
        public void Build_Rebuild_ReplacesOnlyTheGeneratedRoot()
        {
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
            var unrelated = new GameObject("Unrelated root");
            var first = ZoneSetupMenu.Build(layout);

            var second = ZoneSetupMenu.Build(layout);

            Assert.That(first == null, Is.True);
            Assert.That(second.scene, Is.EqualTo(_testScene));
            Assert.That(_testScene.rootCount, Is.EqualTo(_initialRoots.Count + 2));
            Assert.That(unrelated.scene, Is.EqualTo(_testScene));
        }
    }
}
