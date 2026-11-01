using UnityEngine;
using Fusion;

public class PlayerVoiceAudio : NetworkBehaviour
{
    [SerializeField] private AudioSource voiceAudioSource;
    [Networked] public NetworkBool Always2D { get; set; }

    public void UpdateSpatial(bool isInGame)
    {
        if (voiceAudioSource == null) return;
        voiceAudioSource.spatialBlend = !isInGame || Always2D ? 0f : 1f;
    }
}