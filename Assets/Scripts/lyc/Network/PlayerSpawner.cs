using System.Collections.Generic;
using Fusion;
using UnityEngine;


public class PlayerSpawner : SimulationBehaviour,
    IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkObject playerPrefab;

    private readonly Dictionary<PlayerRef, NetworkObject> _players = new();

    public void Initialize(NetworkObject prefab)
    {
        playerPrefab = prefab;
    }

    public void PlayerJoined(PlayerRef player)
    {
        // Host 才能 Spawn
        if (!Runner.IsServer) return;

        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is null!");
            return;
        }

        if (_players.ContainsKey(player)) return;

        Vector3 position = new Vector3(player.AsIndex * 2f, 3f, 0f);

        NetworkObject obj = Runner.Spawn(
            playerPrefab,
            position,
            Quaternion.identity,
            inputAuthority: player
        );

        if (obj == null) return;

        _players.Add(player, obj);

        // 將 NetworkObject 與 PlayerRef 關聯
        Runner.SetPlayerObject(player, obj);

        Debug.Log($"Player {player} spawned");
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!Runner.IsServer) return;

        if (_players.TryGetValue(player, out var obj))
        {
            if (obj != null && obj.IsValid) Runner.Despawn(obj);

            _players.Remove(player);
        }

        Debug.Log($"Player {player} left");
    }
}
