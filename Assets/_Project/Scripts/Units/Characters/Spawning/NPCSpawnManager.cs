using UnityEngine;
using CityRush.Units.Characters.Controllers;

namespace CityRush.Units.Characters.Spawning
{
    public sealed class NPCSpawnManager
    {
        private Transform _root;

        private float _streetLeftX;
        private float _streetRightX;

        private GameObject _agentPrefab;

        public void Enter(GameObject agentPrefab)
        {
            _agentPrefab = agentPrefab;
            _root = new GameObject("NPCsRoot").transform;
        }

        public void Exit()
        {
            ClearAll();

            if (_root != null)
                Object.Destroy(_root.gameObject);

            _root = null;
            _agentPrefab = null;
        }

        public void SetStreetBounds(float leftX, float rightX)
        {
            _streetLeftX = leftX;
            _streetRightX = rightX;
        }

        public void ClearAll()
        {
            if (_root == null) return;

            for (int i = _root.childCount - 1; i >= 0; i--)
                Object.Destroy(_root.GetChild(i).gameObject);
        }

        public void SpawnAgents(int count)
        {
            if (_root == null) return;
            if (_agentPrefab == null) return;

            for (int i = 0; i < count; i++)
            {
                GameObject npc = Object.Instantiate(_agentPrefab, _root);

                float x = Random.Range(_streetLeftX + 1f, _streetRightX - 1f);
                npc.transform.position = new Vector3(x, 0f, 0f);

                NPCController ctrl = npc.GetComponent<NPCController>();
                if (ctrl != null)
                {
                    ctrl.SetStreetBounds(_streetLeftX, _streetRightX);
                    ctrl.MoveDir = Random.value < 0.5f ? -1 : 1;
                    ctrl.MaxSpeed = Random.Range(3f, 7f);
                }
            }
        }
    }
}
