using Fusion;
using UnityEngine;

/// <summary>
/// Attach to Rock_Fall_Trigger with its trigger collider and kinematic Rigidbody.
/// Uses the parent trap's NetworkObject. Only the Host decides triggers and falling.
/// </summary>
[DisallowMultipleComponent]
public class NetworkRockFall : NetworkBehaviour
{
    public NetworkRockGrab ropeGrab;
    [Min(0f)] public float secondsPerLiftedMeter = 5f;
    public LayerMask triggerLayers = ~0;
    [Min(0.01f)] public float fallSpeed = 12f;
    [Min(0f)] public float cooldownSeconds = 2f;

    [Networked] public NetworkBool IsArmed { get; private set; }
    [Networked] public NetworkBool IsFalling { get; private set; }
    [Networked] private TickTimer TriggerTimer { get; set; }
    [Networked] private TickTimer CooldownTimer { get; set; }

    private bool ready;
    private bool triggerPending;
    public float RemainingTriggerSeconds => ready && IsArmed ? TriggerTimer.RemainingTime(Runner) ?? 0f : 0f;
    public float RemainingCooldownSeconds => ready ? CooldownTimer.RemainingTime(Runner) ?? 0f : 0f;
    public bool CanPull => ready && isActiveAndEnabled && !IsFalling && CooldownTimer.ExpiredOrNotRunning(Runner);

    public override void Spawned()
    {
        if (ropeGrab == null || ropeGrab.Object != Object)
        {
            Debug.LogError("Assign NetworkRockGrab under the same NetworkObject.", this);
            enabled = false;
            return;
        }
        ready = true;
    }

    // Host-only entry points called by NetworkRockGrab, not by local grab events.
    public void Set_timer()
    {
        if (!ready || !HasStateAuthority || !CanPull || IsArmed) return;
        float height = ropeGrab.LiftedHeight;
        IsArmed = height > 0f;
        TriggerTimer = IsArmed
            ? TickTimer.CreateFromSeconds(Runner, height * Mathf.Max(0f, secondsPerLiftedMeter))
            : TickTimer.None;
    }

    public void Interrupted()
    {
        if (!ready || !HasStateAuthority) return;
        IsArmed = false;
        TriggerTimer = TickTimer.None;
        triggerPending = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ready || !HasStateAuthority || !IsArmed || ropeGrab == null) return;
        if ((triggerLayers.value & (1 << other.gameObject.layer)) == 0) return;
        // Exclude the entire trap, including decorative ropes and pulleys.
        if (other.transform.IsChildOf(Object.transform)) return;
        triggerPending = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!ready || !HasStateAuthority || !ropeGrab.Ready) return;
        if (IsArmed && (triggerPending || TriggerTimer.Expired(Runner)))
        {
            IsArmed = false;
            IsFalling = true;
            TriggerTimer = TickTimer.None;
        }
        triggerPending = false;
        if (!IsFalling) return;

        ropeGrab.MoveRock(-Mathf.Max(0.01f, fallSpeed) * Runner.DeltaTime);
        if (ropeGrab.LiftedHeight <= 0f)
        {
            IsFalling = false;
            CooldownTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0f, cooldownSeconds));
        }
    }

    private void OnDisable()
    {
        triggerPending = false;
        if (ready && Object != null && Object.IsValid && HasStateAuthority) Interrupted();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ready = false;
        triggerPending = false;
    }
}
