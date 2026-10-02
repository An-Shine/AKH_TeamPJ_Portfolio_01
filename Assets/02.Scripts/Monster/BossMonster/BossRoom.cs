using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossRoom : MonoBehaviour
{
    [SerializeField] private BoxCollider2D _playArea;
    [SerializeField] private Transform _bossCenter;
    [SerializeField] private Transform[] _barragePoints = new Transform[0];
    [SerializeField] private Transform[] _summonPoints = new Transform[0];
    [Tooltip("낙하 원과 소환몹을 놓을 수 없는 벽·장애물 레이어입니다.")]
    [SerializeField] private LayerMask _solidLayers;
    [Tooltip("탄이 플레이어와 벽을 검사할 레이어입니다.")]
    [SerializeField] private LayerMask _projectileHitLayers = ~0;
    [SerializeField, Min(0f)] private float _edgeInset = 0.35f;
    [SerializeField, Min(0f)] private float _bossClearance = 1.5f;

    public Transform[] BarragePoints => _barragePoints;
    public Transform[] SummonPoints => _summonPoints;
    public LayerMask ProjectileHitLayers => _projectileHitLayers;

    public Rect PlayArea
    {
        get
        {
            if (_playArea == null)
            {
                return new Rect();
            }

            Vector2 center = _playArea.transform.TransformPoint(_playArea.offset);
            Vector3 scale = _playArea.transform.lossyScale;
            Vector2 size = Vector2.Scale(_playArea.size,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            return new Rect(center - size * 0.5f, size);
        }
    }

    public Vector3 BossCenterPosition => _bossCenter != null
        ? _bossCenter.position : new Vector3(PlayArea.center.x, PlayArea.center.y, transform.position.z);

    public bool ValidateSetup()
    {
        if (_playArea == null || PlayArea.width <= 0f || PlayArea.height <= 0f)
        {
            Debug.LogError($"{name}: Play Area에 보스방 범위 BoxCollider2D를 연결하세요.", this);
            return false;
        }

        if (Mathf.Abs(Mathf.DeltaAngle(0f, _playArea.transform.eulerAngles.z)) > 0.01f)
        {
            Debug.LogError($"{name}: Play Area의 Z 회전은 0이어야 합니다.", this);
            return false;
        }

        if (!_playArea.isTrigger)
        {
            Debug.LogError($"{name}: 범위 표시용 Play Area의 Is Trigger를 켜세요.", this);
            return false;
        }

        if (!Contains(BossCenterPosition, 0f))
        {
            Debug.LogError($"{name}: Boss Center를 Play Area 안에 배치하세요.", this);
            return false;
        }

        return true;
    }

    public bool Contains(Vector2 point, float padding)
    {
        Rect area = PlayArea;
        return point.x >= area.xMin + padding && point.x <= area.xMax - padding &&
            point.y >= area.yMin + padding && point.y <= area.yMax - padding;
    }

    public bool IsClear(Vector2 point, float radius)
    {
        if (!Contains(point, radius + Mathf.Max(0f, _edgeInset)))
        {
            return false;
        }

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(point, radius, _solidLayers))
        {
            if (collider != null && collider.enabled && !collider.isTrigger)
            {
                return false;
            }
        }

        return true;
    }

    public bool TryRandomPoint(float radius, out Vector2 point)
    {
        Rect area = PlayArea;
        float padding = radius + Mathf.Max(0f, _edgeInset);
        point = area.center;
        if (area.width <= padding * 2f || area.height <= padding * 2f)
        {
            return false;
        }

        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(area.xMin + padding, area.xMax - padding),
                Random.Range(area.yMin + padding, area.yMax - padding));
            float clearance = Mathf.Max(0f, _bossClearance) + radius;
            if ((candidate - (Vector2)BossCenterPosition).sqrMagnitude < clearance * clearance ||
                !IsClear(candidate, radius))
            {
                continue;
            }

            point = candidate;
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (_playArea == null)
        {
            return;
        }

        Rect area = PlayArea;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(area.center.x, area.center.y, transform.position.z),
            new Vector3(area.width, area.height, 0f));
    }
}
