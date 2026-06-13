using UnityEngine;

[DisallowMultipleComponent]
public class AudioGainFilter : MonoBehaviour
{
    public volatile float Gain = 1f;

    public static void PlayClipAtPoint(
        AudioClip clip,
        Vector3 position,
        float volume,
        float spatialBlend = 0f
    )
    {
        if (clip == null)
        {
            return;
        }

        GameObject audioObject = new GameObject("One Shot Audio");
        audioObject.transform.position = position;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.volume = Mathf.Min(volume, 1f);

        AudioGainFilter gainFilter = audioObject.AddComponent<AudioGainFilter>();
        gainFilter.Gain = Mathf.Max(1f, volume);

        AudioCategoryVolume category = audioObject.AddComponent<AudioCategoryVolume>();
        category.IsMusic = false;

        source.Play();
        Destroy(audioObject, clip.length + 0.1f);
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float gain = Gain;

        if (Mathf.Approximately(gain, 1f))
        {
            return;
        }

        for (int i = 0; i < data.Length; i++)
        {
            data[i] *= gain;
        }
    }
}
