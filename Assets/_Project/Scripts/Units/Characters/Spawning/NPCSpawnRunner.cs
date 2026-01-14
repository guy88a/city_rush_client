using System.Collections;
using UnityEngine;

namespace CityRush.Units.Characters.Spawning
{
    public sealed class NPCSpawnRunner : MonoBehaviour
    {
        public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);

        public void CancelAll() => StopAllCoroutines();
    }
}
