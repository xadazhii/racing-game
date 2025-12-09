using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [HideInInspector] public int index;

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CarRaceIdentity identity = other.GetComponentInParent<CarRaceIdentity>();

        Transform carRoot = null;

        if (identity != null)
        {
            carRoot = identity.transform;
        }
        else
        {
            if (other.CompareTag("Player") || other.CompareTag("Opponent"))
            {
                carRoot = other.transform;
            }
            else if (other.transform.root.CompareTag("Player") || other.transform.root.CompareTag("Opponent"))
            {
                carRoot = other.transform.root;
            }
        }

        if (carRoot != null && RaceManager.instance != null)
        {
            RaceManager.instance.CarHitCheckpoint(carRoot, index);
        }
    }
}