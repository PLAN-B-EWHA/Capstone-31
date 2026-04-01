using System.Collections;
using System;
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
            StartCoroutine(LoadWeekJson(_ => { }));
        }
    }

    public void Reload()
    {
        StartCoroutine(LoadWeekJson(_ => { }));
    }

    public IEnumerator LoadWeekJson(Action<string> onDone)
    {
        if (apiClient == null)
        {
            Debug.LogError("ScenarioRawJsonLoader: ApiClient is not assigned.");
            onDone?.Invoke(string.Empty);
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

        onDone?.Invoke(responseText);
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
