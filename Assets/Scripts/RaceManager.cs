using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PROMETEO___Car_Controller.Scripts;

public class RaceManager : MonoBehaviour
{
    public static RaceManager instance;

    [Header("Race Settings")]
    public int totalLaps = 3;

    [Header("Required References")]
    public Transform checkpointsParent;

    [HideInInspector] public float raceStartTime;
    [HideInInspector] public List<RaceData> raceStandings = new List<RaceData>();
    [HideInInspector] public int totalCheckpoints;

    private List<Transform> checkpoints = new List<Transform>();
    private Dictionary<Transform, RaceData> carProgress = new Dictionary<Transform, RaceData>();

    private const int LAP_WEIGHT = 1000000;
    private const int CHECKPOINT_WEIGHT = 100000;
    private const int DISTANCE_WEIGHT = 99999;

    public class RaceData
    {
        public string carName;
        public Transform carTransform;
        public int currentLap = 1;
        public int lastCheckpointHit = -1;
        public bool finished = false;

        public float raceScore = 0;
        public int currentPosition = 0;

        public float totalRaceTime = 0;
        public float currentLapStartTime = 0;
    }

    void Awake()
    {
        if (instance == null) { instance = this; } else { Destroy(gameObject); }
    }

    void Start()
    {
        if (checkpointsParent == null)
        {
            Debug.LogError("🔴 RaceManager: 'Checkpoints Parent' not assigned!");
            return;
        }

        checkpoints.Clear();
        foreach (Transform cp in checkpointsParent)
        {
            if (cp.gameObject.activeSelf)
            {
                checkpoints.Add(cp);
                CheckpointTrigger trigger = cp.GetComponent<CheckpointTrigger>();
                if (trigger == null)
                {
                    trigger = cp.gameObject.AddComponent<CheckpointTrigger>();
                    cp.GetComponent<Collider>().isTrigger = true;
                }
                trigger.index = checkpoints.Count - 1;
            }
        }
        totalCheckpoints = checkpoints.Count;

        if (totalCheckpoints < 2) Debug.LogError("🔴 You need at least 2 checkpoints!");
    }

    void Update()
    {
        if (raceStartTime > 0)
        {
            UpdateRankings();
        }
    }

    public void RegisterCar(Transform car, string carName)
    {
        if (!carProgress.ContainsKey(car))
        {
            string displayName;
            if (car.name == "Prometheus")
            {
                displayName = "YOU";
            }
            else
            {
                displayName = car.name;
            }

            RaceData newData = new RaceData()
            {
                carName = displayName,
                carTransform = car
            };

            carProgress.Add(car, newData);
            raceStandings.Add(newData);

            Debug.Log($"✅ Registered: {displayName}");
        }
    }

    public void StartRaceTimer()
    {
        raceStartTime = Time.time;
        foreach(var data in raceStandings)
        {
            data.currentLapStartTime = raceStartTime;
        }
        Debug.Log("GO! Race timer started!");
    }

    public void CarHitCheckpoint(Transform car, int checkpointIndex)
    {
        if (!carProgress.ContainsKey(car)) return;
        RaceData data = carProgress[car];
        if (data.finished) return;

        if (totalCheckpoints == 0) return;

        int expectedCheckpoint = (data.lastCheckpointHit + 1) % totalCheckpoints;

        if (checkpointIndex == expectedCheckpoint)
        {
            int previousCheckpoint = data.lastCheckpointHit;
            data.lastCheckpointHit = checkpointIndex;

            if (checkpointIndex == 0 && previousCheckpoint == totalCheckpoints - 1)
            {
                 data.currentLap++;
                 data.currentLapStartTime = Time.time;

                 if (data.currentLap > totalLaps)
                 {
                     data.finished = true;
                     data.totalRaceTime = Time.time - raceStartTime;
                     data.raceScore = (data.currentLap * LAP_WEIGHT) - data.totalRaceTime;

                     StopCar(car);
                 }
            }
        }
    }

    void UpdateRankings()
    {
        if (checkpoints.Count == 0) return;

        foreach (var data in raceStandings)
        {
            if (data.finished) continue;

            float lapScore = data.currentLap * LAP_WEIGHT;
            float checkpointScore = data.lastCheckpointHit * CHECKPOINT_WEIGHT;

            int nextIndex = (data.lastCheckpointHit + 1) % totalCheckpoints;
            if (nextIndex >= checkpoints.Count) nextIndex = 0;

            int prevIndex = (data.lastCheckpointHit < 0) ? totalCheckpoints - 1 : data.lastCheckpointHit;
            if (prevIndex >= checkpoints.Count) prevIndex = checkpoints.Count - 1;

            float distToNext = Vector3.Distance(data.carTransform.position, checkpoints[nextIndex].position);
            float totalDist = Vector3.Distance(checkpoints[prevIndex].position, checkpoints[nextIndex].position);
            if (totalDist <= 0.001f) totalDist = 1f;

            float progress = Mathf.Clamp01(1.0f - (distToNext / totalDist));
            data.raceScore = lapScore + checkpointScore + (progress * DISTANCE_WEIGHT);
        }

        raceStandings = raceStandings.OrderByDescending(d => d.raceScore).ToList();

        for (int i = 0; i < raceStandings.Count; i++)
        {
            raceStandings[i].currentPosition = i + 1;
        }
    }

    private void StopCar(Transform car)
    {
        MyCarController myPlayer = car.GetComponent<MyCarController>();
        if (myPlayer != null)
        {
             myPlayer.enabled = false;
             Rigidbody rb = car.GetComponent<Rigidbody>();
             if(rb != null) rb.linearVelocity = Vector3.zero;
        }

        NavMeshAgent agent = car.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        NavAgentFollower ai = car.GetComponent<NavAgentFollower>();
        if (ai != null) ai.enabled = false;

        PrometeoCarController prometeo = car.GetComponent<PrometeoCarController>();
        if (prometeo != null) prometeo.enabled = false;
    }

    public void FreezeAllBots(float duration)
    {
        StartCoroutine(FreezeRoutine(duration));
    }

    IEnumerator FreezeRoutine(float duration)
    {
        Debug.Log("❄️ Freezing bots!");

        List<Rigidbody> frozenRBs = new List<Rigidbody>();
        List<NavMeshAgent> frozenAgents = new List<NavMeshAgent>();

        foreach (var racer in raceStandings)
        {
            if (racer.carTransform.CompareTag("Player")) continue;

            NavMeshAgent agent = racer.carTransform.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                frozenAgents.Add(agent);
            }

            Rigidbody rb = racer.carTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                frozenRBs.Add(rb);
            }
        }

        yield return new WaitForSeconds(duration);

        foreach (var agent in frozenAgents) if(agent != null) agent.isStopped = false;
        foreach (var rb in frozenRBs) if(rb != null) rb.isKinematic = false;

        Debug.Log("☀️ Bots unfrozen");
    }

    public RaceData GetPlayerData(Transform playerTransform)
    {
        if (carProgress.ContainsKey(playerTransform)) return carProgress[playerTransform];
        return null;
    }

    public int GetTotalRacers() { return carProgress.Count; }

    public List<RaceData> GetFinalStandings()
    {
        return raceStandings.OrderBy(d => d.currentPosition).ToList();
    }

    public void FreezeAllCars()
    {
        foreach (var racer in raceStandings)
        {
            if (racer.carTransform == null) continue;

            MyCarController player = racer.carTransform.GetComponent<MyCarController>();
            if (player != null) player.enabled = false;

            NavMeshAgent agent = racer.carTransform.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            NavAgentFollower ai = racer.carTransform.GetComponent<NavAgentFollower>();
            if (ai != null) ai.enabled = false;
        }

        Debug.Log("🔒 All cars locked for countdown");
    }

    public void UnfreezeAllCars()
    {
        foreach (var racer in raceStandings)
        {
            if (racer.carTransform == null) continue;

            NavMeshAgent agent = racer.carTransform.GetComponent<NavMeshAgent>();
            if (agent != null) agent.isStopped = false;

            NavAgentFollower ai = racer.carTransform.GetComponent<NavAgentFollower>();
            if (ai != null) ai.enabled = true;
        }

        Debug.Log("🔓 All cars unlocked - race started!");
    }
}
