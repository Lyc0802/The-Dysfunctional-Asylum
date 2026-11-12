
using System;
using UnityEngine;
using Oculus.Interaction;

public class MultiAxisRotateTransformer : MonoBehaviour, ITransformer
{
    [Serializable]
    public class AxisSettings
    {
        public bool enabled = false;
        public bool useLocal = true;
        public Transform pivot;
        public float minAngle = -45f;
        public float maxAngle = 45f;

        [NonSerialized] public float angle;
    }

    [Header("Rotation Constraints")]
    [SerializeField] private AxisSettings xAxis = new AxisSettings();
    [SerializeField] private AxisSettings yAxis = new AxisSettings();
    [SerializeField] private AxisSettings zAxis = new AxisSettings();

    private IGrabbable grabbable;
    private Vector3 previousGrabPosition;
    private bool isTransforming;

    public void Initialize(IGrabbable grabbable)
    {
        this.grabbable = grabbable;
    }

    public void BeginTransform()
    {
        if (grabbable.GrabPoints.Count == 0) return;

        xAxis.angle = 0f;
        yAxis.angle = 0f;
        zAxis.angle = 0f;

        previousGrabPosition = grabbable.GrabPoints[0].position;
        isTransforming = true;
    }

    public void UpdateTransform()
    {
        if (!isTransforming || grabbable.GrabPoints.Count == 0) return;

        Vector3 currentGrabPosition = grabbable.GrabPoints[0].position;
        Vector3 previous = previousGrabPosition;

        ApplyAxis(yAxis, Vector3.up, ref previous, currentGrabPosition);
        ApplyAxis(xAxis, Vector3.right, ref previous, currentGrabPosition);
        ApplyAxis(zAxis, Vector3.forward, ref previous, currentGrabPosition);

        previousGrabPosition = currentGrabPosition;
    }

    private void ApplyAxis(AxisSettings settings, Vector3 axis, ref Vector3 previous, Vector3 current)
    {
        if (!settings.enabled) return;

        Transform target = grabbable.Transform;

        Vector3 pivotPosition = settings.pivot != null ? settings.pivot.position : target.position;

        Vector3 rotationAxis = settings.useLocal ? target.rotation * axis : axis;

        Vector3 from = Vector3.ProjectOnPlane(previous - pivotPosition, rotationAxis);
        Vector3 to = Vector3.ProjectOnPlane(current - pivotPosition, rotationAxis);

        if (from.sqrMagnitude < 0.000001f || to.sqrMagnitude < 0.000001f) return;

        float deltaAngle = Vector3.SignedAngle(from, to, rotationAxis);
        float newAngle = Mathf.Clamp(settings.angle + deltaAngle, settings.minAngle, settings.maxAngle);
        float appliedAngle = newAngle - settings.angle;

        settings.angle = newAngle;

        if (Mathf.Abs(appliedAngle) < 0.00001f) return;

        Quaternion deltaRotation = Quaternion.AngleAxis(appliedAngle, rotationAxis);

        target.position = pivotPosition + deltaRotation * (target.position - pivotPosition);
        target.rotation = deltaRotation * target.rotation;

        previous = pivotPosition + deltaRotation * (previous - pivotPosition);
    }

    public void EndTransform()
    {
        isTransforming = false;
    }
}
