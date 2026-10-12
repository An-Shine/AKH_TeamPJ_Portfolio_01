#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using _02.Scripts.Monster.Presentation;

namespace _02.Scripts.Monster.BossMonster.Editor
{
    public static class BossVfxValidator
    {
        [MenuItem("Tools/Monster/Boss VFX/Validate Imported Encounter")]
        public static void Validate()
        {
            List<string> failures = new List<string>();
            BossEncounterProfile profile = AssetDatabase.LoadAssetAtPath<BossEncounterProfile>("Assets/08.Data/MonsterPresentation/BossEncounter.asset");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Monster/BossMonster.prefab");
            if (profile == null || profile.Moves == null || profile.Moves.Length != 30)
                failures.Add("BossEncounter.asset에 30개 패턴이 필요합니다.");
            if (prefab == null || prefab.GetComponent<global::BossMonster>() == null || prefab.GetComponent<BossVisualPolish>() == null)
                failures.Add("BossMonster 프리팹의 보스·연출 컴포넌트를 확인하세요.");
            if (profile != null && profile.Moves != null)
            {
                bool ultimate = false;
                foreach (BossEncounterMove move in profile.Moves)
                {
                    if (move == null || !Enum.IsDefined(typeof(BossEncounterPattern), move.Kind))
                    {
                        failures.Add("비어 있거나 유효하지 않은 패턴이 있습니다.");
                        continue;
                    }

                    if (move.Sound == null || move.Sound.Start == null || move.Sound.Loop == null || move.Sound.Impact == null || move.Sound.End == null)
                        failures.Add(move.Kind + " 사운드 연결이 빠졌습니다.");
                    if (move.Kind == BossEncounterPattern.ExecutionNova && move.Enabled)
                    {
                        ultimate = true;
                        if (move.Warning < 4.5f || move.Radius < 1f)
                            failures.Add("섬멸 차징은 4.5초 이상 예고와 안전지대가 필요합니다.");
                    }
                }

                if (!ultimate)
                    failures.Add("ExecutionNova 패턴이 비활성 상태입니다.");
            }

            if (prefab != null)
            {
                MonsterGroundPresentation ground = prefab.GetComponent<MonsterGroundPresentation>();
                if (ground == null)
                {
                    failures.Add("보스 그림자·발자국 컴포넌트가 빠졌습니다.");
                }
                else
                {
                    SerializedObject groundData = new SerializedObject(ground);
                    if (groundData.FindProperty("_profile").objectReferenceValue == null || groundData.FindProperty("_feetAnchor").objectReferenceValue == null)
                        failures.Add("보스 바닥 프로필·발 위치 연결을 확인하세요.");
                }

                Animator animator = prefab.GetComponentInChildren<Animator>(true);
                HashSet<string> clips = new HashSet<string>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                    {
                        if (clip != null && !clip.empty)
                            clips.Add(clip.name);
                    }
                }
                string[] directions =
                {
                    "S",
                    "SE",
                    "E",
                    "NE",
                    "N",
                    "NW",
                    "W",
                    "SW"
                };
                foreach (string stage in new[] { "charge", "channel", "recover" })
                {
                    foreach (string channel in new[] { "melee", "projectile", "explosion" })
                    {
                        foreach (string direction in directions)
                        {
                            string name = "BossMonster_" + stage + "_" + channel + "_" + direction;
                            if (!clips.Contains(name))
                                failures.Add("시전 클립 누락: " + name);
                        }
                    }
                }

                global::BossMonster boss = prefab.GetComponent<global::BossMonster>();
                if (boss != null)
                {
                    SerializedObject serialized = new SerializedObject(boss);
                    if (serialized.FindProperty("_healthFont").objectReferenceValue == null)
                        failures.Add("보스 체력바 폰트 연결이 빠졌습니다.");
                }
            }

            foreach (string name in new[]
            {
                "trace_01",
                "smoke_04",
                "light_01",
                "circle_03",
                "flame_01",
                "explosion_pixelfied",
                "slash_01",
                "dirt_01",
                "spark_01",
                "twirl_02",
                "Effect36",
                "Effect51",
                "Explosion25",
                "ToonExplosion31",
                "Fire01"
            }

            )
            {
                if (Resources.Load<Texture2D>("Monster/NeonVfx/Particles/" + name) == null)
                    failures.Add("무료 VFX 텍스처 누락: " + name);
            }
            foreach (string name in new[]
            {
                "Monster/NeonVfx/NeonParticle",
                "Monster/NeonVfx/BossNeonBody",
                "Monster/BossOverhaul/BossGlow"
            }

            )
            {
                if (Resources.Load<Shader>(name) == null)
                    failures.Add("셰이더 누락: " + name);
            }
            NormalMonsterFeelProfile normal = AssetDatabase.LoadAssetAtPath<NormalMonsterFeelProfile>("Assets/08.Data/MonsterPresentation/NormalMonsterGameFeel.asset");
            if (normal == null || !HasClips(normal.MeleeSlashSounds) || !HasClips(normal.MeleeImpactSounds) || !HasClips(normal.RangeShotSounds) || !HasClips(normal.RangeImpactSounds) || !HasClips(normal.KamikazeChargeSounds) || !HasClips(normal.KamikazeTickSounds) || !HasClips(normal.KamikazeExplosionSounds))
                failures.Add("일반 몹 3종의 공격·타격·자폭 사운드 연결을 확인하세요.");
            foreach (string monster in new[]
            {
                "MeleeMonster",
                "RangeMonster",
                "KamikazeMonster"
            }

            )
            {
                GameObject normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Monster/" + monster + ".prefab");
                NormalMonsterGameFeel feel = normalPrefab != null ? normalPrefab.GetComponent<NormalMonsterGameFeel>() : null;
                if (feel == null || !feel.IsConfigured)
                    failures.Add(monster + " 네온 전투 연출 연결이 빠졌습니다.");
            }

            if (failures.Count > 0)
            {
                Debug.LogError("Boss VFX 연결 확인 실패:\n" + string.Join("\n", failures));
                return;
            }

            Debug.Log($"Boss VFX 검증 완료: 30개 패턴, 120개 보스 사운드, 72개 시전 클립, 5개 추가 애니메이션 VFX, 일반 몹 3종과 보스 그림자·발자국 연결. 추적 공격 범위 {profile.AttackRangeMultiplier:0.00}배, 탄속 {profile.ProjectileSpeedMultiplier:0.00}배, 발동 전 고정 {profile.TargetLockTime:0.00}초. Play에서 실제 전투를 확인하세요.");
        }

        private static bool HasClips(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return false;
            foreach (AudioClip clip in clips)
            {
                if (clip == null)
                    return false;
            }
            return true;
        }
    }
}
#endif

