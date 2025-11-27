using UnityEngine;

namespace CityRush.Core.States
{
    public class GameLoopState : IState
    {
        public void Enter()
        {
            Debug.Log("[GameLoopState] Entered gameplay.");
            // Future: start player control, systems, timers, etc.
        }

        public void Update(float deltaTime)
        {
            Debug.Log($"[GameLoopState] Tick: {deltaTime}");
        }

        public void Exit()
        {
            Debug.Log("[GameLoopState] Exited gameplay.");
        }
    }
}
