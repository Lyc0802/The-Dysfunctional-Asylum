
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
    private Quaternion initialLocalRotation;
    private bool isTransforming;

    public void Initialize(IGrabbable grabbable)
    {
        this.grabbable = grabbable;
        initialLocalRotation = grabbable.Transform.localRotation;

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

        Transform target = grabbable.Transform;
        Vector3 currentGrabPosition = grabbable.GrabPoints[0].position;
        Quaternion previousRotation = target.rotation;
        Vector3 previousPosition = target.position;

        UpdateAxisAngle(yAxis, Vector3.up, currentGrabPosition);
        UpdateAxisAngle(xAxis, Vector3.right, currentGrabPosition);
        UpdateAxisAngle(zAxis, Vector3.forward, currentGrabPosition);

        Quaternion localRotation = initialLocalRotation;
        if (yAxis.enabled) localRotation *= Quaternion.AngleAxis(yAxis.angle, Vector3.up);
        if (xAxis.enabled) localRotation *= Quaternion.AngleAxis(xAxis.angle, Vector3.right);
        if (zAxis.enabled) localRotation *= Quaternion.AngleAxis(zAxis.angle, Vector3.forward);

        target.localRotation = localRotation;

        ApplyPivotPosition(yAxis, Vector3.up, previousRotation, previousPosition);
        ApplyPivotPosition(xAxis, Vector3.right, previousRotation, previousPosition);
        ApplyPivotPosition(zAxis, Vector3.forward, previousRotation, previousPosition);

        previousGrabPosition = currentGrabPosition;
    }

    private void UpdateAxisAngle(AxisSettings settings, Vector3 axis, Vector3 currentGrabPosition)
    {
        if (!settings.enabled) return;

        Transform target = grabbable.Transform;
        Vector3 pivotPosition = settings.pivot != null ? settings.pivot.position : target.position;
        Vector3 rotationAxis = target.TransformDirection(axis);

        Vector3 from = Vector3.ProjectOnPlane(previousGrabPosition - pivotPosition, rotationAxis);
        Vector3 to = Vector3.ProjectOnPlane(currentGrabPosition - pivotPosition, rotationAxis);

        if (from.sqrMagnitude < 0.0004f || to.sqrMagnitude < 0.0004f) return;

        float deltaAngle = Vector3.SignedAngle(from, to, rotationAxis);
        settings.angle = Mathf.Clamp(settings.angle + deltaAngle, settings.minAngle, settings.maxAngle);
    }

    private void ApplyPivotPosition(AxisSettings settings, Vector3 axis, Quaternion previousRotation, Vector3 previousPosition)
    {
        if (!settings.enabled || settings.pivot == null) return;

        Transform target = grabbable.Transform;
        Vector3 pivotPosition = settings.pivot.position;
        Vector3 rotationAxis = previousRotation * axis;

        Quaternion deltaRotation = target.rotation * Quaternion.Inverse(previousRotation);
        target.position = pivotPosition + deltaRotation * (previousPosition - pivotPosition);
    }

    public void EndTransform()
    {
        isTransforming = false;
    }
}
