using UnityEngine;
using Fusion;
using Photon.Voice.Unity;

public class PlayerVoiceAudio : NetworkBehaviour
{
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private Recorder recorder;
    [Networked] public NetworkBool Always2D { get; set; }

    public override void Spawned()
    {
        if (!HasInputAuthority) return;

        VoiceChatManager voiceChatManager = Runner.GetComponent<VoiceChatManager>();
        if (voiceChatManager != null)
        {
            voiceChatManager.localRecorder = recorder;
            voiceChatManager.SetMode(voiceChatManager.isInGame, GetComponent<PlayerInfo>().Device);
        }
    }

    public void UpdateSpatial(bool isInGame)
    {
        if (voiceAudioSource == null) return;
        voiceAudioSource.spatialBlend = !isInGame || Always2D ? 0f : 1f;
    }
}