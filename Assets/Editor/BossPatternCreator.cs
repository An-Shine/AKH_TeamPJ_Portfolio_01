using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BossPatternCreator
{
    private const string FolderPath = "Assets/BossPatterns";
    private static readonly string[] AssetNames =
    {
        "01_FallingProjectiles", "02_SummonRangeMonsters", "03_RandomEdgeBarrage",
        "04_OrbitBarrage", "05_ExpandingShockwave", "06_ScatteredBombs"
    };

    [MenuItem("Tools/Monsters/Create Boss Patterns")]
    private static void CreatePatterns()
    {
        if (!AssetDatabase.IsValidFolder(FolderPath))
        {
            AssetDatabase.CreateFolder("Assets", "BossPatterns");
        }

        if (!AssetDatabase.IsValidFolder(FolderPath))
        {
            Debug.LogError("Assets/BossPatterns 폴더를 만들 수 없습니다.");
            return;
        }

        BossAttackPattern[] patterns = new BossAttackPattern[6];
        for (int i = 0; i < patterns.Length; i++)
        {
            string path = $"{FolderPath}/{AssetNames[i]}.asset";
            BossAttackPattern pattern = AssetDatabase.LoadAssetAtPath<BossAttackPattern>(path);
            if (pattern == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    Debug.LogError($"{path}: 다른 종류의 에셋이 있어 생성하지 않았습니다.");
                    return;
                }

                pattern = ScriptableObject.CreateInstance<BossAttackPattern>();
                pattern.ConfigureDefaults((BossPatternKind)(i + 1));
                AssetDatabase.CreateAsset(pattern, path);
            }

            patterns[i] = pattern;
        }

        AssetDatabase.SaveAssets();
        BossMonster boss = SelectedBoss();
        if (boss == null)
        {
            Debug.Log("새 패턴 6개를 Assets/BossPatterns에 생성했습니다. 보스의 Patterns에 연결하세요.");
            return;
        }

        Undo.RecordObject(boss, "Assign Stationary Boss Patterns");
        SerializedObject data = new SerializedObject(boss);
        SerializedProperty array = data.FindProperty("_patterns");
        array.arraySize = patterns.Length;
        for (int i = 0; i < patterns.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = patterns[i];
        }

        data.ApplyModifiedProperties();
        RecordBossChange(boss);
        Debug.Log($"{boss.name}: 중앙 고정 보스의 새 패턴 6개를 연결했습니다.", boss);
    }

    [MenuItem("Tools/Monsters/Create Boss Patterns", true)]
    private static bool CanCreatePatterns()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("Tools/Monsters/Setup Stationary Boss Room")]
    private static void SetupRoom()
    {
        BossMonster boss = SelectedBoss();
        SerializedObject bossData = new SerializedObject(boss);
        if (bossData.FindProperty("_room").objectReferenceValue != null)
        {
            Selection.activeObject = bossData.FindProperty("_room").objectReferenceValue;
            Debug.Log("이미 연결된 BossRoom을 선택했습니다. 방 크기와 발사점을 조정하세요.");
            return;
        }

        GameObject roomObject = new GameObject("BossRoom");
        Undo.RegisterCreatedObjectUndo(roomObject, "Create Boss Room");
        SceneManager.MoveGameObjectToScene(roomObject, boss.gameObject.scene);
        roomObject.transform.position = boss.transform.position;
        BossRoom room = Undo.AddComponent<BossRoom>(roomObject);
        Transform areaObject = CreateChild(roomObject.transform, "PlayArea", Vector3.zero);
        BoxCollider2D area = Undo.AddComponent<BoxCollider2D>(areaObject.gameObject);
        area.isTrigger = true;
        area.size = new Vector2(16f, 12f);
        Transform center = CreateChild(roomObject.transform, "BossCenter", Vector3.zero);
        Transform edges = CreateChild(roomObject.transform, "BarragePoints", Vector3.zero);
        Vector2[] edgePositions =
        {
            new Vector2(0f, -5.3f), new Vector2(7.3f, -5.3f),
            new Vector2(7.3f, 0f), new Vector2(7.3f, 5.3f),
            new Vector2(0f, 5.3f), new Vector2(-7.3f, 5.3f),
            new Vector2(-7.3f, 0f), new Vector2(-7.3f, -5.3f)
        };
        Transform[] edgePoints = new Transform[edgePositions.Length];
        for (int i = 0; i < edgePoints.Length; i++)
        {
            edgePoints[i] = CreateChild(edges, "BarragePoint_" + i, edgePositions[i]);
        }

        Transform summons = CreateChild(roomObject.transform, "SummonPoints", Vector3.zero);
        Vector2[] summonPositions =
        {
            new Vector2(-4f, -3f), new Vector2(4f, -3f),
            new Vector2(4f, 3f), new Vector2(-4f, 3f)
        };
        Transform[] summonPoints = new Transform[summonPositions.Length];
        for (int i = 0; i < summonPoints.Length; i++)
        {
            summonPoints[i] = CreateChild(summons, "SummonPoint_" + i, summonPositions[i]);
        }

        SerializedObject roomData = new SerializedObject(room);
        roomData.FindProperty("_playArea").objectReferenceValue = area;
        roomData.FindProperty("_bossCenter").objectReferenceValue = center;
        AssignArray(roomData.FindProperty("_barragePoints"), edgePoints);
        AssignArray(roomData.FindProperty("_summonPoints"), summonPoints);
        roomData.ApplyModifiedProperties();
        Undo.RecordObject(boss, "Assign Boss Room");
        bossData.FindProperty("_room").objectReferenceValue = room;
        bossData.ApplyModifiedProperties();
        CreateMissingMuzzles(boss);
        RecordBossChange(boss);
        Selection.activeGameObject = boss.gameObject;
        Debug.Log("BossRoom을 연결했습니다. 실제 방 크기·벽 레이어·발사점과 8방향 총구 위치를 맞춰 주세요.", boss);
    }

    [MenuItem("Tools/Monsters/Setup Stationary Boss Room", true)]
    private static bool CanSetupRoom()
    {
        BossMonster boss = SelectedBoss();
        return !EditorApplication.isPlayingOrWillChangePlaymode && boss != null &&
            boss.gameObject.scene.IsValid() && !PrefabUtility.IsPartOfPrefabAsset(boss) &&
            PrefabStageUtility.GetCurrentPrefabStage() == null;
    }

    private static void CreateMissingMuzzles(BossMonster boss)
    {
        SerializedObject data = new SerializedObject(boss);
        SerializedProperty muzzles = data.FindProperty("_gunMuzzles");
        muzzles.arraySize = 8;
        Transform parent = boss.transform.Find("GunMuzzles");
        if (parent == null)
        {
            parent = CreateChild(boss.transform, "GunMuzzles", Vector3.zero);
        }

        string[] directions = { "S", "SE", "E", "NE", "N", "NW", "W", "SW" };
        for (int i = 0; i < 8; i++)
        {
            SerializedProperty entry = muzzles.GetArrayElementAtIndex(i);
            if (entry.objectReferenceValue != null)
            {
                continue;
            }

            Transform muzzle = parent.Find("Muzzle_" + directions[i]);
            if (muzzle == null)
            {
                muzzle = CreateChild(parent, "Muzzle_" + directions[i], Vector3.zero);
            }

            entry.objectReferenceValue = muzzle;
        }

        data.ApplyModifiedProperties();
    }

    private static void AssignArray(SerializedProperty array, Transform[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static Transform CreateChild(Transform parent, string objectName, Vector3 position)
    {
        GameObject child = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(child, "Create Boss Room Object");
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        return child.transform;
    }

    private static BossMonster SelectedBoss()
    {
        return Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<BossMonster>() : null;
    }

    private static void RecordBossChange(BossMonster boss)
    {
        EditorUtility.SetDirty(boss);
        if (PrefabUtility.IsPartOfPrefabInstance(boss))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(boss);
        }

        if (boss.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(boss.gameObject.scene);
        }
    }
}