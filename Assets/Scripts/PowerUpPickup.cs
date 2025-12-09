using System.Collections;
using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    public enum PowerUpType { Nitro, Shield, Oil, Freeze }
    public PowerUpType type;

    [Header("Settings")]
    public float duration = 5.0f;
    public float respawnTime = 10.0f;

    [Header("Visuals & Physics")]
    public GameObject pickupVisuals;
    public Collider pickupCollider;

    [Header("Effects")]
    public bool rotatePickup = true;
    public float rotationSpeed = 50f;
    public AudioSource pickupSound;

    private void Start()
    {
        if (pickupVisuals == null)
        {
            Transform v = transform.Find("Visual");
            if (v != null) pickupVisuals = v.gameObject;
            else pickupVisuals = gameObject;
        }

        if (pickupCollider == null)
        {
            pickupCollider = GetComponent<Collider>();
        }

        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        if (rotatePickup && pickupVisuals != null && pickupVisuals.activeInHierarchy)
        {
            pickupVisuals.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        MyCarController car = other.GetComponentInParent<MyCarController>();

        if (car != null && (other.transform.root.CompareTag("Player") || other.CompareTag("Player")))
        {
            Debug.Log($"✅ PLAYER picked up: {type}");
            ApplyPowerUp(car);
            StartCoroutine(RespawnRoutine());
        }
    }

    void ApplyPowerUp(MyCarController car)
    {
        if (pickupSound != null) pickupSound.Play();

        if (RaceUI.instance != null)
        {
            RaceUI.instance.ShowPowerUpIcon(type, duration);
            Debug.Log($"✅ Icon {type} shown for {duration}s");
        }
        else
        {
            Debug.LogError("❌ RaceUI.instance NULL!");
        }

        if (NotificationManager.instance == null)
        {
            Debug.LogWarning("⚠️ NotificationManager missing");
            return;
        }

        switch (type)
        {
            case PowerUpType.Nitro:
                NotificationManager.instance.ShowMessage("NITRO ACTIVATED!", Color.red);
                car.ActivateNitro(duration);
                break;

            case PowerUpType.Shield:
                NotificationManager.instance.ShowMessage("SHIELD ON!", Color.green);
                car.ActivateShield(duration);
                break;

            case PowerUpType.Oil:
                NotificationManager.instance.ShowMessage("OIL DROPPED!", new Color(1f, 0.6f, 0.2f));
                car.DropOil();
                break;

            case PowerUpType.Freeze:
                NotificationManager.instance.ShowMessage("FREEZE ACTIVATED!", Color.cyan);
                if (RaceManager.instance != null)
                {
                    RaceManager.instance.FreezeAllBots(duration);
                }
                break;
        }
    }

    IEnumerator RespawnRoutine()
    {
        if (pickupVisuals != null) pickupVisuals.SetActive(false);
        if (pickupCollider != null) pickupCollider.enabled = false;

        Debug.Log($"⏳ Respawn in {respawnTime}s");
        yield return new WaitForSeconds(respawnTime);

        if (pickupVisuals != null) pickupVisuals.SetActive(true);
        if (pickupCollider != null) pickupCollider.enabled = true;

        Debug.Log("✨ Power-up respawned!");
    }
}
