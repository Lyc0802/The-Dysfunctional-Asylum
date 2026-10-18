using UnityEngine;
using Bombanana.Interaction;

public class RigAnchor : MonoBehaviour
{
    public Transform headAnchor;
    public Transform leftHaneAnchor;
    public Transform rightHaneAnchor;

    public Vehicle vehicle;
    public Transform motorHandlebar;
    public Wheelchair wc;

    [SerializeField] private CustomHandVisual leftHandVisual;
    [SerializeField] private CustomHandVisual rightHandVisual;
    
    public Vector3 GetHeadPosition(){ return headAnchor.position;}
    public Vector3 GetLeftHandPosition(){ return leftHaneAnchor.position;}
    public Vector3 GetrightHandPosition(){ return rightHaneAnchor.position;}

    public bool GetIsUsingMotor(){ return vehicle.usingMotor; }
    public Vector2 GetWheelDistance(){ return wc.GetWheelDistance(); }
    public Quaternion GetHandlebarLocalRotation(){ return motorHandlebar.localRotation; }

    public void ReadInput(ref PlayerInputData data)
    {
        data.headPosition = GetHeadPosition();
        data.leftHandPosition = GetLeftHandPosition();
        data.rightHandPosition = GetrightHandPosition();
        data.headRotation =  headAnchor.rotation;
        data.leftHandRotation = leftHaneAnchor.rotation;
        data.rightHandRotation = rightHaneAnchor.rotation;
        data.usingMotor = GetIsUsingMotor();
        data.wheelDistance = GetWheelDistance();
        data.handlebarLocalRotation = GetHandlebarLocalRotation();
        HandTrackingSync.Capture(leftHandVisual, ref data, true);
        HandTrackingSync.Capture(rightHandVisual, ref data, false);
    }

}
