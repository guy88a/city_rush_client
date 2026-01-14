using UnityEngine;
using CityRush.Units.Characters.Controllers;

namespace CityRush.Units.Characters.Spawning
{
    public sealed class NPCSpawnManager
    {
        private NPCSpawnRunner _runner;
        private int _spawnToken; // increments to invalidate pending respawns

        private float _spawnMarginX = 1f;
        private float _minSpeed = 3f;
        private float _maxSpeed = 7f;
        private float _respawnDelayMin = 2f;
        private float _respawnDelayMax = 3f;

        private float _groundY = 0f;

        private Transform _root;

        private float _streetLeftX;
        private float _streetRightX;

        private GameObject _agentPrefab;

        private readonly System.Collections.Generic.List<NPCController> _active = new();
        private readonly System.Collections.Generic.Stack<NPCController> _pool = new();

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
        }

        public void SetStreetBounds(float leftX, float rightX)
        {
            _streetLeftX = leftX;
            _streetRightX = rightX;
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

            for (int i = 0; i < count; i++)
            {
                NPCController ctrl = GetOrCreate();
                if (ctrl == null) continue;

                float x = Random.Range(_streetLeftX + 1f, _streetRightX - 1f);
                ctrl.transform.position = new Vector3(x, 0f, 0f);

                ctrl.SetStreetBounds(_streetLeftX, _streetRightX);
                ctrl.MoveDir = Random.value < 0.5f ? -1 : 1;
                ctrl.MaxSpeed = Random.Range(3f, 7f);

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

            NPCController ctrl = GetOrCreate();
            if (ctrl == null) return;

            float x = Random.Range(_streetLeftX + _spawnMarginX, _streetRightX - _spawnMarginX);
            ctrl.transform.position = new Vector3(x, _groundY, 0f);

            ctrl.SetStreetBounds(_streetLeftX, _streetRightX);
            ctrl.MoveDir = Random.value < 0.5f ? -1 : 1;
            ctrl.MaxSpeed = Random.Range(_minSpeed, _maxSpeed);

            ctrl.gameObject.SetActive(true);
            _active.Add(ctrl);
        }


    }
}
