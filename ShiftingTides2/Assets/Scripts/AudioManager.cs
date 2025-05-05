using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioClip backgroundMusic;
    public AudioClip gavelHitSound;
    public AudioClip heartbeatSound;
    public AudioClip corrIncorrSound;
    public AudioClip crowdSound;

    public float musicVolume = 0.2f;
    public float maxHeartbeatVolume = 0.8f;
    public float minHeartbeatVolume = 0.2f;
    public float gavelHitVolume = 0.5f;
    public float corrIncorrVolume = 0.5f;
    public float crowdVolume = 0.1f;

    private AudioSource audioSourceBG;
    private AudioSource audioSourceGavel;
    private AudioSource audioSourceHeartbeat;
    private AudioSource audioSourceCorrIncorr;
    private AudioSource audioSourceCrowd;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSourceBG = gameObject.AddComponent<AudioSource>();
        audioSourceBG.name = "BackgroundMusic";
        audioSourceBG.clip = backgroundMusic;
        audioSourceBG.loop = true;
        audioSourceBG.volume = musicVolume;
        audioSourceBG.playOnAwake = false;

        audioSourceCrowd = gameObject.AddComponent<AudioSource>();
        audioSourceCrowd.name = "CrowdSound";
        audioSourceCrowd.clip = crowdSound;
        audioSourceCrowd.loop = true;
        audioSourceCrowd.volume = crowdVolume;
        audioSourceCrowd.playOnAwake = false;

        audioSourceGavel = gameObject.AddComponent<AudioSource>();
        audioSourceGavel.name = "GavelHitSound";
        audioSourceGavel.clip = gavelHitSound;
        audioSourceGavel.volume = gavelHitVolume;
        audioSourceGavel.loop = false;
        audioSourceGavel.playOnAwake = false;

        audioSourceHeartbeat = gameObject.AddComponent<AudioSource>();
        audioSourceHeartbeat.name = "HeartbeatSound";
        audioSourceHeartbeat.clip = heartbeatSound;
        audioSourceHeartbeat.loop = true;
        audioSourceHeartbeat.volume = minHeartbeatVolume;
        audioSourceHeartbeat.playOnAwake = false;

        audioSourceCorrIncorr = gameObject.AddComponent<AudioSource>();
        audioSourceCorrIncorr.name = "CorrIncorrSound";
        audioSourceCorrIncorr.clip = corrIncorrSound;
        audioSourceCorrIncorr.volume = corrIncorrVolume;
        audioSourceCorrIncorr.loop = false;
        audioSourceCorrIncorr.playOnAwake = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlayBackgroundMusic()
    {
        audioSourceBG.Play();
        audioSourceCrowd.Play();
    }

    public void PlayGavelHitSound()
    {
        audioSourceGavel.Play();
    }

    public void PlayHeartbeatSound()
    {
        audioSourceHeartbeat.time = 0.0f; // Start from the beginning of the clip
        audioSourceHeartbeat.Play();
        StartCoroutine(AdjustHeartbeatVolume(audioSourceHeartbeat));
        audioSourceHeartbeat.Stop();
    }

    public void PlayCorrIncorrSound(bool isCorrect)
    {
        if (!isCorrect)
        {
           audioSourceCorrIncorr.time = 1.0f; // Start from 1 second into the clip
        }
        audioSourceCorrIncorr.Play();
    }

    private IEnumerator AdjustHeartbeatVolume(AudioSource audioSource)
    {
        float timeElapsed = 0f;
        float duration = 5f;

        // Increase the volume to max over the duration of 5 seconds
        while (timeElapsed < duration)
        {
            audioSource.volume = Mathf.Lerp(minHeartbeatVolume, maxHeartbeatVolume, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = maxHeartbeatVolume; // Ensure it reaches the max volume

        yield return new WaitForSeconds(1f);
    }

}
