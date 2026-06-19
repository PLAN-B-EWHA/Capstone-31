using UnityEngine;
using UnityEngine.UIElements;

public class CommonButton : MonoBehaviour
{
    private UIManager uiManager;
    private VisualElement _myTopBar;

    void Awake() { uiManager = GetComponent<UIManager>(); }

    public void InitCommonButton(VisualElement _root)
    {
        _myTopBar = _root;
        BindCommonButton();
        UpdateStarUI(); // [추가] 시작할 때 별 상태 초기화
    }

    // [추가] 별의 색상을 업데이트하는 핵심 함수
    public void UpdateStarUI()
    {
        if (_myTopBar == null) return;

        int completedCount = UserDataManager.Instance.dailyCompletedCount;

        for (int i = 1; i <= 5; i++)
        {
            // UXML에 Star_1, Star_2... 이름으로 배치가 되어 있어야 합니다.
            VisualElement star = _myTopBar.Q<VisualElement>($"Star_{i}");
            if (star != null)
            {
                star.RemoveFromClassList("star-active");
                star.RemoveFromClassList("star-inactive");

                if (i <= completedCount)
                    star.AddToClassList("star-active");   // 노란색 (완료)
                else
                    star.AddToClassList("star-inactive"); // 회색 (미완료)
            }
        }
    }

    private void BindCommonButton()
    {
        var homeButton = _myTopBar.Q<Button>("Button_Home");
        var familarityButton = _myTopBar.Q<Button>("Button_Familarity");

        homeButton.clicked += () => uiManager.LoadScreen(uiManager._mainLobbyAsset);
    }
}
