using UnityEngine;

public class PCCharacterUI : MonoBehaviour
{
    public PlayerInfo playerInfo;

    public void SetCharacter(int character)
    {
        playerInfo.SetCharacter((PCPlayerCharacter)character);
        gameObject.SetActive(false);
    }
}
