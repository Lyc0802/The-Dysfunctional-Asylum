
using Fusion;
using UnityEngine;

public enum GameState
{
    Lobby,
    InGame,
    GameOver
}

public enum WinningTeam
{
    None,
    PC,
    VR
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float maxBattery = 100f;

    [Header("Spawn Points")]
    [SerializeField] private Transform pcSpawnPoint;
    [SerializeField] private Transform vrSpawnPoint;
    [SerializeField] private Vector3 lobbySpawnPoint = new Vector3(0, 1, 0);

    [Header("Spawn Offsets")]
    [SerializeField] private Vector3 pcSpawnOffset = new Vector3(2, 0, 0);
    [SerializeField] private Vector3 vrSpawnOffset = new Vector3(2, 0, 0);
    [SerializeField] private float lobbySpawnSpacing = 2f;

    [Networked, OnChangedRender(nameof(OnGameStateChanged))]
    public GameState State { get; set; }

    [Networked] public float currentBattery { get; set; }
    [Networked] public float InGameTime { get; set; }
    [SerializeField] private float VRWinningTime = 600f;

    [Networked] public WinningTeam LastWinning { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        State = GameState.Lobby;
        currentBattery = maxBattery;
        InGameTime = 0f;
        LastWinning = WinningTeam.None;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || State != GameState.InGame) return;

        InGameTime += Runner.DeltaTime;
        if (InGameTime >= VRWinningTime)
        {
            EndGame(WinningTeam.VR);
        }
    }

    public void StartGame()
    {
        if (!HasStateAuthority || State != GameState.Lobby) return;

        currentBattery = maxBattery;
        InGameTime = 0f;
        State = GameState.InGame;

        int pcIndex = 0;
        int vrIndex = 0;

        foreach (PlayerRef playerRef in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(playerRef, out NetworkObject obj)) continue;
            if (!obj.TryGetComponent(out PlayerInfo player)) continue;

            Transform spawnPoint;
            Vector3 offset;

            switch (player.Device)
            {
                case DeviceType.PC:
                    spawnPoint = pcSpawnPoint;
                    offset = pcSpawnOffset * pcIndex++;
                    break;

                case DeviceType.VR:
                    spawnPoint = vrSpawnPoint;
                    offset = vrSpawnOffset * vrIndex++;
                    break;

                default:
                    continue;
            }

            if (spawnPoint == null) continue;

            Vector3 position = spawnPoint.TransformPoint(offset);
            player.Teleport(position, spawnPoint.rotation);
        }
    }

    public void EndGame(WinningTeam winner)
    {
        if (!HasStateAuthority || State != GameState.InGame) return;

        LastWinning = winner;
        State = GameState.GameOver;

        int index = 0;

        foreach (PlayerRef playerRef in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(playerRef, out NetworkObject obj)) continue;
            if (!obj.TryGetComponent(out PlayerInfo player)) continue;

            Vector3 position = lobbySpawnPoint + Vector3.right * lobbySpawnSpacing * index++;
            player.Teleport(position, Quaternion.identity);
        }

        State = GameState.Lobby;
    }

    public void SetBattery(float value)
    {
        if (!HasStateAuthority) return;

        currentBattery = Mathf.Clamp(value, 0f, maxBattery);
    }

    public void AddBattery(float amount)
    {
        if (!HasStateAuthority) return;

        SetBattery(currentBattery + amount);
    }

    private void OnGameStateChanged()
    {
        Debug.Log($"Game State: {State}");
    }
}
