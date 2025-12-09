using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [Header("Track List")]
    public AudioClip[] songs;

    private AudioSource audioSource;
    private int currentSongIndex = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        PlayNextSong();
    }

    void Update()
    {
        if (!audioSource.isPlaying)
        {
            PlayNextSong();
        }
    }

    void PlayNextSong()
    {
        if (songs.Length == 0) return;

        audioSource.clip = songs[currentSongIndex];
        audioSource.Play();

        currentSongIndex++;

        if (currentSongIndex >= songs.Length)
        {
            currentSongIndex = 0;
        }
    }
}