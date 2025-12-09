using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GlobalFreeze : MonoBehaviour
{
    public static GlobalFreeze instance;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void FreezeAllBots(float duration)
    {
        StartCoroutine(FreezeRoutine(duration));
    }

    IEnumerator FreezeRoutine(float duration)
    {
        NavMeshAgent[] allBots = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);

        Debug.Log($"❄️ FREEZE! Found {allBots.Length} bots");

        foreach (NavMeshAgent bot in allBots)
        {
            if (bot != null && bot.enabled)
            {
                bot.isStopped = true;
                bot.velocity = Vector3.zero;
            }
        }

        yield return new WaitForSeconds(duration);

        foreach (NavMeshAgent bot in allBots)
        {
            if (bot != null && bot.enabled)
            {
                bot.isStopped = false;
            }
        }

        Debug.Log("☀️ Bots thawed");
    }
}