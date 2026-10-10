using System.Collections.Generic;

namespace _02.Scripts.Monster.NormalMonsters
{
    internal enum MonsterCombatRole
    {
        Melee,
        Ranged,
        Kamikaze
    }

    // Only attack starts are scheduled. Existing animation release points own damage.
    internal sealed class MonsterAttackRhythm
    {
        private const int MaximumConcurrentAttacks = 2;
        private const float StartSpacing = 0.24f;
        private const float BeatRecovery = 0.65f;
        private const float RequestLifetime = 0.45f;

        private struct Request
        {
            public int Owner;
            public MonsterCombatRole Role;
            public float SeenAt;
        }

        private struct Lease
        {
            public int Owner;
            public MonsterCombatRole Role;
            public float ExpiresAt;
        }

        private readonly List<Request> _requests = new List<Request>(16);
        private readonly List<Lease> _leases = new List<Lease>(MaximumConcurrentAttacks);
        private float _nextStart;
        private float _lastActivity;
        private int _startsInBeat;

        public bool TryBegin(int owner, MonsterCombatRole role, float now, float maximumDuration)
        {
            if (float.IsNaN(now) || float.IsInfinity(now) || float.IsNaN(maximumDuration) ||
                float.IsInfinity(maximumDuration))
            {
                return false;
            }

            Prune(now);
            if (HasLease(owner))
                return false;

            int requestIndex = FindRequest(owner);
            Request request = new Request { Owner = owner, Role = role, SeenAt = now };
            if (requestIndex < 0)
            {
                _requests.Add(request);
            }
            else
            {
                _requests[requestIndex] = request;
            }

            if (now < _nextStart || _leases.Count >= MaximumConcurrentAttacks || !RoleAvailable(role))
                return false;

            // A blocked explosive does not prevent an earlier eligible melee/ranged request.
            int firstEligible = -1;
            for (int i = 0; i < _requests.Count; i++)
            {
                if (RoleAvailable(_requests[i].Role))
                {
                    firstEligible = i;
                    break;
                }
            }

            if (firstEligible < 0 || _requests[firstEligible].Owner != owner)
                return false;

            _requests.RemoveAt(firstEligible);
            float duration = maximumDuration < 0.1f ? 0.1f : maximumDuration > 12f ? 12f : maximumDuration;
            _leases.Add(new Lease { Owner = owner, Role = role, ExpiresAt = now + duration + 0.1f });
            _lastActivity = now;
            _startsInBeat++;
            _nextStart = now + (_startsInBeat >= 3 ? BeatRecovery : StartSpacing);
            if (_startsInBeat >= 3)
                _startsInBeat = 0;
            return true;
        }

        public void Release(int owner)
        {
            for (int i = _leases.Count - 1; i >= 0; i--)
            {
                if (_leases[i].Owner == owner)
                    _leases.RemoveAt(i);
            }

            int requestIndex = FindRequest(owner);
            if (requestIndex >= 0)
                _requests.RemoveAt(requestIndex);
        }

        private void Prune(float now)
        {
            for (int i = _requests.Count - 1; i >= 0; i--)
            {
                if (now - _requests[i].SeenAt > RequestLifetime)
                    _requests.RemoveAt(i);
            }

            for (int i = _leases.Count - 1; i >= 0; i--)
            {
                if (now >= _leases[i].ExpiresAt)
                    _leases.RemoveAt(i);
            }

            if (_requests.Count == 0 && _leases.Count == 0 && now - _lastActivity > 1.5f)
            {
                _startsInBeat = 0;
                _nextStart = now;
            }
        }

        private bool RoleAvailable(MonsterCombatRole role)
        {
            if (role != MonsterCombatRole.Kamikaze)
                return true;
            foreach (Lease lease in _leases)
            {
                if (lease.Role == MonsterCombatRole.Kamikaze)
                    return false;
            }

            return true;
        }

        private bool HasLease(int owner)
        {
            foreach (Lease lease in _leases)
            {
                if (lease.Owner == owner)
                    return true;
            }

            return false;
        }

        private int FindRequest(int owner)
        {
            for (int i = 0; i < _requests.Count; i++)
            {
                if (_requests[i].Owner == owner)
                    return i;
            }

            return -1;
        }
    }
}
