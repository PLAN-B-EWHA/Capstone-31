using System.Collections;
using TMPro;
using UnityEngine;

public class ScenarioRawJsonLoader : MonoBehaviour
{
    [SerializeField] private ApiClient apiClient;
    [SerializeField] private int week = 1;
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private TMP_Text outputText;

    private void Start()
    {
        if (loadOnStart)
        {
            StartCoroutine(LoadWeekJson());
        }
    }

    public void Reload()
    {
        StartCoroutine(LoadWeekJson());
    }

    public IEnumerator LoadWeekJson()
    {
        if (apiClient == null)
        {
            Debug.LogError("ScenarioRawJsonLoader: ApiClient is not assigned.");
            SetOutput("ApiClient connection required.");
            yield break;
        }

        string responseText = string.Empty;

        yield return apiClient.Get($"/api/unity/scenarios?week={week}", (code, body) =>
        {
            if (code < 200 || code >= 300)
            {
                responseText = $"Request failed: code={code}\n{body}";
                Debug.LogError(responseText);
                return;
            }

            responseText = body;
            Debug.Log(body);
        });

        SetOutput(responseText);
    }

    private void SetOutput(string text)
    {
        if (outputText != null)
        {
            outputText.text = text;
        }
    }
}
