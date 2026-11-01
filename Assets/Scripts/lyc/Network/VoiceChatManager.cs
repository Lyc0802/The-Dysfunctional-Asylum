using UnityEngine;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;

public enum VoiceGroup : byte
{
    Lobby = 0,
    PC = 1,
    VR = 2
}

public class VoiceChatManager : MonoBehaviour
{
    public FusionVoiceClient voiceClient;
    public Recorder localRecorder;
    public bool isInGame;

    private byte currentGroup;
    private byte subscribedGroup;
    private bool wasJoined;

    public void SetMode(bool inGame, DeviceType device)
    {
        isInGame = inGame;
        currentGroup = !inGame ? (byte)0 : device == DeviceType.PC ? (byte)1 : (byte)2;
        if (localRecorder != null) localRecorder.InterestGroup = currentGroup;
        UpdateGroup();
    }

    private void Update()
    {
        UpdateGroup();
    }

    private void UpdateGroup()
    {
        if (voiceClient == null || voiceClient.Client == null || localRecorder == null) return;

        if (!voiceClient.Client.InRoom)
        {
            wasJoined = false;
            return;
        }

        if (!wasJoined)
        {
            wasJoined = true;
            subscribedGroup = 0;
            localRecorder.InterestGroup = currentGroup;
        }

        if (subscribedGroup == currentGroup) return;

        byte[] remove = subscribedGroup == 0 ? null : new byte[] { subscribedGroup };
        byte[] add = currentGroup == 0 ? null : new byte[] { currentGroup };

        voiceClient.Client.OpChangeGroups(remove, add);
        subscribedGroup = currentGroup;
    }
}