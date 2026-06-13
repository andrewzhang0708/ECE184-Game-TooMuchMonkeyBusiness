using UnityEngine;

[DisallowMultipleComponent]
public class AudioCategoryVolume : MonoBehaviour
{
    public bool IsMusic { get; set; }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float volume = IsMusic
            ? GameAudioSettings.MusicVolume
            : GameAudioSettings.SfxVolume;

        if (Mathf.Approximately(volume, 1f))
        {
            return;
        }

        for (int i = 0; i < data.Length; i++)
        {
            data[i] *= volume;
        }
    }
}
