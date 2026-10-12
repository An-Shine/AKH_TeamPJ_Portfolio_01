using UnityEngine;

public static class ProjectileHitFeedback
{
    private const float SoundInterval = 0.06f;
    private static float _nextSoundTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        _nextSoundTime = 0f;
    }

    public static void Play(Vector3 point, Vector2 direction, NormalMonster monster, GameObject effectPrefab)
    {
        // 보스는 TakeDamage에서 자체 스파크를 생성합니다.
        if (monster != null && monster is not BossMonster && effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab, point, effectPrefab.transform.rotation);
            MonsterCombat.GetEffectSorting(monster.transform, out int layer, out int sortingLayer, out int order);
            foreach (SpriteRenderer renderer in effect.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.gameObject.layer = layer;
                renderer.sortingLayerID = sortingLayer;
                renderer.sortingOrder = order;
            }

            // 현재 프리팹에는 자동 제거 처리가 없어 애니메이션 한 회 길이 후 정리합니다.
            float lifetime = 0.5f;
            Animator animator = effect.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                lifetime = 0f;
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                {
                    lifetime = Mathf.Max(lifetime, clip.length);
                }
            }
            Object.Destroy(effect, Mathf.Max(0.01f, lifetime));
        }

        if (AudioManager.Instance == null || Time.unscaledTime < _nextSoundTime) return;

        _nextSoundTime = Time.unscaledTime + SoundInterval;
        AudioManager.Instance.PlaySFX(SFXType.EnemyDamaged);
    }
}
