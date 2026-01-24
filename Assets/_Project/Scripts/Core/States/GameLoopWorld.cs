using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.Core.Transitions;
using CityRush.Items;
using CityRush.Items.World;
using CityRush.Units.Characters;
using CityRush.Units.Characters.Combat;
using CityRush.Units.Characters.Controllers;
using CityRush.Units.Characters.View;
using CityRush.World.Background;
using CityRush.World.Interior;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
using CityRush.World.Street;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

internal sealed class GameLoopWorld
{
    private PixelPerfectCamera _ppc;
    private int _ppcRefX;
    private int _ppcRefY;
    private bool _ppcCached;

    private readonly Game _game;
    private readonly float _navSpawnGapModifier;

    private Vector3 _streetPosBeforeApartment;
    private Vector3 _streetScaleBeforeApartment;
    private Vector3 _backgroundPosBeforeApartment;
    private bool _apartmentBgStreetPosCached;
    private const float ApartmentBackgroundRootX = -20f;

    private Vector3 _streetPosBeforeADS;
    private Vector3 _streetScaleBeforeADS;
    private bool _streetADSCacheSet;
    private float _streetCamLocalXBeforeADS;

    private bool _adsPanActive;
    private Vector3 _adsCamOrigin;
    private Vector2 _adsPrevMouseScreen;

    private const float AdsMaxPanX = 12f;
    private const float AdsMaxPanY = 12f;

    private Vector3 _adsPanRoot;
    private bool _adsPanRootSet;

    private Vector3 _adsCamPos;
    private bool _adsCamPosSet;

    public float AdsMouseSensitivity { get; set; } = 1f;

    public float StreetBottomY { get; private set; }
    public float StreetTopY { get; private set; }

    private CityRush.Units.Characters.Spawning.NPCSpawnManager _npcs;

    public ScreenFadeController ScreenFade { get; private set; }

    public BackgroundRoot Background { get; private set; }

    public StreetComponent Street { get; private set; }

    private Transform InteriorRoot;
    public CorridorComponent Corridor { get; private set; }
    public ApartmentComponent Apartment { get; private set; }

    public GameObject PlayerInstance { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public BoxCollider2D PlayerCollider { get; private set; }
    public PlayerPlatformerController PlayerController { get; private set; }
    public WeaponShooter PlayerShooter { get; private set; }
    public CharacterWeaponSet PlayerWeapons { get; private set; }

    public float CameraHalfWidth { get; private set; }
    public float StreetLeftX { get; private set; }
    public float StreetRightX { get; private set; }

    public PlayerPOVController PlayerPOV { get; private set; }
    public CharacterUnit PlayerUnit { get; private set; }
    public SniperAimState PlayerAim { get; private set; }
    public GameObject PlayerScopeUI { get; private set; }

    private ItemsDb _itemsDb;

    public GameLoopWorld(Game game, float navSpawnGapModifier = 0.2f)
    {
        _game = game;
        _navSpawnGapModifier = navSpawnGapModifier;
    }

    public void Enter(CorePrefabsRegistry prefabs, MapManager mapManager, ItemsDb itemsDb)
    {
        // Fade Screen
        GameObject fadeGO = Object.Instantiate(prefabs.ScreenFadeCanvasPrefab);
        ScreenFade = fadeGO.GetComponent<ScreenFadeController>();

        // Background
        Background = Object.Instantiate(prefabs.BackgroundPrefab);
        Background.CameraTransform = _game.CameraTransform;

        // Street
        Street = Object.Instantiate(prefabs.StreetPrefab);
        Street.Initialize(_game.GlobalCamera);

        StreetRef streetRef = mapManager.GetCurrentStreet();
        BuildStreet(streetRef);

        // Interior Root (Corridor + Apartment live here, not under Background)
        InteriorRoot = new GameObject("InteriorRoot").transform;

        // Cache bounds and camera metrics (after build)
        Camera cam = _game.GlobalCamera;
        CameraHalfWidth = cam.orthographicSize * cam.aspect;
        _ppc = cam.GetComponent<PixelPerfectCamera>();
        if (_ppc != null)
        {
            _ppcRefX = _ppc.refResolutionX;
            _ppcRefY = _ppc.refResolutionY;
            _ppcCached = true;
        }

        StreetLeftX = Street.LeftBoundX;
        StreetRightX = Street.RightBoundX;

        StreetBottomY = 0f;
        StreetTopY = 10f;

        _npcs = new CityRush.Units.Characters.Spawning.NPCSpawnManager();
        _npcs.Enter(prefabs.NPCPrefab);
        _npcs.SetStreetBounds(StreetLeftX, StreetRightX);
        //_npcs.SpawnAgents(5); // ***TOREMOVE***

        // Player (after Street build)
        PlayerInstance = Object.Instantiate(prefabs.PlayerPrefab);
        PlayerTransform = PlayerInstance.transform;

        PlayerInstance
        .GetComponent<PlayerItemsRuntime>()
        .Init(itemsDb);

        _itemsDb = itemsDb;

        PlayerCollider = PlayerInstance.GetComponent<BoxCollider2D>();
        PlayerController = PlayerInstance.GetComponent<PlayerPlatformerController>();
        PlayerPOV = PlayerInstance.GetComponent<PlayerPOVController>();
        PlayerUnit = PlayerInstance.GetComponent<CharacterUnit>();
        PlayerWeapons = PlayerInstance.GetComponent<CharacterWeaponSet>();
        PlayerShooter = PlayerInstance.GetComponent<WeaponShooter>();
        PlayerAim = PlayerInstance.GetComponent<SniperAimState>();
        PlayerScopeUI = PlayerInstance.transform.Find("UI_SniperScope")?.gameObject;
        if (PlayerScopeUI != null)
            PlayerScopeUI.SetActive(false);

        float spawnX = Street.SpawnX;
        PlayerTransform.position = new Vector3(spawnX, 0f, 0f);

        if (PlayerTransform != null)
        {
            Vector3 p = PlayerTransform.position;
            SpawnItemPickup(itemId: 1001, amount: 1, worldPos: p + new Vector3(3.5f, -1f, 0f));
            SpawnItemPickup(itemId: 1002, amount: 1, worldPos: p + new Vector3(2.5f, -1f, 0f));
            SpawnItemPickup(itemId: 3001, amount: 1, worldPos: p + new Vector3(6.5f, -1f, 0f));
        }
    }

    public void Exit()
    {
        if (Background != null)
            Object.Destroy(Background.gameObject);

        if (Street != null)
            Object.Destroy(Street.gameObject);

        if (Corridor != null)
            Object.Destroy(Corridor.gameObject);

        if (PlayerInstance != null)
            Object.Destroy(PlayerInstance);

        if (Apartment != null)
            Object.Destroy(Apartment.gameObject);

        if (ScreenFade != null)
            Object.Destroy(ScreenFade.gameObject);

        if (InteriorRoot != null)
            Object.Destroy(InteriorRoot.gameObject);

        _npcs?.Exit();

        Background = null;

        InteriorRoot = null;
        Street = null;
        Corridor = null;
        Apartment = null;

        PlayerInstance = null;
        PlayerTransform = null;
        PlayerCollider = null;
        PlayerController = null;
        PlayerPOV = null;
        PlayerUnit = null;
        PlayerWeapons = null;
        PlayerShooter = null;
        PlayerAim = null;
        PlayerScopeUI = null;

        _npcs?.SetStreetSpace(null);
        _npcs?.Exit();
        _npcs = null;

        ScreenFade = null;
    }

    public void UnloadStreet()
    {
        _npcs?.SetStreetSpace(null);
        _npcs?.ClearAll();

        if (Street != null)
            Object.Destroy(Street.gameObject);

        Street = null;
        StreetLeftX = 0f;
        StreetRightX = 0f;
        StreetBottomY = 0f;
        StreetTopY = 0f;
    }

    public void SetStreetActive(bool active)
    {
        if (Street != null)
            Street.gameObject.SetActive(active);

        if (Background != null)
            Background.SetLayersActive(active);
    }

    public void LoadCorridor(CorridorComponent corridorPrefab)
    {
        if (Corridor != null)
            Object.Destroy(Corridor.gameObject);

        Transform parent = InteriorRoot;

        Corridor = parent != null
            ? Object.Instantiate(corridorPrefab, parent)
            : Object.Instantiate(corridorPrefab);

        Corridor.Rebuild();

        if (Background != null)
            Background.SetLayersActive(false);
    }

    public void UnloadCorridor()
    {
        if (Corridor != null)
            Object.Destroy(Corridor.gameObject);

        Corridor = null;
    }

    /// <summary>
    /// Destroys current Street (if any), instantiates a new Street, builds it from JSON, and refreshes bounds.
    /// MapManager.CommitMove(...) should be done by the caller (navigation layer), not here.
    /// </summary>
    public void LoadStreet(CorePrefabsRegistry prefabs, StreetRef streetRef)
    {
        if (Corridor != null)
        {
            Object.Destroy(Corridor.gameObject);
            Corridor = null;
        }

        if (Street != null)
            Object.Destroy(Street.gameObject);

        Street = Object.Instantiate(prefabs.StreetPrefab);
        Street.Initialize(_game.GlobalCamera);

        BuildStreet(streetRef);

        if (Background != null)
            Background.SetLayersActive(true);

        StreetLeftX = Street.LeftBoundX;
        StreetRightX = Street.RightBoundX;

        StreetBottomY = 0f;
        StreetTopY = 10f;

        _npcs?.SetStreetSpace(null);
        _npcs?.SetStreetBounds(StreetLeftX, StreetRightX);
        //_npcs?.ClearAll();
        //_npcs?.SpawnAgents(5); // ***TOREMOVE***
    }

    public void LoadApartment(ApartmentComponent apartmentPrefab, float streetT)
    {
        if (Apartment != null)
            Object.Destroy(Apartment.gameObject);

        if (!_apartmentBgStreetPosCached)
        {
            if (Street != null) _streetPosBeforeApartment = Street.transform.position;
            if (Street != null) _streetScaleBeforeApartment = Street.transform.localScale;
            if (Background != null) _backgroundPosBeforeApartment = Background.transform.position;

            _apartmentBgStreetPosCached = true;
        }

        // Background Y = 13 (keep X/Z)
        if (Background != null)
        {
            Vector3 p = Background.transform.position;
            p.y = 13f;
            Background.transform.position = p;
        }

        SetStreetActive(true);

        Transform parent = InteriorRoot;

        Apartment = parent != null
            ? Object.Instantiate(apartmentPrefab, parent)
            : Object.Instantiate(apartmentPrefab);

        if (Corridor != null)
            Corridor.gameObject.SetActive(false);

        // Move camera to apartment full view anchor
        Transform viewFull = Apartment.transform.Find("Anchors/View_Full");
        if (viewFull != null)
        {
            Vector3 camPos = _game.CameraTransform.position;
            camPos.x = viewFull.position.x;
            camPos.y = viewFull.position.y;
            _game.CameraTransform.position = camPos;
        }

        streetT = Mathf.Clamp01(streetT);

        if (Street != null)
        {
            const float scale = 0.5f;
            const float leftX = 0f;     // most-left building
            const float rightX = -50f;  // most-right building

            // Apply scale for apartment view
            Street.transform.localScale = new Vector3(scale, scale, 1f);

            // Compute target X from door position
            float targetStreetX = Mathf.Lerp(leftX, rightX, streetT);

            // Compensate because street pivot is left-anchored
            float width = Mathf.Abs(StreetRightX - StreetLeftX);
            float offsetX = width * (1f - scale) * 0.5f;

            // Set BOTH X and Y in one place
            Vector3 p = Street.transform.position;
            p.y = 8f;
            p.x = targetStreetX + offsetX;
            Street.transform.position = p;
        }


        if (Background != null)
        {
            Vector3 bp = Background.transform.position;
            bp.x = ApartmentBackgroundRootX;
            Background.transform.position = bp;
        }
    }

    public void UnloadApartment()
    {
        PlayerShooter?.CancelSniperBullet();

        if (Apartment != null)
            Object.Destroy(Apartment.gameObject);

        Apartment = null;

        if (_apartmentBgStreetPosCached)
        {
            if (Background != null) Background.transform.position = _backgroundPosBeforeApartment;
            if (Street != null) Street.transform.position = _streetPosBeforeApartment;
            if (Street != null) Street.transform.localScale = _streetScaleBeforeApartment;
            _apartmentBgStreetPosCached = false;
        }

        SetStreetActive(false);

        if (Corridor != null)
            Corridor.gameObject.SetActive(true);
    }


    /// <summary>
    /// Repositions the player to the correct entering edge and hard-resets camera X to match.
    /// Returns the spawnX used.
    /// </summary>
    public float RepositionPlayerForStreetEntry(MapDirection direction)
    {
        float spawnX = direction == MapDirection.Right
            ? StreetLeftX + (CameraHalfWidth * _navSpawnGapModifier)
            : StreetRightX - (CameraHalfWidth * _navSpawnGapModifier);

        if (PlayerTransform != null)
            PlayerTransform.position = new Vector3(spawnX, PlayerTransform.position.y, 0f);

        Vector3 camPos = _game.CameraTransform.position;

        float minX = StreetLeftX + CameraHalfWidth;
        float maxX = StreetRightX - CameraHalfWidth;

        // IMPORTANT: prevent 1-frame snap on first Tick after fade-in
        camPos.x = Mathf.Clamp(spawnX, minX, maxX);

        _game.CameraTransform.position = camPos;

        return spawnX;
    }

    private void BuildStreet(StreetRef streetRef)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Maps/{streetRef.JsonPath}");

        Street.Build(new StreetLoadRequest(
            streetRef.StreetId,
            jsonAsset.text
        ));
    }

    public void RepositionPlayerForCorridorSpawn()
    {
        if (Corridor == null || PlayerTransform == null)
            return;

        Bounds floor = Corridor.FloorBoundsWorld;

        // Always spawn near the corridor's left edge (2 world units in),
        // and keep the player fully inside the floor bounds.
        float desiredX = floor.min.x + 2f;

        float playerHalfW = 0f;
        float playerHalfH = 0f;

        if (PlayerCollider != null)
        {
            playerHalfW = PlayerCollider.bounds.extents.x;
            playerHalfH = PlayerCollider.bounds.extents.y;
        }

        float minX = floor.min.x + playerHalfW;
        float maxX = floor.max.x - playerHalfW;

        float x = Mathf.Clamp(desiredX, minX, maxX);

        float y = Corridor.FloorTopWorldY_FromCollider;

        PlayerTransform.position = new Vector3(x, y + playerHalfH + 1f, 0f);
    }


    public void CenterCorridorOnCamera()
    {
        if (Corridor == null)
            return;

        Bounds b = Corridor.VisualBoundsWorld;
        if (b.size == Vector3.zero)
            return;

        Vector3 camPos = _game.CameraTransform.position;

        float dx = camPos.x - b.center.x;
        float dy = camPos.y - b.center.y;

        Corridor.transform.position += new Vector3(dx, dy, 0f);
    }

    public void EnterCorridorDoorPOV(Transform focus)
    {
        if (Corridor == null || PlayerPOV == null || focus == null)
            return;

        PlayerPOV.EnterPOV();
        Corridor.EnterDoorPOV(focus, _game.CameraTransform);
    }

    public void ExitCorridorDoorPOV()
    {
        if (Corridor == null || PlayerPOV == null)
            return;

        Corridor.ExitDoorPOV();
        PlayerPOV.ExitPOV();
    }

    public void SetCameraRefResolution(int x, int y)
    {
        if (_ppc == null) return;
        _ppc.refResolutionX = x;
        _ppc.refResolutionY = y;
    }

    public void RestoreCameraRefResolution()
    {
        if (_ppc == null || !_ppcCached) return;
        _ppc.refResolutionX = _ppcRefX;
        _ppc.refResolutionY = _ppcRefY;
    }

    public void Npcs_Clear()
    {
        _npcs?.SetStreetSpace(null);
        _npcs?.ClearAll();
    }

    public void Npcs_SpawnStreet(int count)
    {
        if (_npcs == null) return;

        _npcs.SetStreetSpace(null);
        _npcs.SetGroundY(0f);
        _npcs.ClearAll();
        _npcs.SpawnAgents(count);
    }

    public void Npcs_SpawnApartmentWindow(int count)
    {
        if (_npcs == null || Street == null) return;

        // Street already has p.y = 8f and scale = 0.5f set in LoadApartment(),
        // so street-space -> world-space conversion will account for it.
        _npcs.SetStreetSpace(Street.transform);
        _npcs.SetGroundY(0f);
        _npcs.ClearAll();
        _npcs.SpawnAgents(count);
    }

    public void EnterWindowADS()
    {
        if (Street == null || Apartment == null)
            return;

        if (!_streetADSCacheSet)
        {
            _streetPosBeforeADS = Street.transform.position;
            _streetScaleBeforeADS = Street.transform.localScale;
            _streetADSCacheSet = true;
        }

        _streetCamLocalXBeforeADS = Street.transform.InverseTransformPoint(_game.CameraTransform.position).x;

        Apartment.gameObject.SetActive(false);

        float prevScale = _streetScaleBeforeADS.x; // 0.5

        _npcs?.CacheActiveLocalX();

        Street.transform.localScale = Vector3.one;

        float width = Mathf.Abs(StreetRightX - StreetLeftX);
        float offsetX = width * (1f - prevScale) * 0.5f;

        Vector3 p = Street.transform.position;
        p.x -= offsetX;
        Street.transform.position = p;

        float worldXAfter = Street.transform.TransformPoint(new Vector3(_streetCamLocalXBeforeADS, 0f, 0f)).x;
        float dx = _game.CameraTransform.position.x - worldXAfter;

        Vector3 p2 = Street.transform.position;
        p2.x += dx;
        Street.transform.position = p2;

        _npcs?.RestoreActiveFromCachedLocalX();
        _npcs?.RefreshVisualScale();

        _adsPanActive = true;

        if (!_adsPanRootSet)
        {
            // Fallback safety: if caller forgot to set root on window entry
            _adsPanRoot = _game.CameraTransform.position;
            _adsPanRootSet = true;
        }

        // Restore last ADS camera position (does NOT change root)
        if (_adsCamPosSet)
        {
            Vector3 camPos = _game.CameraTransform.position;
            camPos.x = _adsCamPos.x;
            camPos.y = _adsCamPos.y;
            _game.CameraTransform.position = camPos;
        }

        if (Mouse.current != null)
            _adsPrevMouseScreen = Mouse.current.position.ReadValue();

        if (PlayerScopeUI != null)
            PlayerScopeUI.SetActive(true);
    }

    public void ExitWindowADS()
    {
        if (Street == null || Apartment == null)
            return;

        // Cache last ADS camera position (so next ADS restores it)
        _adsCamPos = _game.CameraTransform.position;
        _adsCamPosSet = true;

        // Restore apartment visuals
        Apartment.gameObject.SetActive(true);

        // Restore street transform back to window/apartment state
        if (_streetADSCacheSet)
        {
            _npcs?.CacheActiveLocalX();

            Street.transform.position = _streetPosBeforeADS;
            Street.transform.localScale = _streetScaleBeforeADS;

            _npcs?.RestoreActiveFromCachedLocalX();
            _npcs?.RefreshVisualScale();

            _streetADSCacheSet = false;
        }

        // Restore non-ADS (window) camera position (X/Y) after exiting ADS
        Vector3 camPos = _game.CameraTransform.position;
        camPos.x = _adsPanRoot.x;
        camPos.y = _adsPanRoot.y;
        _game.CameraTransform.position = camPos;

        // Prevent first-frame jump next ADS due to stale prev mouse
        if (Mouse.current != null)
            _adsPrevMouseScreen = Mouse.current.position.ReadValue();

        if (PlayerScopeUI != null)
            PlayerScopeUI.SetActive(false);

        _adsPanActive = false;
    }


    public void TickWindowADS(float deltaTime)
    {
        // Bullet keeps running as long as we are in apartment mode (this tick is being called).
        PlayerShooter?.TickSniperBullet(deltaTime);

        // ADS pan + shooting only when ADS is active
        if (!_adsPanActive)
            return;

        if (Mouse.current == null)
            return;

        Camera cam = _game.GlobalCamera;
        if (cam == null)
            return;

#if UNITY_EDITOR
        // Debug block stays here if you want it later (no GetComponent here)
        // if (Mouse.current.leftButton.wasPressedThisFrame)
        // {
        //     var prefab = PlayerWeapons != null && PlayerWeapons.SniperWeapon != null
        //         ? PlayerWeapons.SniperWeapon.SniperDebugMarkerPrefab
        //         : null;
        //     PlayerShooter?.DebugSpawnSniperMarker(cam, prefab);
        // }
#endif

        if (Mouse.current.leftButton.wasPressedThisFrame)
            PlayerWeapons?.TryFireSniperADS(cam);

        // ---- your existing pan code continues here unchanged ----
        Vector2 mouseNow = Mouse.current.position.ReadValue();
        Vector2 mousePrev = _adsPrevMouseScreen;
        _adsPrevMouseScreen = mouseNow;

        if (mouseNow == mousePrev)
            return;

        float z = -cam.transform.position.z;

        Vector3 wPrev3 = cam.ScreenToWorldPoint(new Vector3(mousePrev.x, mousePrev.y, z));
        Vector3 wNow3 = cam.ScreenToWorldPoint(new Vector3(mouseNow.x, mouseNow.y, z));

        Vector2 deltaWorld = new Vector2(wNow3.x - wPrev3.x, wNow3.y - wPrev3.y);

        float sens = Mathf.Max(0f, AdsMouseSensitivity);
        deltaWorld *= sens;

        Vector3 camPos = _game.CameraTransform.position;
        camPos.x += deltaWorld.x;
        camPos.y += deltaWorld.y;

        camPos.x = Mathf.Clamp(camPos.x, _adsPanRoot.x - AdsMaxPanX, _adsPanRoot.x + AdsMaxPanX);
        camPos.y = Mathf.Clamp(camPos.y, _adsPanRoot.y - AdsMaxPanY, _adsPanRoot.y + AdsMaxPanY);

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float left = Mathf.Min(StreetLeftX, StreetRightX);
        float right = Mathf.Max(StreetLeftX, StreetRightX);

        float minX = left + halfW;
        float maxX = right - halfW;

        float bottom = Mathf.Min(StreetBottomY, StreetTopY);
        float top = Mathf.Max(StreetBottomY, StreetTopY);

        float minY = bottom + halfH;
        float maxY = top - halfH;

        if (minX <= maxX)
            camPos.x = Mathf.Clamp(camPos.x, minX, maxX);

        if (minY <= maxY)
            camPos.y = Mathf.Clamp(camPos.y, minY, maxY);

        _game.CameraTransform.position = camPos;
    }

    public void WindowPan_SetRootFromCamera()
    {
        _adsPanRoot = _game.CameraTransform.position;
        _adsPanRootSet = true;

        // New window session => ADS starts from the window root.
        _adsCamPos = _adsPanRoot;
        _adsCamPosSet = true;
    }

    public void WindowPan_ClearRoot()
    {
        _adsPanRootSet = false;

        // Leaving window mode => reset ADS position to current (Apartment Full) camera.
        _adsCamPos = _game.CameraTransform.position;
        _adsCamPosSet = true;
    }

    private GameObject SpawnItemPickup(int itemId, int amount, Vector3 worldPos)
    {
        if (Street == null || _itemsDb == null)
            return null;

        GameObject prefab = Resources.Load<GameObject>("Items/Prefabs/ItemPickup");
        if (prefab == null)
        {
            Debug.LogWarning("[Items] Missing pickup prefab at Resources/Items/Prefabs/ItemPickup");
            return null;
        }

        Transform parent = Street.ItemsRoot != null ? Street.ItemsRoot : Street.transform;

        GameObject go = Object.Instantiate(prefab, parent);
        go.transform.position = worldPos;

        ItemPickup pickup = go.GetComponent<ItemPickup>();
        if (pickup == null)
        {
            Debug.LogWarning("[Items] Spawned ItemPickup prefab has no ItemPickup component.");
            return go;
        }

        pickup.SetItem(_itemsDb, itemId, amount); // use YOUR actual API name (see note below)
        return go;
    }


}
