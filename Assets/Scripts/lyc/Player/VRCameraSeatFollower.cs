using UnityEngine;

[DefaultExecutionOrder(1000)]
public class VRCameraSeatFollower : MonoBehaviour
{
    [SerializeField] private Transform seatPoint;
    [SerializeField] private float positionSmooth = 20f;
    [SerializeField] private float rotationSmooth = 20f;

    private void LateUpdate()
    {
        if (seatPoint == null) return;

        float t = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
        float r = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
        Vector3 pos = seatPoint.position;
        pos.y -= 0.6f;
        transform.position = Vector3.Lerp(transform.position, pos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, seatPoint.rotation, r);
    }
    public void SetSeat(Transform t)
    {
        seatPoint = t;
    }
}