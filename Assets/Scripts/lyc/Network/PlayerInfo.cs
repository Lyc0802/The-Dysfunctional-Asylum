using Fusion;
using UnityEngine;

public class PlayerInfo : NetworkBehaviour
{ 
    [Networked] public string playerName { get; set; }
    [Networked] public DeviceType Device { get; set; }

    public GameObject prefabPC;
    public GameObject prefabVR;

    // public Transform playerPC;
    [Header("PC Setting")]
    [SerializeField] private float lookSensitivity = 0.15f;


    public override void Spawned()
    {
        if (HasInputAuthority) {
            // RPC_SetInitial(NetworkManager.Instance.LocalDeviceType, NetworkManager.Instance.playerName);
            RunnerPeerSettings RPS = Runner.GetComponent<RunnerPeerSettings>();
            GameObject cameraObj = new GameObject("PlayerCamera");
            cameraObj.transform.SetParent(transform, false);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;

            cameraObj.tag = "MainCamera";

            Camera playerCamera = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            RPC_SetInitial(RPS.Device, RPS.PlayerName);
            // Set Camera and Link VR Head and other to RunnerPeerSettings
        }
    }

    void SpawnModel()
    {
        switch (Device)
        {
            case DeviceType.PC:
                Instantiate(prefabPC, transform);
                break;
            case DeviceType.VR:
                Instantiate(prefabVR, transform);
                break;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetInitial(DeviceType device, string playerName)
    {
        Device = device;
        this.playerName = playerName;
        Debug.Log($"Player {playerName} [{Object.InputAuthority}]: {Device}");
        RPC_SetModel();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetModel()
    {
        SpawnModel();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out PlayerInputData input)) return;

        switch (Device)
        {
            case DeviceType.PC:
                UpdatePC(input);
                break;

            case DeviceType.VR:
                UpdateVR(input);
                break;
        }
    }

    private void UpdatePC(PlayerInputData input)
    {
        transform.Rotate(Vector3.up, input.Look.x * lookSensitivity);

        Vector2 move = input.Move;
        Vector3 dir = move.y * transform.forward + move.x * transform.right;
        dir.y = 0f;
        dir.Normalize();
        transform.position += dir * 5f * Runner.DeltaTime;
    }

    private void UpdateVR(PlayerInputData input)
    {
        // 處理 VR Input
    }
}
