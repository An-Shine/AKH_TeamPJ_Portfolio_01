#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster.Editor
{
    public static class BossMotionUpgradeValidator
    {
        [MenuItem("Tools/Monster/Boss Motion Upgrade/Validate Resources")]
        public static void Validate()
        {
            List<string> failures = new List<string>();
            TextAsset text = Resources.Load<TextAsset>(BossMotionUpgrade.ResourceRoot + "BossMotionCatalog");
            BossMotionCatalog catalog = text != null ? JsonUtility.FromJson<BossMotionCatalog>(text.text) : null;
            if (catalog == null || catalog.version != 1 || catalog.spriteCount != BossMotionUpgrade.ExpectedSpriteCount
                || catalog.sprites == null || catalog.sprites.Length != BossMotionUpgrade.ExpectedSpriteCount
                || catalog.clips == null || catalog.clips.Length != BossMotionUpgrade.ExpectedClipCount)
            {
                Debug.LogError("보스 동작 카탈로그 누락 또는 개수 오류. 모션 패치 전체를 적용하세요.");
                return;
            }
            HashSet<string> spritePaths = new HashSet<string>();
            HashSet<Sprite> sprites = new HashSet<Sprite>();
            foreach (string path in catalog.sprites)
            {
                Sprite sprite = Resources.Load<Sprite>(path);
                if (!spritePaths.Add(path) || sprite == null)
                {
                    failures.Add("스프라이트 누락 또는 중복: " + path);
                    continue;
                }
                sprites.Add(sprite);
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                if (importer == null || importer.filterMode != FilterMode.Point || importer.mipmapEnabled
                    || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || importer.npotScale != TextureImporterNPOTScale.None || importer.spritePixelsPerUnit <= 0f)
                {
                    failures.Add("Point / Mip Map OFF / Compression None / NPOT None / PPU 필요: " + path);
                }
            }
            HashSet<string> slots = new HashSet<string>();
            foreach (BossMotionClipEntry entry in catalog.clips)
            {
                if (entry == null || entry.family < 0 || entry.family > 3 || entry.stage < 0 || entry.stage > 3
                    || entry.direction < 0 || entry.direction > 7 || entry.family == 3 && entry.stage != 2
                    || !slots.Add(entry.family + ":" + entry.stage + ":" + entry.direction))
                {
                    failures.Add("동작·단계·방향 항목 중복 또는 오류.");
                    continue;
                }
                AnimationClip clip = Resources.Load<AnimationClip>(entry.path);
                if (clip == null || clip.empty || clip.length <= 0f)
                {
                    failures.Add("동작 클립 누락: " + entry.path);
                    continue;
                }
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                if (bindings.Length != 1 || bindings[0].type != typeof(SpriteRenderer)
                    || bindings[0].path != string.Empty || bindings[0].propertyName != "m_Sprite"
                    || AnimationUtility.GetCurveBindings(clip).Length != 0)
                {
                    failures.Add("SpriteRenderer.m_Sprite 전용 클립이어야 합니다: " + entry.path);
                    continue;
                }
                foreach (ObjectReferenceKeyframe frame in AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]))
                {
                    Sprite sprite = frame.value as Sprite;
                    if (sprite == null || !sprites.Contains(sprite))
                        failures.Add("보스 전용 프레임 참조 누락: " + entry.path);
                }
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Monster/BossMonster.prefab");
            Animator animator = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
            if (prefab == null || prefab.GetComponent<global::BossMonster>() == null || animator == null
                || animator.runtimeAnimatorController == null)
            {
                failures.Add("기존 BossMonster 프리팹과 Animator 연결을 확인하세요.");
            }

            if (failures.Count > 0)
            {
                Debug.LogError("보스 동작 리소스 확인 실패:\n" + string.Join("\n", failures));
            }
            else
            {
                Debug.Log("보스 동작 리소스 확인 완료: " + sprites.Count + "개 스프라이트 / " + slots.Count
                    + "개 클립 / 8방향. 준비·대기·발동·회복 연결과 실제 전투는 Play에서 확인하세요.");
            }
        }
    }
}
#endif
