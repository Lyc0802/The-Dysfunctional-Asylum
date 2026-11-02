using UnityEngine;

public class MotorcycleModel : MonoBehaviour
{
     [Header("Reference")]
    [SerializeField] private Transform reference;

    [Header("Objects to Rotate")]
    [SerializeField] private Transform[] targets;

    private Vector3[] initialOffsets;
    private Quaternion[] initialRotations;

    private float initialZ;
    private Quaternion initialParentRotation;

    private void Start()
    {
        if (reference == null || targets == null) return;

        initialZ = reference.localEulerAngles.z;

        initialParentRotation = reference.parent != null
            ? reference.parent.rotation
            : Quaternion.identity;

        initialOffsets = new Vector3[targets.Length];
        initialRotations = new Quaternion[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            initialOffsets[i] =
                Quaternion.Inverse(initialParentRotation) *
                (targets[i].position - reference.position);

            initialRotations[i] =
                Quaternion.Inverse(initialParentRotation) *
                targets[i].rotation;
        }
    }

    private void LateUpdate()
    {
        if (reference == null || initialOffsets == null) return;

        Quaternion parentRotation = reference.parent != null
            ? reference.parent.rotation
            : Quaternion.identity;

        float deltaZ = Mathf.DeltaAngle(
            initialZ,
            reference.localEulerAngles.z
        );

        Quaternion zRotation = Quaternion.AngleAxis(
            deltaZ,
            Vector3.forward
        );

        Quaternion finalRotation = parentRotation * zRotation;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            targets[i].position =
                reference.position +
                finalRotation * initialOffsets[i];

            targets[i].rotation =
                finalRotation * initialRotations[i];
        }
    }
}
