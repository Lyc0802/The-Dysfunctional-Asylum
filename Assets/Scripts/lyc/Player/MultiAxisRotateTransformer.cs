
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
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isTransforming;

    public void Initialize(IGrabbable grabbable)
    {
        this.grabbable = grabbable;

        initialPosition = grabbable.Transform.position;
        initialRotation = grabbable.Transform.rotation;

        xAxis.angle = 0f;
        yAxis.angle = 0f;
        zAxis.angle = 0f;
    }

    public void BeginTransform()
    {
        if (grabbable.GrabPoints.Count == 0) return;

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
        Vector3 rotationAxis = settings.useLocal ? target.TransformDirection(axis) : axis;

        Vector3 from = Vector3.ProjectOnPlane(previous - pivotPosition, rotationAxis);
        Vector3 to = Vector3.ProjectOnPlane(current - pivotPosition, rotationAxis);

        if (from.sqrMagnitude < 0.0004f || to.sqrMagnitude < 0.0004f) return;

        float deltaAngle = Vector3.SignedAngle(from, to, rotationAxis);
        float newAngle = Mathf.Clamp(settings.angle + deltaAngle, settings.minAngle, settings.maxAngle);
        float appliedAngle = newAngle - settings.angle;

        settings.angle = newAngle;

        if (Mathf.Abs(appliedAngle) < 0.0001f) return;

        Quaternion deltaRotation = Quaternion.AngleAxis(appliedAngle, rotationAxis);

        target.position = pivotPosition + deltaRotation * (target.position - pivotPosition);
        target.rotation = deltaRotation * target.rotation;

        previous = pivotPosition + deltaRotation * (previous - pivotPosition);
    }

    public void EndTransform()
    {
        isTransforming = false;
    }

    public void ResetRotation()
    {
        if (grabbable == null) return;

        grabbable.Transform.SetPositionAndRotation(initialPosition, initialRotation);

        xAxis.angle = 0f;
        yAxis.angle = 0f;
        zAxis.angle = 0f;

        if (isTransforming && grabbable.GrabPoints.Count > 0)
            previousGrabPosition = grabbable.GrabPoints[0].position;
    }
}
