using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MissionSheetController : MonoBehaviour
{
    private VisualElement _myLobbyRoot;
    private VisualElement _myBottomSheet;
    private VisualElement _myDimSheet;

    private Vector2 _startPointPos;
    private bool _isSheetOpen;

    public void InitBottomSheet(VisualElement root)
    {
        _myLobbyRoot = root;
        _myBottomSheet = _myLobbyRoot.Q<VisualElement>("BottomSheet");
        _myDimSheet = _myLobbyRoot.Q<VisualElement>("DimSheet");

        _isSheetOpen = false;

        // DimSheet의 ClickEvent 제거 (중복 이벤트 충돌의 주범)

        _myLobbyRoot.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        _myLobbyRoot.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

        _myLobbyRoot.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        _myLobbyRoot.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        _startPointPos = evt.position;
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        float deltaY = evt.position.y - _startPointPos.y;

        // 1. 스와이프 동작 (거리가 50 이상일 때만)
        if (Mathf.Abs(deltaY) > 50f)
        {
            if (deltaY < -50f && !_isSheetOpen)
                OpenSheet();
            else if (deltaY > 50f && _isSheetOpen)
                CloseSheet();
        }
        else
        {
            // 2. 스와이프가 아니라 그냥 '클릭'했을 때의 동작
            // 시트가 열려있고, 클릭한 타겟이 검은색 배경(DimSheet)이라면 닫기
            if (_isSheetOpen && evt.target == _myDimSheet)
            {
                CloseSheet();
            }
        }
    }

    private void OpenSheet()
    {
        _isSheetOpen = true;

        _myDimSheet.style.display = DisplayStyle.Flex;
        _myBottomSheet.AddToClassList("sheetBottom-expended");

        // 약간의 딜레이(10ms)를 주어 Flex 전환 후 애니메이션 클래스가 정상 적용되도록 수정
        _myDimSheet.schedule.Execute(() => {
            _myDimSheet.AddToClassList("sheetDim-visible");
        }).StartingIn(10);
    }

    private void CloseSheet()
    {
        _isSheetOpen = false;

        _myBottomSheet.RemoveFromClassList("sheetBottom-expended");
        _myDimSheet.RemoveFromClassList("sheetDim-visible");

        // 스와이프 충돌을 막기 위해 애니메이션(300ms)이 끝난 후 완전히 숨김
        _myDimSheet.schedule.Execute(() => {
            if (!_isSheetOpen)
                _myDimSheet.style.display = DisplayStyle.None;
        }).StartingIn(300);
    }
}