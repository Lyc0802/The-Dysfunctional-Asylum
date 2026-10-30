using UnityEngine;
using Oculus.Interaction;

// Align before the free transformer records its grab offset, so the next frame
// preserves the alignment and the player can still aim freely with their hand.
public class FlashlightGrabAlignment : MonoBehaviour, ITransformer, IMovementProvider
{
    [SerializeField] private Transform playerView;
    [Tooltip("The spotlight transform; its forward axis is the beam direction.")]
    [SerializeField] private Transform beam;
    [SerializeField] private bool alignOnGrab = true;

    private IGrabbable grabbable;
    private GrabFreeTransformer freeTransformer;
    private MoveTowardsTargetProvider pullProvider;

    // Distance grab normally interpolates from the object's rotation to the
    // controller rotation. Preserve the starting rotation and apply only actual
    // hand rotation deltas, so that interpolation cannot undo beam alignment.
    public IMovement CreateMovement()
    {
        if (pullProvider == null)
            pullProvider = gameObject.AddComponent<MoveTowardsTargetProvider>();
        return new AlignedPullMovement(pullProvider.CreateMovement());
    }

    private sealed class AlignedPullMovement : IMovement
    {
        private readonly IMovement movement;
        private Quaternion sourceRotation;
        private Quaternion targetStartRotation;
        private Quaternion rotation;

        public AlignedPullMovement(IMovement movement) => this.movement = movement;
        public Pose Pose => new Pose(movement.Pose.position, rotation);
        public bool Stopped => movement.Stopped;

        public void StopAndSetPose(Pose pose)
        {
            sourceRotation = rotation = pose.rotation;
            movement.StopAndSetPose(pose);
        }

        public void MoveTo(Pose target)
        {
            targetStartRotation = target.rotation;
            movement.MoveTo(new Pose(target.position, sourceRotation));
        }

        public void UpdateTarget(Pose target)
        {
            rotation = target.rotation * Quaternion.Inverse(targetStartRotation) * sourceRotation;
            movement.UpdateTarget(new Pose(target.position, rotation));
        }

        public void Tick() => movement.Tick();
    }

    public void Initialize(IGrabbable target)
    {
        grabbable = target;
        if (freeTransformer == null)
            freeTransformer = gameObject.AddComponent<GrabFreeTransformer>();
        freeTransformer.Initialize(target);
    }

    public void BeginTransform()
    {
        if (alignOnGrab && beam != null)
        {
            Transform view = playerView;
            if (view == null && Camera.main != null) view = Camera.main.transform;
            if (view != null)
            {
                Quaternion desiredBeamRotation = Quaternion.LookRotation(view.forward, view.up);
                Quaternion correction = desiredBeamRotation * Quaternion.Inverse(beam.rotation);
                // Respect the actual beam's local rotation rather than assuming model +Z.
                grabbable.Transform.rotation = correction * grabbable.Transform.rotation;
            }
        }
        freeTransformer.BeginTransform();
    }

    public void UpdateTransform() => freeTransformer.UpdateTransform();
    public void EndTransform() => freeTransformer.EndTransform();
}
