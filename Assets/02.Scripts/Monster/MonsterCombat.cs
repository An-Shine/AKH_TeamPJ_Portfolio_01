using UnityEngine;

internal static class MonsterCombat
{
    internal static readonly string[] DirectionNames =
        { "S", "SE", "E", "NE", "N", "NW", "W", "SW" };

    private static bool _reportedMissingPlayerManager;

    public static int GetDirectionIndex(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return (Mathf.RoundToInt((angle + 90f) / 45f) + 8) % 8;
    }

    public static Vector2 GetDirectionVector(int direction)
    {
        float angle = (direction * 45f - 90f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    public static bool ValidateAnimatorParameters(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("몬스터 Animator와 Animator Controller를 연결하세요.", animator);
            return false;
        }

        string[] names = { "Direction", "IsMoving", "IsAttacking", "IsHit", "IsDead" };
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < names.Length; i++)
        {
            AnimatorControllerParameterType expected = i == 0
                ? AnimatorControllerParameterType.Int : AnimatorControllerParameterType.Bool;
            bool found = false;
            foreach (AnimatorControllerParameter parameter in parameters)
                if (parameter.name == names[i] && parameter.type == expected)
                    found = true;
            if (!found)
            {
                Debug.LogError($"{animator.name}: Animator에 {names[i]} ({expected}) 파라미터가 필요합니다.", animator);
                return false;
            }
        }
        return true;
    }

    public static Transform FindPlayer(Transform owner)
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return null;

        Transform player = playerObject.transform;
        if (player == owner || player.IsChildOf(owner))
        {
            Debug.LogError($"{owner.name}: 몬스터가 아닌 실제 플레이어에 Player 태그를 설정하세요.", owner);
            return null;
        }
        return player;
    }

    public static float GetPlayerDistanceSquared(
        Transform player, Collider2D[] colliders, Vector2 origin, out Vector2 closestPoint)
    {
        closestPoint = origin;
        if (player == null || !player.gameObject.activeInHierarchy)
            return float.PositiveInfinity;

        float nearest = float.PositiveInfinity;
        if (colliders != null)
        {
            foreach (Collider2D bodyCollider in colliders)
            {
                if (bodyCollider == null || !bodyCollider.enabled || bodyCollider.isTrigger ||
                    !bodyCollider.gameObject.activeInHierarchy)
                    continue;

                Rigidbody2D body = bodyCollider.attachedRigidbody;
                if (body != null && !body.simulated)
                    continue;

                Vector2 point = bodyCollider.ClosestPoint(origin);
                float squaredDistance = (point - origin).sqrMagnitude;
                if (squaredDistance < nearest)
                {
                    nearest = squaredDistance;
                    closestPoint = point;
                }
            }
        }

        if (float.IsPositiveInfinity(nearest))
        {
            closestPoint = player.position;
            nearest = (closestPoint - origin).sqrMagnitude;
        }
        return nearest;
    }

    public static Transform ResolvePlayerCollider(Collider2D collider, Transform knownPlayer)
    {
        if (collider == null)
            return null;

        if (knownPlayer != null &&
            (collider.transform == knownPlayer || collider.transform.IsChildOf(knownPlayer)))
            return knownPlayer;

        for (Transform current = collider.transform; current != null; current = current.parent)
        {
            if (current.CompareTag("Player"))
                return current;
        }
        return null;
    }

    public static bool HasEnabledBodyCollider(Collider2D[] colliders)
    {
        if (colliders == null) return false;
        foreach (Collider2D collider in colliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger ||
                !collider.gameObject.activeInHierarchy) continue;
            Rigidbody2D body = collider.attachedRigidbody;
            if (body == null || body.simulated) return true;
        }
        return false;
    }

    public static bool HasClearAttackPath(
        Vector2 origin, Vector2 target, Transform owner, Transform player)
    {
        if ((target - origin).sqrMagnitude < 0.000001f)
            return true;

        foreach (RaycastHit2D hit in Physics2D.LinecastAll(origin, target))
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger)
                continue;
            if (collider.transform == owner || collider.transform.IsChildOf(owner))
                continue;
            if (ResolvePlayerCollider(collider, player) != null)
                continue;
            if (collider.GetComponentInParent<NormalMonster>() != null)
                continue;
            return false;
        }
        return true;
    }

    public static bool TryTakePlayerDamage(
        Transform player, float damage, Vector3 point, Vector2 direction,
        Color hitColor, bool spawnDefaultImpact = true)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            return false;

        PlayerManager manager = PlayerManager.Instance;
        if (manager == null)
        {
            if (!_reportedMissingPlayerManager)
            {
                Debug.LogWarning("몬스터 피해를 전달할 PlayerManager가 없습니다.");
                _reportedMissingPlayerManager = true;
            }
            return false;
        }

        _reportedMissingPlayerManager = false;
        if (!manager.TryTakeDamage(damage))
            return false;

        if (spawnDefaultImpact)
            MonsterCombatEffect.SpawnImpact(point, direction, hitColor, player);
        return true;
    }

    public static bool TryGetDirectionalState(
        Animator animator, string prefix, string motion, int direction, out int hash)
    {
        string suffix = DirectionNames[direction];
        string titleCase = char.ToUpperInvariant(motion[0]) + motion.Substring(1);
        string[] variants = { motion, titleCase, motion.ToUpperInvariant() };

        foreach (string variant in variants)
        {
            int candidate = Animator.StringToHash($"Base Layer.{prefix}_{variant}_{suffix}");
            if (animator.HasState(0, candidate))
            {
                hash = candidate;
                return true;
            }
        }

        hash = 0;
        return false;
    }

    public static bool IsActionState(AnimatorStateInfo info, int stateHash, string tag)
    {
        return stateHash != 0 ? info.fullPathHash == stateHash : info.IsTag(tag);
    }

    public static void GetEffectSorting(Transform source, out int layer, out int sortingLayer, out int order)
    {
        layer = source != null ? source.gameObject.layer : 0;
        sortingLayer = 0;
        order = 1;
        SpriteRenderer renderer = source != null ? source.GetComponentInChildren<SpriteRenderer>() : null;
        if (renderer != null)
        {
            sortingLayer = renderer.sortingLayerID;
            order = renderer.sortingOrder + 1;
        }
    }
}
