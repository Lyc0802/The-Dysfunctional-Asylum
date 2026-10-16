
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum DeviceType : byte
{
    Unknown = 0,
    PC = 1,
    VR = 2
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Session")]
    [SerializeField] private string sessionName = "";
    [SerializeField] public string playerName = "";
    [SerializeField] private int maxPlayers = 3;

    [Header("Scenes")]
    [SerializeField] private int lobbyBuildIndex = 1;
    [SerializeField] private int gameBuildIndex = 2;

    [Header("Player")]
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private DeviceType deviceType = DeviceType.PC;
    public DeviceType LocalDeviceType => deviceType;

    [Header("Multi-Peer Test")]
    [SerializeField, Range(2, 8)] private int testPlayerCount = 3;
    [SerializeField] private DeviceType testClientDeviceType = DeviceType.PC;

    public NetworkRunner Runner { get; private set; }

    private readonly List<NetworkRunner> _runners = new();
    private bool isStarting;
    private bool isGameSceneLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGame()
    {
        StartSession();
    }

    // 正常模式：只建立一個 Runner
    public async void StartSession()
    {
        if (isStarting || _runners.Count > 0) return;

        isStarting = true;

        try
        {
            Runner = await CreateRunner(0, deviceType, playerName);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            isStarting = false;
        }
    }

    // Editor 測試：建立一個 Host + 多個 Client
    [ContextMenu("Start Multi-Peer Test")]
    public async void StartMultiPeerTest()
    {
#if UNITY_EDITOR
        if (isStarting || _runners.Count > 0) return;

        if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Multiple)
        {
            Debug.LogError("請先將 Fusion NetworkProjectConfig 的 Peer Mode 設為 Multiple");
            return;
        }

        isStarting = true;

        try
        {
            // 第一個 Runner 是 Host
            Runner = await CreateRunner(0, deviceType, playerName);

            if (Runner == null) return;

            // 其他 Runner 都是 Client
            for (int i = 1; i < testPlayerCount; i++)
            {
                string name = $"TestPlayer_{i}";
                NetworkRunner client = await CreateRunner(i, testClientDeviceType, name);
                if (client == null) break;
            }

            Debug.Log($"Multi-Peer Test: {_runners.Count} runners started.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            isStarting = false;
        }
#else
        Debug.LogWarning("Multi-Peer Test 只允許在 Unity Editor 使用");
#endif
    }

    private async Task<NetworkRunner> CreateRunner(int index, DeviceType peerDevice, string peerName)
    {
        var go = new GameObject($"NetworkRunner_{index}");
        DontDestroyOnLoad(go);

        var runner = go.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        var sceneManager = go.AddComponent<NetworkSceneManagerDefault>();

        // 每個 Runner 都有自己的玩家資訊與輸入來源
        var settings = go.AddComponent<RunnerPeerSettings>();
        settings.Initialize(peerDevice, peerName);

        // 使用 NetworkEvents 註冊 OnInput
        // var events = go.AddComponent<NetworkEvents>();
        // // events.OnInput.AddListener(settings.OnInput);
        // events.OnInput ??= new NetworkEvents.InputEvent();
        // events.OnInput.AddListener(settings.OnInput);
        var callbacks = new NetworkDelegates();
        callbacks.OnInput = settings.OnInput;
        runner.AddCallbacks(callbacks);
        // 讓 Multi-Peer 可以控制 Runner 的顯示
        go.AddComponent<RunnerEnableVisibility>();

        // 每個 Runner 都有自己的 Spawner
        var spawner = go.AddComponent<PlayerSpawner>();
        spawner.Initialize(playerPrefab);

        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(lobbyBuildIndex), LoadSceneMode.Single);

        try
        {
            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = sessionName,
                PlayerCount = Mathf.Max(maxPlayers, testPlayerCount),
                Scene = sceneInfo,
                SceneManager = sceneManager
            });

            if (!result.Ok)
            {
                Debug.LogError($"Runner {index} failed: {result.ShutdownReason}");
                Destroy(go);
                return null;
            }

            _runners.Add(runner);

            Debug.Log($"Runner {index}:, Player={runner.LocalPlayer}, Device={peerDevice}");
            return runner;
        }
        catch
        {
            if (runner != null) await runner.Shutdown();
            if (go != null) Destroy(go);
            throw;
        }
    }

    // Host 以 Additive 載入遊戲場景
    public void LoadGameScene()
    {
        if (Runner == null || !Runner.IsRunning || !Runner.IsSceneAuthority) return;
        if (isGameSceneLoaded) return;

        Runner.LoadScene(SceneRef.FromIndex(gameBuildIndex), LoadSceneMode.Additive);
        isGameSceneLoaded = true;
    }

    // 返回 Lobby：因為 Lobby 本來就還在，直接卸載 Game
    public void LoadLobbyScene()
    {
        if (Runner == null || !Runner.IsRunning || !Runner.IsSceneAuthority) return;
        if (!isGameSceneLoaded) return;

        Runner.UnloadScene(SceneRef.FromIndex(gameBuildIndex));
        isGameSceneLoaded = false;
    }

    public async void LeaveSession()
    {
        if (isStarting) return;

        var runners = new List<NetworkRunner>(_runners);
        _runners.Clear();
        Runner = null;

        foreach (var runner in runners)
        {
            if (runner == null) continue;

            try
            {
                await runner.Shutdown();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (runner != null) Destroy(runner.gameObject);
        }

        isGameSceneLoaded = false;
        Debug.Log("All local peers left session.");
    }

    public void SetSessionName(string newName) => sessionName = newName;
    public void SetPlayerName(string newName) => playerName = newName;
}
