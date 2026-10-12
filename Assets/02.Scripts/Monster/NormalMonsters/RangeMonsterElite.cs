using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Monster/Range Monster Elite")]
public sealed class RangeMonsterElite : RangeMonster
{
    protected override bool UsesElitePatterns => true;
}
