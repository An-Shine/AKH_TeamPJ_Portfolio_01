using UnityEngine;

// 생성될 때의 몬스터 크기를 유지합니다. 런타임에 생성한 공격 이펙트는 포함하지 않습니다.
internal sealed class MonsterVisualLock
{
    private readonly Transform _root;
    private readonly Transform[] _transforms;
    private readonly Vector3[] _scales;

    public MonsterVisualLock(Transform root)
    {
        _root = root;
        _transforms = root.GetComponentsInChildren<Transform>(true);
        _scales = new Vector3[_transforms.Length];

        for (int i = 0; i < _transforms.Length; i++)
            _scales[i] = _transforms[i].localScale;

        foreach (Rigidbody2D body in root.GetComponentsInChildren<Rigidbody2D>(true))
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }

    public void Restore()
    {
        if (_root == null)
            return;

        _root.rotation = Quaternion.identity;
        for (int i = 0; i < _transforms.Length; i++)
        {
            if (_transforms[i] != null)
                _transforms[i].localScale = _scales[i];
        }
    }
}