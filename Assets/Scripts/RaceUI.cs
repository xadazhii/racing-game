using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RaceUI : MonoBehaviour
{
    public static RaceUI instance;

    [Header("UI Texts")]
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI lapTimeText;
    public TextMeshProUGUI lapCountText;
    public TextMeshProUGUI positionText;

    [Header("Power-Ups Display")]
    public GameObject powerUpContainer;
    public TextMeshProUGUI powerUpIconText;
    public TextMeshProUGUI powerUpTimerText;

    [Header("Symbols")]
    public string nitroSymbol = "»";
    public string shieldSymbol = "♦";
    public string oilSymbol = "●";
    public string freezeSymbol = "*";

    [Header("Finish")]
    public GameObject resultsPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI yourPositionText;
    public TextMeshProUGUI yourTimeText;
    public Transform resultsListContainer;
    public GameObject resultRowPrefab;

    [Header("Finish Message")]
    public GameObject finishMessagePanel;
    public TextMeshProUGUI finishMessageText;

    [Header("Countdown")]
    public GameObject countdownPanel;
    public TextMeshProUGUI countdownText;

    [Header("Player")]
    public MyCarController playerCar;

    private RaceManager raceManager;
    private float powerUpTimeLeft;
    private bool isPowerUpActive;
    private bool resultsShown = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        raceManager = RaceManager.instance;

        if (powerUpContainer != null)
        {
            powerUpContainer.SetActive(false);
        }

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }

        if (finishMessagePanel != null)
        {
            finishMessagePanel.SetActive(false);
        }

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        resultsShown = false;
    }

    public void StartCountdownFromIntro()
    {
        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        if (countdownPanel != null) countdownPanel.SetActive(true);

        if (playerCar != null) playerCar.enabled = false;

        if (raceManager != null) raceManager.FreezeAllCars();

        string[] countdownMessages = { "3", "2", "1", "GO!" };
        Color[] countdownColors = { Color.red, Color.yellow, Color.green, Color.white };

        for (int i = 0; i < countdownMessages.Length; i++)
        {
            if (countdownText != null)
            {
                countdownText.text = countdownMessages[i];
                countdownText.color = countdownColors[i];
                StartCoroutine(AnimateCountdown(countdownText.transform));
            }

            yield return new WaitForSeconds(1f);
        }

        if (countdownPanel != null) countdownPanel.SetActive(false);

        if (playerCar != null) playerCar.enabled = true;

        if (raceManager != null)
        {
            raceManager.UnfreezeAllCars();
            raceManager.StartRaceTimer();
        }
    }

    IEnumerator AnimateCountdown(Transform textTransform)
    {
        float duration = 0.3f;
        float elapsed = 0f;

        textTransform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, elapsed / duration);
            textTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        textTransform.localScale = Vector3.one * 1.2f;
    }

    void Update()
    {
        if (isPowerUpActive)
        {
            powerUpTimeLeft -= Time.deltaTime;

            if (powerUpTimerText != null)
            {
                powerUpTimerText.text = powerUpTimeLeft.ToString("F1") + "s";
            }

            if (powerUpTimeLeft <= 0)
            {
                isPowerUpActive = false;
                if (powerUpContainer != null)
                {
                    powerUpContainer.SetActive(false);
                }
            }
        }

        if (raceManager == null || playerCar == null) return;
        if (raceManager.raceStartTime == 0) return;

        RaceManager.RaceData data = raceManager.GetPlayerData(playerCar.transform);
        if (data == null) return;

        if (totalTimeText != null)
        {
            float totalT = Time.time - raceManager.raceStartTime;
            totalTimeText.text = FormatTime(totalT);
        }

        if (lapTimeText != null)
        {
            if (data.currentLapStartTime == 0)
            {
                lapTimeText.text = "00:00.00";
            }
            else
            {
                float currentLapT = Time.time - data.currentLapStartTime;
                lapTimeText.text = FormatTime(currentLapT);
            }
        }

        if (lapCountText != null)
        {
            int displayLap = Mathf.Clamp(data.currentLap, 1, raceManager.totalLaps);
            lapCountText.text = $"LAP: {displayLap} / {raceManager.totalLaps}";
        }

        if (positionText != null)
        {
            positionText.text = $"POS: {data.currentPosition} / {raceManager.GetTotalRacers()}";
        }

        if (data.finished && !resultsShown)
        {
            StartCoroutine(ShowFinishSequence());
        }
    }

    IEnumerator ShowFinishSequence()
    {
        resultsShown = true;

        RaceManager.RaceData playerData = raceManager.GetPlayerData(playerCar.transform);

        if (finishMessageText != null)
        {
            finishMessageText.gameObject.SetActive(true);

            string message = "";
            Color color = Color.white;

            if (playerData.currentPosition == 1)
            {
                message = "YOU WIN!\nCONGRATULATIONS!";
                color = new Color(1f, 0.84f, 0f);
            }
            else if (playerData.currentPosition == 2)
            {
                message = "2ND PLACE!\nGREAT JOB!";
                color = Color.white;
            }
            else if (playerData.currentPosition == 3)
            {
                message = "3RD PLACE!\nWELL DONE!";
                color = new Color(0.8f, 0.5f, 0.2f);
            }
            else
            {
                message = $"FINISHED!\n{playerData.currentPosition}TH PLACE";
                color = Color.gray;
            }

            finishMessageText.text = message;
            finishMessageText.color = color;
        }

        yield return new WaitForSecondsRealtime(3f);

        if (finishMessageText != null)
        {
            finishMessageText.gameObject.SetActive(false);
        }

        ShowResults();
    }

    void ShowResults()
    {
        GameObject gameHUD = GameObject.Find("GameHUD");
        if (gameHUD != null) gameHUD.SetActive(false);

        resultsPanel.SetActive(true);

        RaceManager.RaceData playerData = raceManager.GetPlayerData(playerCar.transform);

        if (playerData == null) return;

        if (titleText != null)
        {
            titleText.text = ">>> RACE FINISHED! <<<";
        }

        if (yourPositionText != null)
        {
            string suffix = GetPositionSuffix(playerData.currentPosition);
            yourPositionText.text = $"{playerData.currentPosition}{suffix} PLACE";
        }

        if (yourTimeText != null)
        {
            yourTimeText.text = $"TIME: {FormatTime(playerData.totalRaceTime)}";
        }

        if (resultsListContainer != null && resultRowPrefab != null)
        {
            foreach (Transform child in resultsListContainer)
            {
                Destroy(child.gameObject);
            }

            List<RaceManager.RaceData> finalStandings = raceManager.GetFinalStandings();

            foreach (var racer in finalStandings)
            {
                GameObject row = Instantiate(resultRowPrefab, resultsListContainer);
                TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 3)
                {
                    texts[0].text = racer.currentPosition.ToString();
                    texts[1].text = racer.carName;
                    texts[2].text = FormatTime(racer.totalRaceTime);

                    Color textColor = Color.white;
                    Color bgColor = new Color(0.16f, 0.16f, 0.16f);

                    if (racer.currentPosition == 1)
                    {
                        textColor = new Color(1f, 0.84f, 0f);
                        bgColor = new Color(0.3f, 0.25f, 0f);
                    }
                    else if (racer.currentPosition == 2)
                    {
                        textColor = new Color(0.75f, 0.75f, 0.75f);
                        bgColor = new Color(0.2f, 0.2f, 0.2f);
                    }
                    else if (racer.currentPosition == 3)
                    {
                        textColor = new Color(0.8f, 0.5f, 0.2f);
                        bgColor = new Color(0.25f, 0.15f, 0.05f);
                    }

                    foreach (var t in texts)
                    {
                        if (t != null) t.color = textColor;
                    }

                    Image rowImage = row.GetComponent<Image>();
                    if (rowImage != null) rowImage.color = bgColor;
                }
            }
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
    }

    string GetPositionSuffix(int pos)
    {
        if (pos == 1) return "st";
        if (pos == 2) return "nd";
        if (pos == 3) return "rd";
        return "th";
    }

    public void ShowPowerUpIcon(PowerUpPickup.PowerUpType type, float duration)
    {
        if (powerUpContainer == null) return;

        powerUpContainer.SetActive(true);
        isPowerUpActive = true;
        powerUpTimeLeft = duration;

        if (powerUpIconText != null)
        {
            switch (type)
            {
                case PowerUpPickup.PowerUpType.Nitro:
                    powerUpIconText.text = nitroSymbol;
                    powerUpIconText.color = Color.yellow;
                    break;

                case PowerUpPickup.PowerUpType.Shield:
                    powerUpIconText.text = shieldSymbol;
                    powerUpIconText.color = Color.green;
                    break;

                case PowerUpPickup.PowerUpType.Oil:
                    powerUpIconText.text = oilSymbol;
                    powerUpIconText.color = new Color(1f, 0.6f, 0.2f);
                    break;

                case PowerUpPickup.PowerUpType.Freeze:
                    powerUpIconText.text = freezeSymbol;
                    powerUpIconText.color = Color.cyan;
                    break;
            }
        }

        if (powerUpTimerText != null)
        {
            powerUpTimerText.text = duration.ToString("F1") + "s";
        }
    }

    string FormatTime(float t)
    {
        int minutes = (int)t / 60;
        int seconds = (int)t % 60;
        int milliseconds = (int)(t * 100) % 100;
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    public void RestartRace()
    {
        Debug.Log("🔄 Restarting race...");

        RaceGlobalState.isRestarting = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
