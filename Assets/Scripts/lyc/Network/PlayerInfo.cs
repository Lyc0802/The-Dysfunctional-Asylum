using Fusion;
using UnityEngine;
using Bombanana.Interaction;

public enum PCPlayerCharacter : byte
{
    Unknown = 0,
    Commander = 1,
    Glitch = 2,
    Freddy = 3
}

public class PlayerInfo : NetworkBehaviour
{
    [Networked] public string playerName { get; set; }
    [Networked, OnChangedRender(nameof(OnDeviceChanged))] public DeviceType Device { get; set; }


    private RunnerPeerSettings rps;
    private bool _modelSpawned;

    [Header("Movement")]
    [SerializeField] private NetworkCharacterController networkCharacterController;
    [SerializeField] private VehicleCharacterMotor characterMotor;

    [Header("PC Setting")]
    [Networked] public PCPlayerCharacter character { get; set; }
    public GameObject prefabPC;
    public Commander commander;
    public Glitch glitch;
    public Freddy freddy;

    [Header("VR Setting")]
    public GameObject prefabVR;
    public GameObject VRRigPrefab;
    public GameObject vrRig;

    public VRModel vrModel;

    public Transform headAnchor;
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;

    public Wheelchair wheelchair;
    public MotorcycleController motorcycleController;

    [Networked] private float MotorcycleSpeed { get; set; }
    [Networked] public bool isUsingMonitor { get; set; }

    [Networked] private Vector2 WheelAngles { get; set; }
    [Networked] private Quaternion HandlebarRotation { get; set; }
    [Networked] private NetworkBool UsingMotor { get; set; }

    [Networked] private Vector3 HeadPosition { get; set; }
    [Networked] private Quaternion HeadRotation { get; set; }

    [Networked] private Vector3 LeftHandPosition { get; set; }
    [Networked] private Quaternion LeftHandRotation { get; set; }

    [Networked] private Vector3 RightHandPosition { get; set; }
    [Networked] private Quaternion RightHandRotation { get; set; }

    [Header("Remote Hand Visuals")]
    [SerializeField] private CustomHandVisual leftHandVisual;
    [SerializeField] private CustomHandVisual rightHandVisual;

    [Networked, Capacity(26)] private NetworkArray<uint> LeftBones => default;
    [Networked, Capacity(26)] private NetworkArray<uint> RightBones => default;

    [Networked] private NetworkBool LeftHandVisible { get; set; }
    [Networked] private NetworkBool RightHandVisible { get; set; }

    [Networked] private Vector3 LeftOrientPosition { get; set; }
    [Networked] private Vector3 RightOrientPosition { get; set; }

    [Networked] private Quaternion LeftOrientRotation { get; set; }
    [Networked] private Quaternion RightOrientRotation { get; set; }

    private void Awake()
    {
        if (networkCharacterController == null)
            networkCharacterController = GetComponent<NetworkCharacterController>();

        if (characterMotor == null)
            characterMotor = GetComponent<VehicleCharacterMotor>();

        if (commander == null)
            commander = GetComponent<Commander>();

        if (glitch == null)
            glitch = GetComponent<Glitch>();

        if (freddy == null)
            freddy = GetComponent<Freddy>();

        if (wheelchair == null)
            wheelchair = GetComponent<Wheelchair>();

        if (motorcycleController == null)
            motorcycleController = GetComponent<MotorcycleController>();
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            rps = Runner.GetComponent<RunnerPeerSettings>();
            RPC_SetInitial(rps.Device, rps.PlayerName);

            if (rps.Device == DeviceType.PC)
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform, false);
                cameraObj.transform.localPosition = Vector3.zero;
                cameraObj.transform.localRotation = Quaternion.identity;
                cameraObj.tag = "MainCamera";

                cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();

                if (commander != null)
                    commander.cameraPivot = cameraObj.transform;
                if (glitch != null)
                    glitch.cameraPivot = cameraObj.transform;
                if (freddy != null)
                    freddy.cameraPivot = cameraObj.transform;
            }
            else if (rps.Device == DeviceType.VR)
            {
                vrRig = Instantiate(
                    VRRigPrefab,
                    transform.position - new Vector3(0f, 0.6f, 0f),
                    transform.rotation
                );

                vrRig.GetComponent<VRCameraSeatFollower>().SetSeat(transform);

                rps.SetRigAnchor(vrRig.GetComponent<RigAnchor>());
                vrRig.GetComponent<Vehicle>().playerInfo = this;
            }
        }

        SpawnModel();
    }

    private void OnDeviceChanged()
    {
        SpawnModel();
    }

    private void SpawnModel()
    {
        if (_modelSpawned || Device == DeviceType.Unknown) return;

        _modelSpawned = true;

        switch (Device)
        {
            case DeviceType.PC:
            {
                GameObject modelPC = Instantiate(prefabPC, transform);

                if (HasInputAuthority)
                {
                    foreach (Renderer renderer in modelPC.GetComponentsInChildren<Renderer>(true))
                        renderer.enabled = false;
                }

                break;
            }

            case DeviceType.VR:
            {
                GameObject modelVR = Instantiate(prefabVR, transform);
                vrModel = modelVR.GetComponent<VRModel>();

                foreach (CustomHandVisual hand in modelVR.GetComponentsInChildren<CustomHandVisual>(true))
                {
                    hand.UpdateVisibilityAutomatically = false;
                    hand.enabled = false;

                    if (hand.Handedness == Oculus.Interaction.Input.Handedness.Left)
                        leftHandVisual = hand;
                    else
                        rightHandVisual = hand;
                }

                if (HasInputAuthority)
                {
                    foreach (Renderer renderer in modelVR.GetComponentsInChildren<Renderer>(true))
                        renderer.enabled = false;
                }

                break;
            }
        }
    }

    public void SetCharacter(PCPlayerCharacter character)
    {
        RPC_SetCharacter(character);
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetCharacter(PCPlayerCharacter character)
    {
        this.character = character;
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetInitial(DeviceType device, string name)
    {
        Device = device;
        playerName = name;

        Debug.Log($"Player {name} [{Object.InputAuthority}]: {Device}");
    }

    public void SendMonitor()
    {
        if (isUsingMonitor)
            MonitorManager.Instance.RPC_SendRelease(this);
        else
            MonitorManager.Instance.RPC_SendRequest(this);
    }

    public void AddMonitor(int amount)
    {
        MonitorManager.Instance.RPC_AddCurrentMonitor(amount, this);
    }

    public void SetIsUsingMonitor(bool b)
    {
        ChangeVisibilityOfVehicle(b);
    }

    public void ChangeVisibilityOfVehicle(bool b)
    {
        if (vrRig == null) return;

        vrRig.GetComponent<Vehicle>().ToggleVisibility(!b);
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority) return;

        if (networkCharacterController != null)
            networkCharacterController.Teleport(position, rotation);
        else
            transform.SetPositionAndRotation(position, rotation);

        RPC_TeleportLocalRig(position, rotation);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_TeleportLocalRig(Vector3 position, Quaternion rotation)
    {
        if (vrRig == null) return;

        vrRig.transform.SetPositionAndRotation(
            position - new Vector3(0f, 0.6f, 0f),
            rotation
        );
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out PlayerInputData input)) return;
        if (!HasStateAuthority && !HasInputAuthority) return;

        switch (Device)
        {
            case DeviceType.PC:
                UpdatePC(input);
                break;

            case DeviceType.VR:
                UpdateVR(input);

                if (HasStateAuthority)
                    UpdateHandTracking(input);

                break;
        }
    }

    private void UpdatePC(PlayerInputData input)
    {
        switch (character)
        {
            case PCPlayerCharacter.Commander:
                if (commander == null) break;
                commander.Drive(input.Move, Runner.DeltaTime);
                if (HasInputAuthority && Runner.IsForward)
                    commander.Look(input.Look);
                break;
            case PCPlayerCharacter.Glitch:
                if (glitch == null) break;
                glitch.Move(input.Move, Runner.DeltaTime);
                if (HasInputAuthority && Runner.IsForward)
                    glitch.Look(input.Look);
                break;
            case PCPlayerCharacter.Freddy:
            case PCPlayerCharacter.Unknown:
            default:
                if (freddy == null) break;
                freddy.Move(input.Move, Runner.DeltaTime);
                if (HasInputAuthority && Runner.IsForward)
                    freddy.Look(input.Look);
                break;
        }        
    }

    private void UpdateVR(PlayerInputData input)
    {
        if (isUsingMonitor) return;

        if (HasStateAuthority)
        {
            HeadPosition = input.headPosition;
            HeadRotation = input.headRotation;

            LeftHandPosition = input.leftHandPosition;
            LeftHandRotation = input.leftHandRotation;

            RightHandPosition = input.rightHandPosition;
            RightHandRotation = input.rightHandRotation;

            UsingMotor = input.usingMotor;
            HandlebarRotation = input.handlebarLocalRotation;
        }


        if (input.usingMotor)
        {
            if (motorcycleController == null) return;
            if (GameManager.Instance.currentBattery > 0)
            {
                 MotorcycleSpeed = motorcycleController.MoveMotorcycle(
                    Runner.DeltaTime,
                    input.handlebarLocalRotation,
                    MotorcycleSpeed
                );

                float motorPowerComsumptionCoef = -.05f;

                if (HasStateAuthority && Mathf.Abs(MotorcycleSpeed) > 0.01f)
                {
                    GameManager.Instance.AddBattery(Mathf.Abs(MotorcycleSpeed) * Runner.DeltaTime * motorPowerComsumptionCoef);
                }
            }
           
        }
        else
        {
            if (wheelchair == null) return;

            wheelchair.MoveWheelchair(input.wheelDistance, Runner.DeltaTime);
        }

        if (HasStateAuthority && !input.usingMotor)
        {
            VRModel wheelSettings = vrModel != null
                ? vrModel
                : prefabVR.GetComponent<VRModel>();

            if (wheelSettings != null)
            {
                WheelAngles += wheelSettings.DistanceToAngles(input.wheelDistance);
            }
        }
    }

    private unsafe void UpdateHandTracking(PlayerInputData input)
    {
        if (!HasStateAuthority) return;

        LeftHandVisible = input.leftHandVisible;
        RightHandVisible = input.rightHandVisible;

        LeftOrientPosition = input.leftOrientPosition;
        RightOrientPosition = input.rightOrientPosition;

        LeftOrientRotation = input.leftOrientRotation;
        RightOrientRotation = input.rightOrientRotation;

        for (int i = 0; i < PlayerInputData.MaxHandBones; i++)
        {
            LeftBones.Set(i, input.leftBones[i]);
            RightBones.Set(i, input.rightBones[i]);
        }
    }

    public override void Render()
    {
        // if (HasInputAuthority || Device != DeviceType.VR) return;

        if (headAnchor != null)
            headAnchor.SetPositionAndRotation(HeadPosition, HeadRotation);

        if (leftHandAnchor != null)
            leftHandAnchor.SetPositionAndRotation(LeftHandPosition, LeftHandRotation);

        if (rightHandAnchor != null)
            rightHandAnchor.SetPositionAndRotation(RightHandPosition, RightHandRotation);

        if (isUsingMonitor) return;

        if (vrModel != null)
        {
            vrModel.SetModel(UsingMotor);

            if (UsingMotor)
                vrModel.UpdateMotor(HandlebarRotation);
            else
                vrModel.UpdateWheelchair(WheelAngles);
        }

        if (leftHandVisual != null)
            HandTrackingSync.Apply(leftHandVisual, LeftBones, LeftOrientPosition, LeftOrientRotation, LeftHandVisible);

        if (rightHandVisual != null)
            HandTrackingSync.Apply(rightHandVisual, RightBones, RightOrientPosition, RightOrientRotation, RightHandVisible);
    }

    public Quaternion GetHeadRotation()
    {
        return HeadRotation;
    }
}