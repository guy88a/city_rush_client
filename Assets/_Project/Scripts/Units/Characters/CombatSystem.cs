using System;
using UnityEngine;

namespace CityRush.Units.Characters
{
    [DisallowMultipleComponent]
    public sealed class CombatSystem : MonoBehaviour
    {
        public bool IsAiming { get; private set; }

        public event Action OnAimStarted;
        public event Action OnAimCanceled;
        public event Action OnAimReleased;

        public void StartAim()
        {
            if (IsAiming) return;
            IsAiming = true;
            OnAimStarted?.Invoke();
        }

        public void CancelAim()
        {
            if (!IsAiming) return;
            IsAiming = false;
            OnAimCanceled?.Invoke();
        }

        public void ReleaseAim()
        {
            if (!IsAiming) return;
            IsAiming = false;
            OnAimReleased?.Invoke();
        }
    }
}
