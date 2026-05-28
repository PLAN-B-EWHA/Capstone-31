using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CommonButton : MonoBehaviour
{
    private UIManager uiManager;
    private VisualElement _myTopBar;

    void Awake()
    {
        uiManager = GetComponent<UIManager>();
    }

    public void InitCommonButton(VisualElement _root)
    {
        _myTopBar = _root;
        BindCommonButton();
    }

    // 버튼 바인드 함수
    private void BindCommonButton()
    {
        var homeButton = _myTopBar.Q<Button>("Button_Home");
        var familarityButton = _myTopBar.Q<Button>("Button_Familarity");

        homeButton.RegisterCallback<ClickEvent>(OnHomeButtonClicked);
        familarityButton.RegisterCallback<ClickEvent>(OnFamilarityButtonClicked);
    }

    private void OnHomeButtonClicked(ClickEvent evt)
    {
        Debug.Log("홈 버튼 클릭되었습니다.");
        uiManager.LoadScreen(uiManager._mainLobbyAsset);
    }

    private void OnFamilarityButtonClicked(ClickEvent evt)
    {
        Debug.Log("친밀도 버튼이 클릭되었습니다.");
    }
}
