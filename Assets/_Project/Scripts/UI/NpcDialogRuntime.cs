using System;
using UnityEngine;

namespace CityRush.UI
{
    [DisallowMultipleComponent]
    public sealed class NpcDialogRuntime : MonoBehaviour
    {
        public int CurrentNpcId { get; private set; } = -1;

        public event Action<int> OpenedForNpc;
        public event Action Closed;

        public void OpenForNpc(int npcId)
        {
            CurrentNpcId = npcId;
            gameObject.SetActive(true);
            OpenedForNpc?.Invoke(npcId);
        }

        public void Close()
        {
            CurrentNpcId = -1;
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }
}
