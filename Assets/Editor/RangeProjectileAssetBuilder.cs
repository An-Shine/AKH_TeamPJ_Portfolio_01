#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

internal static class RangeProjectileAssetBuilder
{
    private const string ResourceRoot = "Assets/MonsterResources/RangeProjectileResources";
    private const float PixelsPerUnit = 32f;
    private const float ProjectileRadius = 0.12f;

    [MenuItem("Tools/Monster/Build Range Projectile")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Play를 종료한 다음 실행하세요.");
            return;
        }

        GameObject selectedPrefab = Selection.activeObject as GameObject;
        string monsterPath = AssetDatabase.GetAssetPath(selectedPrefab);

        if (selectedPrefab == null || !monsterPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
            selectedPrefab.GetComponentsInChildren<RangeMonster>(true).Length != 1)
        {
            Debug.LogError("Project 창에서 RangeMonster가 하나 들어 있는 원거리 몬스터 프리팹을 선택하세요.");
            return;
        }

        GameObject monsterRoot = null;

        try
        {
            ValidateResources();
            monsterRoot = PrefabUtility.LoadPrefabContents(monsterPath);
            RangeMonster monster = monsterRoot.GetComponentInChildren<RangeMonster>(true);
            SerializedObject monsterData = new SerializedObject(monster);
            SerializedProperty projectileSlot = GetProperty(monsterData, "_projectilePrefab");
            RangeProjectile existingProjectile = projectileSlot.objectReferenceValue as RangeProjectile;

            if (existingProjectile != null)
            {
                GetProperty(new SerializedObject(existingProjectile), "_fireSound");
            }
            else
            {
                ValidateProjectileScript();
            }

            SpriteRenderer sourceRenderer = monster.GetComponentInChildren<SpriteRenderer>(true);
            Sprite[] flightFrames = ImportFrames("Projectile", 8);
            Sprite[] impactFrames = ImportFrames("Impact", 5);
            AudioClip fireSound = LoadSound("laserSmall_000.ogg");
            AudioClip playerHitSound = LoadSound("explosionCrunch_000.ogg");
            AudioClip obstacleHitSound = LoadSound("impactMetal_002.ogg");

            string output = AssetDatabase.GenerateUniqueAssetPath(ResourceRoot + "/Generated");

            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(ResourceRoot, Path.GetFileName(output))))
            {
                throw new InvalidOperationException("생성 결과 폴더를 만들지 못했습니다.");
            }

            AnimationClip flightClip = CreateClip(output + "/RangeProjectileFly.anim", flightFrames, 16f, true);
            AnimationClip impactClip = CreateClip(output + "/RangeProjectileHit.anim", impactFrames, 20f, false);
            AnimatorController flightController = AnimatorController.CreateAnimatorControllerAtPathWithClip(
                output + "/RangeProjectileFly.controller", flightClip);
            AnimatorController impactController = AnimatorController.CreateAnimatorControllerAtPathWithClip(
                output + "/RangeProjectileHit.controller", impactClip);

            if (flightController == null || impactController == null)
            {
                throw new InvalidOperationException("Animator Controller를 만들지 못했습니다.");
            }

            GameObject impactPrefab = CreateImpactPrefab(output, impactFrames[0], impactController, sourceRenderer);
            GameObject projectilePrefab = CreateProjectilePrefab(output, flightFrames[0], flightController,
                sourceRenderer, impactPrefab, fireSound, playerHitSound, obstacleHitSound, existingProjectile);

            projectileSlot.objectReferenceValue = projectilePrefab.GetComponent<RangeProjectile>();
            monsterData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(monsterRoot, monsterPath, out bool saved);

            if (!saved)
            {
                throw new InvalidOperationException("RangeMonster 프리팹에 투사체 연결을 저장하지 못했습니다.");
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = projectilePrefab;
            EditorGUIUtility.PingObject(projectilePrefab);
            Debug.Log($"원거리 투사체 생성 및 연결 완료: {output}\n피해량 10, 발사/피격/벽 충돌 사운드를 연결했습니다.",
                projectilePrefab);
        }
        catch (Exception exception)
        {
            Debug.LogError($"원거리 투사체 설정 실패: {exception.Message}");
        }
        finally
        {
            if (monsterRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(monsterRoot);
            }
        }
    }

    private static void ValidateResources()
    {
        for (int i = 1; i <= 8; i++)
        {
            RequireFile($"{ResourceRoot}/Projectile/frame{i:000}.png");
        }

        for (int i = 1; i <= 5; i++)
        {
            RequireFile($"{ResourceRoot}/Impact/frame{i:000}.png");
        }

        RequireFile(ResourceRoot + "/Audio/laserSmall_000.ogg");
        RequireFile(ResourceRoot + "/Audio/explosionCrunch_000.ogg");
        RequireFile(ResourceRoot + "/Audio/impactMetal_002.ogg");
    }

    private static void RequireFile(string assetPath)
    {
        string absolutePath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));

        if (!File.Exists(absolutePath))
        {
            throw new InvalidOperationException($"리소스가 없습니다: {assetPath}");
        }
    }

    private static void ValidateProjectileScript()
    {
        GameObject temporary = new GameObject("RangeProjectileScriptCheck");

        try
        {
            RangeProjectile projectile = temporary.AddComponent<RangeProjectile>();
            GetProperty(new SerializedObject(projectile), "_fireSound");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(temporary);
        }
    }

    private static Sprite[] ImportFrames(string folder, int count)
    {
        Sprite[] frames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            string path = $"{ResourceRoot}/{folder}/frame{i + 1:000}.png";
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
            throw new InvalidOperationException($"사운드를 불러오지 못했습니다: {path}");
        }

        return clip;
    }

    private static AnimationClip CreateClip(string path, Sprite[] frames, float frameRate, bool loop)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = Path.GetFileNameWithoutExtension(path);
        clip.frameRate = frameRate;

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];

        for (int i = 0; i < frames.Length; i++)
        {
            keys[i].time = i / frameRate;
            keys[i].value = frames[i];
        }

        keys[frames.Length].time = frames.Length / frameRate;
        keys[frames.Length].value = loop ? frames[0] : null;

        EditorCurveBinding binding = new EditorCurveBinding();
        binding.path = string.Empty;
        binding.type = typeof(SpriteRenderer);
        binding.propertyName = "m_Sprite";
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static GameObject CreateVisual(string name, Sprite firstFrame,
        RuntimeAnimatorController controller, SpriteRenderer sourceRenderer, int orderOffset)
    {
        GameObject root = new GameObject(name);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = firstFrame;
        renderer.sortingLayerID = sourceRenderer != null ? sourceRenderer.sortingLayerID : 0;
        renderer.sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder + orderOffset : orderOffset;

        if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
        {
            renderer.sharedMaterial = sourceRenderer.sharedMaterial;
        }

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return root;
    }

    private static GameObject CreateImpactPrefab(string output, Sprite firstFrame,
        RuntimeAnimatorController controller, SpriteRenderer sourceRenderer)
    {
        GameObject root = CreateVisual("RangeProjectileImpact", firstFrame, controller, sourceRenderer, 3);

        try
        {
            return SavePrefab(root, output + "/RangeProjectileImpact.prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateProjectilePrefab(string output, Sprite firstFrame,
        RuntimeAnimatorController controller, SpriteRenderer sourceRenderer, GameObject impactPrefab,
        AudioClip fireSound, AudioClip playerHitSound, AudioClip obstacleHitSound, RangeProjectile existingProjectile)
    {
        GameObject root = CreateVisual("RangeProjectile", firstFrame, controller, sourceRenderer, 2);

        try
        {
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = ProjectileRadius;

            RangeProjectile projectile = root.AddComponent<RangeProjectile>();
            SerializedObject data = new SerializedObject(projectile);
            GetProperty(data, "_moveSpeed").floatValue = 8f;
            GetProperty(data, "_lifeTime").floatValue = 3f;
            GetProperty(data, "_damage").floatValue = 10f;
            GetProperty(data, "_sweepRadius").floatValue = ProjectileRadius;
            GetProperty(data, "_hitLayers").intValue = ~0;
            GetProperty(data, "_impactEffectPrefab").objectReferenceValue = impactPrefab;
            GetProperty(data, "_impactEffectLifetime").floatValue = 0.3f;
            GetProperty(data, "_fireSound").objectReferenceValue = fireSound;
            GetProperty(data, "_playerHitSound").objectReferenceValue = playerHitSound;
            GetProperty(data, "_obstacleHitSound").objectReferenceValue = obstacleHitSound;
            GetProperty(data, "_soundVolume").floatValue = 0.75f;

            if (existingProjectile != null)
            {
                SerializedObject previous = new SerializedObject(existingProjectile);
                GetProperty(data, "_soundMixerGroup").objectReferenceValue =
                    GetProperty(previous, "_soundMixerGroup").objectReferenceValue;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, output + "/RangeProjectile.prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);

        if (!saved || prefab == null)
        {
            throw new InvalidOperationException($"프리팹을 저장하지 못했습니다: {path}");
        }

        return prefab;
    }

    private static SerializedProperty GetProperty(SerializedObject data, string name)
    {
        SerializedProperty property = data.FindProperty(name);

        if (property == null)
        {
            throw new InvalidOperationException($"{name} 항목이 없습니다. 제공한 최신 RangeProjectile.cs로 교체하세요.");
        }

        return property;
    }
}
#endif
