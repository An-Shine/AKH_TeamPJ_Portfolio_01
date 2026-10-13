using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _02.Scripts.Monster.Presentation.Editor
{
    public static class NormalMonsterUpgradeValidator
    {
        private const int ExpectedFrameCount = 830;
        private const int ExpectedSpriteCount = 28;

        [MenuItem("Tools/Monster/Normal Range Upgrade/Validate Resources")]
        public static void ValidateResources()
        {
            List<string> failures = new List<string>();
            string[] roots =
            {
                "Assets/Resources/Monster/Melee",
                "Assets/Resources/Monster/Range",
                "Assets/Resources/Monster/Kamikaze"
            };
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", roots);
            int frameCount = 0;
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/PatternVfx/"))
                    continue;
                frameCount++;
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.spriteImportMode != SpriteImportMode.Single
                    || Mathf.Abs(importer.spritePixelsPerUnit - 96f) > 0.01f
                    || importer.filterMode != FilterMode.Point || importer.mipmapEnabled
                    || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || importer.npotScale != TextureImporterNPOTScale.None)
                {
                    failures.Add("도트 2배 확대 Import 설정 오류: " + path);
                }
            }

            if (frameCount != ExpectedFrameCount)
                failures.Add("일반 몬스터 프레임 개수: " + frameCount + " / 기대값: " + ExpectedFrameCount);
            HashSet<Sprite> sprites = new HashSet<Sprite>();
            HashSet<Texture2D> textures = new HashSet<Texture2D>();
            for (int pattern = 0; pattern < RangeAttackPatterns.Count; pattern++)
            {
                if ((RangeAttackPattern)pattern == RangeAttackPattern.Single)
                    continue;
                string resourcePath = RangePatternVfx.ResourceRoot + (RangeAttackPattern)pattern;
                Sprite[] atlas = Resources.LoadAll<Sprite>(resourcePath);
                if (atlas.Length != 4)
                    failures.Add("탄환·총구·적중·잔상 4개 Sprite 필요: " + resourcePath);
                HashSet<string> names = new HashSet<string>();
                foreach (Sprite sprite in atlas)
                {
                    sprites.Add(sprite);
                    textures.Add(sprite.texture);
                    names.Add(sprite.name);
                    TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                    if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple
                        || importer.filterMode != FilterMode.Point || importer.mipmapEnabled
                        || importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        failures.Add("패턴 이펙트 Import 설정 오류: " + resourcePath);
                    }
                }

                if (!names.Contains("Projectile") || !names.Contains("Muzzle")
                    || !names.Contains("Impact") || !names.Contains("Trail"))
                {
                    failures.Add("패턴 이펙트 이름 또는 Sprite 참조 누락: " + resourcePath);
                }
            }

            if (sprites.Count != ExpectedSpriteCount || textures.Count != 7)
                failures.Add("기본·Elite·호환 리소스 7개 Atlas / 28개 Sprite가 필요합니다.");
            Material bulletMaterial = Resources.Load<Material>(RangePatternVfx.ResourceRoot + "RangePatternUnlit");
            Material aimMaterial = Resources.Load<Material>(RangePatternVfx.ResourceRoot + "RangePatternAim");
            Material bodyMaterial = Resources.Load<Material>("Monster/NormalGameFeel/NormalMonsterOpaque");
            if (!SupportsShader(bulletMaterial, "Universal Render Pipeline/2D/Sprite-Unlit-Default")
                || !SupportsShader(bodyMaterial, "Universal Render Pipeline/2D/Sprite-Unlit-Default")
                || !SupportsShader(aimMaterial, "Universal Render Pipeline/Particles/Unlit"))
            {
                failures.Add("몬스터 본체·탄환·조준선의 URP Unlit 재질 연결을 확인하세요.");
            }

            string[] normalPrefabs = { "MeleeMonster", "RangeMonster", "RangeMonsterElite", "KamikazeMonster" };
            foreach (string prefabName in normalPrefabs)
            {
                GameObject normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Monster/" + prefabName + ".prefab");
                SpriteRenderer body = normalPrefab != null ? normalPrefab.GetComponent<SpriteRenderer>() : null;
                if (body == null || body.sharedMaterial != bodyMaterial || Mathf.Abs(body.color.a - 1f) > 0.001f)
                    failures.Add("일반 몬스터의 본체 재질·불투명도 연결 오류: " + prefabName);
            }
            ValidateRangePrefab("RangeMonster", false, failures);
            ValidateRangePrefab("RangeMonsterElite", true, failures);

            if (failures.Count > 0)
            {
                Debug.LogError("일반 몬스터 업그레이드 확인 실패:\n" + string.Join("\n", failures));
            }
            else
            {
                Debug.Log("일반 몬스터 리소스 확인 완료: " + frameCount + "개 2배 도트 프레임 / "
                    + "기본 단발 + 추가 2종 / Elite 전용 3종 / " + sprites.Count + "개 이펙트. "
                    + "실제 전투, 피격 취소, 벽 충돌, 일시 정지는 Play에서 확인하세요.");
            }
        }

        private static void ValidateRangePrefab(string prefabName, bool elite, List<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Monster/" + prefabName + ".prefab");
            RangeMonster monster = prefab != null ? prefab.GetComponent<RangeMonster>() : null;
            if (monster == null || monster.IsElite != elite)
            {
                failures.Add("기본·Elite Script 연결 오류: " + prefabName);
                return;
            }

            SerializedObject settings = new SerializedObject(monster);
            if (settings.FindProperty("_projectilePrefab").objectReferenceValue == null
                || !settings.FindProperty("_useVariedPatterns").boolValue
                || settings.FindProperty("_attackSpeedMultiplier").floatValue < 2.3f
                || elite && settings.FindProperty("_minimumAimTime").floatValue < 0.3f)
            {
                failures.Add("탄막·전조·공격 속도·투사체 연결 오류: " + prefabName);
            }
        }

        private static bool SupportsShader(Material material, string shaderName)
        {
            return material != null && material.shader != null && material.shader.isSupported
                && material.shader.name == shaderName;
        }
    }
}
