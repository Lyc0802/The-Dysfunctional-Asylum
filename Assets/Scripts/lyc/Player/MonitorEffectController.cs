using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MonitorEffectController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Material monitorMaterial;
    [SerializeField] private bool isBroken;

    private UniversalAdditionalCameraData cameraData;
    private static readonly int IsBrokenID = Shader.PropertyToID("_IsBroken");

    private void Awake()
    {
        cameraData = playerCamera.GetUniversalAdditionalCameraData();
    }

    private void Start()
    {
        SetBroken(isBroken);
    }

    public void SetBroken(bool broken)
    {
        isBroken = broken;
        monitorMaterial.SetFloat(IsBrokenID, broken ? 1f : 0f);
    }

    public void UsingMonitor(bool isUsingMonitor)
    {
        cameraData.SetRenderer(isUsingMonitor ? 1 : 0);
    }
}