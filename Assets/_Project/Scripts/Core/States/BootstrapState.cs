using CityRush.Core.Services;
using CityRush.Items;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
using UnityEngine;


namespace CityRush.Core.States
{
    public class BootstrapState : IState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly GameContext _context;


        public BootstrapState(GameStateMachine gameStateMachine, GameContext context)
        {
            _gameStateMachine = gameStateMachine;
            _context = context;
        }


        public void Enter()
        {
            _context.Get<ILoggerService>()?.Info("[BootstrapState] Entered.");


            // Load ItemsDB (Resources/Items/ItemsDB.json)
            TextAsset itemsJson = Resources.Load<TextAsset>("Items/ItemsDB");
            if (itemsJson == null)
            {
                _context.Get<ILoggerService>()?.Error("[BootstrapState] Missing ItemsDB at Resources/Items/ItemsDB.json");
            }
            else
            {
                var dto = JsonUtility.FromJson<ItemsDbDto>(itemsJson.text);
                if (!ItemsDb.TryCreateFromDto(dto, out ItemsDb itemsDb, out string err))
                {
                    _context.Get<ILoggerService>()?.Error($"[BootstrapState] ItemsDB parse failed: {err}");
                }
                else
                {
                    _context.Set(itemsDb);
                    _context.Get<ILoggerService>()?.Info($"[BootstrapState] ItemsDB loaded. version={itemsDb.Version} count={itemsDb.Count}");

                    // sanity check (temporary)
                    if (itemsDb.TryGet(1001, out var it))
                    {
                        string weaponDefId = it.IsWeapon ? it.Weapon.WeaponDefinitionId : "<none>";
                        Debug.Log($"[ItemsDB] Sanity itemId=1001 name='{it.Name}' cat='{it.Category}' icon='{it.IconKey}' weaponDefId='{weaponDefId}'");
                    }
                    else
                    {
                        Debug.LogError("[ItemsDB] Sanity failed: missing itemId=1001");
                    }
                }
            }


            // Load raw map data
            TextAsset json = Resources.Load<TextAsset>("Maps/LibertyState");
            var mapData = JsonUtility.FromJson<MapData>(json.text);


            // Create map runtime manager
            var mapManager = new MapManager(mapData);


            // Register into context
            _context.Set(mapManager);


            _gameStateMachine.Enter<LoadLevelState>();
        }


        public void Exit()
        {
            Debug.Log("[BootstrapState] Exited.");
        }


        public void Update(float deltaTime) { }
    }
}