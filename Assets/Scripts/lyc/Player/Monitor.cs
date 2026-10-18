using UnityEngine;

public class Monitor : MonoBehaviour
{
    // private Quaternion originalRotation;
    // void Awake()
    // {
    //     originalRotation = transform.rotation;
    // }
    public void SetRotation(Quaternion userRotation){
        transform.rotation = userRotation;
    }
}
