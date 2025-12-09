using UnityEngine;

public class CarEngineSound : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Settings")]
    
    public float startOffset = 0.0f;
    public float stopDelay = 0.1f;

    private float currentTimer = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.Stop();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (!audioSource.isPlaying)
            {
                audioSource.time = startOffset;
                audioSource.Play();
            }
        }

        if (Input.GetKey(KeyCode.W))
        {
            currentTimer = stopDelay;
        }
        else
        {
            if (currentTimer > 0)
            {
                currentTimer -= Time.deltaTime;
            }
            else
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }
    }
}