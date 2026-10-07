#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace _02.Scripts.Monster.Presentation.Editor
{
    public sealed class NormalMonsterGameFeelSetupWindow : EditorWindow
    {
        private const string ProfileDirectory = "Assets/08.Data/MonsterPresentation";
        private const string FeelProfilePath = ProfileDirectory + "/NormalMonsterGameFeel.asset";
        private const string GroundProfilePath = ProfileDirectory + "/NormalMonsterGround.asset";
        private const string AudioDirectory = "Assets/Resources/Monster/NormalGameFeel/Audio/";
        private static readonly string[] TexturePlatforms = { "Standalone", "Android", "iPhone", "WebGL" };
        [SerializeField] private NormalMonsterFeelProfile _feelProfile;
        [SerializeField] private MonsterGroundProfile _groundProfile;
        [SerializeField] private AudioMixerGroup _soundMixerGroup;
        [SerializeField] private float _attackMultiplier = 2f;
        [SerializeField] private bool _sharpenSprites = true;
        private string _status;
        [MenuItem("Tools/Monster/Normal Monster Game Feel")]
        public static void Open()
        {
            GetWindow<NormalMonsterGameFeelSetupWindow>("Normal Monster Game Feel");
        }

        private void OnEnable()
        {
            if (_feelProfile == null)
            {
                _feelProfile = AssetDatabase.LoadAssetAtPath<NormalMonsterFeelProfile>(FeelProfilePath);
            }

            if (_groundProfile == null)
            {
                _groundProfile = AssetDatabase.LoadAssetAtPath<MonsterGroundProfile>(GroundProfilePath);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("일반 몬스터 게임필", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("근접·원거리·자폭 몬스터에 그림자/발자국을 연결하고, 근접·원거리 공격과 연출을 강화합니다.", MessageType.Info);
            _feelProfile = (NormalMonsterFeelProfile)EditorGUILayout.ObjectField("전투 연출 설정", _feelProfile, typeof(NormalMonsterFeelProfile), false);
            _groundProfile = (MonsterGroundProfile)EditorGUILayout.ObjectField("그림자·발자국 설정", _groundProfile, typeof(MonsterGroundProfile), false);
            _soundMixerGroup = (AudioMixerGroup)EditorGUILayout.ObjectField("SFX Mixer Group", _soundMixerGroup, typeof(AudioMixerGroup), false);
            _attackMultiplier = EditorGUILayout.Slider("공격 속도 배율", _attackMultiplier, 0.5f, 4f);
            _sharpenSprites = EditorGUILayout.Toggle("몬스터 픽셀 표시 선명하게", _sharpenSprites);
            EditorGUILayout.Space();
            if (GUILayout.Button("일반 몬스터 프리팹과 열린 씬에 적용", GUILayout.Height(34f)))
            {
                Install();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }
        }

        private void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _status = "Play를 종료한 뒤 적용하세요.";
                return;
            }

            try
            {
                EnsureFolder(ProfileDirectory);
                EnsureProfiles();
                HashSet<string> textures = new HashSet<string>();
                int prefabCount = ConfigurePrefabs(textures);
                int sceneCount = ConfigureSceneMonsters(textures);
                int textureCount = _sharpenSprites ? SharpenTextures(textures) : 0;
                global::_02.Scripts.Monster.NormalMonsters.Editor.ItemRoomAlertSetup.ConfigureRooms();
                AssetDatabase.SaveAssets();
                _status = $"프리팹 {prefabCount}개, 씬 몬스터 {sceneCount}개, 텍스처 {textureCount}개 적용. " + "Ctrl+S로 열린 씬을 저장하세요. 공격 배율은 재적용해도 중복 곱하지 않습니다.";
                Debug.Log(_status);
            }
            catch (Exception exception)
            {
                _status = "적용 중 오류가 발생했습니다. Console의 새 오류를 확인하세요.";
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void EnsureProfiles()
        {
            if (_feelProfile == null)
            {
                _feelProfile = AssetDatabase.LoadAssetAtPath<NormalMonsterFeelProfile>(FeelProfilePath);
                if (_feelProfile == null)
                {
                    _feelProfile = CreateInstance<NormalMonsterFeelProfile>();
                    AssetDatabase.CreateAsset(_feelProfile, FeelProfilePath);
                }
            }

            if (_groundProfile == null)
            {
                _groundProfile = AssetDatabase.LoadAssetAtPath<MonsterGroundProfile>(GroundProfilePath);
                if (_groundProfile == null)
                {
                    _groundProfile = CreateInstance<MonsterGroundProfile>();
                    AssetDatabase.CreateAsset(_groundProfile, GroundProfilePath);
                }
            }

            SerializedObject feel = new SerializedObject(_feelProfile);
            AssignClipsIfEmpty(feel, "_meleeChargeSounds", "MeleeCharge", 3);
            AssignClipsIfEmpty(feel, "_meleeSlashSounds", "MeleeSlash", 4);
            AssignClipsIfEmpty(feel, "_meleeImpactSounds", "MeleeImpact", 4);
            AssignClipsIfEmpty(feel, "_rangeChargeSounds", "RangeCharge", 3);
            AssignClipsIfEmpty(feel, "_rangeShotSounds", "RangeShot", 4);
            AssignClipsIfEmpty(feel, "_rangeImpactSounds", "RangeImpact", 4);
            if (_soundMixerGroup != null)
            {
                feel.FindProperty("_soundMixerGroup").objectReferenceValue = _soundMixerGroup;
            }

            feel.ApplyModifiedProperties();
            SerializedObject ground = new SerializedObject(_groundProfile);
            ground.FindProperty("_showShadow").boolValue = true;
            ground.FindProperty("_showFootprints").boolValue = true;
            ground.FindProperty("_showDust").boolValue = true;
            ground.FindProperty("_useAnimationEvents").boolValue = false;
            AssignClipsIfEmpty(ground, "_stepSounds", "Footstep", 4);
            if (_soundMixerGroup != null)
            {
                ground.FindProperty("_soundMixerGroup").objectReferenceValue = _soundMixerGroup;
            }

            ground.ApplyModifiedProperties();
            EditorUtility.SetDirty(_feelProfile);
            EditorUtility.SetDirty(_groundProfile);
        }

        private static void AssignClipsIfEmpty(SerializedObject data, string propertyName, string prefix, int count)
        {
            SerializedProperty array = data.FindProperty(propertyName);
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue != null)
                {
                    return;
                }
            }

            array.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                string path = AudioDirectory + prefix + "_" + (i + 1).ToString("00") + ".wav";
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    throw new InvalidOperationException("사운드 파일이 없습니다: " + path);
                }

                array.GetArrayElementAtIndex(i).objectReferenceValue = clip;
            }
        }

        private int ConfigurePrefabs(HashSet<string> textures)
        {
            int count = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || PrefabUtility.GetPrefabAssetType(asset) == PrefabAssetType.Model || asset.GetComponentInChildren<global::BossMonster>(true) != null || !ContainsNormalMonster(asset))
                {
                    continue;
                }

                EditorUtility.DisplayProgressBar("일반 몬스터 프리팹 적용", path, i / (float)guids.Length);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (NormalMonster monster in root.GetComponentsInChildren<NormalMonster>(true))
                    {
                        if (ConfigureMonster(monster, false))
                        {
                            count++;
                        }
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                {
                    if (dependency.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !dependency.Contains("/Boss/") && !dependency.Contains("/BossOverhaul/"))
                    {
                        textures.Add(dependency);
                    }
                }
            }

            return count;
        }

        private static bool ContainsNormalMonster(GameObject root)
        {
            foreach (NormalMonster monster in root.GetComponentsInChildren<NormalMonster>(true))
            {
                if (IsNormalType(monster))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNormalType(NormalMonster monster)
        {
            return monster is MeleeMonster || monster is RangeMonster || monster is KamikazeMonster;
        }

        private int ConfigureSceneMonsters(HashSet<string> textures)
        {
            int count = 0;
            NormalMonster[] monsters = Object.FindObjectsByType<NormalMonster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (NormalMonster monster in monsters)
            {
                if (EditorUtility.IsPersistent(monster) || !monster.gameObject.scene.IsValid() || !ConfigureMonster(monster, true))
                {
                    continue;
                }

                EditorSceneManager.MarkSceneDirty(monster.gameObject.scene);
                SpriteRenderer renderer = monster.GetComponentInChildren<SpriteRenderer>(true);
                AddTexture(renderer != null ? renderer.sprite : null, textures);
                Animator animator = monster.GetComponentInChildren<Animator>(true);
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                    {
                        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                        {
                            foreach (ObjectReferenceKeyframe key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                            {
                                AddTexture(key.value as Sprite, textures);
                            }
                        }
                    }
                }

                count++;
            }

            return count;
        }

        private bool ConfigureMonster(NormalMonster monster, bool undo)
        {
            if (!IsNormalType(monster))
            {
                return false;
            }

            SpriteRenderer body = monster.GetComponentInChildren<SpriteRenderer>(true);
            if (body == null)
            {
                Debug.LogWarning(monster.name + ": 본체 SpriteRenderer가 없습니다.", monster);
                return false;
            }

            NormalMonsterGameFeel feel = monster.GetComponent<NormalMonsterGameFeel>();
            if (feel == null)
            {
                feel = undo ? Undo.AddComponent<NormalMonsterGameFeel>(monster.gameObject) : monster.gameObject.AddComponent<NormalMonsterGameFeel>();
            }

            MonsterGroundPresentation ground = monster.GetComponent<MonsterGroundPresentation>();
            if (ground == null)
            {
                ground = undo ? Undo.AddComponent<MonsterGroundPresentation>(monster.gameObject) : monster.gameObject.AddComponent<MonsterGroundPresentation>();
            }

            if (undo)
            {
                Undo.RecordObjects(new Object[] { feel, ground, monster }, "Configure Normal Monster Game Feel");
            }

            feel.enabled = true;
            ground.enabled = true;
            SerializedObject feelData = new SerializedObject(feel);
            feelData.FindProperty("_profile").objectReferenceValue = _feelProfile;
            feelData.FindProperty("_bodyRenderer").objectReferenceValue = body;
            feelData.ApplyModifiedProperties();
            SerializedObject groundData = new SerializedObject(ground);
            groundData.FindProperty("_profile").objectReferenceValue = _groundProfile;
            groundData.FindProperty("_bodyRenderer").objectReferenceValue = body;
            groundData.FindProperty("_animator").objectReferenceValue = monster.GetComponentInChildren<Animator>(true);
            groundData.ApplyModifiedProperties();
            if (monster is MeleeMonster || monster is RangeMonster)
            {
                SerializedObject combat = new SerializedObject(monster);
                combat.FindProperty("_attackSpeedMultiplier").floatValue = _attackMultiplier;
                combat.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(feel);
            EditorUtility.SetDirty(ground);
            EditorUtility.SetDirty(monster);
            if (undo)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(feel);
                PrefabUtility.RecordPrefabInstancePropertyModifications(ground);
                PrefabUtility.RecordPrefabInstancePropertyModifications(monster);
            }

            return true;
        }

        private static void AddTexture(Sprite sprite, HashSet<string> textures)
        {
            if (sprite != null)
            {
                string path = AssetDatabase.GetAssetPath(sprite.texture);
                if (!string.IsNullOrEmpty(path))
                {
                    textures.Add(path);
                }
            }
        }

        private static int SharpenTextures(HashSet<string> paths)
        {
            int count = 0;
            foreach (string path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                {
                    continue;
                }

                EditorUtility.DisplayProgressBar("몬스터 픽셀 표시 설정", path, count / (float)Mathf.Max(1, paths.Count));
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                int requiredSize = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(width, height)), 32, 16384);
                importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, requiredSize);
                foreach (string platform in TexturePlatforms)
                {
                    TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
                    if (!settings.overridden)
                    {
                        continue;
                    }

                    settings.textureCompression = TextureImporterCompression.Uncompressed;
                    settings.format = TextureImporterFormat.RGBA32;
                    settings.maxTextureSize = Mathf.Max(settings.maxTextureSize, requiredSize);
                    importer.SetPlatformTextureSettings(settings);
                }

                importer.SaveAndReimport();
                count++;
            }

            return count;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string child = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(child))
                {
                    AssetDatabase.CreateFolder(parent, parts[i]);
                }

                parent = child;
            }
        }
    }
}
#endif
