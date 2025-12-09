using UnityEngine;
using System.Collections;

public class FreezePickup : MonoBehaviour
{
    [Header("Settings")]
    public float duration = 5f;
    public float respawnTime = 10f;
    public GameObject visualModel;

    private Collider myCollider;

    void Start()
    {
        myCollider = GetComponent<Collider>();
        if (myCollider != null) myCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        MyCarController car = other.GetComponentInParent<MyCarController>();

        if (car != null && (other.transform.root.CompareTag("Player") || other.CompareTag("Player")))
        {
            Debug.Log("✅ PLAYER picked up: Freeze");

            if (RaceUI.instance != null)
            {
                Debug.Log($"📞 Calling ShowPowerUpIcon(Freeze, {duration})");
                RaceUI.instance.ShowPowerUpIcon(PowerUpPickup.PowerUpType.Freeze, duration);
            }
            else
            {
                Debug.LogError("❌ RaceUI.instance is NULL!");
            }

            ActivatePowerUp();

            StartCoroutine(RespawnRoutine());
        }
    }

    void ActivatePowerUp()
    {
        if (NotificationManager.instance != null)
        {
            NotificationManager.instance.ShowMessage("FREEZE ACTIVATED!", Color.cyan);
        }

        if (RaceManager.instance != null)
        {
            RaceManager.instance.FreezeAllBots(duration);
            Debug.Log("❄️ Freezing bots!");
        }
    }

    IEnumerator RespawnRoutine()
    {
        if (visualModel != null) visualModel.SetActive(false);
        if (myCollider != null) myCollider.enabled = false;

        Debug.Log($"⏳ Respawn in {respawnTime}s");
        yield return new WaitForSeconds(respawnTime);

        if (visualModel != null) visualModel.SetActive(true);
        if (myCollider != null) myCollider.enabled = true;

        Debug.Log("✨ Power-up respawned!");
    }
}