#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _02.Scripts.Monster.BossMonster.Editor
{
    public static class BossVfxUpgradeValidator
    {
        [MenuItem("Tools/Monster/Boss VFX Upgrade/Validate Resources")]
        public static void Validate()
        {
            List<string> failures = new List<string>();
            TextAsset data = Resources.Load<TextAsset>(BossVfxUpgrade.ResourceRoot + "BossVfxCatalog");
            BossVfxCatalog catalog = data != null ? JsonUtility.FromJson<BossVfxCatalog>(data.text) : null;
            if (catalog == null || catalog.textures == null || catalog.textures.Length != 50)
                failures.Add("50개 보스 전용 텍스처가 들어 있는 BossVfxCatalog.json을 확인하세요.");
            HashSet<string> keys = new HashSet<string>();
            int frames = 0;
            if (catalog != null && catalog.textures != null)
            {
                foreach (BossVfxTextureDefinition entry in catalog.textures)
                {
                    if (entry == null || !keys.Add(entry.key))
                    {
                        failures.Add("카탈로그에 비어 있거나 중복된 키가 있습니다.");
                        continue;
                    }
                    Texture2D texture = Resources.Load<Texture2D>(entry.path);
                    if (texture == null)
                    {
                        failures.Add("텍스처 누락: " + entry.path);
                        continue;
                    }
                    if (entry.columns < 1 || entry.rows < 1 || entry.frames < 1
                        || entry.frames > entry.columns * entry.rows)
                    {
                        failures.Add("프레임 설정 오류: " + entry.key);
                    }

                    if (texture.width != entry.columns * entry.frameWidth
                        || texture.height != entry.rows * entry.frameHeight)
                    {
                        failures.Add("시트 크기가 변경되었습니다: " + entry.key);
                    }

                    string path = AssetDatabase.GetAssetPath(texture);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null || importer.filterMode != FilterMode.Point
                        || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed
                        || importer.npotScale != TextureImporterNPOTScale.None)
                    {
                        failures.Add("Point / Mip Map OFF / Compression None / NPOT None 필요: " + entry.key);
                    }

                    frames += entry.frames;
                }
            }
            foreach (string shaderName in new[] { "BossCrispVfx", "BossCrispBody", "BossCrispRim" })
            {
                Shader shader = Resources.Load<Shader>(BossVfxUpgrade.ResourceRoot + shaderName);
                if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                    failures.Add("셰이더 누락 또는 컴파일 오류: " + shaderName);
            }
            BossEncounterProfile profile = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>(
                "Assets/08.Data/MonsterPresentation/BossEncounter.asset");
            if (profile == null || profile.Moves == null || profile.Moves.Length != 30)
                failures.Add("기존 30패턴 BossEncounter.asset 연결을 확인하세요.");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Monster/BossMonster.prefab");
            global::BossMonster boss = prefab != null ? prefab.GetComponent<global::BossMonster>() : null;
            if (boss == null)
            {
                failures.Add("기존 BossMonster 프리팹이 없습니다. 이 패치는 test(4).unitypackage 기준입니다.");
            }
            else
            {
                SerializedProperty reference = new SerializedObject(boss).FindProperty("_encounterProfile");
                if (reference == null || reference.objectReferenceValue == null)
                    failures.Add("기존 보스 프리팹의 Encounter Profile 연결이 필요합니다.");
            }
            if (failures.Count > 0)
            {
                Debug.LogError("보스 VFX 업그레이드 확인 실패:\n" + string.Join("\n", failures));
                return;
            }
            Debug.Log("보스 VFX 리소스 확인 완료: " + keys.Count + "개 텍스처 / " + frames
                + "개 프레임 / 보스 전용 셰이더 3개 / 기존 30패턴 연결. 실제 전투의 색·타이밍·성능은 Play에서 확인하세요.");
        }
    }

    public sealed class BossVfxResourceGallery : EditorWindow
    {
        private BossVfxCatalog _catalog;
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private Vector2 _scroll;

        [MenuItem("Tools/Monster/Boss VFX Upgrade/Resource Gallery")]
        public static void Open()
        {
            GetWindow<BossVfxResourceGallery>("Boss VFX Resources");
        }

        private void OnEnable()
        {
            minSize = new Vector2(530f, 380f);
            TextAsset data = Resources.Load<TextAsset>(BossVfxUpgrade.ResourceRoot + "BossVfxCatalog");
            _catalog = data != null ? JsonUtility.FromJson<BossVfxCatalog>(data.text) : null;
            _textures.Clear();
            if (_catalog != null && _catalog.textures != null)
            {
                foreach (BossVfxTextureDefinition entry in _catalog.textures)
                    _textures[entry.key] = Resources.Load<Texture2D>(entry.path);
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("보스 전용 원본 리소스 애니메이션", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("원본 텍스처의 프레임을 보여 줍니다. 실제 보스 패턴의 합성·타격 타이밍은 Play에서 확인하세요. 클릭하면 해당 리소스를 Project 창에서 선택합니다.", MessageType.Info);
            if (_catalog == null || _catalog.textures == null)
            {
                EditorGUILayout.HelpBox("BossVfxCatalog.json이 없습니다. 업그레이드 패키지를 Import하세요.", MessageType.Error);
                return;
            }
            int columns = Mathf.Max(2, Mathf.FloorToInt((position.width - 24f) / 174f));
            int rows = Mathf.CeilToInt(_catalog.textures.Length / (float)columns);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    if (index >= _catalog.textures.Length)
                        break;
                    BossVfxTextureDefinition entry = _catalog.textures[index];
                    _textures.TryGetValue(entry.key, out Texture2D texture);
                    EditorGUILayout.BeginVertical(GUILayout.Width(166f));
                    Rect rect = GUILayoutUtility.GetRect(156f, 132f, GUILayout.Width(156f));
                    EditorGUI.DrawRect(rect, new Color(0.035f, 0.05f, 0.085f));
                    if (texture != null)
                    {
                        int frame = entry.frames > 1 ? (int)(EditorApplication.timeSinceStartup * entry.fps) % entry.frames : 0;
                        Rect uv = new Rect((frame % entry.columns) / (float)entry.columns,
                            1f - (frame / entry.columns + 1f) / entry.rows,
                            1f / entry.columns, 1f / entry.rows);
                        float aspect = entry.frameWidth / (float)entry.frameHeight;
                        float width = Mathf.Min(rect.width - 12f, (rect.height - 12f) * aspect);
                        float height = width / aspect;
                        Rect image = new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
                        GUI.DrawTextureWithTexCoords(image, texture, uv, true);
                        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                        {
                            Selection.activeObject = texture;
                            EditorGUIUtility.PingObject(texture);
                            Event.current.Use();
                        }
                    }
                    EditorGUILayout.LabelField(entry.key, EditorStyles.miniBoldLabel, GUILayout.Width(158f));
                    EditorGUILayout.LabelField(entry.frames + " frames", EditorStyles.miniLabel, GUILayout.Width(158f));
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
