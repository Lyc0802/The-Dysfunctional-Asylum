using UnityEngine;
using Oculus.Interaction;

// Slide along the handle's initial local Y axis in its parent's coordinate frame.
public class GeneratorLocalSlide : MonoBehaviour, ITransformer
{
    [Tooltip("Travel from the initial position, measured in parent local units.")]
    public float minOffset = -0.5f;
    public float maxOffset = 1.5f;

    private IGrabbable grabbable;
    private Transform railFrame;
    private Vector3 origin;
    private Vector3 axis;
    private Quaternion rotation;
    private float grabStart;
    private float offsetStart;

    public void Initialize(IGrabbable target)
    {
        grabbable = target;
        railFrame = target.Transform.parent;
        origin = target.Transform.localPosition;
        rotation = target.Transform.localRotation;
        axis = (rotation * Vector3.up).normalized;
    }

    private Vector3 ToRailSpace(Vector3 position) =>
        railFrame != null ? railFrame.InverseTransformPoint(position) : position;

    public void BeginTransform()
    {
        if (grabbable.GrabPoints.Count == 0) return;
        grabStart = Vector3.Dot(ToRailSpace(grabbable.GrabPoints[0].position), axis);
        offsetStart = Vector3.Dot(grabbable.Transform.localPosition - origin, axis);
    }

    public void UpdateTransform()
    {
        if (grabbable.GrabPoints.Count == 0) return;
        float handPosition = Vector3.Dot(ToRailSpace(grabbable.GrabPoints[0].position), axis);
        float offset = Mathf.Clamp(offsetStart + handPosition - grabStart,
            Mathf.Min(minOffset, maxOffset), Mathf.Max(minOffset, maxOffset));
        grabbable.Transform.localPosition = origin + axis * offset;
        grabbable.Transform.localRotation = rotation;
    }

    public void EndTransform() { }
}
