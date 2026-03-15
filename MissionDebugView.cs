using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionDebugView : MonoBehaviour
{
    [SerializeField] private TMP_Text missionText;
    [SerializeField] private ScrollRect scrollRect;

    public void ShowMissions(UnityMissionPayload[] missions)
    {
        if (missionText == null)
        {
            Debug.LogWarning("MissionDebugView missionText가 연결되지 않았습니다.");
            return;
        }

        if (missions == null || missions.Length == 0)
        {
            missionText.text = "미션 없음";
            ResetScrollToTop();
            return;
        }

        var sb = new StringBuilder();

        foreach (var mission in missions)
        {
            sb.AppendLine($"미션ID: {mission.missionId}");
            sb.AppendLine($"이름: {mission.missionName}");
            sb.AppendLine($"타입: {mission.missionTypeString}");
            sb.AppendLine($"키워드: {mission.targetKeyword}");
            sb.AppendLine($"감정: {mission.targetEmotionString}");

            if (mission.expression_data != null &&
                mission.expression_data.characterDialogue != null &&
                mission.expression_data.characterDialogue.Length > 0)
            {
                sb.AppendLine("대사:");
                foreach (var line in mission.expression_data.characterDialogue)
                {
                    sb.AppendLine($"- {line}");
                }
            }

            if (mission.situation_data != null)
            {
                if (!string.IsNullOrEmpty(mission.situation_data.question))
                {
                    sb.AppendLine($"질문: {mission.situation_data.question}");
                }

                if (mission.situation_data.options != null && mission.situation_data.options.Length > 0)
                {
                    sb.AppendLine("선택지:");
                    foreach (var option in mission.situation_data.options)
                    {
                        sb.AppendLine($"- {option.id}. {option.text}");
                    }
                }
            }

            sb.AppendLine("--------------------");
        }

        missionText.text = sb.ToString();
        ResetScrollToTop();
    }

    private void ResetScrollToTop()
    {
        if (scrollRect == null)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ResetScrollToTopCoroutine());
    }

    private IEnumerator ResetScrollToTopCoroutine()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;

        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
