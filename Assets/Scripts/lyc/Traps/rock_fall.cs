using UnityEngine;

// Attach this component to the trigger object, not necessarily the rock.
public class rock_fall : MonoBehaviour
{
    [Header("References")]
    public Rope_Grab ropeGrab;

    [Header("Trigger window")]
    [Min(0f), Tooltip("X seconds = lifted height in metres * this value.")]
    public float secondsPerLiftedMeter = 5f;
    [Tooltip("Only colliders on these layers can trigger the rock.")]
    public LayerMask triggerLayers = ~0;

    [Header("Fall")]
    [Min(0.01f)] public float fallSpeed = 12f;

    [Min(0f), Tooltip("Seconds after landing before the rock can be pulled again.")]
    public float cooldownSeconds = 2f;

    public float RemainingTriggerSeconds => armed ? Mathf.Max(0f, deadline - Time.time) : 0f;
    public bool IsFalling => falling;
    public float RemainingCooldownSeconds => Mathf.Max(0f, cooldownUntil - Time.time);
    public bool CanPull => !falling && RemainingCooldownSeconds <= 0f;

    private float deadline;
    private bool armed;
    private bool falling;
    private float cooldownUntil;

    private void Awake()
    {
        if (ropeGrab == null)
        {
            Debug.LogError("Assign Rope Grab on the rock trigger.", this);
            enabled = false;
            return;
        }
        ropeGrab.fallTrigger = this;
    }

    // Called once when the rope is released. Releasing twice must not extend the window.
    public void Set_timer()
    {
        if (!enabled || ropeGrab == null || !CanPull) return;
        float height = ropeGrab.LiftedHeight;
        float duration = height * Mathf.Max(0f, secondsPerLiftedMeter);
        armed = height > 0f;
        deadline = Time.time + duration;
    }

    public void Interrupted()
    {
        armed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!armed || Time.time > deadline || ropeGrab == null) return;
        if ((triggerLayers.value & (1 << other.gameObject.layer)) == 0) return;
        // The trap's own rock and rope must not activate it.
        if (other.transform.IsChildOf(ropeGrab.Rock.transform) ||
            other.transform.IsChildOf(ropeGrab.rock_rope.transform) ||
            other.transform.IsChildOf(ropeGrab.This_rope.transform)) return;

        armed = false;
        falling = true;
    }

    public bool Falling()
    {
        if (ropeGrab == null) return false;
        ropeGrab.MoveRock(-Mathf.Max(0.01f, fallSpeed) * Time.deltaTime);
        return ropeGrab.LiftedHeight > 0f;
    }

    private void Update()
    {
        if (armed && Time.time >= deadline)
        {
            armed = false;
            falling = true;
        }

        if (falling && !Falling())
        {
            falling = false;
            cooldownUntil = Time.time + Mathf.Max(0f, cooldownSeconds);
        }
    }

    private void OnDisable()
    {
        Interrupted();
    }
}
