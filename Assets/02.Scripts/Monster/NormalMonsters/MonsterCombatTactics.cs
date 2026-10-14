using System;
using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.NormalMonsters
{
    // Existing room containment, navigation and swept collision still execute every move.
    internal sealed class MonsterCombatTactics
    {
        private sealed class Encounter
        {
            public readonly List<MonsterCombatTactics> Members = new List<MonsterCombatTactics>(16);
            public readonly MonsterAttackRhythm Rhythm = new MonsterAttackRhythm();
            public Transform Player;
            public MonsterRoomBounds Room;
            public int SceneHandle;
            public float FormationAngle;
            private Vector2 _lastPlayerPosition;
            private Vector2 _playerVelocity;
            private float _sampleTime;

            public Encounter(Transform player, MonsterRoomBounds room, int sceneHandle, Vector2 origin)
            {
                Player = player;
                Room = room;
                SceneHandle = sceneHandle;
                _lastPlayerPosition = player.position;
                _sampleTime = Time.time;
                Vector2 radial = origin - _lastPlayerPosition;
                FormationAngle = Mathf.Atan2(radial.y, radial.x) * Mathf.Rad2Deg;
            }

            public Vector2 PredictedPosition(float leadTime)
            {
                Vector2 position = Player.position;
                float elapsed = Time.time - _sampleTime;
                if (elapsed >= 0.08f)
                {
                    Vector2 difference = position - _lastPlayerPosition;
                    Vector2 measured = difference.sqrMagnitude > 36f ? Vector2.zero : difference / elapsed;
                    _playerVelocity = Vector2.Lerp(_playerVelocity, Vector2.ClampMagnitude(measured, 6f), 0.4f);
                    _lastPlayerPosition = position;
                    _sampleTime = Time.time;
                }

                return position + Vector2.ClampMagnitude(_playerVelocity * leadTime, 1.25f);
            }
        }

        private static readonly List<Encounter> Encounters = new List<Encounter>(8);
        private static readonly float[] CandidateOffsets = { 0f, 35f, -35f, 70f, -70f, 110f, -110f, 180f };
        private static readonly float[] FormationOffsets = { 0f, 60f, -60f, 120f, -120f, 180f };
        private readonly NormalMonster _owner;
        private readonly MonsterCombatRole _role;
        private readonly Func<Vector2, bool> _canMove;
        private readonly Func<Vector2, Vector2, bool> _hasSight;
        private readonly int _ownerId;
        private Encounter _encounter;
        private Vector2 _goal;
        private Vector2 _goalPlayerPosition;
        private float _nextDecision;
        private bool _hasGoal;
        private bool _enabled;

        public MonsterCombatTactics(NormalMonster owner, MonsterCombatRole role,
            Func<Vector2, bool> canMove, Func<Vector2, Vector2, bool> hasSight)
        {
            _owner = owner;
            _role = role;
            _canMove = canMove;
            _hasSight = hasSight;
            _ownerId = owner.GetInstanceID();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEncounters()
        {
            foreach (Encounter encounter in Encounters)
            {
                foreach (MonsterCombatTactics member in encounter.Members)
                {
                    member._encounter = null;
                    member._hasGoal = false;
                    member._nextDecision = 0f;
                }
            }

            Encounters.Clear();
        }

        public void Enable()
        {
            _enabled = true;
            _hasGoal = false;
            _nextDecision = 0f;
        }

        public void Disable()
        {
            LeaveEncounter();
            _enabled = false;
        }

        public void SetTarget(Transform player)
        {
            if (!_enabled || _owner.IsDead || player == null || !player.gameObject.activeInHierarchy)
            {
                LeaveEncounter();
                return;
            }

            MonsterRoomBounds room = _owner.HomeRoom;
            int sceneHandle = _owner.gameObject.scene.handle;
            if (_encounter != null && _encounter.Player == player && _encounter.Room == room &&
                _encounter.SceneHandle == sceneHandle)
            {
                return;
            }

            LeaveEncounter();
            foreach (Encounter encounter in Encounters)
            {
                if (encounter.Player == player && encounter.Room == room && encounter.SceneHandle == sceneHandle)
                {
                    _encounter = encounter;
                    break;
                }
            }

            if (_encounter == null)
            {
                _encounter = new Encounter(player, room, sceneHandle, _owner.transform.position);
                Encounters.Add(_encounter);
            }

            _encounter.Members.Add(this);
            _hasGoal = false;
            _nextDecision = 0f;
        }

        public bool TryBeginAttack(float maximumDuration)
        {
            return _encounter != null && Time.timeScale > 0f &&
                   _encounter.Rhythm.TryBegin(_ownerId, _role, Time.time, maximumDuration);
        }

        public void ReleaseAttack()
        {
            _encounter?.Rhythm.Release(_ownerId);
        }

        public void InvalidateGoal()
        {
            _hasGoal = false;
            _nextDecision = 0f;
        }

        public Vector2 Destination(Vector2 playerPosition, float preferredDistance, bool commitApproach)
        {
            if (_encounter == null || commitApproach)
                return playerPosition;

            bool playerMoved = (playerPosition - _goalPlayerPosition).sqrMagnitude > 0.49f;
            if (Time.time >= _nextDecision || playerMoved)
            {
                SelectGoal(playerPosition, preferredDistance);
            }

            if (_hasGoal)
                return _goal;
            Vector2 origin = _owner.transform.position;
            return _role == MonsterCombatRole.Ranged && _hasSight(origin, playerPosition) ? origin : playerPosition;
        }

        private void SelectGoal(Vector2 playerPosition, float preferredDistance)
        {
            Vector2 origin = _owner.transform.position;
            int slot = FormationSlot();
            float angle = _encounter.FormationAngle + FormationOffsets[slot % FormationOffsets.Length];
            float radius = Mathf.Max(0.35f, preferredDistance);
            Vector2 center = playerPosition;
            if (_role == MonsterCombatRole.Melee)
            {
                radius *= 0.82f;
            }
            else if (_role == MonsterCombatRole.Ranged)
            {
                angle += 30f;
            }
            else
            {
                // Prediction uses observed movement; committed close-range charges stay direct.
                center = _encounter.PredictedPosition(0.32f);
                Vector2 interceptDirection = center - origin;
                angle = Mathf.Atan2(interceptDirection.y, interceptDirection.x) * Mathf.Rad2Deg + 180f;
                angle += slot % 2 == 0 ? 28f : -28f;
                radius *= 0.7f;
            }

            bool found = false;
            float bestScore = float.PositiveInfinity;
            Vector2 best = playerPosition;
            for (int i = 0; i < CandidateOffsets.Length; i++)
            {
                float radians = (angle + CandidateOffsets[i]) * Mathf.Deg2Rad;
                Vector2 candidate = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
                if (!_canMove(candidate))
                    continue;
                bool visible = _hasSight(candidate, playerPosition);
                float score = Mathf.Abs(CandidateOffsets[i]) * 0.012f + (candidate - origin).magnitude * 0.16f;
                score += visible ? 0f : _role == MonsterCombatRole.Ranged ? 12f : 3f;
                score += _hasSight(origin, candidate) ? 0f : 2f;
                if (_hasGoal)
                {
                    score += Mathf.Min((candidate - _goal).sqrMagnitude * 0.12f, 0.6f);
                }

                score += CrowdPenalty(candidate);
                if (score < bestScore)
                {
                    found = true;
                    bestScore = score;
                    best = candidate;
                }
            }

            _hasGoal = found;
            _goal = best;
            _goalPlayerPosition = playerPosition;
            _nextDecision = Time.time + 0.4f + (uint)_ownerId % 7 * 0.015f;
        }

        private int FormationSlot()
        {
            int slot = 0;
            foreach (MonsterCombatTactics member in _encounter.Members)
            {
                if (member == this)
                    break;
                if (member._role == _role && !member._owner.IsDead)
                    slot++;
            }

            return slot;
        }

        private float CrowdPenalty(Vector2 candidate)
        {
            float penalty = 0f;
            foreach (MonsterCombatTactics member in _encounter.Members)
            {
                if (member == this || member._owner == null || member._owner.IsDead)
                    continue;
                Vector2 position = member._hasGoal ? member._goal : (Vector2)member._owner.transform.position;
                float distance = (candidate - position).magnitude;
                if (distance < 1.25f)
                    penalty += (1.25f - distance) * 5f;
            }

            return penalty;
        }

        private void LeaveEncounter()
        {
            if (_encounter == null)
                return;
            _encounter.Rhythm.Release(_ownerId);
            _encounter.Members.Remove(this);
            if (_encounter.Members.Count == 0)
                Encounters.Remove(_encounter);
            _encounter = null;
            _hasGoal = false;
        }
    }
}
