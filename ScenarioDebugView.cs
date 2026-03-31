using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 시나리오 배열을 사람이 읽기 쉬운 텍스트로 출력하는 디버그 UI입니다.
// GameFlowController가 ShowScenarios()로 배열 전체를 넘기고,
// Prev/Next 버튼은 이 컴포넌트의 ShowPrev/ShowNext에 연결해 페이지처럼 넘깁니다.
public class ScenarioDebugView : MonoBehaviour
{
    // 현재 시나리오 본문을 출력할 TMP 텍스트.
    [SerializeField] private TMP_Text scenarioText;
    // 시나리오가 길 때 새 항목으로 바꿀 때마다 위로 올리기 위한 ScrollRect.
    [SerializeField] private ScrollRect scrollRect;
    // "1 / N" 형태의 페이지 표시용 텍스트. 없어도 동작은 합니다.
    [SerializeField] private TMP_Text pageText;

    // 마지막으로 전달받은 시나리오 전체 배열.
    private ScenarioPayload[] scenarios = System.Array.Empty<ScenarioPayload>();
    // 현재 화면에 표시 중인 인덱스.
    private int currentIndex;

    public void ShowScenarios(ScenarioPayload[] loadedScenarios)
    {
        // 텍스트 슬롯이 비어 있으면 데이터를 받아도 화면에 쓸 수 없습니다.
        if (scenarioText == null)
        {
            Debug.LogWarning("ScenarioDebugView scenarioText가 연결되지 않았습니다.");
            return;
        }

        // 새 배열을 받을 때는 항상 첫 번째 시나리오부터 다시 보여줍니다.
        scenarios = loadedScenarios ?? System.Array.Empty<ScenarioPayload>();
        currentIndex = 0;
        RenderCurrent();
    }

    public void ShowNext()
    {
        // Next 버튼 OnClick에서 연결하는 메서드입니다.
        if (scenarios == null || scenarios.Length == 0)
        {
            return;
        }

        // 마지막 다음은 다시 첫 번째로 순환합니다.
        currentIndex = (currentIndex + 1) % scenarios.Length;
        RenderCurrent();
    }

    public void ShowPrev()
    {
        // Prev 버튼 OnClick에서 연결하는 메서드입니다.
        if (scenarios == null || scenarios.Length == 0)
        {
            return;
        }

        // 첫 번째 이전은 마지막으로 이동합니다.
        currentIndex = (currentIndex - 1 + scenarios.Length) % scenarios.Length;
        RenderCurrent();
    }

    private void RenderCurrent()
    {
        // 현재 인덱스의 시나리오 하나를 문자열로 만들어 TMP 텍스트에 그립니다.
        if (scenarioText == null)
        {
            Debug.LogWarning("ScenarioDebugView scenarioText가 연결되지 않았습니다.");
            return;
        }

        if (scenarios == null || scenarios.Length == 0)
        {
            // 결과가 없을 때도 빈 화면 대신 명확한 상태를 보여줍니다.
            scenarioText.text = "시나리오 없음";
            UpdatePageText();
            ResetScrollToTop();
            return;
        }

        // 배열 전체가 아니라 현재 선택된 항목 하나만 표시합니다.
        var scenario = scenarios[currentIndex];
        var sb = new StringBuilder();

        sb.AppendLine($"ID: {scenario.scenario_id}");
        sb.AppendLine($"제목: {scenario.metadata?.lobby_title}");
        sb.AppendLine($"주차: {scenario.metadata?.week}");
        sb.AppendLine($"테마: {scenario.metadata?.theme}");

        if (!string.IsNullOrEmpty(scenario.metadata?.relationship_stage))
        {
            sb.AppendLine($"관계 단계: {scenario.metadata.relationship_stage}");
        }

        if (scenario.dialogue_flow != null && scenario.dialogue_flow.Length > 0)
        {
            // 대화 턴과 선택지를 순서대로 이어붙여 사람이 읽기 쉬운 텍스트로 만듭니다.
            sb.AppendLine("대화:");
            foreach (var turn in scenario.dialogue_flow)
            {
                if (turn.npc_utterance != null && turn.npc_utterance.Length > 0)
                {
                    sb.AppendLine($"- 턴 {turn.turn_id}: {string.Join(" / ", turn.npc_utterance)}");
                }

                if (turn.options != null && turn.options.Length > 0)
                {
                    sb.AppendLine("  선택지:");
                    foreach (var option in turn.options)
                    {
                        sb.AppendLine($"  - ({option.score}) {option.text}");
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(scenario.final_summary?.total_learning_point))
        {
            sb.AppendLine($"학습 포인트: {scenario.final_summary.total_learning_point}");
        }

        // 본문, 페이지 표시, 스크롤 위치를 한 번에 갱신합니다.
        scenarioText.text = sb.ToString();
        UpdatePageText();
        ResetScrollToTop();
    }

    private void UpdatePageText()
    {
        // pageText는 선택사항이라 비어 있으면 그냥 건너뜁니다.
        if (pageText == null)
        {
            return;
        }

        if (scenarios == null || scenarios.Length == 0)
        {
            pageText.text = "0 / 0";
            return;
        }

        pageText.text = $"{currentIndex + 1} / {scenarios.Length}";
    }

    private void ResetScrollToTop()
    {
        // 새 시나리오를 볼 때 항상 위에서부터 읽을 수 있게 스크롤을 초기화합니다.
        if (scrollRect == null)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ResetScrollToTopCoroutine());
    }

    private IEnumerator ResetScrollToTopCoroutine()
    {
        // UI 레이아웃이 한 프레임 뒤에 갱신될 수 있어 두 프레임에 걸쳐 보정합니다.
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
