using Fusion;
using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Host/Client version of Rope_Grab. Use with GrabSync and NetworkRockFall under
/// the SAME NetworkObject. Observes Grabbable automatically; no grab UnityEvents needed.
/// Remove/disable the original Rope_Grab and its event bindings when installing.
/// </summary>
[DisallowMultipleComponent]
public class NetworkRockGrab : NetworkBehaviour
{
    public GameObject rock_rope;
    public GameObject Rock;
    public GameObject This_rope;
    public GrabSync grabSync;
    public Grabbable grabbable;
    public NetworkRockFall fallTrigger;
    [Min(0f)] public float maxLiftHeight = 2f;
    [Min(0.01f)] public float maxPullTravel = 1f;
    [Range(5f, 60f)] public float updatesPerSecond = 20f;

    [Networked] public float LiftedHeight { get; private set; }
    [Networked] public float PullOffset { get; private set; }
    [Networked] public PlayerRef Puller { get; private set; }
    [Networked] private int PullSession { get; set; }
    [Networked] private float LastTravel { get; set; }
    public bool IsPulling => Ready && Puller != PlayerRef.None;
    public bool Ready { get; private set; }

    private Vector3 initialRockPosition;
    private Vector3 initialRopePosition;
    private Vector3 initialRopeScale;
    private Vector3 initialHandlePosition;
    private float unitsPerScale;
    private float maximumHeight;
    private bool localPulling;
    private bool blockedUntilRelease;
    private int localSession;
    private float lastOffset;
    private float totalTravel;
    private float nextSendTime;

    private void Awake()
    {
        if (grabSync == null) grabSync = GetComponent<GrabSync>();
        if (This_rope != null && grabbable == null) grabbable = This_rope.GetComponent<Grabbable>();
        if (Rock == null || rock_rope == null || This_rope == null ||
            grabbable == null || grabSync == null || fallTrigger == null)
        {
            Debug.LogError("Assign rock, ropes, Grabbable, GrabSync and NetworkRockFall.", this);
            enabled = false;
            return;
        }
        initialRockPosition = Rock.transform.position;
        initialRopePosition = rock_rope.transform.position;
        initialRopeScale = rock_rope.transform.localScale;
        initialHandlePosition = This_rope.transform.position;
        unitsPerScale = rock_rope.transform.TransformVector(Vector3.up).magnitude /
            Mathf.Max(Mathf.Abs(initialRopeScale.y), 0.0001f);
        unitsPerScale = Mathf.Max(unitsPerScale, 0.0001f);
        maximumHeight = Mathf.Min(Mathf.Max(0f, maxLiftHeight),
            Mathf.Max(0f, initialRopeScale.y * 2f * unitsPerScale - 0.01f));
    }

    public override void Spawned()
    {
        if (!enabled) return;
        if (grabSync.Object != Object || fallTrigger.Object != Object)
        {
            Debug.LogError("All rock trap network scripts must share one NetworkObject.", this);
            enabled = false;
            return;
        }
        Ready = true;
        ApplyRockPose();
    }

    private float ReadOffset() => Mathf.Clamp(
        Vector3.Dot(This_rope.transform.position - initialHandlePosition, Vector3.down),
        0f, Mathf.Max(0.01f, maxPullTravel));

    private void LateUpdate()
    {
        if (!Ready) return;
        bool selected = grabbable.GrabPoints.Count > 0;
        if (!selected)
        {
            FinishLocalPull();
            blockedUntilRelease = false;
            This_rope.transform.position = initialHandlePosition + Vector3.down * PullOffset;
            return;
        }
        if (!fallTrigger.CanPull)
        {
            FinishLocalPull();
            blockedUntilRelease = true;
            return;
        }
        if (!grabSync.CanApplyGrab || blockedUntilRelease)
        {
            FinishLocalPull();
            return;
        }
        if (!localPulling)
        {
            localPulling = true;
            unchecked { ++localSession; }
            lastOffset = ReadOffset();
            totalTravel = 0f;
            nextSendTime = Time.unscaledTime;
            RPC_BeginPull(localSession, lastOffset);
        }
        float offset = ReadOffset();
        totalTravel += offset - lastOffset;
        lastOffset = offset;
        if (Time.unscaledTime >= nextSendTime)
        {
            nextSendTime = Time.unscaledTime + 1f / Mathf.Clamp(updatesPerSecond, 5f, 60f);
            RPC_Pull(localSession, totalTravel, offset);
        }
    }

    private void FinishLocalPull()
    {
        if (!localPulling) return;
        localPulling = false;
        if (Ready && Object != null && Object.IsValid && Runner.IsRunning)
            RPC_EndPull(localSession, totalTravel);
    }

    private bool CanControl(PlayerRef source)
    {
        if (!Ready || !isActiveAndEnabled || !grabSync.isActiveAndEnabled ||
            source == PlayerRef.None || grabSync.CurrentGrabber != source ||
            !Runner.TryGetPlayerObject(source, out var player) || player == null) return false;
        var info = player.GetComponent<PlayerInfo>();
        return info != null && info.Device == DeviceType.VR && player.InputAuthority == source;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_BeginPull(int session, float offset, RpcInfo info = default)
    {
        if (!CanControl(info.Source) || !fallTrigger.CanPull || !Finite(offset)) return;
        if (Puller == info.Source && PullSession == session) return;
        Puller = info.Source;
        PullSession = session;
        LastTravel = 0f;
        PullOffset = Mathf.Clamp(offset, 0f, maxPullTravel);
        fallTrigger.Interrupted();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_Pull(int session, float travel, float offset, RpcInfo info = default)
    {
        if (!CanControl(info.Source) || Puller != info.Source || PullSession != session ||
            !fallTrigger.CanPull || !Finite(travel) || !Finite(offset)) return;
        ApplyTravel(travel);
        PullOffset = Mathf.Clamp(offset, 0f, maxPullTravel);
    }

    private void ApplyTravel(float travel)
    {
        // Each stroke is bounded by the configured grab travel. Absolute samples avoid
        // duplicating movement and preserve accumulated travel between network sends.
        float bounded = Mathf.Clamp(travel, -maxPullTravel, maxPullTravel);
        MoveRock((bounded - LastTravel) / 2f);
        LastTravel = bounded;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_EndPull(int session, float travel, RpcInfo info = default)
    {
        // Release is valid even if GrabSync's release arrived first.
        if (!Ready || Puller != info.Source || info.Source == PlayerRef.None ||
            PullSession != session || !Finite(travel)) return;
        if (fallTrigger.CanPull) ApplyTravel(travel);
        EndAuthorityPull();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Ready || !HasStateAuthority) return;
        if (IsPulling && !CanControl(Puller)) EndAuthorityPull();
        ApplyRockPose();
    }

    private void EndAuthorityPull()
    {
        if (!IsPulling) return;
        Puller = PlayerRef.None;
        PullOffset = 0f;
        LastTravel = 0f;
        fallTrigger.Set_timer();
    }

    public void MoveRock(float distance)
    {
        if (!Ready || !HasStateAuthority || !Finite(distance)) return;
        LiftedHeight = Mathf.Clamp(LiftedHeight + distance, 0f, maximumHeight);
        ApplyRockPose();
    }

    private void ApplyRockPose()
    {
        Rock.transform.position = initialRockPosition + Vector3.up * LiftedHeight;
        rock_rope.transform.position = initialRopePosition + Vector3.up * (LiftedHeight / 2f);
        Vector3 scale = initialRopeScale;
        scale.y -= LiftedHeight / (2f * unitsPerScale);
        rock_rope.transform.localScale = scale;
    }

    public override void Render()
    {
        if (Ready) ApplyRockPose();
    }

    private void OnDisable()
    {
        FinishLocalPull();
        if (Ready && Object != null && Object.IsValid && HasStateAuthority) EndAuthorityPull();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Ready = false;
        localPulling = false;
        blockedUntilRelease = false;
    }
}
