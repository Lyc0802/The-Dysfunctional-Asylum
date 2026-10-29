using Fusion;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using Oculus.Interaction;

public class InputModuleDeviceChecker : MonoBehaviour
{
    private RunnerPeerSettings settings;

    public bool IsVR => settings != null && settings.Device == DeviceType.VR;

    public InputSystemUIInputModule inputSystemUIInputModule;
    public PointableCanvasModule pointableCanvasModule;

    private void Start()
    {
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null) return;

        settings = runner.GetComponent<RunnerPeerSettings>();
        if (settings == null) return;

        if (IsVR)
        {
            inputSystemUIInputModule.enabled = false;
            pointableCanvasModule.enabled = true;
        }
        else
        {
            pointableCanvasModule.enabled = false;
            inputSystemUIInputModule.enabled = true;
        }
    }
}