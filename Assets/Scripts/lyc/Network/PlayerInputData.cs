using Fusion;
using UnityEngine;

public enum PlayerButton
{
    Jump,
    Attack,
    Dash ,
    Interact
}
public unsafe struct PlayerInputData : INetworkInput
{
    public const int MaxHandBones = 26;

    public bool isPC;

    // PC
    public Vector2 Move;
    public Vector2 Look;
    public NetworkButtons Buttons;

    // VR
    public Vector3 headPosition;
    public Vector3 leftHandPosition;
    public Vector3 rightHandPosition;
    public Quaternion headRotation;
    public Quaternion leftHandRotation;
    public Quaternion rightHandRotation;

    // VR Hand Tracking
    public NetworkBool leftHandVisible;
    public NetworkBool rightHandVisible;

    public fixed uint leftBones[MaxHandBones];
    public fixed uint rightBones[MaxHandBones];

    public Vector3 leftOrientPosition;
    public Vector3 rightOrientPosition;
    public Quaternion leftOrientRotation;
    public Quaternion rightOrientRotation;

    // VR Vehicle
    public bool usingMotor;
    public Vector2 wheelDistance;
    public Quaternion handlebarLocalRotation;
}