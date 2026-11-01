
using UnityEngine;
using Fusion;

public class MonitorManager : NetworkBehaviour
{
    public static MonitorManager Instance;

    public Monitor[] monitors;

    [Header("Monitor Damage")]
    [SerializeField] private float defaultBreakDuration = 10f;

    [Networked, OnChangedRender(nameof(OnCurrentMonitorChanged))]
    public int currentMonitor { get; set; }

    [Networked]
    public PlayerInfo currentUser { get; set; }

    [Networked, Capacity(32)]
    public NetworkArray<TickTimer> brokenTimers => default;

    private int lastEffectMonitor = -1;
    private bool lastEffectBroken;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            if (HasStateAuthority) Runner.Despawn(Object);
            return;
        }

        Instance = this;

        if (HasStateAuthority) currentMonitor = 0;
        lastEffectMonitor = -1;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private bool IsValidMonitor(int index)
    {
        return monitors != null && index >= 0 && index < monitors.Length && index < brokenTimers.Length && monitors[index] != null;
    }

    private bool IsValidPlayer(PlayerInfo player, PlayerRef source)
    {
        return player != null && player.Object != null && player.Object.InputAuthority == source && source != PlayerRef.None;
    }

    private bool CanControlMonitor(PlayerInfo player, PlayerRef source)
    {
        return IsValidPlayer(player, source) && currentUser == player;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendRequest(PlayerInfo player, RpcInfo info = default)
    {
        if (!IsValidPlayer(player, info.Source)) return;

        bool isApproved = currentUser == null && IsValidMonitor(currentMonitor);

        if (isApproved)
        {
            currentUser = player;
            player.isUsingMonitor = true;
        }

        RPC_SendRespond(player.Object.InputAuthority, isApproved, true, player);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendRelease(PlayerInfo player, RpcInfo info = default)
    {
        if (!IsValidPlayer(player, info.Source)) return;

        bool isApproved = currentUser == player;

        if (isApproved)
        {
            player.isUsingMonitor = false;
            currentUser = null;
        }

        RPC_SendRespond(player.Object.InputAuthority, isApproved, false, player);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SendRespond([RpcTarget] PlayerRef target, bool isApproved, bool isGetIn, PlayerInfo player)
    {
        if (!isApproved || player == null || player.Object == null || !player.Object.HasInputAuthority || player.vrRig == null) return;

        var follower = player.vrRig.GetComponent<VRCameraSeatFollower>();
        var effect = player.vrRig.GetComponent<MonitorEffectController>();

        if (isGetIn)
        {
            if (IsValidMonitor(currentMonitor))
            {
                Vector3 v = monitors[currentMonitor].transform.position;
                v.y -= 1.2f;
                player.vrRig.transform.position = v;
            }

            if (follower != null) follower.isUsingMonitor = true;

            if (effect != null)
            {
                effect.UsingMonitor(true);
                effect.SetBroken(IsMonitorBroken(currentMonitor));
            }

            player.SetIsUsingMonitor(true);
            lastEffectMonitor = currentMonitor;
            lastEffectBroken = IsMonitorBroken(currentMonitor);
        }
        else
        {
            player.vrRig.transform.localPosition = new Vector3(0f, -0.6f, 0f);

            if (follower != null) follower.isUsingMonitor = false;
            if (effect != null) effect.UsingMonitor(false);

            player.SetIsUsingMonitor(false);
            lastEffectMonitor = -1;
            lastEffectBroken = false;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SetCurrentMonitor(int index, PlayerInfo player, RpcInfo info = default)
    {
        if (!CanControlMonitor(player, info.Source) || !IsValidMonitor(index)) return;
        currentMonitor = index;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_AddCurrentMonitor(int amount, PlayerInfo player, RpcInfo info = default)
    {
        if (!CanControlMonitor(player, info.Source) || monitors == null || monitors.Length == 0) return;

        int next = ((currentMonitor + amount) % monitors.Length + monitors.Length) % monitors.Length;
        if (!IsValidMonitor(next)) return;

        currentMonitor = next;
    }

    public void BreakMonitor(int index, float seconds)
    {
        if (!HasStateAuthority || !IsValidMonitor(index) || seconds <= 0f) return;
        brokenTimers.Set(index, TickTimer.CreateFromSeconds(Runner, seconds));
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_RequestBreakMonitor(int index, PlayerInfo player, RpcInfo info = default)
    {
        if (!IsValidPlayer(player, info.Source) || !IsValidMonitor(index)) return;
        if (player.Device != DeviceType.VR) return;

        BreakMonitor(index, defaultBreakDuration);
    }

    public void RepairMonitor(int index)
    {
        if (!HasStateAuthority || !IsValidMonitor(index)) return;
        brokenTimers.Set(index, TickTimer.None);
    }

    public bool IsMonitorBroken(int index)
    {
        if (Runner == null || !IsValidMonitor(index)) return false;
        return !brokenTimers[index].ExpiredOrNotRunning(Runner);
    }

    public float GetMonitorRemainingTime(int index)
    {
        if (!IsMonitorBroken(index)) return 0f;
        return brokenTimers[index].RemainingTime(Runner) ?? 0f;
    }

    private void OnCurrentMonitorChanged()
    {
        UpdateLocalMonitor(true);
    }

    private void UpdateLocalMonitor(bool force = false)
    {
        if (currentUser == null || currentUser.Object == null || !currentUser.Object.HasInputAuthority) return;
        if (!IsValidMonitor(currentMonitor) || currentUser.vrRig == null) return;

        bool broken = IsMonitorBroken(currentMonitor);
        bool monitorChanged = lastEffectMonitor != currentMonitor;

        if (!force && !monitorChanged && lastEffectBroken == broken) return;

        if (monitorChanged || force)
        {
            Vector3 v = monitors[currentMonitor].transform.position;
            v.y -= 1.2f;
            currentUser.vrRig.transform.position = v;
        }

        var effect = currentUser.vrRig.GetComponent<MonitorEffectController>();
        if (effect != null) effect.SetBroken(broken);

        lastEffectMonitor = currentMonitor;
        lastEffectBroken = broken;
    }

    public bool isUsed()
    {
        return currentUser != null;
    }

    public override void Render()
    {
        if (currentUser == null || !IsValidMonitor(currentMonitor)) return;

        monitors[currentMonitor].SetRotation(currentUser.GetHeadRotation());
        UpdateLocalMonitor();
    }
}
