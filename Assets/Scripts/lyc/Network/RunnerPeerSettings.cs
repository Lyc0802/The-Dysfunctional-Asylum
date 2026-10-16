
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class RunnerPeerSettings : MonoBehaviour
{
    public DeviceType Device { get; private set; }
    public string PlayerName { get; private set; }
    [Header("PC Setting")]
    private Vector2 mouseDelta = Vector2.zero;

    void Update()
    {
        mouseDelta += Mouse.current.delta.ReadValue();;
    }
    public void Initialize(DeviceType device, string playerName)
    {
        Device = device;
        PlayerName = playerName;
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        PlayerInputData data = new PlayerInputData();

        switch (Device)
        {
            case DeviceType.PC:
                ReadPCInput(ref data);
                break;

            case DeviceType.VR:
                ReadVRInput(ref data);
                break;
        }

        input.Set(data);
    }

    private void ReadPCInput(ref PlayerInputData data)
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.wKey.isPressed) data.Move.y += 1;
        if (Keyboard.current.sKey.isPressed) data.Move.y -= 1;
        if (Keyboard.current.aKey.isPressed) data.Move.x -= 1;
        if (Keyboard.current.dKey.isPressed) data.Move.x += 1;

        data.Move = data.Move.normalized;
        data.Look = mouseDelta;
        mouseDelta = Vector2.zero;
    }

    private void ReadVRInput(ref PlayerInputData data)
    {
        // TODO: 讀取 VR Controller / Hand Tracking
    }
}
