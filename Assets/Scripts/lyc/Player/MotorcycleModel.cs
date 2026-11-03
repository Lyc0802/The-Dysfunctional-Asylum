using UnityEngine;

public class MotorcycleModel : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform reference;

    [Header("Objects to Rotate")]
    [SerializeField] private Transform[] targets;

    private float initialZ;
    private float[] initialTargetZ;

    private void Start()
    {
        if (reference == null || targets == null) return;

        initialZ = reference.localEulerAngles.z;
        initialTargetZ = new float[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;
            initialTargetZ[i] = targets[i].localEulerAngles.z;
        }
    }

    private void LateUpdate()
    {
        if (reference == null || initialTargetZ == null) return;

        float deltaZ = Mathf.DeltaAngle(initialZ, reference.localEulerAngles.z);

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            Vector3 angles = targets[i].localEulerAngles;
            angles.z = initialTargetZ[i] + deltaZ;
            targets[i].localEulerAngles = angles;
        }
    }
}