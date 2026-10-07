#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster.Editor
{
    public sealed class BossOverhaulSetupWindow : EditorWindow
    {
        private const string DefaultPrefabPath = "Assets/03.Prefabs/Monster/BossMonster.prefab";
        private const string ProfilePath = "Assets/08.Data/MonsterPresentation/BossEncounter.asset";
        private const string ControllerPath = "Assets/MonsterResources/BossOverhaul/Generated/BossEncounter.controller";

        private GameObject _bossPrefab;
        private global::BossMonster _sceneBoss;
        private global::BossRoom _room;
        private Camera _camera;
        private BossEncounterProfile _profile;
        private bool _applyChaseDefaults = true;
        private bool _useEnemySorting = true;
        private Vector2 _scroll;
        private string _message;

        [MenuItem("Tools/Monster/Boss Overhaul Setup")]
        public static void Open()
        {
            GetWindow<BossOverhaulSetupWindow>("Boss Overhaul");
        }

        private void OnEnable()
        {
            minSize = new Vector2(460f, 430f);
            _bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPrefabPath);
            _profile = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>(ProfilePath);
            RefreshSceneReferences();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("보스 24패턴 + 3단계 전투 적용", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("대형 효과와 96개 사운드가 포함되어 있습니다. 체력 단계에 따라 10종 → 18종 → 24종이 열립니다.", MessageType.Info);
            _bossPrefab = (GameObject)EditorGUILayout.ObjectField("Project 보스 프리팹", _bossPrefab, typeof(GameObject), false);
            _sceneBoss = (global::BossMonster)EditorGUILayout.ObjectField("Hierarchy 씬 보스", _sceneBoss, typeof(global::BossMonster), true);
            _room = (global::BossRoom)EditorGUILayout.ObjectField("씬 BossRoom", _room, typeof(global::BossRoom), true);
            _camera = (Camera)EditorGUILayout.ObjectField("쉐이크 카메라", _camera, typeof(Camera), true);
            _profile = (BossEncounterProfile)EditorGUILayout.ObjectField("전투 설정", _profile, typeof(BossEncounterProfile), false);
            _applyChaseDefaults = EditorGUILayout.Toggle("추적 속도 0.8 적용", _applyChaseDefaults);
            _useEnemySorting = EditorGUILayout.Toggle("Enemy Sorting Layer 적용", _useEnemySorting);
            EditorGUILayout.Space();
            if (GUILayout.Button("현재 씬 연결 다시 찾기"))
            {
                RefreshSceneReferences();
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("프리팹과 씬 보스에 적용", GUILayout.Height(36f)))
                {
                    Apply();
                }
                if (GUILayout.Button("전투 설정 열기") && _profile != null)
                {
                    Selection.activeObject = _profile;
                    EditorGUIUtility.PingObject(_profile);
                }
                if (GUILayout.Button("기존 설정에 빠진 패턴 추가") && _profile != null)
                {
                    Undo.RecordObject(_profile, "Add Missing Boss Patterns");
                    int added = _profile.AddMissingPatterns();
                    EditorUtility.SetDirty(_profile);
                    AssetDatabase.SaveAssets();
                    _message = added + "개 패턴을 추가했습니다. 기존 항목의 설정은 유지됩니다.";
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("설정은 한 번 적용하면 저장됩니다. 적용 후 씬을 Ctrl+S로 저장하세요. 체력·피해·기존 스프라이트·Player 코드는 바꾸지 않습니다.", MessageType.None);
            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RefreshSceneReferences()
        {
            _sceneBoss = null;
            if (Selection.activeGameObject != null)
            {
                _sceneBoss = Selection.activeGameObject.GetComponentInParent<global::BossMonster>();
                if (_sceneBoss != null && EditorUtility.IsPersistent(_sceneBoss))
                {
                    _sceneBoss = null;
                }
            }
#if UNITY_2022_2_OR_NEWER
            global::BossMonster[] bosses = UnityEngine.Object.FindObjectsByType<global::BossMonster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            global::BossRoom[] rooms = UnityEngine.Object.FindObjectsByType<global::BossRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            global::BossMonster[] bosses = UnityEngine.Object.FindObjectsOfType<global::BossMonster>(true);
            global::BossRoom[] rooms = UnityEngine.Object.FindObjectsOfType<global::BossRoom>(true);
#endif
            if (_sceneBoss == null && bosses.Length == 1)
            {
                _sceneBoss = bosses[0];
            }
            _room = null;
            if (_sceneBoss != null)
            {
                SerializedObject serialized = new SerializedObject(_sceneBoss);
                _room = serialized.FindProperty("_room").objectReferenceValue as global::BossRoom;
                foreach (global::BossRoom room in rooms)
                {
                    if (_room == null && room.gameObject.scene == _sceneBoss.gameObject.scene &&
                        room.Contains(_sceneBoss.transform.position, 0f))
                    {
                        _room = room;
                    }
                }
            }
            if (_room == null && rooms.Length == 1)
            {
                _room = rooms[0];
            }
            _camera = Camera.main;
        }

        private void Apply()
        {
            GameObject contents = null;
            try
            {
                if (_bossPrefab == null && _sceneBoss == null)
                {
                    throw new InvalidOperationException("보스 프리팹 또는 씬의 BossMonster를 연결하세요.");
                }
                string prefabPath = _bossPrefab != null ? AssetDatabase.GetAssetPath(_bossPrefab) : null;
                if (_bossPrefab != null)
                {
                    if (!prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Project 창의 .prefab 파일을 연결하세요.");
                    }
                    contents = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (contents.GetComponentInChildren<global::BossMonster>(true) == null)
                    {
                        throw new InvalidOperationException("선택한 프리팹에 BossMonster가 없습니다.");
                    }
                }
                if (_profile == null)
                {
                    EnsureFolder("Assets/08.Data/MonsterPresentation");
                    _profile = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>(ProfilePath);
                    if (_profile == null)
                    {
                        _profile = ScriptableObject.CreateInstance<BossEncounterProfile>();
                        _profile.InitializeDefaults();
                        AssetDatabase.CreateAsset(_profile, ProfilePath);
                    }
                }
                if (_profile.Moves == null || _profile.Moves.Length == 0)
                {
                    _profile.InitializeDefaults();
                    EditorUtility.SetDirty(_profile);
                }
                else if (_profile.AddMissingPatterns() > 0)
                {
                    EditorUtility.SetDirty(_profile);
                }
                if (contents != null)
                {
                    Configure(contents.GetComponentInChildren<global::BossMonster>(true), false);
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
                if (_sceneBoss != null)
                {
                    Undo.RecordObject(_sceneBoss, "Apply Boss Overhaul");
                    Configure(_sceneBoss, true);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(_sceneBoss);
                    EditorSceneManager.MarkSceneDirty(_sceneBoss.gameObject.scene);
                }
                AssetDatabase.SaveAssets();
                Selection.activeObject = _profile;
                _message = "보스 전투 적용 완료. 씬을 저장하고 플레이하세요. 전투 설정의 Moves에서 사운드와 효과 프리팹을 교체할 수 있습니다.";
            }
            catch (Exception exception)
            {
                _message = exception.Message;
                Debug.LogError("보스 적용 실패: " + exception.Message);
            }
            finally
            {
                if (contents != null)
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        private void Configure(global::BossMonster boss, bool sceneObject)
        {
            SerializedObject serialized = new SerializedObject(boss);
            serialized.FindProperty("_encounterProfile").objectReferenceValue = _profile;
            serialized.FindProperty("_directionAnimator").objectReferenceValue = boss.GetComponentInChildren<Animator>(true);
            if (_applyChaseDefaults)
            {
                serialized.FindProperty("_enableSlowChase").boolValue = true;
                serialized.FindProperty("_slowMoveSpeed").floatValue = 0.8f;
                serialized.FindProperty("_roomEdgePadding").floatValue = 1.1f;
            }
            if (sceneObject)
            {
                if (_room != null)
                {
                    serialized.FindProperty("_room").objectReferenceValue = _room;
                }
                if (_camera != null)
                {
                    serialized.FindProperty("_shakeCamera").objectReferenceValue = _camera;
                }
            }
            serialized.ApplyModifiedProperties();
            Animator animator = boss.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                ConfigureController(animator, sceneObject);
            }
            if (_useEnemySorting)
            {
                foreach (SortingLayer layer in SortingLayer.layers)
                {
                    if (layer.name != "Enemy")
                    {
                        continue;
                    }
                    foreach (SpriteRenderer renderer in boss.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (sceneObject)
                        {
                            Undo.RecordObject(renderer, "Boss Sorting Layer");
                        }
                        renderer.sortingLayerID = layer.id;
                        EditorUtility.SetDirty(renderer);
                        if (sceneObject)
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        }
                    }
                    break;
                }
            }
            EditorUtility.SetDirty(boss);
        }

        private static void ConfigureController(Animator animator, bool sceneObject)
        {
            if (!(animator.runtimeAnimatorController is AnimatorController source))
            {
                return;
            }
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                EnsureFolder("Assets/MonsterResources/BossOverhaul/Generated");
                string sourcePath = AssetDatabase.GetAssetPath(source);
                if (!AssetDatabase.CopyAsset(sourcePath, ControllerPath))
                {
                    throw new InvalidOperationException("보스 Animator Controller 복사에 실패했습니다: " + sourcePath);
                }
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                foreach (AnimatorControllerLayer layer in controller.layers)
                {
                    RemoveAutomaticTransitions(layer.stateMachine);
                }
                EditorUtility.SetDirty(controller);
            }
            if (sceneObject)
            {
                Undo.RecordObject(animator, "Boss Animation Controller");
            }
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);
            if (sceneObject)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }
        }

        private static void RemoveAutomaticTransitions(AnimatorStateMachine machine)
        {
            machine.anyStateTransitions = new AnimatorStateTransition[0];
            machine.entryTransitions = new AnimatorTransition[0];
            foreach (ChildAnimatorState state in machine.states)
            {
                state.state.transitions = new AnimatorStateTransition[0];
                EditorUtility.SetDirty(state.state);
            }
            foreach (ChildAnimatorStateMachine child in machine.stateMachines)
            {
                machine.SetStateMachineTransitions(child.stateMachine, new AnimatorTransition[0]);
                RemoveAutomaticTransitions(child.stateMachine);
            }
            EditorUtility.SetDirty(machine);
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
