#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using _02.Scripts.Monster.NormalMonsters;

namespace _02.Scripts.Editor
{
    internal static class MeleeKamikazeAssetBuilder
    {
        private const string ResourceRoot = "Assets/MonsterResources/MeleeKamikazeResources";
        private const float PixelsPerUnit = 32f;
        private const float MeleeFrameRate = 24f;
        private const float ExplosionFrameRate = 20f;

        [MenuItem("Tools/Monster/Build Melee Effects")]
        private static void BuildMelee()
        {
            Build("MeleeMonster", "MeleeAttack", "meleeAttack.ogg",
                "_attackEffectPrefab", "_attackEffectLifetime", "_attackRange",
                MeleeFrameRate);
        }

        [MenuItem("Tools/Monster/Build Kamikaze Effects")]
        private static void BuildKamikaze()
        {
            Build("KamikazeMonster", "KamikazeExplosion", "kamikazeExplosion.ogg",
                "_explosionEffectPrefab", "_explosionEffectLifetime", "_explosionDamageRadius",
                ExplosionFrameRate);
        }

        private static void Build(
            string monsterType,
            string frameFolder,
            string soundFile,
            string effectField,
            string lifetimeField,
            string radiusField,
            float frameRate)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Play를 종료한 다음 실행하세요.");
                return;
            }

            GameObject selectedPrefab = Selection.activeObject as GameObject;
            string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);

            if (selectedPrefab == null ||
                !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"Project 창에서 {monsterType} 프리팹을 선택하세요.");
                return;
            }

            GameObject monsterRoot = null;

            try
            {
                monsterRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                MonoBehaviour monster = FindMonster(monsterRoot, monsterType);
                SerializedObject monsterData = new SerializedObject(monster);
                SerializedProperty effectSlot = GetProperty(monsterData, effectField);
                SerializedProperty lifetime = GetProperty(monsterData, lifetimeField);
                float radius = Mathf.Max(0.1f, GetProperty(monsterData, radiusField).floatValue);

                Sprite[] frames = ImportFrames(frameFolder);
                AudioClip sound = LoadSound(soundFile);
                SpriteRenderer sourceRenderer = monster.GetComponent<SpriteRenderer>();

                if (sourceRenderer == null)
                {
                    sourceRenderer = monster.GetComponentInChildren<SpriteRenderer>(true);
                }

                string output = CreateOutputFolder(monsterType);
                GameObject effectPrefab = CreateEffectPrefab(
                    output, frameFolder, frames, sound, frameRate, radius,
                    sourceRenderer, monster.gameObject.layer);

                effectSlot.objectReferenceValue = effectPrefab;
                lifetime.floatValue = frames.Length / frameRate + 0.1f;
                monsterData.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(monsterRoot, prefabPath, out bool saved);

                if (!saved)
                {
                    throw new InvalidOperationException("몬스터 프리팹에 이펙트 연결을 저장하지 못했습니다.");
                }

                AssetDatabase.SaveAssets();
                Selection.activeObject = effectPrefab;
                EditorGUIUtility.PingObject(effectPrefab);
                Debug.Log($"{monsterType} 이펙트·사운드 연결 완료: {output}", effectPrefab);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{monsterType} 설정 실패: {exception.Message}");
            }
            finally
            {
                if (monsterRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(monsterRoot);
                }
            }
        }

        private static MonoBehaviour FindMonster(GameObject root, string typeName)
        {
            MonoBehaviour result = null;

            // 클래스 이름으로 확인해서 몬스터의 네임스페이스가 달라도 연결합니다.
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Name != typeName)
                {
                    continue;
                }

                if (result != null)
                {
                    throw new InvalidOperationException($"{typeName}가 하나 들어 있는 프리팹을 선택하세요.");
                }

                result = component;
            }

            if (result == null)
            {
                throw new InvalidOperationException($"선택한 프리팹에 {typeName} 스크립트가 없습니다.");
            }

            return result;
        }

        private static Sprite[] ImportFrames(string folderName)
        {
            string folder = ResourceRoot + "/" + folderName;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                throw new InvalidOperationException($"리소스 폴더가 없습니다: {folder}");
            }

            string absoluteFolder = Path.Combine(Application.dataPath, folder.Substring("Assets/".Length));
            string[] paths = Directory.GetFiles(absoluteFolder, "frame*.png", SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.Ordinal);

            if (paths.Length == 0)
            {
                throw new InvalidOperationException($"이펙트 프레임이 없습니다: {folder}/frame001.png");
            }

            Sprite[] frames = new Sprite[paths.Length];

            for (int i = 0; i < paths.Length; i++)
            {
                string path = folder + "/" + Path.GetFileName(paths[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                {
                    throw new InvalidOperationException($"이미지를 가져오지 못했습니다: {path}");
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();

                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                if (frames[i] == null)
                {
                    throw new InvalidOperationException($"Sprite를 불러오지 못했습니다: {path}");
                }
            }

            return frames;
        }

        private static AudioClip LoadSound(string filename)
        {
            string path = ResourceRoot + "/Audio/" + filename;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (clip == null)
            {
                throw new InvalidOperationException($"사운드가 없습니다: {path}");
            }

            return clip;
        }

        private static string CreateOutputFolder(string monsterType)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath(ResourceRoot + "/Generated_" + monsterType);
            string guid = AssetDatabase.CreateFolder(ResourceRoot, Path.GetFileName(path));

            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException("이펙트 프리팹을 저장할 폴더를 만들지 못했습니다.");
            }

            return path;
        }

        private static GameObject CreateEffectPrefab(
            string output,
            string effectName,
            Sprite[] frames,
            AudioClip sound,
            float frameRate,
            float radius,
            SpriteRenderer sourceRenderer,
            int layer)
        {
            GameObject root = new GameObject(effectName);

            try
            {
                root.layer = layer;

                // 이미지 전체의 지름을 공격·폭발 반경에 맞춥니다.
                float spriteDiameter = Mathf.Max(frames[0].bounds.size.x, frames[0].bounds.size.y);
                root.transform.localScale = Vector3.one * (radius * 2f / Mathf.Max(0.01f, spriteDiameter));

                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingLayerID = sourceRenderer != null ? sourceRenderer.sortingLayerID : 0;
                renderer.sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder + 2 : 2;

                if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
                {
                    renderer.sharedMaterial = sourceRenderer.sharedMaterial;
                }

                MonsterEffectPlayback playback = root.AddComponent<MonsterEffectPlayback>();
                SerializedObject playbackData = new SerializedObject(playback);
                SerializedProperty frameArray = GetProperty(playbackData, "_frames");
                frameArray.arraySize = frames.Length;

                for (int i = 0; i < frames.Length; i++)
                {
                    frameArray.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                }

                GetProperty(playbackData, "_frameRate").floatValue = frameRate;
                GetProperty(playbackData, "_sound").objectReferenceValue = sound;
                GetProperty(playbackData, "_soundVolume").floatValue = 0.75f;
                playbackData.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, output + "/" + effectName + ".prefab");

                if (prefab == null)
                {
                    throw new InvalidOperationException("이펙트 프리팹을 저장하지 못했습니다.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static SerializedProperty GetProperty(SerializedObject target, string name)
        {
            SerializedProperty property = target.FindProperty(name);

            if (property == null)
            {
                throw new InvalidOperationException($"{target.targetObject.name}: 필요한 필드 {name}이 없습니다.");
            }

            return property;
        }
    }
}
#endif
