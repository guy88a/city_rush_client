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

    public ScreenFadeController ScreenFade { get; private set; }

    public BackgroundRoot Background { get; private set; }

    public StreetComponent Street { get; private set; }
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

        Background = null;
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

        Transform parent = Background != null ? Background.transform : null;

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

    public void LoadApartment(ApartmentComponent apartmentPrefab)
    {
        if (Apartment != null)
            Object.Destroy(Apartment.gameObject);

        SetStreetActive(true);

        Transform parent = Background != null ? Background.transform : null;

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
    }

    public void UnloadApartment()
    {
        if (Apartment != null)
            Object.Destroy(Apartment.gameObject);

        Apartment = null;

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

        float x = floor.center.x;
        float y = Corridor.FloorTopWorldY_FromCollider;

        float playerHalfH = 0f;
        if (PlayerCollider != null)
            playerHalfH = PlayerCollider.bounds.extents.y;

        PlayerTransform.position = new Vector3(x, y + playerHalfH + 1f, 0f);

        //Vector3 camPos = _game.CameraTransform.position;
        //camPos.x = x;
        //_game.CameraTransform.position = camPos;
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
