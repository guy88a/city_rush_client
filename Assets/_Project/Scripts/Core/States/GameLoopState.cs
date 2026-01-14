using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.Core.Services;    // whatever namespace ILoggerService is in
using CityRush.Core.States;
using CityRush.Units;
using CityRush.Units.Characters; // CombatSystem
using CityRush.World.Interior;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameLoopState : IState
{
    private readonly Game _game;
    private readonly GameContext _context;

    private CorePrefabsRegistry _prefabs;
    private MapManager _mapManager;

    private GameLoopWorld _world;
    private GameLoopNavigation _navigation;

    private ApartmentDoor _activeApartmentDoor;

    private const int DefaultNpcCount = 5;

    private ILoggerService _logger;

    private CombatSystem _playerCombat;
    private bool _combatHooksBound;

    // Keep same effective behavior as before (0.2f was hardcoded in LoadNextStreet).
    private const float NavSpawnGapModifier = 0.2f;

    private float _enterBuildingStreetT;
    private bool _enterBuildingStreetTSet;

    private enum LoopMode
    {
        Street,
        Corridor,
        DoorPOV,
        ApartmentFull,
        ApartmentWindow
    }

    private LoopMode _mode;
    private bool _isTransitioning;

    private int _apartmentFullRefX;
    private int _apartmentFullRefY;
    private bool _apartmentFullRefCached;

    private Vector3 _returnStreetPlayerPos;
    private Vector3 _returnStreetCameraPos;
    private Vector3 _returnCorridorCameraPos;

    private MapDirection _pendingStreetDirection;
    private bool _pendingStreetDirectionSet;

    private CorridorExitTrigger _corridorExitTrigger;

    // Transition runner (non-capturing fade callbacks)
    private System.Action _transitionOutWork;
    private System.Action _transitionInDone;
    private bool _transitionFreezePlayer;
    private bool _transitionUnfreezePlayer;
    private bool _unfreezeAtFadeInStart;

    // Small transition payload (avoid capturing locals)
    private Vector3 _pendingCameraPos;
    private bool _pendingCameraPosSet;

    private int _pendingRefX;
    private int _pendingRefY;
    private bool _pendingRefSet;

    public GameLoopState(Game game, GameContext context)
    {
        _game = game;
        _context = context;
    }

    public void Enter()
    {
        _logger = _context.Get<ILoggerService>();
        _logger?.Info("[GameLoopState] Entered.");

        _prefabs = _context.GetData<CorePrefabsRegistry>();
        _mapManager = _context.GetData<MapManager>();

        _world = new GameLoopWorld(_game, NavSpawnGapModifier);
        _world.Enter(_prefabs, _mapManager);

        _navigation = new GameLoopNavigation(_game, _world, _prefabs, _mapManager);
        _navigation.Enter();

        _mode = LoopMode.Street;
        _isTransitioning = false;

        _world.Npcs_SpawnStreet(DefaultNpcCount);

        if (_world?.PlayerController != null)
            _world.PlayerController.OnBuildingDoorInteract += HandleBuildingDoorInteract;

        if (_world?.PlayerController != null)
            _world.PlayerController.OnApartmentDoorInteract += HandleApartmentDoorInteract;

        if (_world?.PlayerController != null)
            _world.PlayerController.OnWorldObjectInteract += HandleWorldObjectInteract;

        EnsurePlayerCombatBound();
    }

    public void Exit()
    {
        if (_world?.PlayerController != null)
            _world.PlayerController.OnBuildingDoorInteract -= HandleBuildingDoorInteract;

        if (_corridorExitTrigger != null)
            _corridorExitTrigger.ExitRequested -= HandleCorridorExitRequested;

        if (_world?.PlayerController != null)
            _world.PlayerController.OnApartmentDoorInteract -= HandleApartmentDoorInteract;

        if (_world?.PlayerController != null)
            _world.PlayerController.OnWorldObjectInteract -= HandleWorldObjectInteract;

        _corridorExitTrigger = null;

        if (_playerCombat != null && _combatHooksBound)
        {
            _playerCombat.OnAimStarted -= HandleAimStarted;
            _playerCombat.OnAimCanceled -= HandleAimCanceled;
            _playerCombat.OnAimReleased -= HandleAimReleased;
        }

        _playerCombat = null;
        _combatHooksBound = false;

        _world?.Exit();

        _navigation = null;
        _world = null;

        _prefabs = null;
        _mapManager = null;

        ClearTransition();
    }

    public void Update(float deltaTime)
    {
        if (_isTransitioning)
            return;

        switch (_mode)
        {
            case LoopMode.Street:
                _navigation?.Tick(deltaTime);

                if (_navigation != null && _navigation.TryConsumeStreetTransition(out MapDirection dir))
                {
                    _pendingStreetDirection = dir;
                    _pendingStreetDirectionSet = true;

                    StartTransition(
                        outWork: StreetTransitionOutWork,
                        inDone: StreetTransitionInDone,
                        freezePlayer: false,
                        unfreezePlayer: true,
                        unfreezeAtFadeInStart: true
                    );
                }

                return;

            case LoopMode.Corridor:
                // Corridor idle. Door / exit triggers drive transitions via events.
                return;

            case LoopMode.DoorPOV:
                TickDoorPOV();
                return;

            case LoopMode.ApartmentFull:
                TickApartmentFull();
                return;

            case LoopMode.ApartmentWindow:
                TickApartmentWindow();
                return;
        }
    }

    // ----------------------------
    // Transition Runner
    // ----------------------------

    private void StartTransition(
        System.Action outWork,
        System.Action inDone,
        bool freezePlayer,
        bool unfreezePlayer,
        bool unfreezeAtFadeInStart
    )
    {
        if (_isTransitioning || _world == null || _world.ScreenFade == null)
            return;

        _isTransitioning = true;

        _transitionOutWork = outWork;
        _transitionInDone = inDone;
        _transitionFreezePlayer = freezePlayer;
        _transitionUnfreezePlayer = unfreezePlayer;
        _unfreezeAtFadeInStart = unfreezeAtFadeInStart;

        if (_transitionFreezePlayer && _world.PlayerController != null)
            _world.PlayerController.Freeze();

        _world.ScreenFade.FadeOut(OnTransitionFadeOutComplete);
    }

    private void OnTransitionFadeOutComplete()
    {
        // Apply optional camera move (payload)
        if (_pendingCameraPosSet)
        {
            Vector3 camPos = _game.CameraTransform.position;
            camPos.x = _pendingCameraPos.x;
            camPos.y = _pendingCameraPos.y;
            _game.CameraTransform.position = camPos;

            _pendingCameraPosSet = false;
        }

        // Apply optional ref-res (payload)
        if (_pendingRefSet)
        {
            _world.SetCameraRefResolution(_pendingRefX, _pendingRefY);
            _pendingRefSet = false;
        }

        _transitionOutWork?.Invoke();

        _world.ScreenFade.FadeIn(OnTransitionFadeInComplete);

        if (_unfreezeAtFadeInStart && _transitionUnfreezePlayer && _world.PlayerController != null)
        {
            _world.PlayerController.Unfreeze();
            _transitionUnfreezePlayer = false; // prevent double unfreeze in FadeInComplete
        }
    }

    private void OnTransitionFadeInComplete()
    {
        if (_transitionUnfreezePlayer && _world.PlayerController != null)
            _world.PlayerController.Unfreeze();

        _transitionInDone?.Invoke();

        ClearTransition();
    }

    private void ClearTransition()
    {
        _isTransitioning = false;

        _transitionOutWork = null;
        _transitionInDone = null;

        _transitionFreezePlayer = false;
        _transitionUnfreezePlayer = false;
        _unfreezeAtFadeInStart = false;

        _pendingCameraPosSet = false;
        _pendingRefSet = false;
    }

    private void StreetTransitionOutWork()
    {
        if (!_pendingStreetDirectionSet)
            return;

        _pendingStreetDirectionSet = false;

        MapDirection direction = _pendingStreetDirection;

        if (!_mapManager.CanMove(direction))
            return;

        // Destroy old street BEFORE commit move (same ordering as before)
        //if (_world.Street != null)
        //    Object.Destroy(_world.Street.gameObject);

        _mapManager.CommitMove(direction);

        StreetRef nextStreet = _mapManager.GetCurrentStreet();
        _world.LoadStreet(_prefabs, nextStreet);

        _world.RepositionPlayerForStreetEntry(direction);

        _world.RepositionPlayerForStreetEntry(direction);
        _world.Npcs_SpawnStreet(DefaultNpcCount);
    }

    private void StreetTransitionInDone()
    {
        _navigation.EndStreetTransition();
    }

    private void EnterCorridorOutWork()
    {
        _world.Npcs_Clear();

        _world.SetStreetActive(false);
        _world.LoadCorridor(_prefabs.CorridorPrefab);
        _world.CenterCorridorOnCamera();

        Physics2D.SyncTransforms();

        _world.RepositionPlayerForCorridorSpawn();
    }

    private void EnterCorridorInDone()
    {
        _mode = LoopMode.Corridor;
        BindCorridorExitTrigger();
    }

    private void DoorPOVEnterApartmentOutWork()
    {
        _world.Npcs_Clear();

        // Preserve current behavior: do NOT exit POV here.
        float t = _enterBuildingStreetTSet ? _enterBuildingStreetT : 0.5f;
        _world.LoadApartment(_prefabs.ApartmentPrefab, t);

        _apartmentFullRefX = 3840;
        _apartmentFullRefY = 2160;
        _apartmentFullRefCached = true;

        _world.SetCameraRefResolution(_apartmentFullRefX, _apartmentFullRefY);
    }

    private void DoorPOVEnterApartmentInDone()
    {
        _mode = LoopMode.ApartmentFull;
    }

    private void ExitApartmentToCorridorOutWork()
    {
        _world.UnloadApartment();

        // Preserve existing flow: exit POV when leaving apartment back to corridor.
        _world.ExitCorridorDoorPOV();

        _game.CameraTransform.position = _returnCorridorCameraPos;

        _world.RestoreCameraRefResolution();

        _apartmentFullRefCached = false;
        _activeApartmentDoor = null;
    }

    private void ExitApartmentToCorridorInDone()
    {
        _mode = LoopMode.Corridor;
    }

    private void EnterApartmentWindowInDone()
    {
        _world.Npcs_SpawnApartmentWindow(DefaultNpcCount);
        _mode = LoopMode.ApartmentWindow;

        _mode = LoopMode.ApartmentWindow;
    }

    private void ExitApartmentWindowOutWork()
    {
        _world.Npcs_Clear();

        // Move camera to apartment full view anchor
        Transform viewFull = _world.Apartment != null
            ? _world.Apartment.transform.Find("Anchors/View_Full")
            : null;

        if (viewFull != null)
        {
            Vector3 camPos = _game.CameraTransform.position;
            camPos.x = viewFull.position.x;
            camPos.y = viewFull.position.y;
            _game.CameraTransform.position = camPos;
        }

        if (_apartmentFullRefCached)
            _world.SetCameraRefResolution(_apartmentFullRefX, _apartmentFullRefY);
    }

    private void ExitApartmentWindowInDone()
    {
        _mode = LoopMode.ApartmentFull;
    }

    private void ExitCorridorToStreetOutWork()
    {
        if (_corridorExitTrigger != null)
            _corridorExitTrigger.ExitRequested -= HandleCorridorExitRequested;

        _corridorExitTrigger = null;

        _enterBuildingStreetTSet = false;

        // Load the same street (no CommitMove)
        _world.UnloadCorridor();
        _world.SetStreetActive(true);

        // Restore player + camera where they were before entering corridor
        _world.PlayerTransform.position = _returnStreetPlayerPos;
        _game.CameraTransform.position = _returnStreetCameraPos;

        _world.Npcs_SpawnStreet(DefaultNpcCount);

        // Reset navigation state cleanly
        _navigation.Enter();
    }

    private void ExitCorridorToStreetInDone()
    {
        _mode = LoopMode.Street;
    }


    private void SetPendingCameraMove(Vector3 focus)
    {
        _pendingCameraPos = focus;
        _pendingCameraPosSet = true;
    }

    private void SetPendingRefResolution(int x, int y)
    {
        _pendingRefX = x;
        _pendingRefY = y;
        _pendingRefSet = true;
    }

    // ----------------------------
    // Mode Ticks
    // ----------------------------

    private void TickDoorPOV()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            _world.ExitCorridorDoorPOV();
            _mode = LoopMode.Corridor;

            _activeApartmentDoor = null;
            return;
        }

        if (!Keyboard.current.wKey.wasPressedThisFrame)
            return;

        if (_activeApartmentDoor == null || _prefabs?.ApartmentPrefab == null)
            return;

        StartTransition(
            outWork: DoorPOVEnterApartmentOutWork,
            inDone: DoorPOVEnterApartmentInDone,
            freezePlayer: false,
            unfreezePlayer: false,
            unfreezeAtFadeInStart: false
        );
    }

    private void TickApartmentFull()
    {
        // S in FULL apartment view => exit back to corridor
        if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
        {
            StartTransition(
                outWork: ExitApartmentToCorridorOutWork,
                inDone: ExitApartmentToCorridorInDone,
                freezePlayer: false,
                unfreezePlayer: false,
                unfreezeAtFadeInStart: false
            );

            return;
        }

        // LMB on window target => go to window view
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 screen = Mouse.current.position.ReadValue();
        float z = -_game.GlobalCamera.transform.position.z; // assumes world plane is z=0
        Vector3 wp3 = _game.GlobalCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
        Vector2 wp = new Vector2(wp3.x, wp3.y);

        Collider2D hit = Physics2D.OverlapPoint(wp);
        if (hit == null)
            return;

        ApartmentWindowNavTarget target = hit.GetComponentInParent<ApartmentWindowNavTarget>();
        if (target == null)
            return;

        Vector3 focus = target.GetCameraFocusPosition();

        // Prepare payload: camera + ref res are applied automatically during FadeOut.
        SetPendingCameraMove(focus);
        SetPendingRefResolution(1920, 1080);

        StartTransition(
            outWork: null,
            inDone: EnterApartmentWindowInDone,
            freezePlayer: false,
            unfreezePlayer: false,
            unfreezeAtFadeInStart: false
        );
    }

    private void TickApartmentWindow()
    {
        EnsurePlayerCombatBound();

        // ADS (example: RMB hold/release)
        if (Mouse.current != null && _playerCombat != null)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
                _playerCombat.StartAim();

            if (Mouse.current.rightButton.wasReleasedThisFrame)
                _playerCombat.ReleaseAim();
        }

        // S => exit window view
        if (Keyboard.current == null || !Keyboard.current.sKey.wasPressedThisFrame)
            return;

        // Hard stop aim when leaving window mode
        _playerCombat?.CancelAim();

        StartTransition(
            outWork: ExitApartmentWindowOutWork,
            inDone: ExitApartmentWindowInDone,
            freezePlayer: false,
            unfreezePlayer: false,
            unfreezeAtFadeInStart: false
        );
    }

    // ----------------------------
    // Interactions / Events
    // ----------------------------

    private void HandleBuildingDoorInteract(BuildingDoor door)
    {
        if (_isTransitioning || _mode != LoopMode.Street)
            return;

        _returnStreetPlayerPos = _world.PlayerTransform.position;
        _returnStreetCameraPos = _game.CameraTransform.position;

        float doorX = door != null ? door.transform.position.x : _world.PlayerTransform.position.x;

        float left = Mathf.Min(_world.StreetLeftX, _world.StreetRightX);
        float right = Mathf.Max(_world.StreetLeftX, _world.StreetRightX);

        _enterBuildingStreetT = Mathf.InverseLerp(left, right, doorX);
        _enterBuildingStreetTSet = true;

        StartTransition(
            outWork: EnterCorridorOutWork,
            inDone: EnterCorridorInDone,
            freezePlayer: true,
            unfreezePlayer: true,
            unfreezeAtFadeInStart: false
        );
    }

    private void BindCorridorExitTrigger()
    {
        if (_corridorExitTrigger != null)
            _corridorExitTrigger.ExitRequested -= HandleCorridorExitRequested;

        _corridorExitTrigger = Object.FindFirstObjectByType<CorridorExitTrigger>();

        if (_corridorExitTrigger != null)
            _corridorExitTrigger.ExitRequested += HandleCorridorExitRequested;
    }

    private void HandleCorridorExitRequested()
    {
        if (_isTransitioning || _mode != LoopMode.Corridor)
            return;

        StartTransition(
            outWork: ExitCorridorToStreetOutWork,
            inDone: ExitCorridorToStreetInDone,
            freezePlayer: true,
            unfreezePlayer: true,
            unfreezeAtFadeInStart: false
        );
    }

    public void EnterCorridorDoorPOV(Transform focus)
    {
        if (_isTransitioning || _mode != LoopMode.Corridor)
            return;

        _returnCorridorCameraPos = _game.CameraTransform.position;

        _world.EnterCorridorDoorPOV(focus);
        _mode = LoopMode.DoorPOV;
    }

    public void ExitCorridorDoorPOV()
    {
        if (_isTransitioning || _mode != LoopMode.DoorPOV)
            return;

        _world.ExitCorridorDoorPOV();
        _mode = LoopMode.Corridor;
    }

    private void HandleApartmentDoorInteract(ApartmentDoor door)
    {
        if (_isTransitioning || _mode != LoopMode.Corridor)
            return;

        _activeApartmentDoor = door;
        EnterCorridorDoorPOV(door.transform);
    }

    private void HandleWorldObjectInteract(WorldObjectUnit worldObject)
    {
        if (_isTransitioning)
            return;

        // Allow interactions in street + apartment window (as you said street objects appear there too).
        if (_mode != LoopMode.Street && _mode != LoopMode.ApartmentWindow)
            return;

        if (worldObject == null)
            return;

        // Next step: route to interaction components (Destroyable, Lockpickable, Readable, etc.)
        _logger?.Info($"[WorldObject] Interact: {worldObject.EntryKey} guid={worldObject.InstanceGuid}");
    }


    // ----------------------------
    // Player Stuff
    // ----------------------------
    private void EnsurePlayerCombatBound()
    {
        if (_playerCombat == null)
        {
            _playerCombat = _world != null ? _world.PlayerCombat : null;

            if (_playerCombat == null)
                _logger?.Info("[Combat] ERROR: PlayerCombat (CombatSystem) missing on Player prefab root.");
        }

        if (_playerCombat == null || _combatHooksBound) return;

        _playerCombat.OnAimStarted += HandleAimStarted;
        _playerCombat.OnAimCanceled += HandleAimCanceled;
        _playerCombat.OnAimReleased += HandleAimReleased;

        _combatHooksBound = true;
        _logger?.Info("[Combat] Bound player combat logs.");
    }

    private void HandleAimStarted() => _logger?.Info("[Combat] AimStarted");
    private void HandleAimCanceled() => _logger?.Info("[Combat] AimCanceled");
    private void HandleAimReleased() => _logger?.Info("[Combat] AimReleased");

}
