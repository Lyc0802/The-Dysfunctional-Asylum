using Fusion;
using UnityEngine;
using Oculus.Interaction.Locomotion;
using Bombanana.Interaction;

public class PlayerInfo : NetworkBehaviour
{ 
    [Networked] public string playerName { get; set; }
    [Networked, OnChangedRender(nameof(OnDeviceChanged))] public DeviceType Device { get; set; }
    
    private RunnerPeerSettings rps;
    private bool _modelSpawned;
    // public Transform playerPC;
    [Header("PC Setting")]
    public GameObject prefabPC;
    public Commander commander;
    [SerializeField] private float lookSensitivity = 0.15f;


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

    

    
    public override void Spawned()
    {
        if (HasInputAuthority) {
            // RPC_SetInitial(NetworkManager.Instance.LocalDeviceType, NetworkManager.Instance.playerName);
            rps = Runner.GetComponent<RunnerPeerSettings>();
            RPC_SetInitial(rps.Device, rps.PlayerName);
            if (rps.Device == DeviceType.PC )
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform, false);
                cameraObj.transform.localPosition = Vector3.zero;
                cameraObj.transform.localRotation = Quaternion.identity;

                cameraObj.tag = "MainCamera";

                Camera playerCamera = cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();
                if (commander!=null)
                {
                    commander.cameraPivot = cameraObj.transform;
                }
            }else if (rps.Device == DeviceType.VR)
            {
                vrRig = Instantiate(VRRigPrefab, transform.position - new Vector3(0, 0.6f, 0), transform.rotation);
                // vrRig = Instantiate(VRRigPrefab, transform);

                vrRig.GetComponent<VRCameraSeatFollower>().SetSeat(transform);
                Runner.GetComponent<RunnerPeerSettings>().SetRigAnchor(vrRig.GetComponent<RigAnchor>());
                vrRig.GetComponent<Vehicle>().playerInfo = this;
            }
            
            // Set Camera and Link VR Head and other to RunnerPeerSettings
        }

        SpawnModel();
    }

    private void OnDeviceChanged()
    {
        SpawnModel();
    }

    void SpawnModel()
    {
        if (_modelSpawned || Device == DeviceType.Unknown) return;

        _modelSpawned = true;

        switch (Device)
        {
            case DeviceType.PC:
                GameObject modelPC = Instantiate(prefabPC, transform);
                // Destroy(GetComponent<Wheelchair>());
                // Destroy(GetComponent<MotorcycleController>());
                if (HasInputAuthority)
                {
                    foreach (Renderer renderer in modelPC.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.enabled = false;
                    }
                }
                break;
            case DeviceType.VR:
                
                GameObject modelVR = Instantiate(prefabVR, transform);
                vrModel=modelVR.GetComponent<VRModel>();
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
                    {
                        renderer.enabled = false;
                    }
                }
            break;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetInitial(DeviceType device, string playerName)
    {
        Device = device;
        this.playerName = playerName;
        Debug.Log($"Player {playerName} [{Object.InputAuthority}]: {Device}");
    }

    public void SendMonitor()
    {
        // Debug.Log("Monitor: is Send");

        if (isUsingMonitor)
        {
            MonitorManager.Instance.RPC_SendRelease(this);
        }
        else
        {
            MonitorManager.Instance.RPC_SendRequest(this);
        }
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
        vrRig.GetComponent<Vehicle>().ToggleVisibility(!b);
    }

        
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority) return;

        NetworkTransform nt = GetComponent<NetworkTransform>();

        if (nt != null)
            nt.Teleport(position, rotation);
        else
            transform.SetPositionAndRotation(position, rotation);

        RPC_TeleportLocalRig(position, rotation);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_TeleportLocalRig(Vector3 position, Quaternion rotation)
    {
        if (Device != DeviceType.VR || vrRig == null) return;

        vrRig.transform.SetPositionAndRotation(
            position - new Vector3(0, 0.6f, 0),
            rotation
        );
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
                UpdateHandTracking(input);
                break;
        }
    }

    private void UpdatePC(PlayerInputData input)
    {
        // transform.Rotate(Vector3.up, input.Look.x * lookSensitivity);

        // Vector2 move = input.Move;
        // Vector3 dir = move.y * transform.forward + move.x * transform.right;
        // dir.y = 0f;
        // dir.Normalize();
        // transform.position += dir * 5f * Runner.DeltaTime;
        if (commander != null)
        {
            commander.Drive(input.Move, Runner.DeltaTime);
            commander.Look(input.Look);
        }
    }

    private void UpdateVR(PlayerInputData input)
    {
        if (!HasStateAuthority) return;

        HeadPosition = input.headPosition;
        HeadRotation = input.headRotation;

        LeftHandPosition = input.leftHandPosition;
        LeftHandRotation = input.leftHandRotation;

        RightHandPosition = input.rightHandPosition;
        RightHandRotation = input.rightHandRotation;
        if (isUsingMonitor) return;

        UsingMotor = input.usingMotor;

        if (input.usingMotor)
        {
            HandlebarRotation = input.handlebarLocalRotation;
            motorcycleController.MoveMotorcycle(Runner.DeltaTime, input.handlebarLocalRotation);
        }
        else
        {
            VRModel wheelSettings = vrModel != null ? vrModel : prefabVR.GetComponent<VRModel>();

            WheelAngles += wheelSettings.DistanceToAngles(input.wheelDistance);
            wheelchair.MoveWheelchair(input.wheelDistance);
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
        if (HasInputAuthority) return;

        // Head
        headAnchor.SetPositionAndRotation(HeadPosition, HeadRotation);

        // Hands
        leftHandAnchor.SetPositionAndRotation(LeftHandPosition, LeftHandRotation);
        rightHandAnchor.SetPositionAndRotation(RightHandPosition, RightHandRotation);

        // Finger Bones
        if (!isUsingMonitor)
        {
            // model update !!!

            // vrModel.UpdateWheelchair(input.wheelDistance);
            // vrModel.UpdateMotor(input.handlebarLocalRotation);
            if (vrModel != null)
            {
                vrModel.SetModel(UsingMotor);

                if (UsingMotor)
                    vrModel.UpdateMotor(HandlebarRotation);
                else
                    vrModel.UpdateWheelchair(WheelAngles);
            }

            HandTrackingSync.Apply(leftHandVisual, LeftBones, LeftOrientPosition, LeftOrientRotation, LeftHandVisible);
            HandTrackingSync.Apply(rightHandVisual, RightBones, RightOrientPosition, RightOrientRotation, RightHandVisible);
        }
       
        
        // HandTrackingSync.Apply(leftHandVisual, LeftBones, leftHandAnchor.position, leftHandAnchor.rotation, true);
        // HandTrackingSync.Apply(rightHandVisual, RightBones, rightHandAnchor.position, rightHandAnchor.rotation, true);
    }

    public Quaternion GetHeadRotation() {return HeadRotation;}
}
