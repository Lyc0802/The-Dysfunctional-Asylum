using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public Transform Palm;
    public GameObject panel;
    public bool isVisible = true;

    public bool usingMotor = false;
    public Wheelchair wheelchair;
    public MotorcycleController motor;
    public PlayerInfo playerInfo;


    void Update()
    {
        panel.SetActive(Vector3.Angle(Vector3.up, Palm.up) < 55f );
    }

    public void ToggleVisibility(bool b)
    {
        isVisible = b;
        wheelchair.gameObject.SetActive(!usingMotor && isVisible);
        motor.gameObject.SetActive(usingMotor && isVisible);
    }

    public void ToggleVehicle()
    {
        usingMotor = !usingMotor;
        wheelchair.gameObject.SetActive(!usingMotor && isVisible);
        motor.gameObject.SetActive(usingMotor && isVisible);
    }

    public void GetIntoMonitor()
    {
        // PlayerInfo playerInfo = transform.parent.GetComponent<PlayerInfo>();
        // Debug.Log("Monitor: is Press;。。
        playerInfo.SendMonitor();
    }
}
