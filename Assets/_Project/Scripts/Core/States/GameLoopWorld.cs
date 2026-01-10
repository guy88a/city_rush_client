using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.Core.Transitions;
using CityRush.Units.Characters.Controllers;
using CityRush.Units.Characters.View;
using CityRush.World.Background;
using CityRush.World.Interior;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
using CityRush.World.Street;
using UnityEngine;
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

    public float CameraHalfWidth { get; private set; }
    public float StreetLeftX { get; private set; }
    public float StreetRightX { get; private set; }

    public PlayerPOVController PlayerPOV { get; private set; }

    public GameLoopWorld(Game game, float navSpawnGapModifier = 0.2f)
    {
        _game = game;
        _navSpawnGapModifier = navSpawnGapModifier;
    }

    public void Enter(CorePrefabsRegistry prefabs, MapManager mapManager)
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

        // Player (after Street build)
        PlayerInstance = Object.Instantiate(prefabs.PlayerPrefab);
        PlayerTransform = PlayerInstance.transform;
        PlayerCollider = PlayerInstance.GetComponent<BoxCollider2D>();
        PlayerController = PlayerInstance.GetComponent<PlayerPlatformerController>();
        PlayerPOV = PlayerInstance.GetComponent<PlayerPOVController>();

        float spawnX = Street.SpawnX;
        PlayerTransform.position = new Vector3(spawnX, 0f, 0f);
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

        ScreenFade = null;
    }

    public void UnloadStreet()
    {
        if (Street != null)
            Object.Destroy(Street.gameObject);

        Street = null;
        StreetLeftX = 0f;
        StreetRightX = 0f;
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
            const float leftX = 0f;     // most-left building
            const float rightX = -50f;  // most-right building
            const float scale = 0.6f;

            // Apply scale for apartment view
            Street.transform.localScale = new Vector3(scale, scale, 1f);

            // Compute target X from door position
            float targetStreetX = Mathf.Lerp(leftX, rightX, streetT);

            // Compensate because street pivot is left-anchored
            float width = Mathf.Abs(StreetRightX - StreetLeftX);
            float offsetX = width * (1f - scale) * 0.5f;

            // Set BOTH X and Y in one place
            Vector3 p = Street.transform.position;
            p.y = 4f;
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
}
