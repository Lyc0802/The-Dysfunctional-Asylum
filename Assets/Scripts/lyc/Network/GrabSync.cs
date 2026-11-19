using System.Collections.Generic;
using Fusion;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using Behaviour = UnityEngine.Behaviour;

/// <summary>
/// Host/Client grab reservation. Add under a scene/spawned NetworkObject and assign
/// ONE local Grabbable plus its controller/hand interactables. Empty arrays are
/// discovered below that Grabbable. No UnityEvent wiring is required.
/// Only synchronizes permission, not transforms, rope movement or trap state.
/// Gameplay code must check CanApplyGrab before applying local grab input.
/// </summary>
[DisallowMultipleComponent]
public class GrabSync : NetworkBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private GrabInteractable[] controllerGrabs;
    [SerializeField] private HandGrabInteractable[] handGrabs;

    [Networked] public PlayerRef CurrentGrabber { get; private set; }
    [Networked] private int GrabRequestId { get; set; }

    public bool IsOccupied => ready && CurrentGrabber != PlayerRef.None;
    public bool CanApplyGrab => ready && isActiveAndEnabled && localRequestActive &&
        approvedRequestId == localRequestId && CurrentGrabber == Runner.LocalPlayer &&
        GrabRequestId == localRequestId;

    private readonly List<Behaviour> interactables = new List<Behaviour>();
    // Restore only components disabled by this script, preserving initially disabled ones.
    private readonly HashSet<Behaviour> disabledBySync = new HashSet<Behaviour>();
    private bool ready;
    private bool localRequestActive;
    private int localRequestId;
    private int approvedRequestId;
    private int rejectedFrame = -1;

    private void Awake()
    {
        if (grabbable == null) grabbable = GetComponentInChildren<Grabbable>(true);
        if (grabbable == null)
        {
            Debug.LogError("GrabSync requires one assigned Grabbable.", this);
            enabled = false;
            return;
        }

        if (controllerGrabs == null || controllerGrabs.Length == 0)
            controllerGrabs = grabbable.GetComponentsInChildren<GrabInteractable>(true);
        if (handGrabs == null || handGrabs.Length == 0)
            handGrabs = grabbable.GetComponentsInChildren<HandGrabInteractable>(true);

        foreach (var grab in controllerGrabs) AddInteractable(grab);
        foreach (var grab in handGrabs) AddInteractable(grab);
        if (interactables.Count == 0)
        {
            Debug.LogError("Assign the controller/hand interactables belonging to this Grabbable.", this);
            enabled = false;
            return;
        }
        SetLocalAvailability(false);
    }

    private void AddInteractable(Behaviour interactable)
    {
        if (interactable != null && !interactables.Contains(interactable))
            interactables.Add(interactable);
    }

    public override void Spawned()
    {
        ready = true;
        localRequestActive = false;
        approvedRequestId = 0;
        UpdateAvailability();
    }

    private bool IsVRPlayer(PlayerRef player)
    {
        if (player == PlayerRef.None || !Runner.IsPlayerValid(player) ||
            !Runner.TryGetPlayerObject(player, out NetworkObject playerObject) || playerObject == null)
            return false;

        var info = playerObject.GetComponent<PlayerInfo>();
        return info != null && info.Object != null && info.Object.IsValid &&
            info.Object.InputAuthority == player && info.Device == DeviceType.VR;
    }

    private void LateUpdate()
    {
        if (!ready || grabbable == null) return;
        UpdateAvailability();

        // Count all grab points: releasing one hand must not unlock the other hand.
        bool selected = grabbable.GrabPoints.Count > 0;
        if (!selected)
        {
            ReleaseLocalRequest();
            return;
        }

        if (!localRequestActive && IsVRPlayer(Runner.LocalPlayer) &&
            (CurrentGrabber == PlayerRef.None || CurrentGrabber == Runner.LocalPlayer) &&
            Time.frameCount > rejectedFrame)
        {
            localRequestActive = true;
            approvedRequestId = 0;
            unchecked { ++localRequestId; }
            if (localRequestId == 0) ++localRequestId;
            RPC_RequestGrab(localRequestId);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || CurrentGrabber == PlayerRef.None) return;
        // Also releases reservations when the player object disappears or changes device.
        if (!IsVRPlayer(CurrentGrabber)) ClearReservation();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_RequestGrab(int requestId, RpcInfo info = default)
    {
        bool accepted = isActiveAndEnabled && requestId != 0 && IsVRPlayer(info.Source) &&
            (CurrentGrabber == PlayerRef.None || CurrentGrabber == info.Source);
        if (accepted)
        {
            CurrentGrabber = info.Source;
            GrabRequestId = requestId;
        }
        if (info.Source != PlayerRef.None)
            RPC_GrabResult(info.Source, requestId, accepted);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GrabResult([RpcTarget] PlayerRef target, int requestId, bool accepted)
    {
        if (!ready || !localRequestActive || requestId != localRequestId) return;
        if (accepted)
        {
            approvedRequestId = requestId;
            return;
        }

        ReleaseLocalRequest();
        rejectedFrame = Time.frameCount;
        // Disabling the SDK interactables cancels existing selections through its lifecycle.
        // Do not disable the Grabbable, colliders, visuals or the NetworkObject.
        SetLocalAvailability(false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_ReleaseGrab(int requestId, RpcInfo info = default)
    {
        // A delayed release from an older grab cannot release a newer reservation.
        if (CurrentGrabber == info.Source && GrabRequestId == requestId)
            ClearReservation();
    }

    private void ClearReservation()
    {
        CurrentGrabber = PlayerRef.None;
        GrabRequestId = 0;
    }

    private void ReleaseLocalRequest()
    {
        if (!localRequestActive) return;
        localRequestActive = false;
        approvedRequestId = 0;
        if (ready && Object != null && Object.IsValid && Runner != null && Runner.IsRunning)
            RPC_ReleaseGrab(localRequestId);
    }

    private void UpdateAvailability()
    {
        bool allowed = ready && isActiveAndEnabled && IsVRPlayer(Runner.LocalPlayer) &&
            (CurrentGrabber == PlayerRef.None || CurrentGrabber == Runner.LocalPlayer) &&
            Time.frameCount > rejectedFrame;
        SetLocalAvailability(allowed);
        if (!allowed) ReleaseLocalRequest();
    }

    private void SetLocalAvailability(bool allowed)
    {
        foreach (var interactable in interactables)
        {
            if (interactable == null) continue;
            if (!allowed && interactable.enabled)
            {
                disabledBySync.Add(interactable);
                interactable.enabled = false;
            }
            else if (allowed && disabledBySync.Remove(interactable))
            {
                interactable.enabled = true;
            }
        }
    }

    private void OnDisable()
    {
        ReleaseLocalRequest();
        if (ready && Object != null && Object.IsValid && HasStateAuthority)
            ClearReservation();
        // Fail closed while synchronization is disabled; OnEnable/LateUpdate reopens it.
        SetLocalAvailability(false);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ready = false;
        localRequestActive = false;
        approvedRequestId = 0;
        SetLocalAvailability(false);
    }
}
