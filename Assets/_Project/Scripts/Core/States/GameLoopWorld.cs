using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.Core.Transitions;
using CityRush.Units.Characters.Controllers;
using CityRush.World.Background;
using CityRush.World.Interior;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
using CityRush.World.Street;
using UnityEngine;

internal sealed class GameLoopWorld
{
    private readonly Game _game;
    private readonly float _navSpawnGapModifier;

    public ScreenFadeController ScreenFade { get; private set; }

    public BackgroundRoot Background { get; private set; }

    public StreetComponent Street { get; private set; }
    public CorridorComponent Corridor { get; private set; }

    public GameObject PlayerInstance { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public BoxCollider2D PlayerCollider { get; private set; }
    public PlayerPlatformerController PlayerController { get; private set; }

    public float CameraHalfWidth { get; private set; }
    public float StreetLeftX { get; private set; }
    public float StreetRightX { get; private set; }

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
        StreetLeftX = Street.LeftBoundX;
        StreetRightX = Street.RightBoundX;

        // Player (after Street build)
        PlayerInstance = Object.Instantiate(prefabs.PlayerPrefab);
        PlayerTransform = PlayerInstance.transform;
        PlayerCollider = PlayerInstance.GetComponent<BoxCollider2D>();
        PlayerController = PlayerInstance.GetComponent<PlayerPlatformerController>();

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

        Background = null;
        Street = null;
        Corridor = null;

        PlayerInstance = null;
        PlayerTransform = null;
        PlayerCollider = null;
        PlayerController = null;
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
        camPos.x = spawnX;
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


}
