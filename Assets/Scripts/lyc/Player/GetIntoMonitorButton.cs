using UnityEngine;

public class GetIntoMonitorButton : MonoBehaviour
{
    public void GetIntoMonitor()
    {
        Vehicle vehicle = FindFirstObjectByType<Vehicle>();
        if (vehicle != null)
        {
            vehicle.GetIntoMonitor();
        }
    }
}
