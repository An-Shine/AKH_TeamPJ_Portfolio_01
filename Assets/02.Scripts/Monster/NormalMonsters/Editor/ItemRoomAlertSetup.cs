#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _02.Scripts.Monster.NormalMonsters.Editor
{
    public static class ItemRoomAlertSetup
    {
        private const string AreaName = "ItemRoomDetectionArea";
        [MenuItem("Tools/Monster/Configure Item Room Alerts")]
        public static void ConfigureRooms()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Play를 종료한 뒤 아이템 방 감지를 설정하세요.");
                return;
            }

            ItemRoomMonsterSpawner[] spawners = Object.FindObjectsByType<ItemRoomMonsterSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int configured = 0;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure Item Room Alerts");
            foreach (ItemRoomMonsterSpawner spawner in spawners)
            {
                if (EditorUtility.IsPersistent(spawner) || !spawner.gameObject.scene.IsValid())
                {
                    continue;
                }

                SerializedObject spawnerData = new SerializedObject(spawner);
                SerializedProperty roomProperty = spawnerData.FindProperty("_room");
                MonsterRoomBounds room = roomProperty.objectReferenceValue as MonsterRoomBounds;
                if (room == null)
                {
                    room = spawner.GetComponentInParent<MonsterRoomBounds>();
                }

                if (room == null || !room.IsConfigured)
                {
                    Debug.LogWarning($"{spawner.name}: Room과 방 범위를 먼저 연결하세요.", spawner);
                    continue;
                }

                Collider2D area = room.ActivationArea;
                if (area != null && !area.isTrigger)
                {
                    Debug.LogWarning($"{room.name}: Activation Area에는 Is Trigger가 켜진 감지 콜라이더를 연결하세요.", room);
                    continue;
                }

                if (area == null)
                {
                    Transform existing = room.transform.Find(AreaName);
                    area = existing != null ? existing.GetComponent<BoxCollider2D>() : null;
                    if (area == null)
                    {
                        GameObject areaObject = new GameObject(AreaName);
                        areaObject.layer = room.gameObject.layer;
                        SceneManager.MoveGameObjectToScene(areaObject, room.gameObject.scene);
                        Undo.RegisterCreatedObjectUndo(areaObject, "Create Item Room Area");
                        Undo.SetTransformParent(areaObject.transform, room.transform, "Parent Item Room Area");
                        areaObject.transform.localPosition = Vector3.zero;
                        areaObject.transform.localRotation = Quaternion.identity;
                        areaObject.transform.localScale = Vector3.one;
                        area = Undo.AddComponent<BoxCollider2D>(areaObject);
                    }

                    Undo.RecordObject(area, "Configure Item Room Area");
                    BoxCollider2D box = (BoxCollider2D)area;
                    box.offset = room.LocalCenter;
                    box.size = room.LocalSize;
                    box.isTrigger = true;
                    EditorUtility.SetDirty(box);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(box);
                }

                Undo.RecordObject(room, "Configure Room Detection");
                room.EnablePlayerEntryDetection();
                room.SetActivationArea(area);
                EditorUtility.SetDirty(room);
                PrefabUtility.RecordPrefabInstancePropertyModifications(room);
                roomProperty.objectReferenceValue = room;
                spawnerData.ApplyModifiedProperties();
                EditorUtility.SetDirty(spawner);
                PrefabUtility.RecordPrefabInstancePropertyModifications(spawner);
                EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
                EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
                configured++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            if (configured == 0)
            {
                Debug.LogWarning("설정한 아이템 방이 없습니다. 아이템 방이 있는 게임 씬을 열고 Room 연결을 확인하세요.");
                return;
            }

            Debug.Log($"아이템 방 {configured}곳의 진입 감지를 설정했습니다. 기존 Area를 쓰려면 Room의 Activation Area를 변경하고 Ctrl+S로 씬을 저장하세요.");
        }
    }
}
#endif
