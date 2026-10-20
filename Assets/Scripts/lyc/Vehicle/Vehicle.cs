using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public Transform Palm;
    public GameObject panel;

    public bool usingMotor = false;
    public Wheelchair wheelchair;
    public MotorcycleController motor;


    void Update()
    {
        panel.SetActive(Vector3.Angle(Vector3.up, Palm.up) < 55f );
    }

    public void ToggleVehicle()
    {
        usingMotor = !usingMotor;
        wheelchair.gameObject.SetActive(!usingMotor);
        motor.gameObject.SetActive(usingMotor);
    }

    public void GetIntoMonitor()
    {
        PlayerInfo playerInfo = transform.parent.GetComponent<PlayerInfo>();
        playerInfo.SendMonitor();
    }
}
