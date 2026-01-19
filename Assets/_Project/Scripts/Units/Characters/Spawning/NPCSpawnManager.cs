using UnityEngine;
using CityRush.Units.Characters.Movement;
using CityRush.Units.Characters.Controllers;

namespace CityRush.Units.Characters.Spawning
{
    public sealed class NPCSpawnManager
    {
        private NPCSpawnRunner _runner;
        private int _spawnToken; // increments to invalidate pending respawns

        private float _spawnMarginX = 1f;
        private float _respawnDelayMin = 2f;
        private float _respawnDelayMax = 3f;

        private float _groundY = 0f;

        private Transform _root;

        // Treat these as "street-space" bounds (local/design space).
        private float _streetLeftX;
        private float _streetRightX;

        // Optional transform that converts street-space -> world-space (used for ApartmentWindow).
        private Transform _streetSpace;

        private GameObject _agentPrefab;

        private readonly System.Collections.Generic.List<NPCController> _active = new();
        private readonly System.Collections.Generic.Stack<NPCController> _pool = new();

        private readonly System.Collections.Generic.List<float> _cachedLocalX = new();
        private bool _hasCachedLocalX;

        public void Enter(GameObject agentPrefab)
        {
            _agentPrefab = agentPrefab;
            _root = new GameObject("NPCsRoot").transform;
            _runner = _root.gameObject.AddComponent<NPCSpawnRunner>();
            _spawnToken++;
        }

        public void Exit()
        {
            ClearAll();

            if (_root != null)
                Object.Destroy(_root.gameObject);

            _spawnToken++;
            _runner?.CancelAll();

            _root = null;
            _agentPrefab = null;
            _streetSpace = null;
        }


        public void SetStreetBounds(float leftX, float rightX)
        {
            _streetLeftX = leftX;
            _streetRightX = rightX;
        }

        public void SetStreetSpace(Transform streetSpace)
        {
            _streetSpace = streetSpace;
        }

        public void SetGroundY(float y)
        {
            _groundY = y;
        }

        public void ClearAll()
        {
            _spawnToken++;
            _runner?.CancelAll();

            for (int i = _active.Count - 1; i >= 0; i--)
                ReturnToPool(_active[i]);

            _active.Clear();
        }

        public void SpawnAgents(int count)
        {
            if (_root == null || _agentPrefab == null) return;

            float leftLocal = Mathf.Min(_streetLeftX, _streetRightX);
            float rightLocal = Mathf.Max(_streetLeftX, _streetRightX);

            float minX = leftLocal + _spawnMarginX;
            float maxX = rightLocal - _spawnMarginX;

            if (maxX <= minX)
                return;

            GetWorldStreetBounds(out float leftWorld, out float rightWorld);

            for (int i = 0; i < count; i++)
            {
                NPCController ctrl = GetOrCreate();
                if (ctrl == null) continue;

                float xLocal = Random.Range(minX, maxX);
                ctrl.transform.position = ToWorld(xLocal, _groundY);
                ApplyVisualScale(ctrl);

                ctrl.SetStreetBounds(leftWorld, rightWorld);
                ctrl.MoveDir = Random.value < 0.5f ? -1 : 1;
                ctrl.MaxSpeed = Random.Range(CharacterSpeedSettings.MinWalkSpeed, CharacterSpeedSettings.MaxWalkSpeed);

                ctrl.gameObject.SetActive(true);
                _active.Add(ctrl);
            }
        }

        private NPCController GetOrCreate()
        {
            NPCController ctrl;

            if (_pool.Count > 0)
            {
                ctrl = _pool.Pop();
            }
            else
            {
                GameObject go = Object.Instantiate(_agentPrefab, _root);
                ctrl = go.GetComponent<NPCController>();
            }

            if (ctrl != null)
            {
                ctrl.OnDespawn = HandleDespawn;
                if (ctrl.transform.parent != _root)
                    ctrl.transform.SetParent(_root, false);
            }

            return ctrl;
        }

        private void HandleDespawn(NPCController ctrl)
        {
            ReturnToPool(ctrl);
            _active.Remove(ctrl);

            ScheduleRespawnOne();
        }

        private void ReturnToPool(NPCController ctrl)
        {
            if (ctrl == null) return;

            PhysicsObject phys = ctrl.GetComponent<PhysicsObject>();
            if (phys != null)
                phys.ResetExternalImpulse();

            ctrl.gameObject.SetActive(false);
            _pool.Push(ctrl);
        }

        private void ScheduleRespawnOne()
        {
            if (_runner == null) return;

            int token = _spawnToken;
            float delay = Random.Range(_respawnDelayMin, _respawnDelayMax);
            _runner.Run(RespawnAfterDelay(delay, token));
        }

        private System.Collections.IEnumerator RespawnAfterDelay(float delay, int token)
        {
            yield return new WaitForSeconds(delay);

            // canceled / street changed / cleared
            if (token != _spawnToken) yield break;

            SpawnOne();
        }

        private void SpawnOne()
        {
            if (_root == null || _agentPrefab == null) return;

            float leftLocal = Mathf.Min(_streetLeftX, _streetRightX);
            float rightLocal = Mathf.Max(_streetLeftX, _streetRightX);

            float minX = leftLocal + _spawnMarginX;
            float maxX = rightLocal - _spawnMarginX;

            if (maxX <= minX)
                return;

            NPCController ctrl = GetOrCreate();
            if (ctrl == null) return;

            float xLocal = Random.Range(minX, maxX);
            ctrl.transform.position = ToWorld(xLocal, _groundY);
            ApplyVisualScale(ctrl);

            GetWorldStreetBounds(out float leftWorld, out float rightWorld);

            ctrl.SetStreetBounds(leftWorld, rightWorld);
            ctrl.MoveDir = Random.value < 0.5f ? -1 : 1;
            ctrl.MaxSpeed = Random.Range(CharacterSpeedSettings.MinWalkSpeed, CharacterSpeedSettings.MaxWalkSpeed);

            ctrl.gameObject.SetActive(true);
            _active.Add(ctrl);
        }

        private Vector3 ToWorld(float xLocal, float yLocal)
        {
            if (_streetSpace == null)
                return new Vector3(xLocal, yLocal, 0f);

            return _streetSpace.TransformPoint(new Vector3(xLocal, yLocal, 0f));
        }

        private void GetWorldStreetBounds(out float leftWorld, out float rightWorld)
        {
            if (_streetSpace == null)
            {
                leftWorld = _streetLeftX;
                rightWorld = _streetRightX;
            }
            else
            {
                leftWorld = _streetSpace.TransformPoint(new Vector3(_streetLeftX, 0f, 0f)).x;
                rightWorld = _streetSpace.TransformPoint(new Vector3(_streetRightX, 0f, 0f)).x;
            }

            if (leftWorld > rightWorld)
            {
                float t = leftWorld;
                leftWorld = rightWorld;
                rightWorld = t;
            }
        }

        private void ApplyVisualScale(NPCController ctrl)
        {
            if (ctrl == null) return;

            float s = 1f;

            // Window mode: street is scaled down (0.5f), so match that visually.
            if (_streetSpace != null)
                s = _streetSpace.lossyScale.x;

            ctrl.transform.localScale = new Vector3(s, s, 1f);
        }

        public void RefreshVisualScale()
        {
            for (int i = 0; i < _active.Count; i++)
                ApplyVisualScale(_active[i]);
        }

        public void CacheActiveLocalX()
        {
            _cachedLocalX.Clear();
            _hasCachedLocalX = false;

            if (_streetSpace == null)
                return;

            for (int i = 0; i < _active.Count; i++)
            {
                NPCController ctrl = _active[i];
                if (ctrl == null)
                {
                    _cachedLocalX.Add(0f);
                    continue;
                }

                float xLocal = _streetSpace.InverseTransformPoint(ctrl.transform.position).x;
                _cachedLocalX.Add(xLocal);
            }

            _hasCachedLocalX = true;
        }

        public void RestoreActiveFromCachedLocalX()
        {
            if (!_hasCachedLocalX || _streetSpace == null)
                return;

            int n = Mathf.Min(_active.Count, _cachedLocalX.Count);

            for (int i = 0; i < n; i++)
            {
                NPCController ctrl = _active[i];
                if (ctrl == null) continue;

                float xLocal = _cachedLocalX[i];

                // Snap back to correct world position on the (possibly rescaled) street.
                Vector3 pos = ToWorld(xLocal, _groundY);
                ctrl.transform.position = pos;

                // Optional safety: stop �fall impulse� if they had velocity (cheap + robust).
                Rigidbody2D rb = ctrl.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 v = rb.linearVelocity;
                    if (v.y < 0f) v.y = 0f;
                    rb.linearVelocity = v;
                }

                ApplyVisualScale(ctrl);
            }

            _hasCachedLocalX = false;
            _cachedLocalX.Clear();
        }

    }
}
