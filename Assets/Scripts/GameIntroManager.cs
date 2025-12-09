using System.Collections;
using PROMETEO___Car_Controller.Scripts;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Video;

public class GameIntroManager : MonoBehaviour
{
    [Header("References to objects")]
    public PrometeoCarController playerCarController;
    public VideoPlayer videoPlayer;
    public GameObject playButton;

    private NavAgentFollower[] aiCars;

    [Header("References to UI")]
    public GameObject gameHUD;

    [Header("Video clips")]
    public VideoClip introVideoClip;
    public VideoClip loopingMenuVideoClip;

    void Start()
    {
        aiCars = FindObjectsByType<NavAgentFollower>(FindObjectsSortMode.None);

        playButton.SetActive(false);
        if (gameHUD != null) { gameHUD.SetActive(false); }

        if (playerCarController != null)
        {
            playerCarController.enabled = false;
            Rigidbody rb = playerCarController.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        foreach (NavAgentFollower ai in aiCars)
        {
            if (ai != null)
            {
                ai.enabled = false;
                NavMeshAgent agent = ai.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.velocity = Vector3.zero;
                    agent.isStopped = true;
                }
            }
        }

        StartCoroutine(ManageIntroSequence());
    }

    private IEnumerator ManageIntroSequence()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("Video Player not assigned!");
            playButton.SetActive(true);
            yield break;
        }

        if (introVideoClip != null)
        {
            videoPlayer.clip = introVideoClip;
            videoPlayer.isLooping = false;
            videoPlayer.Play();
            yield return new WaitForSeconds((float)videoPlayer.clip.length);
        }

        if (loopingMenuVideoClip != null)
        {
            videoPlayer.clip = loopingMenuVideoClip;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        playButton.SetActive(true);
    }

    public void StartRace()
    {
        playButton.SetActive(false);
        if (gameHUD != null) { gameHUD.SetActive(true); }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.enabled = false;
        }

        if (RaceUI.instance != null)
        {
            RaceUI.instance.StartCountdownFromIntro();
        }
    }
}
