using UnityEngine;

public class WheelchairWheel : MonoBehaviour
{
    public enum Axis
    {
        X,
        Y,
        Z
    }

    [Header("Collider Settings")]
    [SerializeField] private Axis axis = Axis.Z;
    [SerializeField] private int count = 12;
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private float thickness = 0.08f;

    [Header("Wheel Movement")]
    [SerializeField] private bool invertDirection = false;

    private Quaternion _previousRotation;

    private void Awake()
    {
        _previousRotation = transform.localRotation;
    }

    /// <summary>
    /// Returns the distance traveled by the wheel
    /// since the previous call.
    /// </summary>
    public float GetDeltaDistance()
    {
        Quaternion currentRotation = transform.localRotation;

        Quaternion deltaRotation =
            Quaternion.Inverse(_previousRotation) * currentRotation;

        deltaRotation.ToAngleAxis(
            out float angle,
            out Vector3 deltaAxis
        );

        // Normalize angle to [-180, 180]
        if (angle > 180f)
            angle -= 360f;

        // Get wheel rotation axis
        Vector3 rotationAxis = axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _ => Vector3.forward
        };

        // Extract rotation around the wheel axis
        float signedAngle =
            angle * Vector3.Dot(deltaAxis, rotationAxis);

        _previousRotation = currentRotation;

        // Convert angle to rolling distance
        float distance =
            signedAngle * Mathf.Deg2Rad * radius;

        return invertDirection ? -distance : distance;
    }

    [ContextMenu("Generate Colliders")]
    public void GenerateColliders()
    {
        count = Mathf.Max(3, count);

        // Remove previously generated colliders
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child.name.StartsWith("Collider_"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        Quaternion axisRotation = axis switch
        {
            Axis.X => Quaternion.Euler(0, 90, 0),
            Axis.Y => Quaternion.Euler(-90, 0, 0),
            _ => Quaternion.identity
        };

        float segmentLength =
            2f * radius * Mathf.Tan(Mathf.PI / count) * 1.1f;

        for (int i = 0; i < count; i++)
        {
            float angle = i * 360f / count;
            float rad = angle * Mathf.Deg2Rad;

            GameObject segment =
                new GameObject($"Collider_{i}");

            segment.transform.SetParent(transform, false);

            // Position on XY plane, then rotate to selected axis
            Vector3 position = new Vector3(
                radius * Mathf.Cos(rad),
                radius * Mathf.Sin(rad),
                0f
            );

            segment.transform.localPosition =
                axisRotation * position;

            segment.transform.localRotation =
                axisRotation * Quaternion.Euler(0, 0, angle + 90f);

            BoxCollider col =
                segment.AddComponent<BoxCollider>();

            col.size = new Vector3(
                segmentLength,
                thickness,
                thickness
            );

            col.isTrigger = true;
        }
    }
}