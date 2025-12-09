using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class OilSlick : MonoBehaviour
{
    [Header("Effect Settings")]
    public float slowFactor = 0.5f;
    public float duration = 3.0f;

    [Header("Respawn Settings")]
    public float respawnTime = 10f;
    public GameObject visualModel;

    private float activationTime = 1.0f;
    private float timer = 0;
    private Collider myCollider;
    private Renderer[] allRenderers;
    private bool isActive = true;

    void Start()
    {
        myCollider = GetComponent<Collider>();
        allRenderers = GetComponentsInChildren<Renderer>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        Debug.Log($"💧 OilSlick created! Renderers: {allRenderers.Length}");
    }

    void Update()
    {
        timer += Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        if (timer < activationTime) return;

        MyCarController playerCar = other.GetComponentInParent<MyCarController>();
        if (playerCar != null)
        {
            if (gameObject.CompareTag("Player") && other.CompareTag("Player"))
            {
                return;
            }

            if (NotificationManager.instance != null)
                NotificationManager.instance.ShowMessage("Oops! You slipped!", Color.red);

            Debug.Log("🚗 Player hit oil!");
            playerCar.ApplySlow(slowFactor, duration);

            StartCoroutine(RespawnRoutine());
            return;
        }

        NavMeshAgent botAgent = other.GetComponentInParent<NavMeshAgent>();
        if (botAgent != null)
        {
            string botName = other.transform.root.name;

            if (NotificationManager.instance != null)
            {
                NotificationManager.instance.ShowMessage($"{botName} hit oil!", Color.yellow);
            }

            Debug.Log($"🤖 {botName} hit oil!");
            StartCoroutine(SlowBotRoutine(botAgent, slowFactor, duration));

            StartCoroutine(RespawnRoutine());
        }
    }

    IEnumerator SlowBotRoutine(NavMeshAgent agent, float factor, float time)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            float originalSpeed = agent.speed;
            agent.speed *= factor;

            Debug.Log($"🐌 {agent.name} slowed to {agent.speed}");

            yield return new WaitForSeconds(time);

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.speed = originalSpeed;
                Debug.Log($"✅ {agent.name} speed restored");
            }
        }
    }

    IEnumerator RespawnRoutine()
    {
        isActive = false;

        Debug.Log("💧 Oil disappeared...");

        if (visualModel != null)
        {
            visualModel.SetActive(false);
        }
        else
        {
            foreach (var renderer in allRenderers)
            {
                if (renderer != null) renderer.enabled = false;
            }
        }

        if (myCollider != null) myCollider.enabled = false;

        yield return new WaitForSeconds(respawnTime);

        Debug.Log("🔄 Oil respawned!");

        if (visualModel != null)
        {
            visualModel.SetActive(true);
        }
        else
        {
            foreach (var renderer in allRenderers)
            {
                if (renderer != null) renderer.enabled = true;
            }
        }

        if (myCollider != null) myCollider.enabled = true;

        timer = 0;
        isActive = true;
    }
}
