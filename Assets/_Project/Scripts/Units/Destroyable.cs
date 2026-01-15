using UnityEngine;

namespace CityRush.Units
{
    [DisallowMultipleComponent]
    public sealed class Destroyable : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private int hp = 1;

        [Header("Interact")]
        [SerializeField] private bool interactDestroys = true;

        [Header("Visuals (optional)")]
        [SerializeField] private GameObject intactRoot;
        [SerializeField] private GameObject destroyedRoot;

        [Header("Disable On Destroy (optional)")]
        [SerializeField] private Collider2D[] collidersToDisable;
        [SerializeField] private Rigidbody2D rbToDisable;

        [Header("Start State")]
        [SerializeField] private bool startDestroyed;

        public bool IsDestroyed { get; private set; }

        private void Awake()
        {
            if (startDestroyed)
                SetDestroyed(true);
            else
                SetDestroyed(false);
        }

        // Called from E-interact
        public bool TryInteract()
        {
            if (!interactDestroys) return false;
            return ApplyDamage(1);
        }

        // Called from projectile
        public bool TryHit(int damage)
        {
            return ApplyDamage(damage);
        }

        private bool ApplyDamage(int damage)
        {
            if (IsDestroyed) return false;
            if (damage <= 0) return false;

            hp -= damage;
            if (hp > 0) return true;

            SetDestroyed(true);
            return true;
        }

        private void SetDestroyed(bool destroyed)
        {
            IsDestroyed = destroyed;

            if (intactRoot != null) intactRoot.SetActive(!destroyed);
            if (destroyedRoot != null) destroyedRoot.SetActive(destroyed);

            if (collidersToDisable != null)
            {
                for (int i = 0; i < collidersToDisable.Length; i++)
                {
                    if (collidersToDisable[i] != null)
                        collidersToDisable[i].enabled = !destroyed;
                }
            }

            if (rbToDisable != null)
                rbToDisable.simulated = !destroyed;
        }
    }
}
