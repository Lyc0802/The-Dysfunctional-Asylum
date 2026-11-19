using UnityEngine;

public class PCCharacterChangeUITrigger : MonoBehaviour
{
    public GameObject UI;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerInfo player)) return;
        if (!player.HasInputAuthority || player.Device != DeviceType.PC) return;

        Debug.Log($"Trigger Scene: {gameObject.scene.name}, Player Scene: {other.gameObject.scene.name}");
        Debug.Log(System.Environment.StackTrace);

        if (UI.activeSelf) return;
        UI.GetComponent<PCCharacterUI>().playerInfo = player;
        UI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"EXIT | Trigger:{GetInstanceID()} | Collider:{other.GetInstanceID()} | Frame:{Time.frameCount}");
    }
}