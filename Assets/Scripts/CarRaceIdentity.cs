using UnityEngine;

public class CarRaceIdentity : MonoBehaviour
{
    [Header("Racer Name")]
    public string racerName = "Player";

    void Start()
    {
        if (RaceManager.instance != null)
        {
            RaceManager.instance.RegisterCar(transform, racerName);

            if (CompareTag("Player"))
            {
                RaceManager.instance.StartRaceTimer();
            }
        }
    }
}