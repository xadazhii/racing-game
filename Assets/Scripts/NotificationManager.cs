using UnityEngine;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager instance;

    [Header("Settings")]
    public GameObject textPrefab;
    public Transform contentPanel;
    public float messageDuration = 3.0f;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void ShowMessage(string message, Color color)
    {
        if (textPrefab == null || contentPanel == null) return;

        GameObject newObj = Instantiate(textPrefab, contentPanel);
        newObj.transform.localScale = Vector3.one;

        TextMeshProUGUI txt = newObj.GetComponentInChildren<TextMeshProUGUI>();
        
        if (txt != null)
        {
            txt.text = message;
            txt.color = color;
        }

        Destroy(newObj, messageDuration);
    }
}