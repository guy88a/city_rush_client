using UnityEngine;
using CityRush.Units; // NpcDB, NpcCategory, NpcIdentity, NpcRuntimeData
using CityRush.Units.Characters.Movement;
using CityRush.Units.Characters.Controllers;
using CityRush.Units.Characters.Combat;
using CityRush.World.Addresses;

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

        private float _bleedLeftX;
        private float _bleedRightX;
        private bool _hasBleedBounds;

        // Optional transform that converts street-space -> world-space (used for ApartmentWindow).
        private Transform _streetSpace;

        private GameObject _pedestrianPrefab;

        // Data-driven selection
        private NpcDB _npcDb;
        private GameObject _residentPrefab;
        private readonly System.Collections.Generic.List<GameObject> _residentInstances = new();

        private readonly System.Collections.Generic.List<NPCController> _active = new();
        private readonly System.Collections.Generic.Stack<NPCController> _pool = new();

        private readonly System.Collections.Generic.List<float> _cachedLocalX = new();
        private bool _hasCachedLocalX;

        private SniperDistanceStep _distanceStep;

        private int _streetPedestriansMaxCount;
        private int[] _streetPedestriansNpcIds;
        private bool _hasStreetPedestriansPool;

        public void Enter(GameObject pedestrianPrefab)
        {
            _pedestrianPrefab = pedestrianPrefab;
            _root = new GameObject("NPCsRoot").transform;
            _runner = _root.gameObject.AddComponent<NPCSpawnRunner>();
            _distanceStep = _root.gameObject.AddComponent<SniperDistanceStep>();
            _distanceStep.SetStep(2);
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
            _pedestrianPrefab = null;
            _streetSpace = null;

            _npcDb = null;
            _residentPrefab = null;
        }

        public void SetNpcDb(NpcDB npcDb) => _npcDb = npcDb;

        public void SetResidentPrefab(GameObject residentPrefab) => _residentPrefab = residentPrefab;

        public void SetStreetBounds(float leftX, float rightX)
        {
            _streetLeftX = leftX;
            _streetRightX = rightX;
        }

        public void SetBleedBounds(float leftX, float rightX)
        {
            _bleedLeftX = leftX;
            _bleedRightX = rightX;
            _hasBleedBounds = true;
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

            // Residents (destroy + clear)
            for (int i = 0; i < _residentInstances.Count; i++)
            {
                var go = _residentInstances[i];
                if (go != null)
                    UnityEngine.Object.Destroy(go);
            }
            _residentInstances.Clear();

            _hasStreetPedestriansPool = false;
            _streetPedestriansMaxCount = 0;
            _streetPedestriansNpcIds = null;
        }

        // Old API kept (spawns normal NPCs with npcId=0)
        public void SpawnPedestrians(int count)
        {
            SpawnPedestriansByNpcId(npcId: 0, count);
        }

        // Backward-compatible wrapper (optional to remove later)
        public void SpawnAgents(int count)
        {
            SpawnPedestrians(count);
        }

        // New API (npcId + count). Prefab is chosen by NpcDB category:
        // Resident => resident prefab in street center, idle.
        // else => normal NPC prefab from Enter(...)
        public void SpawnByNpcId(int npcId, int count)
        {
            if (_root == null)
                return;

            bool isResident = false;

            if (_npcDb != null && _npcDb.TryGet(npcId, out var def) && def != null)
                isResident = (def.Category == NpcCategory.Resident);

            Debug.Log($"[NPCSpawnManager] SpawnByNpcId npcId={npcId} count={count} resident={isResident}");

            if (isResident)
                SpawnResidentsByNpcId(npcId, count);
            else
                SpawnPedestriansByNpcId(npcId, count);
        }

        public void SpawnResidentAtAddress(
            int npcId,
            StreetAddressTag streetTag,
            int buildingIndex,
            int floorIndex,
            int apartmentIndex,
            string formattedAddress = null
        )
        {
            if (_root == null)
                return;

            if (_residentPrefab == null)
            {
                Debug.LogWarning($"[NPCSpawnManager] residentPrefab is null. Cannot spawn Resident npcId={npcId}.");
                return;
            }

            Vector3 spawnWorld = default;
            bool hasAnchor = false;

            if (streetTag != null && TryResolveBuildingTag(streetTag, buildingIndex, out var buildingTag) && buildingTag != null)
            {
                float groundWorldY = ToWorld(0f, _groundY).y;
                spawnWorld = new Vector3(buildingTag.transform.position.x, groundWorldY, 0f);
                hasAnchor = true;
            }

            if (!hasAnchor)
            {
                float leftLocal = Mathf.Min(_streetLeftX, _streetRightX);
                float rightLocal = Mathf.Max(_streetLeftX, _streetRightX);
                float centerLocalX = (leftLocal + rightLocal) * 0.5f;
                spawnWorld = ToWorld(centerLocalX, _groundY); ;
            }

            var go = Object.Instantiate(_residentPrefab, _root);
            if (go == null)
                return;

            ApplyNpcIdAndData(go, npcId);

            go.transform.position = spawnWorld;
            ApplyVisualScale(go.transform);

            var ctrl = go.GetComponent<NPCController>();
            if (ctrl != null)
            {
                ctrl.MoveDir = 0;
                ctrl.MaxSpeed = 0f;
            }

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            var home = go.GetComponent<CityRush.Units.NpcHomeAddress>();
            if (home == null)
                home = go.AddComponent<CityRush.Units.NpcHomeAddress>();

            home.Set(streetTag, buildingIndex, floorIndex, apartmentIndex, formattedAddress);

            go.SetActive(true);
            _residentInstances.Add(go);
        }

        private static bool TryResolveBuildingTag(StreetAddressTag streetTag, int buildingIndex, out BuildingAddressTag buildingTag)
        {
            buildingTag = null;

            if (streetTag == null)
                return false;

            if (streetTag.TryGetBuildingByIndex(buildingIndex, out buildingTag) && buildingTag != null)
                return true;

            var all = streetTag.GetComponentsInChildren<BuildingAddressTag>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (b != null && b.BuildingIndex == buildingIndex)
                {
                    buildingTag = b;
                    return true;
                }
            }

            return false;
        }


        private void SpawnPedestriansByNpcId(int npcId, int count)
        {
            if (_root == null || _pedestrianPrefab == null) return;

            GetWorldDespawnBounds(out float leftWorld, out float rightWorld);

            for (int i = 0; i < count; i++)
            {
                NPCController ctrl = GetOrCreate();
                if (ctrl == null) continue;

                if (!TryPickSpawnInBleedGap(out float xLocal, out int moveDir))
                    break;

                ApplyNpcIdAndData(ctrl.gameObject, npcId);

                ctrl.transform.position = ToWorld(xLocal, _groundY);
                ApplyVisualScale(ctrl);

                ctrl.SetStreetBounds(leftWorld, rightWorld); // now bleed bounds (world)
                ctrl.MoveDir = moveDir;                      // left-spawn => right, right-spawn => left
                ctrl.MaxSpeed = Random.Range(CharacterSpeedSettings.MinWalkSpeed, CharacterSpeedSettings.MaxWalkSpeed);

                ctrl.gameObject.SetActive(true);
                _active.Add(ctrl);
            }
        }

        private void SpawnResidentsByNpcId(int npcId, int count)
        {
            if (_root == null) return;

            if (_residentPrefab == null)
            {
                Debug.LogWarning($"[NPCSpawnManager] residentPrefab is null. Cannot spawn Resident npcId={npcId}.");
                return;
            }

            float leftLocal = Mathf.Min(_streetLeftX, _streetRightX);
            float rightLocal = Mathf.Max(_streetLeftX, _streetRightX);
            float centerLocalX = (leftLocal + rightLocal) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var go = Object.Instantiate(_residentPrefab, _root);
                if (go == null) continue;

                ApplyNpcIdAndData(go, npcId);

                go.transform.position = ToWorld(centerLocalX, _groundY);
                ApplyVisualScale(go.transform);

                // Idle: disable controller if it exists (do not patrol/travel).
                var ctrl = go.GetComponent<NPCController>();
                if (ctrl != null)
                {
                    ctrl.MoveDir = 0;
                    ctrl.MaxSpeed = 0f;
                }

                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;

                go.SetActive(true);
                _residentInstances.Add(go);
            }
        }

        private void ApplyNpcIdAndData(GameObject go, int npcId)
        {
            if (go == null)
                return;

            if (npcId > 0)
            {
                var identity = go.GetComponent<NpcIdentity>();
                if (identity != null)
                    identity.SetId(npcId);
                else
                    Debug.LogWarning($"[NPCSpawnManager] Spawned object missing NpcIdentity. npcId={npcId}", go);
            }

            // Important for pooled NPCs: Start() won't re-run, so re-apply.
            var runtime = go.GetComponent<NpcRuntimeData>();
            if (runtime != null)
                runtime.ApplyFromDb();
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
                GameObject go = Object.Instantiate(_pedestrianPrefab, _root);
                ctrl = go.GetComponent<NPCController>();
            }

            if (ctrl != null)
            {
                ctrl.OnDespawn = HandleDespawn;
                if (ctrl.transform.parent != _root)
                    ctrl.transform.SetParent(_root, false);

                ctrl.enabled = true;
            }

            return ctrl;
        }

        private void HandleDespawn(NPCController ctrl)
        {
            if (ctrl == null)
                return;

            // Keep the same instance (and same NpcIdentity) on respawn.
            DeactivateForRespawn(ctrl);
            ScheduleRespawnExisting(ctrl);
        }

        private void DeactivateForRespawn(NPCController ctrl)
        {
            if (ctrl == null)
                return;

            PhysicsObject phys = ctrl.GetComponent<PhysicsObject>();
            if (phys != null)
                phys.ResetExternalImpulse();

            var rb = ctrl.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            ctrl.gameObject.SetActive(false);
        }

        private void ScheduleRespawnExisting(NPCController ctrl)
        {
            if (_runner == null || ctrl == null)
                return;

            int token = _spawnToken;
            float delay = Random.Range(_respawnDelayMin, _respawnDelayMax);
            _runner.Run(RespawnExistingAfterDelay(delay, token, ctrl));
        }

        private System.Collections.IEnumerator RespawnExistingAfterDelay(float delay, int token, NPCController ctrl)
        {
            yield return new WaitForSeconds(delay);

            if (token != _spawnToken)
                yield break;

            RespawnExisting(ctrl);
        }

        private void RespawnExisting(NPCController ctrl)
        {
            if (ctrl == null)
                return;

            if (!TryPickSpawnInBleedGap(out float xLocal, out int moveDir))
                return;

            ctrl.transform.position = ToWorld(xLocal, _groundY);
            ApplyVisualScale(ctrl);

            GetWorldDespawnBounds(out float leftWorld, out float rightWorld);

            ctrl.SetStreetBounds(leftWorld, rightWorld);
            ctrl.MoveDir = moveDir;
            ctrl.MaxSpeed = Random.Range(CharacterSpeedSettings.MinWalkSpeed, CharacterSpeedSettings.MaxWalkSpeed);

            ctrl.enabled = true;
            ctrl.OnDespawn = HandleDespawn;

            ctrl.gameObject.SetActive(true);
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

        private void ScheduleRespawnOne(int npcId)
        {
            if (_runner == null) return;

            int token = _spawnToken;
            float delay = Random.Range(_respawnDelayMin, _respawnDelayMax);
            _runner.Run(RespawnAfterDelay(delay, token, npcId));
        }

        private System.Collections.IEnumerator RespawnAfterDelay(float delay, int token, int npcId)
        {
            yield return new WaitForSeconds(delay);

            // canceled / street changed / cleared
            if (token != _spawnToken) yield break;

            SpawnOne(npcId);
        }

        private void SpawnOne(int npcId)
        {
            if (_root == null || _pedestrianPrefab == null) return;

            NPCController ctrl = GetOrCreate();
            if (ctrl == null) return;

            if (!TryPickSpawnInBleedGap(out float xLocal, out int moveDir))
                return;

            ApplyNpcIdAndData(ctrl.gameObject, npcId);

            ctrl.transform.position = ToWorld(xLocal, _groundY);
            ApplyVisualScale(ctrl);

            GetWorldDespawnBounds(out float leftWorld, out float rightWorld);

            ctrl.SetStreetBounds(leftWorld, rightWorld); // now bleed bounds (world)
            ctrl.MoveDir = moveDir;
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

        private void GetWorldDespawnBounds(out float leftWorld, out float rightWorld)
        {
            if (_hasBleedBounds)
            {
                if (_streetSpace == null)
                {
                    leftWorld = _bleedLeftX;
                    rightWorld = _bleedRightX;
                }
                else
                {
                    leftWorld = _streetSpace.TransformPoint(new Vector3(_bleedLeftX, 0f, 0f)).x;
                    rightWorld = _streetSpace.TransformPoint(new Vector3(_bleedRightX, 0f, 0f)).x;
                }

                if (leftWorld > rightWorld)
                {
                    float t = leftWorld;
                    leftWorld = rightWorld;
                    rightWorld = t;
                }

                return;
            }

            GetWorldStreetBounds(out leftWorld, out rightWorld);
        }

        private bool TryPickSpawnInBleedGap(out float xLocal, out int moveDir)
        {
            float leftLocal = Mathf.Min(_streetLeftX, _streetRightX);
            float rightLocal = Mathf.Max(_streetLeftX, _streetRightX);

            if (!_hasBleedBounds)
            {
                float minX = leftLocal + _spawnMarginX;
                float maxX = rightLocal - _spawnMarginX;
                if (maxX <= minX)
                {
                    xLocal = 0f;
                    moveDir = 1;
                    return false;
                }

                xLocal = Random.Range(minX, maxX);
                moveDir = Random.value < 0.5f ? -1 : 1;
                return true;
            }

            float bleedLeftLocal = Mathf.Min(_bleedLeftX, _bleedRightX);
            float bleedRightLocal = Mathf.Max(_bleedLeftX, _bleedRightX);

            float leftMin = bleedLeftLocal + _spawnMarginX;
            float leftMax = leftLocal - _spawnMarginX;

            float rightMin = rightLocal + _spawnMarginX;
            float rightMax = bleedRightLocal - _spawnMarginX;

            bool leftOk = leftMax > leftMin;
            bool rightOk = rightMax > rightMin;

            if (!leftOk && !rightOk)
            {
                xLocal = 0f;
                moveDir = 1;
                return false;
            }

            bool spawnLeft = (leftOk && rightOk) ? (Random.value < 0.5f) : leftOk;

            if (spawnLeft)
            {
                xLocal = Random.Range(leftMin, leftMax);
                moveDir = 1;   // left bleed => walk right
            }
            else
            {
                xLocal = Random.Range(rightMin, rightMax);
                moveDir = -1;  // right bleed => walk left
            }

            return true;
        }

        private void ApplyVisualScale(NPCController ctrl)
        {
            if (ctrl == null) return;
            ApplyVisualScale(ctrl.transform);
        }

        private void ApplyVisualScale(Transform t)
        {
            if (t == null) return;

            float s = 1f;

            // Window mode: street is scaled down (0.5f), so match that visually.
            if (_streetSpace != null)
                s = _streetSpace.lossyScale.x;

            t.localScale = new Vector3(s, s, 1f);
        }

        public void RefreshVisualScale()
        {
            for (int i = 0; i < _active.Count; i++)
                ApplyVisualScale(_active[i]);

            for (int i = 0; i < _residentInstances.Count; i++)
            {
                var go = _residentInstances[i];
                if (go != null)
                    ApplyVisualScale(go.transform);
            }
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

                // Optional safety: stop fall impulse if they had velocity (cheap + robust).
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

        public void ConfigureStreetPedestrians(int maxCount, int[] npcIds)
        {
            _streetPedestriansMaxCount = Mathf.Max(0, maxCount);
            _streetPedestriansNpcIds = npcIds;
            _hasStreetPedestriansPool = (_streetPedestriansMaxCount > 0)
                                        && (_streetPedestriansNpcIds != null)
                                        && (_streetPedestriansNpcIds.Length > 0);
        }

        public void SpawnConfiguredStreetPedestrians()
        {
            if (!_hasStreetPedestriansPool)
                return;

            for (int i = 0; i < _streetPedestriansMaxCount; i++)
            {
                int npcId = PickRandomStreetPedestrianNpcId();
                if (npcId <= 0)
                    continue;

                SpawnOne(npcId);
            }
        }

        // Used only when the street is (re)loaded. Spawns inside street bounds (not on bleed).
        public void SpawnConfiguredStreetPedestriansOnStreetLoad()
        {
            if (!_hasStreetPedestriansPool)
                return;

            for (int i = 0; i < _streetPedestriansMaxCount; i++)
            {
                int npcId = PickRandomStreetPedestrianNpcId();
                if (npcId <= 0)
                    continue;

                SpawnOneOnStreet(npcId);
            }
        }

        private void SpawnOneOnStreet(int npcId)
        {
            if (_root == null || _pedestrianPrefab == null)
                return;

            NPCController ctrl = GetOrCreate();
            if (ctrl == null)
                return;

            if (!TryPickSpawnOnStreet(out float xLocal, out int moveDir))
                return;

            ApplyNpcIdAndData(ctrl.gameObject, npcId);

            ctrl.transform.position = ToWorld(xLocal, _groundY);
            ApplyVisualScale(ctrl);

            GetWorldDespawnBounds(out float leftWorld, out float rightWorld);

            ctrl.SetStreetBounds(leftWorld, rightWorld); // keep bleed bounds (world)
            ctrl.MoveDir = moveDir;
            ctrl.MaxSpeed = Random.Range(CharacterSpeedSettings.MinWalkSpeed, CharacterSpeedSettings.MaxWalkSpeed);

            ctrl.gameObject.SetActive(true);
            _active.Add(ctrl);
        }

        private bool TryPickSpawnOnStreet(out float xLocal, out int moveDir)
        {
            float leftLocal = Mathf.Min(_streetLeftX, _streetRightX);
            float rightLocal = Mathf.Max(_streetLeftX, _streetRightX);

            float minX = leftLocal + _spawnMarginX;
            float maxX = rightLocal - _spawnMarginX;

            if (maxX <= minX)
            {
                xLocal = 0f;
                moveDir = 1;
                return false;
            }

            xLocal = Random.Range(minX, maxX);
            moveDir = Random.value < 0.5f ? -1 : 1;
            return true;
        }

        private int PickRandomStreetPedestrianNpcId()
        {
            if (_streetPedestriansNpcIds == null || _streetPedestriansNpcIds.Length == 0)
                return 0;

            int idx = Random.Range(0, _streetPedestriansNpcIds.Length);
            return _streetPedestriansNpcIds[idx];
        }

    }
}
