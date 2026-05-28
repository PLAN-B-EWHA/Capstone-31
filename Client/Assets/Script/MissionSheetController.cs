using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MissionSheetController : MonoBehaviour
{
    private UIManager UIManager;

    private VisualElement _myLobbyRoot;

    private VisualElement _myBottomSheet;
    private VisualElement _myDimSheet;

    // 터치 시작 위치
    private Vector2 startPointPos;

    // 현재 바텀 시트 상태
    private bool isSheetOpen;

    void Awake()
    {
        UIManager = GetComponent<UIManager>();
    }

    public void InitBottomSheet(VisualElement root)
    {
        _myLobbyRoot = root;

        _myBottomSheet = _myLobbyRoot.Q<VisualElement>("BottomSheet");
        _myDimSheet = _myLobbyRoot.Q<VisualElement>("DimSheet");

        if(_myBottomSheet == null || _myDimSheet == null)
        {
            Debug.LogError("BottomSheet 또는 DimSheet를 찾을 수 없습니다.");
            return;
        }

        isSheetOpen = false;

        // 포인터(마우스/터치) 기존 이벤트 해제 후 재등록
        _myBottomSheet.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        _myBottomSheet.RegisterCallback<PointerDownEvent>(OnPointerDown);

        _myBottomSheet.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        _myBottomSheet.RegisterCallback<PointerUpEvent>(OnPointerUp);


        // DimSheet를 클릭했을 때도 패널이 닫히도록 설정
        _myDimSheet.RegisterCallback<ClickEvent>(OnClick);
        _myDimSheet.RegisterCallback<ClickEvent>(OnClick);
    }

    // 포인터(마우스/터치) 캡처
    private void OnPointerDown(PointerDownEvent evt)
    {
        // 누른 순간의 위치 저장
        startPointPos = evt.position;

        // 터치가 요소 밖으로 나가도 인식하도록 캡처
        _myBottomSheet.CapturePointer(evt.pointerId);
    }

    // 포인터(마우스/터치) 릴리즈
    private void OnPointerUp(PointerUpEvent evt)
    {
        // 포인터 캡처 해제
        _myBottomSheet.ReleasePointer(evt.pointerId); // 캡처 해제

        // 누른 위치와 뗀 위치의 Y축 차이 계산
        float deltaY = evt.position.y - startPointPos.y;

        // 위로 스와이프 (Y값이 작아짐, -50은 오작동 방지용 임계값) -> 바텀 시트 열림
        if (deltaY < -50f && !isSheetOpen)
            OpenSheet();
        // 아래로 스와이프 (Y값이 커짐, 50은 오작동 방지용 임계값) -> 바텀 시트 닫힘
        else if (deltaY > 50f && isSheetOpen)
            CloseSheet();
    }

    private void OnClick(ClickEvent evt)
    {
        if (isSheetOpen)
            CloseSheet();
    }

    private void OpenSheet()
    {
        isSheetOpen = true;

        // 열림 상태 클래스 추가
        _myBottomSheet.AddToClassList("sheetBottom-expended");

        // 딤 시트 켜기 -> 서서히 어두워지기
        _myDimSheet.style.display = DisplayStyle.Flex;
        _myDimSheet.AddToClassList("sheetDim-visible");
    }

    private void CloseSheet()
    {
        isSheetOpen = false;

        // 열림 상태 클래스 제거 (기본 상태로 돌아감)
        _myBottomSheet.RemoveFromClassList("sheetBottom-expended");

        _myDimSheet.RemoveFromClassList("sheetDim-visible");

        // 투명해진 후 클릭을 방지하기 위해 0.3초 뒤에 Display None 처리
        _myDimSheet.schedule.Execute(() => _myDimSheet.style.display = DisplayStyle.None).StartingIn(300);
    }
}
