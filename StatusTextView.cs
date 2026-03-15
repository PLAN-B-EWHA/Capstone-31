using TMPro;
using UnityEngine;

public class StatusTextView : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;

    public void SetStatus(string message)
    {
        if (statusText == null)
        {
            Debug.LogWarning("StatusTextView statusText가 연결되지 않았습니다.");
            return;
        }

        statusText.text = message;
    }
}
