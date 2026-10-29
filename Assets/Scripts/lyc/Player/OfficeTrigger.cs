using Fusion;
using UnityEngine;

public class OfficeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // if (!HasStateAuthority) return;
        if (!other.TryGetComponent(out PlayerInfo player)) return;
        if (player.Device != DeviceType.PC) return;

        GameManager.Instance.EndGame(WinningTeam.PC);
    }
}